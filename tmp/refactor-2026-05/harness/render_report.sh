#!/usr/bin/env bash
# 데이터 디렉토리의 결과들을 읽어 baseline.md 조립. 이전 리포트는 history/ 로 백업.
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$SCRIPT_DIR"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="${2:-$ROOT/tmp/refactor-2026-05/reports/baseline.md}"
mkdir -p "$(dirname "$OUT")"

# 이전 리포트를 history/ 에 보관해 델타 추적 가능
if [ -f "$OUT" ]; then
    HIST_DIR="$(dirname "$OUT")/history"
    mkdir -p "$HIST_DIR"
    cp "$OUT" "$HIST_DIR/$(date '+%Y-%m-%dT%H-%M-%S').md"
fi

bucket_count() {
    file="$1"; lo="$2"; hi="$3"
    if [ "$hi" = "+" ]; then
        awk -F'\t' -v lo="$lo" '$1>=lo' "$file" | wc -l | tr -d ' '
    else
        awk -F'\t' -v lo="$lo" -v hi="$hi" '$1>=lo && $1<=hi' "$file" | wc -l | tr -d ' '
    fi
}

{
echo "# Refactor Baseline Report"
echo
echo "_생성일: $(date '+%Y-%m-%d %H:%M:%S')_"
echo "_프로젝트: ${ROOT}_"
echo

# ----- 1. 파일/라인 -----
echo "## 1. 파일/라인 통계"
echo
if [ -f "$DATA_DIR/file_stats.kv" ]; then
    echo "| 범주 | 파일 수 | 총 라인 |"
    echo "|---|---:|---:|"
    printf "| 런타임 | %s | %s |\n" "$(kv_read "$DATA_DIR/file_stats.kv" runtime_files)" "$(kv_read "$DATA_DIR/file_stats.kv" runtime_lines)"
    printf "| 에디터 | %s | %s |\n" "$(kv_read "$DATA_DIR/file_stats.kv" editor_files)" "$(kv_read "$DATA_DIR/file_stats.kv" editor_lines)"
    printf "| 테스트 | %s | %s |\n" "$(kv_read "$DATA_DIR/file_stats.kv" test_files)" "$(kv_read "$DATA_DIR/file_stats.kv" test_lines)"
fi
echo

# ----- 2. 파일 라인 분포 -----
if [ -f "$DATA_DIR/file_lines.tsv" ]; then
    echo "## 2. 파일 라인 분포 (런타임)"
    echo
    echo "| 구간 | 파일 수 |"
    echo "|---|---:|"
    for r in "0:50" "51:100" "101:200" "201:300" "301:500" "501:+"; do
        lo="${r%:*}"; hi="${r#*:}"
        c=$(bucket_count "$DATA_DIR/file_lines.tsv" "$lo" "$hi")
        label="${lo}-${hi}"
        [ "$hi" = "+" ] && label="${lo}+"
        printf "| %s | %d |\n" "$label" "$c"
    done
    echo
    echo "### 상위 10개"
    echo
    sort -rn -t $'\t' -k1 "$DATA_DIR/file_lines.tsv" | head -10 | awk -F'\t' -v root="$ROOT/" '{
        p=$2; sub(root, "", p); printf "- **%d** lines — %s\n", $1, p
    }'
    echo
fi

# ----- 3. 함수 길이 -----
if [ -s "$DATA_DIR/method_lengths.tsv" ]; then
    echo "## 3. 함수 길이 분포"
    echo
    TOTAL=$(wc -l < "$DATA_DIR/method_lengths.tsv" | tr -d ' ')
    echo "총 메서드 추출: ${TOTAL}개 (휴리스틱 추정)"
    echo
    echo "| 라인 수 구간 | 메서드 수 | 비율 |"
    echo "|---|---:|---:|"
    for r in "1:10" "11:20" "21:40" "41:60" "61:+"; do
        lo="${r%:*}"; hi="${r#*:}"
        c=$(bucket_count "$DATA_DIR/method_lengths.tsv" "$lo" "$hi")
        pct=$(awk -v c="$c" -v t="$TOTAL" 'BEGIN { if (t>0) printf "%.1f%%", (c/t)*100; else print "0%" }')
        label="${lo}-${hi}"
        [ "$hi" = "+" ] && label="${lo}+"
        printf "| %s | %d | %s |\n" "$label" "$c" "$pct"
    done
    echo
    echo "### 상위 15개"
    echo
    sort -rn -t $'\t' -k1 "$DATA_DIR/method_lengths.tsv" | head -15 | awk -F'\t' -v root="$ROOT/" '{
        p=$3; sub(root, "", p); printf "- **%d** lines — `%s` in %s\n", $1, $2, p
    }'
    echo
fi

