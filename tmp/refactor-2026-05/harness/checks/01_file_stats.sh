#!/usr/bin/env bash
# 파일/라인 통계 + 런타임 파일별 라인 수 TSV
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT_KV="$DATA_DIR/file_stats.kv"
OUT_LINES="$DATA_DIR/file_lines.tsv"
: > "$OUT_KV"
: > "$OUT_LINES"

kv_write "$OUT_KV" "runtime_files" "$(wc -l < "$RUNTIME_LIST" | tr -d ' ')"
kv_write "$OUT_KV" "runtime_lines" "$(sum_lines "$RUNTIME_LIST")"
kv_write "$OUT_KV" "editor_files"  "$(wc -l < "$EDITOR_LIST" | tr -d ' ')"
kv_write "$OUT_KV" "editor_lines"  "$(sum_lines "$EDITOR_LIST")"
kv_write "$OUT_KV" "test_files"    "$(wc -l < "$TEST_LIST" | tr -d ' ')"
kv_write "$OUT_KV" "test_lines"    "$(sum_lines "$TEST_LIST")"

while IFS= read -r f; do
    [ -z "$f" ] && continue
    n=$(wc -l < "$f" | tr -d ' ')
    printf '%s\t%s\n' "$n" "$f" >> "$OUT_LINES"
done < "$RUNTIME_LIST"

# 최대 파일 라인 수 (gate.sh에서 사용)
MAX_LINES=$(awk -F'\t' 'BEGIN{m=0} { if ($1+0>m) m=$1+0 } END{ print m+0 }' "$OUT_LINES")
kv_write "$OUT_KV" "max_file_lines" "$MAX_LINES"

echo "file_stats: done" >&2
