#!/usr/bin/env bash
# 67개 SerializeField-without-OnValidate 파일에 표준 OnValidate 일괄 추가.
# 마지막 `}` 직전에 OnValidate 메서드 삽입. Idempotent.
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$SCRIPT_DIR/lib/common.sh"
detect_root

LIST="$DATA_DIR/di_serialize_no_validate.list"
[ -f "$LIST" ] || { echo "Run harness first" >&2; exit 1; }

# 삽입할 snippet (별도 파일로)
SNIPPET=$(mktemp)
trap 'rm -f "$SNIPPET"' EXIT
cat > "$SNIPPET" <<'EOF'

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
EOF

ADDED=0
SKIPPED=0
while IFS= read -r f; do
    [ -z "$f" ] && continue
    [ -f "$f" ] || continue
    if grep -qE 'void[[:space:]]+OnValidate[[:space:]]*\(' "$f" 2>/dev/null; then
        SKIPPED=$((SKIPPED + 1))
        continue
    fi
    # 마지막 `^}$` 줄 번호 찾기
    LAST=$(grep -n '^}[[:space:]]*$' "$f" | tail -1 | cut -d: -f1)
    [ -z "$LAST" ] && continue
    # head + snippet + tail 조합
    {
        head -n $((LAST - 1)) "$f"
        cat "$SNIPPET"
        tail -n +$LAST "$f"
    } > "$f.tmp" && mv "$f.tmp" "$f"
    ADDED=$((ADDED + 1))
done < "$LIST"

echo "Added OnValidate to $ADDED files (skipped $SKIPPED already-had)"
