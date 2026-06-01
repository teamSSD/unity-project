#!/usr/bin/env bash
# di_getcomponent_no_require.tsv의 각 (file, type) 쌍에 대해
# 클래스 선언 직전에 [RequireComponent(typeof(Type))] 추가.
# Idempotent.
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$SCRIPT_DIR/lib/common.sh"
detect_root

LIST="$DATA_DIR/di_getcomponent_no_require.tsv"
[ -f "$LIST" ] || { echo "Run harness first" >&2; exit 1; }

ADDED=0
while IFS=$'\t' read -r f type; do
    [ -z "$f" ] || [ -z "$type" ] && continue
    [ -f "$f" ] || continue
    # 이미 [RequireComponent(typeof(TYPE))] 있으면 skip
    if grep -qE "RequireComponent\(typeof\(${type}\)\)" "$f" 2>/dev/null; then
        continue
    fi
    # 클래스 선언 위에 attribute 삽입 (첫 "public class" 또는 "public partial class")
    CLASS_LINE=$(grep -nE '^public[[:space:]]+(partial[[:space:]]+)?(abstract[[:space:]]+|sealed[[:space:]]+)?(class|struct)' "$f" | head -1 | cut -d: -f1)
    [ -z "$CLASS_LINE" ] && continue
    # 클래스 선언 위 줄에 이미 [Xxx] attribute 있으면 그 위에 추가
    awk -v ln="$CLASS_LINE" -v t="$type" '
        NR == ln { print "[RequireComponent(typeof(" t "))]" }
        { print }
    ' "$f" > "$f.tmp" && mv "$f.tmp" "$f"
    ADDED=$((ADDED + 1))
done < "$LIST"

echo "Added [RequireComponent] to $ADDED entries"
