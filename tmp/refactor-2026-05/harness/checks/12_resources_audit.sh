#!/usr/bin/env bash
# Resources/ 폴더 인벤토리화 — Phase 2-D 준비.
# 각 파일에 대해 (1) 코드의 Resources.Load 키 매칭 (2) 씬/프리팹/에셋의 GUID 참조 검사.
# 출력:
#   data/resources_inventory.tsv : key|guid|path|used_by_code|used_by_asset|verdict
#   data/resources_used.list     : 사용중 파일 (절대경로)
#   data/resources_unused.list   : 미사용 후보 (절대경로)
#   data/resources_summary.kv    : 총계
set -eu
export LC_ALL=C
export LANG=C

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

RES_DIR="$ROOT/Assets/Resources"
OUT_INV="$DATA_DIR/resources_inventory.tsv"
OUT_USED="$DATA_DIR/resources_used.list"
OUT_UNUSED="$DATA_DIR/resources_unused.list"
OUT_SUM="$DATA_DIR/resources_summary.kv"
: > "$OUT_INV"; : > "$OUT_USED"; : > "$OUT_UNUSED"; : > "$OUT_SUM"

if [ ! -d "$RES_DIR" ]; then
    echo "Resources folder not found: $RES_DIR" >&2
    kv_write "$OUT_SUM" "total" "0"
    exit 0
fi

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

echo "[1/4] 코드에서 Resources.Load 키 + LoadAll path + 동적 폴더 추출..." >&2
# 1a. 정적 키: Resources.Load("...")
xargs0 "$RUNTIME_LIST" grep -hoE 'Resources\.Load[A-Za-z]*[^"]*"[^"]+"' 2>/dev/null \
    | grep -oE '"[^"]+"' | tr -d '"' | sort -u > "$WORK/code_keys.txt"
# 1b. ResourcePaths 등 const string도 보완 (정적 키와 dynamic 폴더 prefix 모두)
xargs0 "$RUNTIME_LIST" grep -hoE 'public const string [A-Za-z_]+[[:space:]]*=[[:space:]]*"[^"]+"' 2>/dev/null \
    | grep -oE '"[^"]+"' | tr -d '"' | sort -u >> "$WORK/code_keys.txt"
sort -u "$WORK/code_keys.txt" -o "$WORK/code_keys.txt"
N_CODE_KEYS=$(wc -l < "$WORK/code_keys.txt" | tr -d ' ')

# 1c. LoadAll path (폴더 일괄 로드) — 그 폴더 하위 모든 파일이 사용중
# Resources.LoadAll<T>(ResourcePaths.X.Y) 형식 → ResourcePaths.X.Y의 const 값을 추출
# 또는 Resources.LoadAll<T>("literal path") 직접 문자열
xargs0 "$RUNTIME_LIST" grep -hoE 'Resources\.LoadAll[A-Za-z<>]*\([^)]+\)' 2>/dev/null > "$WORK/loadall_raw.txt"
# 직접 문자열 추출
grep -oE '"[^"]+"' "$WORK/loadall_raw.txt" 2>/dev/null | tr -d '"' | sort -u > "$WORK/loadall_folders.txt" || true
# ResourcePaths.X.Y 참조 추출 후 const 정의에서 값 조회
grep -oE 'ResourcePaths\.[A-Za-z]+\.[A-Za-z]+' "$WORK/loadall_raw.txt" 2>/dev/null | sort -u > "$WORK/loadall_const_refs.txt" || true
while IFS= read -r ref; do
    [ -z "$ref" ] && continue
    name=$(echo "$ref" | awk -F'.' '{print $NF}')
    # ResourcePaths.cs에서 해당 const 값 조회
    val=$(grep -E "public const string $name[[:space:]]*=" "$ROOT/Assets/Scripts/Schema/Common/ResourcePaths.cs" 2>/dev/null \
          | grep -oE '"[^"]+"' | tr -d '"' | head -1)
    [ -n "$val" ] && echo "$val" >> "$WORK/loadall_folders.txt"
done < "$WORK/loadall_const_refs.txt"
sort -u "$WORK/loadall_folders.txt" -o "$WORK/loadall_folders.txt" 2>/dev/null || true

