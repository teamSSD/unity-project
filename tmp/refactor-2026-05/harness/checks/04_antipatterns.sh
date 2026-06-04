#!/usr/bin/env bash
# 일반 안티패턴 카운트 + Singleton 파일 목록 + 매직 넘버
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT_AP="$DATA_DIR/antipatterns.tsv"
OUT_SINGLE="$DATA_DIR/singleton_files.list"
OUT_MN="$DATA_DIR/magic_numbers.kv"
: > "$OUT_AP"
: > "$OUT_SINGLE"
: > "$OUT_MN"

count_p() {
    label="$1"; pattern="$2"
    n=$(xargs0 "$RUNTIME_LIST" grep -E "$pattern" 2>/dev/null | wc -l | tr -d ' ')
    printf '%s\t%s\n' "$label" "$n" >> "$OUT_AP"
}

count_p "singleton_decl"        'public static[[:space:]]+[A-Za-z_<>]+[[:space:]]+Instance'
# instance_access — facade .Instance 호출만. 제외:
# - Composition Root (GameSessionRoot, CatalogProvider): ADR-001 정당
# - .NET 시스템 라이브러리 (BindingFlags 등)
n=$(xargs0 "$RUNTIME_LIST" grep -hE '\.Instance\b' 2>/dev/null \
    | grep -vE 'GameSessionRoot\.Instance|CatalogProvider\.Instance|BindingFlags\.Instance' \
    | wc -l | tr -d ' ')
printf 'instance_access\t%s\n' "$n" >> "$OUT_AP"
count_p "public_mutable_field"  '^[[:space:]]+public[[:space:]]+[A-Za-z_<>][A-Za-z0-9_<>,\[\]\. ]*[[:space:]]+[a-z][A-Za-z0-9_]*[[:space:]]*[=;]'
count_p "static_mutable_field"  '^[[:space:]]+(public|private|protected|internal)?[[:space:]]*static[[:space:]]+[A-Za-z_<>][A-Za-z0-9_<>,\[\]\. ]*[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*[=;]'
count_p "find_object"           'GameObject\.Find|FindObjectOfType|FindObjectsOfType|FindAnyObjectOfType|FindFirstObjectByType'
count_p "send_message"          'SendMessage(Upwards)?\('
count_p "resources_load"        'Resources\.Load'
count_p "debug_log"             'Debug\.Log\('
count_p "empty_catch"           'catch[[:space:]]*\([^)]*\)[[:space:]]*\{[[:space:]]*\}'
count_p "todo_comment"          '//[[:space:]]*(TODO|FIXME|HACK|XXX)'
count_p "yield_return_null"     'yield return null;'
count_p "case_label"            'case[[:space:]]+[A-Za-z0-9_.]+:'
count_p "event_subscribe"       '[+][=][[:space:]]*[A-Za-z_][A-Za-z0-9_.]*[[:space:]]*[;\(]'
count_p "event_unsubscribe"     '[-][=][[:space:]]*[A-Za-z_][A-Za-z0-9_.]*[[:space:]]*[;\(]'
count_p "serialize_field"       '\[SerializeField\]'
count_p "camera_main"           'Camera\.main'
count_p "mono_behaviour_class"  'class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*:[[:space:]]*MonoBehaviour'

xargs0 "$RUNTIME_LIST" grep -lE 'public static[[:space:]]+[A-Za-z_<>]+[[:space:]]+Instance' 2>/dev/null > "$OUT_SINGLE" || true

MN_COUNT=$(xargs0 "$RUNTIME_LIST" grep -hnE '[^A-Za-z0-9_."\[][0-9]+\.?[0-9]*f?' 2>/dev/null \
    | grep -vE '^[^:]*:[^:]*://' \
    | grep -vE '(^|[^A-Za-z0-9_])[01](\b|f|\.0|\.0f)([^A-Za-z0-9_]|$)' \
    | grep -vE 'using |namespace |GetType|Length|Count|Substring' \
    | wc -l | tr -d ' ')
kv_write "$OUT_MN" "magic_number_lines" "$MN_COUNT"

echo "antipatterns: done" >&2
