#!/usr/bin/env bash
# Phase 게이트 검사.
#   bash gate.sh              → 모든 게이트 현재 상태 출력 (exit 0)
#   bash gate.sh <gate>       → 해당 게이트 strict 검사. 위반 시 exit 1.
# 사용 예: bash gate.sh yield_return_null  (Phase 2-C 후 CI에서 호출)
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$SCRIPT_DIR"
. "$HARNESS_DIR/lib/common.sh"
detect_root

GATE="${1:-}"

get_val() {
    file="$1"; key="$2"
    if [ ! -f "$DATA_DIR/$file" ]; then
        echo "?"; return
    fi
    val=$(kv_read "$DATA_DIR/$file" "$key")
    [ -z "$val" ] && echo "?" || echo "$val"
}

# 게이트 카탈로그 — (이름 | 파일 | 키 | 목표 | 도달 Phase)
GATES="
yield_return_null|antipatterns.tsv|yield_return_null|0|Phase 2-C
ienumerator_methods|async_adoption.kv|ienumerator_methods|0|Phase 2-C
start_coroutine_calls|async_adoption.kv|start_coroutine_calls|0|Phase 2-C
resources_load_calls|asset_loading.kv|resources_load_calls|0|Phase 2-D
resources_folder_files|asset_loading.kv|resources_folder_files|0|Phase 2-D
self_rolled_singleton|di_safety.kv|self_rolled_singleton|0|Phase 3
awake_instance_hits|di_safety.kv|awake_instance_hits|0|Phase 3
model_singleton_access|di_safety.kv|model_singleton_access|0|Phase 3 (3-A 후)
"

if [ -z "$GATE" ]; then
    printf "%-26s %10s %8s   %s\n" "GATE" "CURRENT" "TARGET" "EXPECTED_BY"
    printf "%-26s %10s %8s   %s\n" "----" "-------" "------" "-----------"
    echo "$GATES" | while IFS='|' read -r name file key target phase; do
        [ -z "$name" ] && continue
        v=$(get_val "$file" "$key")
        if [ "$v" = "?" ]; then
            status="NO_DATA"
        elif [ "$v" -le "$target" ] 2>/dev/null; then
            status="OK"
        else
            status="FAIL"
        fi
        printf "%-26s %10s %8s   %s [%s]\n" "$name" "$v" "$target" "$phase" "$status"
    done
    exit 0
fi

# strict 검사
echo "$GATES" | while IFS='|' read -r name file key target phase; do
    [ -z "$name" ] && continue
    if [ "$name" = "$GATE" ]; then
        v=$(get_val "$file" "$key")
        if [ "$v" = "?" ]; then
            echo "WARN: $GATE — data not generated. Run run_all.sh first." >&2
            exit 2
        fi
        if [ "$v" -gt "$target" ] 2>/dev/null; then
            echo "FAIL: $GATE = $v (target $target, expected $phase)" >&2
            exit 1
        fi
        echo "OK: $GATE = $v" >&2
        exit 0
    fi
done

echo "Unknown gate: $GATE" >&2
echo "Available:" >&2
echo "$GATES" | awk -F'|' '$1 != "" { print "  " $1 }' >&2
exit 2
