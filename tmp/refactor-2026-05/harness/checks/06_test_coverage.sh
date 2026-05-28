#!/usr/bin/env bash
# 테스트 커버리지 프록시 — 런타임 클래스명이 테스트 파일에 등장하는 비율
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT_KV="$DATA_DIR/test_coverage.kv"
OUT_COV="$DATA_DIR/covered_classes.list"
OUT_CLASSES="$DATA_DIR/all_classes.list"
: > "$OUT_KV"
: > "$OUT_COV"
: > "$OUT_CLASSES"

xargs0 "$RUNTIME_LIST" grep -hoE '(public|internal)?[[:space:]]*(static[[:space:]]+|sealed[[:space:]]+|abstract[[:space:]]+|partial[[:space:]]+)*class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*' 2>/dev/null \
    | awk '{print $NF}' \
    | grep -vE '^(for|if|while|switch|using|return|new|of|to)$' \
    | sort -u > "$OUT_CLASSES"

TOTAL=$(wc -l < "$OUT_CLASSES" | tr -d ' ')
COVERED=0
if [ -s "$TEST_LIST" ]; then
    while IFS= read -r cls; do
        [ -z "$cls" ] && continue
        if xargs0 "$TEST_LIST" grep -lE "\b${cls}\b" >/dev/null 2>&1; then
            COVERED=$((COVERED + 1))
            echo "$cls" >> "$OUT_COV"
        fi
    done < "$OUT_CLASSES"
fi
kv_write "$OUT_KV" "total_classes" "$TOTAL"
kv_write "$OUT_KV" "covered_classes" "$COVERED"

echo "test_coverage: $COVERED/$TOTAL" >&2
