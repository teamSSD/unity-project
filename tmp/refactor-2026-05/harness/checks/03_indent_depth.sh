#!/usr/bin/env bash
# 파일별 최대 들여쓰기 깊이 (4-space 기준, 탭은 4로 환산)
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/indent_depths.tsv"
: > "$OUT"

extract_max_indent() {
    file="$1"
    awk '
        {
            line=$0
            gsub(/\t/, "    ", line)
            if (match(line, /^ */)) {
                if (line ~ /^[ ]*$/) next
                depth=int(RLENGTH/4)
                if (depth > max) max=depth
            }
        }
        END { print (max ? max : 0) }
    ' "$file"
}

while IFS= read -r f; do
    [ -z "$f" ] && continue
    d=$(extract_max_indent "$f")
    printf '%s\t%s\n' "$d" "$f" >> "$OUT"
done < "$RUNTIME_LIST"

echo "indent_depth: done" >&2
