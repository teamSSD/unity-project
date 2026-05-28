#!/usr/bin/env bash
# Phase 2-A 자동 카테고리화 — 각 .cs 파일의 새 asmdef 폴더 경로 추정
# 출력: tmp/refactor-2026-05/plan/phase2a_mapping.tsv
# 형식: source<TAB>destination<TAB>asmdef<TAB>feature<TAB>note
set -eu
export LC_ALL=C
export LANG=C

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/../harness" && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

# 간접 MonoBehaviour 상속 후보 — abstract class X : MonoBehaviour 패턴 동적 탐지
MB_BASES_RAW=$(tr '\n' '\0' < "$RUNTIME_LIST" | xargs -0 grep -lE "abstract[[:space:]]+(partial[[:space:]]+)?class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*:[[:space:]]*MonoBehaviour" 2>/dev/null \
    | xargs -I{} basename {} .cs | tr '\n' '|' | sed 's/|$//')
MB_BASES="MonoBehaviour|SingletonMonoBehaviour|${MB_BASES_RAW}"
echo "MB bases discovered: ${MB_BASES}" >&2

# 간접 ScriptableObject 상속 후보 — class X : ScriptableObject (abstract 또는 concrete)
SO_BASES_RAW=$(tr '\n' '\0' < "$RUNTIME_LIST" | xargs -0 grep -lE "class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*:[[:space:]]*ScriptableObject" 2>/dev/null \
    | xargs -I{} basename {} .cs | tr '\n' '|' | sed 's/|$//')
SO_BASES="ScriptableObject|${SO_BASES_RAW}"
echo "SO bases discovered: ${SO_BASES}" >&2

PLAN_DIR="$(cd "$SCRIPT_DIR/../plan" && pwd)"
OUT="$PLAN_DIR/phase2a_mapping.tsv"
{
  echo "# Auto-generated Phase 2-A mapping draft"
  echo "# Format: source<TAB>destination<TAB>asmdef<TAB>feature<TAB>note"
  echo "# Review needed. Especially: feature=?, complex Models split"
} > "$OUT"

