#!/usr/bin/env bash
# 이벤트 구독 누수 후보 — += 는 있는데 같은 핸들러 -= 가 없는 파일
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/event_leaks.tsv"
: > "$OUT"

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

while IFS= read -r f; do
    [ -z "$f" ] && continue
    # 좌측에 '.' 강제 (instance.Event 패턴만) — 산술 연산 (timer += Time.deltaTime, total += amount) 오탐 제거
    SUBS=$(grep -oE '[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_.]*[[:space:]]*\+=[[:space:]]*[A-Za-z_][A-Za-z0-9_.]*' "$f" 2>/dev/null \
        | awk '{ sub(/[ \t]*\+=[ \t]*/, "|"); print }' | sort -u)
    [ -z "$SUBS" ] && continue
    echo "$SUBS" | while IFS='|' read -r evt handler; do
        [ -z "$evt" ] && continue
        if ! grep -qE "${evt}[[:space:]]*-=[[:space:]]*${handler}" "$f" 2>/dev/null; then
            echo "1"
        fi
    done | wc -l | tr -d ' ' > "$WORK/cnt"
    LEAKED=$(cat "$WORK/cnt")
    if [ "$LEAKED" -gt 0 ]; then
        printf '%s\t%s\n' "$LEAKED" "$f" >> "$OUT"
    fi
done < "$RUNTIME_LIST"

N=$(wc -l < "$OUT" | tr -d ' ')
KV="$DATA_DIR/event_leaks.kv"
: > "$KV"
kv_write "$KV" "event_leaks_files" "$N"

echo "event_leaks: $N files" >&2