# 1d. 동적 키 패턴 — Resources.Load<>(variable + ...)가 있는 영역의 prefix 추적
# const string으로 정의된 prefix (예: "ScriptableObjects/FoodData/")는 그 prefix 하위 모두 사용
# code_keys.txt에 prefix 형태(/로 끝나는 것)가 있으면 그것도 LoadAll-equivalent 처리
grep -E '/$' "$WORK/code_keys.txt" 2>/dev/null >> "$WORK/loadall_folders.txt" || true
sort -u "$WORK/loadall_folders.txt" -o "$WORK/loadall_folders.txt"
N_LOADALL=$(wc -l < "$WORK/loadall_folders.txt" | tr -d ' ')
echo "    정적 키: $N_CODE_KEYS, LoadAll/dynamic prefix 폴더: $N_LOADALL" >&2

echo "[2/4] 씬/프리팹/에셋의 GUID 참조 추출..." >&2
find "$ROOT/Assets" \( -name "*.unity" -o -name "*.prefab" -o -name "*.asset" -o -name "*.controller" -o -name "*.mat" \) \
    ! -path "*/Resources/*" \
    -exec grep -hoE 'guid: [a-f0-9]{32}' {} \; 2>/dev/null \
    | awk '{print $2}' | sort -u > "$WORK/asset_guids.txt"
N_GUIDS=$(wc -l < "$WORK/asset_guids.txt" | tr -d ' ')
echo "    참조 GUID: $N_GUIDS" >&2

echo "[3/4] Resources/ 파일별 매칭..." >&2
total=0
used=0
unused=0
while IFS= read -r f; do
    [ -z "$f" ] && continue
    total=$((total + 1))
    rel_to_resources=${f#$RES_DIR/}
    key="${rel_to_resources%.*}"
    meta="${f}.meta"
    guid=""
    [ -f "$meta" ] && guid=$(awk '/^guid:/{print $2; exit}' "$meta")

    used_by_code=0
    grep -qFx "$key" "$WORK/code_keys.txt" && used_by_code=1

    used_by_asset=0
    if [ -n "$guid" ]; then
        grep -qFx "$guid" "$WORK/asset_guids.txt" && used_by_asset=1
    fi

    # LoadAll/dynamic prefix 매칭: key가 폴더 prefix로 시작하면 사용중
    used_by_loadall=0
    while IFS= read -r prefix; do
        [ -z "$prefix" ] && continue
        # prefix가 "ScriptableObjects/FoodData" 면 "ScriptableObjects/FoodData/cabbage" 매칭
        # prefix가 "/"로 끝나면 그대로, 아니면 + "/" 후 매칭
        normalized="${prefix%/}"
        case "$key" in
            "$normalized"|"$normalized"/*) used_by_loadall=1; break ;;
        esac
    done < "$WORK/loadall_folders.txt"

    # TextMesh Pro 폴더는 항상 사용중 (Unity TMP 패키지가 require)
    case "$key" in
        "TextMesh Pro/"*) used_by_loadall=1 ;;
    esac

    if [ "$used_by_code" = "1" ] || [ "$used_by_asset" = "1" ] || [ "$used_by_loadall" = "1" ]; then
        verdict="USED"
        echo "$f" >> "$OUT_USED"
        used=$((used + 1))
    else
        verdict="UNUSED"
        echo "$f" >> "$OUT_UNUSED"
        unused=$((unused + 1))
    fi

    printf "%s\t%s\t%s\t%s\t%s\t%s\t%s\n" "$key" "$guid" "$rel_to_resources" "$used_by_code" "$used_by_asset" "$used_by_loadall" "$verdict" >> "$OUT_INV"
done < <(find "$RES_DIR" -type f -not -name "*.meta")

echo "[4/4] 요약..." >&2
kv_write "$OUT_SUM" "total" "$total"
kv_write "$OUT_SUM" "used" "$used"
kv_write "$OUT_SUM" "unused" "$unused"
kv_write "$OUT_SUM" "code_keys_extracted" "$N_CODE_KEYS"
kv_write "$OUT_SUM" "asset_guids_extracted" "$N_GUIDS"

# 카테고리별 (확장자) 미사용 분포
echo "" >> "$OUT_SUM"
echo "# 확장자별 미사용 후보" >> "$OUT_SUM"
awk -F'\t' '$6=="UNUSED" { n=split($3, parts, "."); print parts[n] }' "$OUT_INV" \
    | sort | uniq -c | sort -rn | awk '{print "unused_ext_" $2 "\t" $1}' >> "$OUT_SUM"

echo "" >&2
echo "resources_audit: total=$total, used=$used, unused=$unused" >&2
echo "  Output: $OUT_INV" >&2
echo "  Used list: $OUT_USED" >&2
echo "  Unused list: $OUT_UNUSED" >&2