# Feature 추출 (현재 경로 기반 휴리스틱)
get_feature() {
    path="$1"
    case "$path" in
        */Entities/Cooking/cooking/Bento/*)         echo "Cooking" ;;
        */Entities/Cooking/cooking/CookingTool/*)   echo "Cooking" ;;
        */Entities/Cooking/cooking/Food/*)          echo "Cooking" ;;
        */Entities/Cooking/cooking/OrderTicket/*)   echo "Cooking" ;;
        */Entities/Cooking/cooking/Description/*)   echo "Cooking" ;;
        */Entities/Cooking/cooking/dataType/*)      echo "Cooking" ;;
        */Entities/Cooking/cooking/*)               echo "Cooking" ;;
        */Entities/Cooking/customer/*)              echo "Cooking" ;;
        */Entities/Cooking/minigame/*)              echo "Cooking" ;;
        */Entities/Cooking/*)                       echo "Cooking" ;;
        */Entities/Garden/*)                        echo "Garden" ;;
        */Entities/ItemShop/*)                      echo "Shop" ;;
        */Entities/Mall/Dialogue/*)                 echo "Mall" ;;
        */Entities/Mall/delivery/*)                 echo "Mall" ;;
        */Entities/Mall/*)                          echo "Mall" ;;
        */Entities/RecipeBook/menu/*)               echo "UI" ;;
        */Entities/RecipeBook/inventory/*)          echo "UI" ;;
        */Entities/RecipeBook/*)                    echo "UI" ;;
        */Entities/SO/*)                            echo "Common" ;;
        */Entities/Stats/*)                         echo "Common" ;;
        */Managers/cooking/*)                       echo "Cooking" ;;
        */Managers/garden/*)                        echo "Garden" ;;
        */Managers/shop/*)                          echo "Shop" ;;
        */Managers/*)                               echo "Common" ;;
        */Controllers/*)                            echo "Common" ;;
        */Systems/Commons/*)                        echo "Common" ;;
        */Systems/MiniGameSystem/*)                 echo "Cooking" ;;
        */Systems/GardenSystem/*)                   echo "Garden" ;;
        */Systems/ShopSystem/*)                     echo "Shop" ;;
        */UI/Settlement/*)                          echo "Mall" ;;
        */UI/stats/*)                               echo "UI" ;;
        */UI/general/*)                             echo "UI" ;;
        */UI/*)                                     echo "UI" ;;
        */Interfaces/Cooking/*)                     echo "Cooking" ;;
        */Interfaces/Gardening/*)                   echo "Garden" ;;
        */Interfaces/Shop/*)                        echo "Shop" ;;
        */Interfaces/*)                             echo "Common" ;;
        */Utilities/dataIO/*)                       echo "Common" ;;
        */Utilities/editor/*)                       echo "Editor" ;;
        */Utilities/mouseControl/*)                 echo "Common" ;;
        */Utilities/randoms/*)                      echo "Common" ;;
        */Utilities/*)                              echo "Common" ;;
        */Scripts/Editor/*)                         echo "Common" ;;
        *)                                          echo "?" ;;
    esac
}

# Dominant declaration 탐지. 출력: "type|line_no|has_serializable|name_match"
#   type: static | monobehaviour | scriptableobject | interface | enum | abstract | class | unknown
#   has_serializable: 1 (dominant class 바로 위에 [Serializable]) | 0
#   name_match: 1 (파일명과 클래스명 일치) | 0 (fallback)
get_dominant_info() {
    f="$1"
    name="$2"

    # Try 1: 파일명과 정확히 일치하는 class/interface/enum
    line_no=$(grep -nE "^[^A-Za-z]*(public[[:space:]]+)?(static[[:space:]]+|sealed[[:space:]]+|abstract[[:space:]]+|partial[[:space:]]+)*(class|interface|enum)[[:space:]]+${name}([[:space:]]|:|<|$|\{)" "$f" 2>/dev/null | head -1 | cut -d: -f1)
    name_match=1

    # Try 2: 첫 public class/interface/enum (파일명 불일치 fallback)
    if [ -z "$line_no" ]; then
        line_no=$(grep -nE "^[^A-Za-z]*public[[:space:]]+(static[[:space:]]+|sealed[[:space:]]+|abstract[[:space:]]+|partial[[:space:]]+)*(class|interface|enum)" "$f" 2>/dev/null | head -1 | cut -d: -f1)
        name_match=0
    fi

    [ -z "$line_no" ] && { echo "unknown|0|0|0"; return; }

    # 클래스 선언부 추출: line_no 부터 첫 '{' 가 나타나는 줄까지 (최대 5줄)
    end_line=$line_no
    max_la=$((line_no + 4))
    while [ "$end_line" -le "$max_la" ]; do
        if sed -n "${end_line}p" "$f" | grep -q '{'; then break; fi
        end_line=$((end_line + 1))
    done
    line_text=$(sed -n "${line_no},${end_line}p" "$f" | tr '\n' ' ')

    # 타입 판정 (우선순위 순서)
    type="class"
    if echo "$line_text" | grep -qE "static[[:space:]]+(partial[[:space:]]+)?class"; then
        type="static"
    elif echo "$line_text" | grep -qE "class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[^{]*:[^{]*(${MB_BASES})"; then
        type="monobehaviour"
    elif echo "$line_text" | grep -qE "class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[^{]*:[^{]*(${SO_BASES})"; then
        type="scriptableobject"
    elif echo "$line_text" | grep -qE "interface[[:space:]]+[A-Za-z_]"; then
        type="interface"
    elif echo "$line_text" | grep -qE "enum[[:space:]]+[A-Za-z_]"; then
        type="enum"
    elif echo "$line_text" | grep -qE "abstract[[:space:]]+(partial[[:space:]]+)?class"; then
        type="abstract"
    fi

    # [Serializable] on dominant class? (바로 윗줄 확인)
    has_ser=0
    prev_line=$((line_no - 1))
    if [ "$prev_line" -ge 1 ]; then
        sed -n "${prev_line}p" "$f" | grep -qE '^\[(System\.)?Serializable\]' && has_ser=1
    fi

    echo "$type|$line_no|$has_ser|$name_match"
}

# 분류 결정
categorize() {
    f="$1"
    feature=$(get_feature "$f")
    rel=${f#$ROOT/}
    base=$(basename "$f")

    # Editor 폴더
    case "$rel" in
        Assets/Editor/*|Assets/Scripts/Editor/*|Assets/Scripts/Utilities/editor/*)
            printf "%s\tAssets/Scripts/Editor/%s\tEditor\t%s\t에디터 도구\n" "$rel" "$base" "$feature"
            return
            ;;
    esac

    name="${base%.cs}"
    info=$(get_dominant_info "$f" "$name")
    dom=$(echo "$info" | cut -d'|' -f1)
    has_ser_dom=$(echo "$info" | cut -d'|' -f3)
    name_match=$(echo "$info" | cut -d'|' -f4)

    # name_match=0이면 fallback 사용 → note에 표시 (rename 후보)
    name_note=""
    [ "$name_match" = "0" ] && name_note=" [filename≠class]"

    # MonoBehaviour인데 abstract인 경우 (BaseStorage 등)도 Unity로
    if [ "$dom" = "abstract" ]; then
        grep -qE "abstract[[:space:]]+(partial[[:space:]]+)?class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[^{]*:[^{]*MonoBehaviour" "$f" 2>/dev/null && dom="monobehaviour"
    fi

    case "$dom" in
        scriptableobject)
            printf "%s\tAssets/Scripts/Schema/Config/%s/%s\tSchema/Config\t%s\tScriptableObject%s\n" "$rel" "$feature" "$base" "$feature" "$name_note"
            ;;
        enum)
            printf "%s\tAssets/Scripts/Schema/Config/%s/%s\tSchema/Config\t%s\tenum%s\n" "$rel" "$feature" "$base" "$feature" "$name_note"
            ;;
        interface)
            extra=""
            grep -qE '^[[:space:]]*public[[:space:]]+static[[:space:]]+class' "$f" 2>/dev/null && extra="+static"
            printf "%s\tAssets/Scripts/Schema/Interfaces/%s/%s\tSchema/Interfaces\t%s\tinterface%s%s\n" "$rel" "$feature" "$base" "$feature" "$extra" "$name_note"
            ;;
        monobehaviour)
            printf "%s\tAssets/Scripts/Unity/%s/%s\tUnity\t%s\tMonoBehaviour%s\n" "$rel" "$feature" "$base" "$feature" "$name_note"
            ;;
        static)
            extra=""
            grep -qE '^\[(System\.)?Serializable\]' "$f" 2>/dev/null && extra="+SaveData"
            printf "%s\tAssets/Scripts/Domain/%s/%s\tDomain\t%s\tstatic 유틸%s%s\n" "$rel" "$feature" "$base" "$feature" "$extra" "$name_note"
            ;;
        abstract)
            printf "%s\tAssets/Scripts/Domain/%s/%s\tDomain\t%s\tabstract class%s\n" "$rel" "$feature" "$base" "$feature" "$name_note"
            ;;
        class)
            if [ "$has_ser_dom" = "1" ]; then
                printf "%s\tAssets/Scripts/Schema/State/%s/%s\tSchema/State\t%s\t[Serializable] POCO%s\n" "$rel" "$feature" "$base" "$feature" "$name_note"
            else
                # 보조 [Serializable] 클래스가 있으면 note에 표시
                extra=""
                grep -qE '^\[(System\.)?Serializable\]' "$f" 2>/dev/null && extra="+SaveData"
                printf "%s\tAssets/Scripts/Domain/%s/%s\tDomain\t%s\t순수 C# 클래스%s%s\n" "$rel" "$feature" "$base" "$feature" "$extra" "$name_note"
            fi
            ;;
        unknown)
            printf "%s\t?\t?\t%s\tdominant 선언 없음 — 수동 검토%s\n" "$rel" "$feature" "$name_note"
            ;;
    esac
}

# 런타임 파일 카테고리화
while IFS= read -r f; do
    [ -z "$f" ] && continue
    categorize "$f" >> "$OUT"
done < "$RUNTIME_LIST"

# 에디터 파일 카테고리화
while IFS= read -r f; do
    [ -z "$f" ] && continue
    feature=$(get_feature "$f")
    rel=${f#$ROOT/}
    base=$(basename "$f")
    printf "%s\tAssets/Scripts/Editor/%s\tEditor\t%s\t에디터 도구\n" "$rel" "$base" "$feature" >> "$OUT"
done < "$EDITOR_LIST"

# 요약 (stderr)
{
    echo ""
    echo "=== Mapping draft summary ==="
    total=$(grep -vE '^#' "$OUT" | wc -l | tr -d ' ')
    echo "Total files: $total"
    for asmdef in "Schema/Config" "Schema/Interfaces" "Schema/State" "Domain" "Unity" "Editor"; do
        c=$(awk -F'\t' -v a="$asmdef" '$3==a' "$OUT" | wc -l | tr -d ' ')
        echo "  $asmdef: $c"
    done
    echo ""
    echo "=== feature=? (검토 필요) ==="
    awk -F'\t' '$4=="?" { print "  " $1 }' "$OUT" | head -20
    n_unknown=$(awk -F'\t' '$4=="?"' "$OUT" | wc -l | tr -d ' ')
    [ "$n_unknown" -gt 20 ] && echo "  ... 외 $((n_unknown - 20))개"
} >&2

echo "" >&2
echo "Mapping written: $OUT" >&2
