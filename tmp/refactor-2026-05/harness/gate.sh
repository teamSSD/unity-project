#!/usr/bin/env bash
# Phase 게이트 검사 + 베이스라인 delta.
#   bash gate.sh              → 모든 게이트 현재 상태 + delta 출력 (exit 0)
#   bash gate.sh <gate>       → 해당 게이트 strict 검사. 위반 시 exit 1.
#   bash gate.sh --snapshot   → 현재 값을 baseline.kv로 저장 (Phase 끝 또는 새 베이스라인 시점)
#   bash gate.sh --check-regression → 베이스라인 대비 악화 항목만 출력. 악화 있으면 exit 1.
#
# 게이트 카탈로그는 마스터 진단의 정량 목표 기반. 측정된 모든 항목 등록.
# diagnosis_status.md가 단일 원천. 이 게이트는 자동 검증 가능한 부분.
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$SCRIPT_DIR"
. "$HARNESS_DIR/lib/common.sh"
detect_root

GATE="${1:-}"
BASELINE_FILE="$DATA_DIR/baseline_gates.kv"

get_val() {
    file="$1"; key="$2"
    if [ ! -f "$DATA_DIR/$file" ]; then
        echo "?"; return
    fi
    val=$(kv_read "$DATA_DIR/$file" "$key")
    [ -z "$val" ] && echo "?" || echo "$val"
}

get_baseline() {
    key="$1"
    [ -f "$BASELINE_FILE" ] || { echo "?"; return; }
    val=$(kv_read "$BASELINE_FILE" "$key")
    [ -z "$val" ] && echo "?" || echo "$val"
}

# 게이트 카탈로그 — (이름 | 파일 | 키 | 목표 | 도달 Phase)
# 마스터 진단 + ADR 인덱스 정량 목표 전체 반영.
GATES="
yield_return_null|antipatterns.tsv|yield_return_null|0|Phase 2-C
ienumerator_methods|async_adoption.kv|ienumerator_methods|0|Phase 2-C
start_coroutine_calls|async_adoption.kv|start_coroutine_calls|0|Phase 2-C
resources_load_calls|asset_loading.kv|resources_load_calls|0|Phase 2-D
resources_folder_files|asset_loading.kv|resources_folder_files|0|Phase 2-D
self_rolled_singleton|di_safety.kv|self_rolled_singleton|0|Phase 3
awake_instance_hits|di_safety.kv|awake_instance_hits|0|Phase 3
model_singleton_access|di_safety.kv|model_singleton_access|0|Phase 3 (3-A 후)
serialize_no_validate|di_safety.kv|serialize_no_validate|5|Phase 2 (마스터 진단)
getcomponent_no_require|di_safety.kv|getcomponent_no_require|30|Phase 2 (마스터 진단)
event_leaks_files|event_leaks.kv|event_leaks_files|10|Phase 1 Critical (1주 이내)
instance_access|antipatterns.tsv|instance_access|10|Phase 4 (ADR)
function_over41|method_metrics.kv|method_over41|12|Phase 4 (마스터 진단)
function_over61|method_metrics.kv|method_over61|0|Phase 4 (마스터 진단)
max_file_lines|file_stats.kv|max_file_lines|300|Phase 4 (마스터 진단)
debug_log|antipatterns.tsv|debug_log|30|Phase 4 (마스터 진단)
test_coverage_pct|test_coverage.kv|test_coverage_pct|25|Phase 4 (ADR — \xe2\x89\xa5)
"

# test_coverage_pct는 ≥로 비교 (높을수록 좋음). 다른 건 ≤ (낮을수록 좋음).
is_higher_better() {
    case "$1" in
        test_coverage_pct) return 0 ;;
        *) return 1 ;;
    esac
}

# 베이스라인 스냅샷 저장
if [ "$GATE" = "--snapshot" ]; then
    : > "$BASELINE_FILE"
    echo "$GATES" | while IFS='|' read -r name file key target phase; do
        [ -z "$name" ] && continue
        v=$(get_val "$file" "$key")
        [ "$v" = "?" ] && continue
        kv_write "$BASELINE_FILE" "$name" "$v"
    done
    echo "Baseline saved → $BASELINE_FILE" >&2
    exit 0
fi

