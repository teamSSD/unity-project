#!/usr/bin/env bash
# 파일 리스트 생성 — 다른 모든 체크의 전제 조건
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

if [ ! -d "$SRC" ]; then
    echo "ERROR: $SRC not found" >&2
    exit 1
fi

generate_lists

echo "setup: runtime=$(wc -l < "$RUNTIME_LIST" | tr -d ' '), editor=$(wc -l < "$EDITOR_LIST" | tr -d ' '), test=$(wc -l < "$TEST_LIST" | tr -d ' ')" >&2