# ----- 4. 들여쓰기 -----
if [ -s "$DATA_DIR/indent_depths.tsv" ]; then
    echo "## 4. 들여쓰기 뎁스 (파일별 최대, 4-space 기준)"
    echo
    echo "| 최대 뎁스 | 파일 수 |"
    echo "|---:|---:|"
    for d in 3 4 5 6 7; do
        c=$(awk -F'\t' -v d="$d" '$1==d' "$DATA_DIR/indent_depths.tsv" | wc -l | tr -d ' ')
        printf "| %d | %d |\n" "$d" "$c"
    done
    c=$(awk -F'\t' '$1>=8' "$DATA_DIR/indent_depths.tsv" | wc -l | tr -d ' ')
    printf "| ≥8 | %d |\n" "$c"
    echo
    echo "### 깊은 뎁스 상위 10개"
    echo
    sort -rn -t $'\t' -k1 "$DATA_DIR/indent_depths.tsv" | head -10 | awk -F'\t' -v root="$ROOT/" '{
        p=$2; sub(root, "", p); printf "- depth **%d** — %s\n", $1, p
    }'
    echo
fi

# ----- 5. 안티패턴 -----
if [ -s "$DATA_DIR/antipatterns.tsv" ]; then
    echo "## 5. 안티패턴 카운트"
    echo
    echo "| 패턴 | 발생 수 |"
    echo "|---|---:|"
    while IFS=$'\t' read -r label count; do
        printf "| %s | %s |\n" "$label" "$count"
    done < "$DATA_DIR/antipatterns.tsv"
    if [ -f "$DATA_DIR/magic_numbers.kv" ]; then
        printf "| magic_number_lines (대략) | %s |\n" "$(kv_read "$DATA_DIR/magic_numbers.kv" magic_number_lines)"
    fi
    echo
fi

# ----- 6. Singleton 파일 -----
if [ -s "$DATA_DIR/singleton_files.list" ]; then
    echo "## 6. Singleton 보유 파일"
    echo
    awk -v root="$ROOT/" '{ p=$0; sub(root, "", p); printf "- %s\n", p }' "$DATA_DIR/singleton_files.list"
    echo
fi

# ----- 7. DI Safety -----
if [ -f "$DATA_DIR/di_safety.kv" ]; then
    echo "## 7. DI Safety"
    echo
    echo "| 항목 | 값 |"
    echo "|---|---:|"
    printf "| 클래스 내부에서 \`.Instance\` 호출 (1+ 발생 파일) | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" hidden_dep_files)"
    printf "| Awake/Start/OnEnable에서 \`.Instance\` 접근 (메서드 수) | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" awake_instance_hits)"
    printf "| \`[SerializeField]\` 있는데 \`OnValidate\` 없음 (파일) | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" serialize_no_validate)"
    printf "| \`GetComponent<X>\` 있는데 \`[RequireComponent(typeof(X))]\` 없음 | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" getcomponent_no_require)"
    printf "| 자체 Singleton (\`SingletonMonoBehaviour<T>\` 미상속) | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" self_rolled_singleton)"
    printf "| Entities/ 안에서 \`.Instance\` 접근 | %s |\n" "$(kv_read "$DATA_DIR/di_safety.kv" model_singleton_access)"
    echo

    if [ -s "$DATA_DIR/di_hidden_deps.tsv" ]; then
        echo "### 7.1 Hidden Dependency 상위 10개 (\`.Instance\` 호출 많은 파일)"
        echo
        sort -rn -t $'\t' -k1 "$DATA_DIR/di_hidden_deps.tsv" | head -10 | awk -F'\t' -v root="$ROOT/" '{
            p=$2; sub(root, "", p); printf "- **%d** — %s\n", $1, p
        }'
        echo
    fi

    if [ -s "$DATA_DIR/di_awake_instance.tsv" ]; then
        echo "### 7.2 Awake/Start/OnEnable에서 \`.Instance\` 접근"
        echo
        awk -F'\t' -v root="$ROOT/" '{ p=$2; sub(root, "", p); printf "- \`%s\` — %s\n", $1, p }' "$DATA_DIR/di_awake_instance.tsv"
        echo
    fi

    if [ -s "$DATA_DIR/di_serialize_no_validate.list" ]; then
        echo "### 7.3 \`[SerializeField]\` 있는데 \`OnValidate\` 없는 파일 (상위 15)"
        echo
        head -15 "$DATA_DIR/di_serialize_no_validate.list" | awk -v root="$ROOT/" '{ p=$0; sub(root, "", p); printf "- %s\n", p }'
        n_total=$(wc -l < "$DATA_DIR/di_serialize_no_validate.list" | tr -d ' ')
        if [ "$n_total" -gt 15 ]; then
            echo "- ... 외 $((n_total - 15))개"
        fi
        echo
    fi

    if [ -s "$DATA_DIR/di_getcomponent_no_require.tsv" ]; then
        echo "### 7.4 \`GetComponent<X>\` ← \`[RequireComponent(typeof(X))]\` 부재"
        echo
        head -20 "$DATA_DIR/di_getcomponent_no_require.tsv" | awk -F'\t' -v root="$ROOT/" '{ p=$1; sub(root, "", p); printf "- %s — missing \`%s\`\n", p, $2 }'
        n_total=$(wc -l < "$DATA_DIR/di_getcomponent_no_require.tsv" | tr -d ' ')
        if [ "$n_total" -gt 20 ]; then
            echo "- ... 외 $((n_total - 20))개"
        fi
        echo
    fi

    if [ -s "$DATA_DIR/di_self_rolled_singleton.list" ]; then
        echo "### 7.5 자체 Singleton (\`SingletonMonoBehaviour<T>\` 미상속)"
        echo
        awk -v root="$ROOT/" '{ p=$0; sub(root, "", p); printf "- %s\n", p }' "$DATA_DIR/di_self_rolled_singleton.list"
        echo
    fi

    if [ -s "$DATA_DIR/di_model_instance_access.list" ]; then
        echo "### 7.6 \`Entities/\` 안에서 \`.Instance\` 접근 (3-Layer 위반 후보)"
        echo
        awk -v root="$ROOT/" '{ p=$0; sub(root, "", p); printf "- %s\n", p }' "$DATA_DIR/di_model_instance_access.list"
        echo
    fi