# 회귀 검사만 (CI/pre-commit 용)
if [ "$GATE" = "--check-regression" ]; then
    if [ ! -f "$BASELINE_FILE" ]; then
        echo "WARN: baseline 없음. bash gate.sh --snapshot 먼저 실행" >&2
        exit 2
    fi
    REGRESSED=0
    echo "$GATES" | while IFS='|' read -r name file key target phase; do
        [ -z "$name" ] && continue
        cur=$(get_val "$file" "$key")
        base=$(get_baseline "$name")
        [ "$cur" = "?" ] || [ "$base" = "?" ] && continue
        if is_higher_better "$name"; then
            if [ "$cur" -lt "$base" ] 2>/dev/null; then
                printf "REGRESSION: %s = %s (was %s)\n" "$name" "$cur" "$base" >&2
                echo "1" >> /tmp/_gate_regression
            fi
        else
            if [ "$cur" -gt "$base" ] 2>/dev/null; then
                printf "REGRESSION: %s = %s (was %s, +%d)\n" "$name" "$cur" "$base" "$((cur - base))" >&2
                echo "1" >> /tmp/_gate_regression
            fi
        fi
    done
    if [ -s /tmp/_gate_regression 2>/dev/null ]; then
        rm -f /tmp/_gate_regression
        exit 1
    fi
    echo "No regression" >&2
    exit 0
fi

# 전체 출력 (delta 포함)
if [ -z "$GATE" ]; then
    printf "%-26s %10s %8s %8s   %s\n" "GATE" "CURRENT" "BASELINE" "TARGET" "EXPECTED_BY"
    printf "%-26s %10s %8s %8s   %s\n" "----" "-------" "--------" "------" "-----------"
    echo "$GATES" | while IFS='|' read -r name file key target phase; do
        [ -z "$name" ] && continue
        v=$(get_val "$file" "$key")
        base=$(get_baseline "$name")
        status="UNKNOWN"
        delta=""
        if [ "$v" = "?" ]; then
            status="NO_DATA"
        else
            if is_higher_better "$name"; then
                if [ "$v" -ge "$target" ] 2>/dev/null; then status="OK"; else status="FAIL"; fi
            else
                if [ "$v" -le "$target" ] 2>/dev/null; then status="OK"; else status="FAIL"; fi
            fi
            # delta 계산
            if [ "$base" != "?" ] && [ "$base" != "$v" ]; then
                if is_higher_better "$name"; then
                    [ "$v" -gt "$base" ] 2>/dev/null && delta=" ↑$((v-base))" || delta=" ↓$((base-v))"
                else
                    [ "$v" -gt "$base" ] 2>/dev/null && delta=" ↑$((v-base))⚠️" || delta=" ↓$((base-v))"
                fi
            fi
        fi
        printf "%-26s %10s %8s %8s   %s [%s]%s\n" "$name" "$v" "$base" "$target" "$phase" "$status" "$delta"
    done
    echo "" >&2
    echo "↑=증가, ↓=감소. ⚠️=악화. 베이스라인은 bash gate.sh --snapshot으로 갱신." >&2
    echo "마스터 진단 상태는 diagnosis_status.md 참조 (canonical)." >&2
    exit 0
fi

# 단일 게이트 strict 검사
echo "$GATES" | while IFS='|' read -r name file key target phase; do
    [ -z "$name" ] && continue
    if [ "$name" = "$GATE" ]; then
        v=$(get_val "$file" "$key")
        if [ "$v" = "?" ]; then
            echo "WARN: $GATE — data not generated. Run run_all.sh first." >&2
            exit 2
        fi
        if is_higher_better "$name"; then
            if [ "$v" -lt "$target" ] 2>/dev/null; then
                echo "FAIL: $GATE = $v (target ≥$target, expected $phase)" >&2
                exit 1
            fi
        else
            if [ "$v" -gt "$target" ] 2>/dev/null; then
                echo "FAIL: $GATE = $v (target ≤$target, expected $phase)" >&2
                exit 1
            fi
        fi
        echo "OK: $GATE = $v" >&2
        exit 0
    fi
done

echo "Unknown gate: $GATE" >&2
echo "Available:" >&2
echo "$GATES" | awk -F'|' '$1 != "" { print "  " $1 }' >&2
exit 2
