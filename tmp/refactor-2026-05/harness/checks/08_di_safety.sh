#!/usr/bin/env bash
# DI Safety — 숨겨진 의존성, 초기화 순서 race, 인스펙터 검증 누락, 자체 Singleton, 레이어 위반 등
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT_KV="$DATA_DIR/di_safety.kv"
OUT_HIDDEN="$DATA_DIR/di_hidden_deps.tsv"
OUT_AWAKE="$DATA_DIR/di_awake_instance.tsv"
OUT_NOVAL="$DATA_DIR/di_serialize_no_validate.list"
OUT_NOREQ="$DATA_DIR/di_getcomponent_no_require.tsv"
OUT_SELFSG="$DATA_DIR/di_self_rolled_singleton.list"
OUT_MODEL="$DATA_DIR/di_model_instance_access.list"
: > "$OUT_KV"
: > "$OUT_HIDDEN"
: > "$OUT_AWAKE"
: > "$OUT_NOVAL"
: > "$OUT_NOREQ"
: > "$OUT_SELFSG"
: > "$OUT_MODEL"

# --- 1. Hidden dependency density: 클래스 내부 .Instance 호출 횟수 ---
while IFS= read -r f; do
    [ -z "$f" ] && continue
    n=$(grep -cE '\.Instance\b' "$f" 2>/dev/null || true)
    [ -z "$n" ] && n=0
    if [ "$n" -gt 0 ]; then
        printf '%s\t%s\n' "$n" "$f" >> "$OUT_HIDDEN"
    fi
done < "$RUNTIME_LIST"

# --- 2. Awake/Start/OnEnable 본문에 .Instance 접근 (초기화 순서 race) ---
# 메서드 본문을 추출해 .Instance 매칭 여부 확인.
extract_init_methods_with_instance() {
    file="$1"
    awk -v fname="$file" '
        BEGIN { depth=0; in_method=0; method_name=""; method_start=0; body="" }
        function emit() {
            if (body ~ /\.Instance/) {
                printf "%s\t%s:%d\n", method_name, fname, method_start
            }
        }
        {
            line=$0
            if (!in_method) {
                # 시그니처: void/IEnumerator/Task 반환형 + Awake/Start/OnEnable + ()
                if (line ~ /(void|IEnumerator|Task)/ && match(line, /[ \t](Awake|Start|OnEnable)[ \t]*\(\)/)) {
                    if (match(line, /(Awake|Start|OnEnable)/)) {
                        method_name=substr(line, RSTART, RLENGTH)
                    }
                    method_start=NR
                    in_method=1
                    body=""
                    if (line ~ /\{[ \t]*$/) depth=1
                    else if (line ~ /=>/) {
                        # expression-bodied
                        if (line ~ /\.Instance/) printf "%s\t%s:%d\n", method_name, fname, method_start
                        in_method=0
                    }
                }
            } else {
                body = body "\n" line
                if (depth==0 && line ~ /\{/) depth=1
                else {
                    n=gsub(/\{/, "&", line); depth+=n
                    n=gsub(/\}/, "&", line); depth-=n
                    if (depth<=0) {
                        emit()
                        in_method=0
                        depth=0
                        body=""
                    }
                }
            }
        }
    ' "$file"
}

while IFS= read -r f; do
    [ -z "$f" ] && continue
    extract_init_methods_with_instance "$f" >> "$OUT_AWAKE"
done < "$RUNTIME_LIST"

# --- 3. [SerializeField] 있는데 OnValidate 없음 ---
while IFS= read -r f; do
    [ -z "$f" ] && continue
    if grep -qE '\[SerializeField\]' "$f" 2>/dev/null; then
        if ! grep -qE 'void[[:space:]]+OnValidate[[:space:]]*\(' "$f" 2>/dev/null; then
            echo "$f" >> "$OUT_NOVAL"
        fi
    fi
done < "$RUNTIME_LIST"

# --- 4. GetComponent<X> 있는데 [RequireComponent(typeof(X))] 부재 ---
while IFS= read -r f; do
    [ -z "$f" ] && continue
    REQ=$(grep -oE 'RequireComponent\(typeof\([A-Za-z_][A-Za-z0-9_]*\)\)' "$f" 2>/dev/null \
        | awk '{ sub(/.*typeof\(/, ""); sub(/\)\).*/, ""); print }' | sort -u)
    GETS=$(grep -oE 'GetComponent(InChildren|InParent|sInChildren|s)?<[A-Za-z_][A-Za-z0-9_]*>' "$f" 2>/dev/null \
        | awk '{ sub(/.*</, ""); sub(/>$/, ""); print }' | sort -u)
    [ -z "$GETS" ] && continue
    for g in $GETS; do
        if ! echo "$REQ" | grep -qx "$g" 2>/dev/null; then
            printf '%s\t%s\n' "$f" "$g" >> "$OUT_NOREQ"
        fi
    done
done < "$RUNTIME_LIST"

# --- 5. 자체 Singleton (SingletonMonoBehaviour<T> 미상속) ---
while IFS= read -r f; do
    [ -z "$f" ] && continue
    if grep -qE 'public static[[:space:]]+[A-Za-z_<>]+[[:space:]]+Instance' "$f" 2>/dev/null; then
        if ! grep -qE ':[[:space:]]*SingletonMonoBehaviour<' "$f" 2>/dev/null; then
            case "$f" in
                */SingletonMonoBehaviour.cs) ;;  # 베이스 클래스 자체는 제외
                *) echo "$f" >> "$OUT_SELFSG" ;;
            esac
        fi
    fi
done < "$RUNTIME_LIST"

# --- 6. Entities/ 안에서 .Instance 접근 (3-Layer 위반 후보) ---
while IFS= read -r f; do
    [ -z "$f" ] && continue
    case "$f" in
        */Entities/*)
            if grep -qE '\.Instance\b' "$f" 2>/dev/null; then
                echo "$f" >> "$OUT_MODEL"
            fi
            ;;
    esac
done < "$RUNTIME_LIST"

# Summary
kv_write "$OUT_KV" "hidden_dep_files"       "$(wc -l < "$OUT_HIDDEN" | tr -d ' ')"
kv_write "$OUT_KV" "awake_instance_hits"    "$(wc -l < "$OUT_AWAKE" | tr -d ' ')"
kv_write "$OUT_KV" "serialize_no_validate"  "$(wc -l < "$OUT_NOVAL" | tr -d ' ')"
kv_write "$OUT_KV" "getcomponent_no_require" "$(wc -l < "$OUT_NOREQ" | tr -d ' ')"
kv_write "$OUT_KV" "self_rolled_singleton"  "$(wc -l < "$OUT_SELFSG" | tr -d ' ')"
kv_write "$OUT_KV" "model_singleton_access" "$(wc -l < "$OUT_MODEL" | tr -d ' ')"

echo "di_safety: done" >&2
