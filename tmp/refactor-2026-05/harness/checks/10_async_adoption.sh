#!/usr/bin/env bash
# Async Adoption — UniTask 마이그레이션 추적 (ADR-005 게이트)
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/async_adoption.kv"
: > "$OUT"

# IEnumerator coroutine method declarations (generic IEnumerator<T>은 컬렉션이므로 제외)
N_IENUM=$(xargs0 "$RUNTIME_LIST" grep -hE '(^|[^A-Za-z_])IEnumerator[[:space:]]+[A-Za-z_][A-Za-z0-9_]*[[:space:]]*\(' 2>/dev/null \
    | grep -vE 'IEnumerator<' \
    | wc -l | tr -d ' ')

# yield return / yield break 문 (yield return null은 antipatterns에 있음)
N_YIELD=$(xargs0 "$RUNTIME_LIST" grep -hE '^[[:space:]]*yield[[:space:]]+(return|break)' 2>/dev/null \
    | wc -l | tr -d ' ')

# StartCoroutine 호출
N_STARTCO=$(xargs0 "$RUNTIME_LIST" grep -hE 'StartCoroutine[[:space:]]*\(' 2>/dev/null \
    | wc -l | tr -d ' ')

# UniTask methods (async UniTask, async UniTaskVoid, async UniTask<T>)
N_UNITASK_METHOD=$(xargs0 "$RUNTIME_LIST" grep -hE 'async[[:space:]]+UniTask' 2>/dev/null \
    | wc -l | tr -d ' ')

# await 사용
N_AWAIT=$(xargs0 "$RUNTIME_LIST" grep -hE '\bawait[[:space:]]+' 2>/dev/null \
    | wc -l | tr -d ' ')

# UniTask using 파일
N_UNITASK_USING=$(xargs0 "$RUNTIME_LIST" grep -lE '^using[[:space:]]+Cysharp\.Threading\.Tasks' 2>/dev/null \
    | wc -l | tr -d ' ')

kv_write "$OUT" "ienumerator_methods"   "$N_IENUM"
kv_write "$OUT" "yield_statements"      "$N_YIELD"
kv_write "$OUT" "start_coroutine_calls" "$N_STARTCO"
kv_write "$OUT" "unitask_methods"       "$N_UNITASK_METHOD"
kv_write "$OUT" "await_statements"      "$N_AWAIT"
kv_write "$OUT" "unitask_using_files"   "$N_UNITASK_USING"

echo "async_adoption: IEnumerator=$N_IENUM, yield=$N_YIELD, StartCoroutine=$N_STARTCO, UniTask=$N_UNITASK_METHOD" >&2
