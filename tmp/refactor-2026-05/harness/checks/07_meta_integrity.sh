#!/usr/bin/env bash
# .meta 무결성 — 누락된 .meta와 고아 .meta
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/meta_integrity.kv"
: > "$OUT"

NO_META=0
while IFS= read -r f; do
    [ -z "$f" ] && continue
    [ -f "${f}.meta" ] || NO_META=$((NO_META + 1))
done < "$ALL_CS_LIST"

ORPHAN_META=0
while IFS= read -r m; do
    [ -z "$m" ] && continue
    src="${m%.meta}"
    [ -e "$src" ] || ORPHAN_META=$((ORPHAN_META + 1))
done < <(find "$ROOT/Assets" -name "*.meta")

kv_write "$OUT" "missing_meta" "$NO_META"
kv_write "$OUT" "orphan_meta" "$ORPHAN_META"

echo "meta_integrity: missing=$NO_META, orphan=$ORPHAN_META" >&2