fi

# ----- 8. 테스트 커버리지 -----
if [ -f "$DATA_DIR/test_coverage.kv" ]; then
    echo "## 8. 테스트 커버리지 프록시"
    echo
    T=$(kv_read "$DATA_DIR/test_coverage.kv" total_classes)
    C=$(kv_read "$DATA_DIR/test_coverage.kv" covered_classes)
    PCT=$(awk -v c="$C" -v t="$T" 'BEGIN { if (t>0) printf "%.1f%%", (c/t)*100; else print "0%" }')
    echo "- 런타임 클래스: **${T}**"
    echo "- 테스트에서 언급되는 클래스: **${C}** (${PCT})"
    echo
    if [ -s "$DATA_DIR/covered_classes.list" ]; then
        echo "### 참조되는 클래스"
        sort "$DATA_DIR/covered_classes.list" | awk '{printf "- %s\n", $0}'
        echo
    fi
fi

# ----- 9. 이벤트 누수 -----
if [ -f "$DATA_DIR/event_leaks.tsv" ]; then
    echo "## 9. 이벤트 누수 후보"
    echo
    if [ ! -s "$DATA_DIR/event_leaks.tsv" ]; then
        echo "- 없음 ✓"
    else
        sort -rn -t $'\t' -k1 "$DATA_DIR/event_leaks.tsv" | head -15 | awk -F'\t' -v root="$ROOT/" '{
            p=$2; sub(root, "", p); printf "- **%d** 누수 후보 — %s\n", $1, p
        }'
    fi
    echo
fi

# ----- 10. .meta -----
if [ -f "$DATA_DIR/meta_integrity.kv" ]; then
    echo "## 10. .meta 무결성"
    echo
    echo "- .cs 파일 중 .meta 누락: **$(kv_read "$DATA_DIR/meta_integrity.kv" missing_meta)**"
    echo "- 고아 .meta: **$(kv_read "$DATA_DIR/meta_integrity.kv" orphan_meta)**"
    echo
fi

# ----- 11. Async Adoption -----
if [ -f "$DATA_DIR/async_adoption.kv" ]; then
    echo "## 11. Async Adoption (UniTask 마이그레이션 추적, ADR-005)"
    echo
    echo "| 지표 | 값 | 목표 (Phase 2-C 후) |"
    echo "|---|---:|---:|"
    printf "| \`IEnumerator\` 메서드 | %s | 0 |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" ienumerator_methods)"
    printf "| \`yield\` 문 | %s | 0 |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" yield_statements)"
    printf "| \`StartCoroutine\` 호출 | %s | 0 |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" start_coroutine_calls)"
    printf "| \`async UniTask\` 메서드 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" unitask_methods)"
    printf "| \`await\` 문 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" await_statements)"
    printf "| UniTask using 파일 수 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/async_adoption.kv" unitask_using_files)"
    echo
fi

# ----- 12. Asset Loading -----
if [ -f "$DATA_DIR/asset_loading.kv" ]; then
    echo "## 12. Asset Loading (Addressables 마이그레이션 추적, ADR-007)"
    echo
    echo "| 지표 | 값 | 목표 (Phase 2-D 후) |"
    echo "|---|---:|---:|"
    printf "| \`Resources.Load\` 호출 | %s | 0 |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" resources_load_calls)"
    printf "| \`Assets/Resources/\` 파일 수 | %s | 0 |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" resources_folder_files)"
    printf "| \`AssetReference\` 선언 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" asset_reference_decls)"
    printf "| \`LoadAssetAsync\` 호출 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" load_asset_async_calls)"
    printf "| \`InstantiateAsync\` 호출 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" instantiate_async_calls)"
    printf "| Addressables using 파일 수 | %s | (증가) |\n" "$(kv_read "$DATA_DIR/asset_loading.kv" addressables_using_files)"
    echo
fi

echo "---"
echo
echo "_재실행: \`bash tmp/refactor-2026-05/harness/run_all.sh\`_"
} > "$OUT"

echo "Report written: $OUT" >&2
