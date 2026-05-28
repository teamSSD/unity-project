#!/usr/bin/env bash
# Shared helpers for refactor harness (Bash 3.2 호환, macOS BSD 도구 호환)

# 비-UTF8 바이트가 있는 일부 .cs 파일에서 awk가 죽는 것을 막기 위해 C 로케일 강제
export LC_ALL=C
export LANG=C

# HARNESS_DIR: harness/ 디렉토리의 절대경로 (이 파일의 부모)
HARNESS_DIR="${HARNESS_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
DATA_DIR="$(cd "$HARNESS_DIR/.." && pwd)/data"
mkdir -p "$DATA_DIR"

# 프로젝트 루트와 파일 리스트 경로를 설정.
# 첫 인자가 주어지면 그것을 ROOT로 사용, 아니면 환경변수 ROOT, 그것도 없으면 자동 감지.
detect_root() {
    if [ -n "${1:-}" ]; then
        ROOT="$1"
    elif [ -n "${ROOT:-}" ]; then
        :
    else
        # harness/ 의 세 단계 위가 프로젝트 루트 (tmp/refactor-NNNN/harness/)
        ROOT="$(cd "$HARNESS_DIR/../../.." && pwd)"
    fi
    SRC="$ROOT/Assets/Scripts"
    RUNTIME_LIST="$DATA_DIR/_runtime.list"
    EDITOR_LIST="$DATA_DIR/_editor.list"
    TEST_LIST="$DATA_DIR/_test.list"
    ALL_CS_LIST="$DATA_DIR/_all_cs.list"
}

# 파일 리스트 재생성 — 00_setup.sh 에서만 호출.
generate_lists() {
    find "$SRC" -name "*.cs" -not -path "*/Tests/*" -not -path "*/Editor/*" | sort > "$RUNTIME_LIST"
    { find "$SRC/Editor" -name "*.cs" 2>/dev/null || true; \
      find "$ROOT/Assets/Editor" -name "*.cs" 2>/dev/null || true; } | sort > "$EDITOR_LIST"
    { find "$ROOT/Assets/Tests" -name "*.cs" 2>/dev/null || true; \
      find "$ROOT/Assets/Scripts/Tests" -name "*.cs" 2>/dev/null || true; } | sort > "$TEST_LIST"
    find "$ROOT/Assets" -name "*.cs" | sort > "$ALL_CS_LIST"
}

# xargs0 LIST CMD... : 리스트 파일을 NUL 구분으로 변환해 xargs -0 에 전달.
# 공백 포함 경로(예: "My project/...")를 안전하게 처리.
xargs0() {
    list="$1"; shift
    tr '\n' '\0' < "$list" | xargs -0 "$@"
}

# 리스트의 모든 파일에 대해 wc -l 합산.
sum_lines() {
    list="$1"
    if [ ! -s "$list" ]; then echo "0"; return; fi
    total=0
    while IFS= read -r f; do
        [ -z "$f" ] && continue
        n=$(wc -l < "$f" 2>/dev/null | tr -d ' ')
        [ -z "$n" ] && n=0
        total=$((total + n))
    done < "$list"
    echo "$total"
}

# 키-값 파일에 한 줄 추가 (탭 구분).
kv_write() {
    file="$1"; key="$2"; value="$3"
    printf '%s\t%s\n' "$key" "$value" >> "$file"
}

# 키-값 파일에서 값 읽기. 없으면 빈 문자열 출력.
kv_read() {
    file="$1"; key="$2"
    [ -f "$file" ] || return 0
    awk -F'\t' -v k="$key" '$1==k { print $2; exit }' "$file"
}
