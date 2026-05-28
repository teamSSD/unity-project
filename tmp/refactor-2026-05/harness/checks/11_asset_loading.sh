#!/usr/bin/env bash
# Asset Loading — Resources → Addressables 마이그레이션 추적 (ADR-007 게이트)
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HARNESS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
. "$HARNESS_DIR/lib/common.sh"
detect_root "${1:-}"

OUT="$DATA_DIR/asset_loading.kv"
: > "$OUT"

# Resources.Load 호출 (raw — 주석/dead-#if 포함, 참고용)
N_RES_LOAD_RAW=$(xargs0 "$RUNTIME_LIST" grep -hE 'Resources\.Load' 2>/dev/null | wc -l | tr -d ' ')

# Resources.Load 호출 (effective — 주석 + #if UNITY_EDITOR 블록 제외, 게이트 기준)
# awk: #if UNITY_EDITOR 블록 안은 skip, 주석(// 시작) skip, "Resources.Load" 포함 라인만 카운트.
N_RES_LOAD=$(xargs0 "$RUNTIME_LIST" awk '
    BEGIN { skip = 0 }
    /^[[:space:]]*#if[[:space:]]+UNITY_EDITOR/ { skip = 1; next }
    /^[[:space:]]*#endif/ { if (skip) { skip = 0; next } }
    skip { next }
    /^[[:space:]]*\/\// { next }
    /Resources\.Load/ { count++ }
    END { print count + 0 }
' 2>/dev/null | awk '{ s += $1 } END { print s + 0 }')

# Assets/Resources/ 폴더 파일 수 (.meta + TextMesh Pro/ 제외; TMP는 Unity 패키지 종속).
RES_DIR="$ROOT/Assets/Resources"
if [ -d "$RES_DIR" ]; then
    N_RES_FILES=$(find "$RES_DIR" -type f -not -name "*.meta" -not -path "*/TextMesh Pro/*" 2>/dev/null | wc -l | tr -d ' ')
else
    N_RES_FILES=0
fi

# AssetReference 필드 선언 (AssetReference, AssetReferenceGameObject, AssetReferenceT 등)
N_ASSETREF=$(xargs0 "$RUNTIME_LIST" grep -hE 'AssetReference([A-Za-z]+)?' 2>/dev/null | wc -l | tr -d ' ')

# LoadAssetAsync 호출
N_LOADASYNC=$(xargs0 "$RUNTIME_LIST" grep -hE 'LoadAssetAsync' 2>/dev/null | wc -l | tr -d ' ')

# InstantiateAsync 호출 (Addressables 또는 AssetReference)
N_INSTASYNC=$(xargs0 "$RUNTIME_LIST" grep -hE 'InstantiateAsync' 2>/dev/null | wc -l | tr -d ' ')

# Addressables using 파일 수
N_ADDR_USING=$(xargs0 "$RUNTIME_LIST" grep -lE '^using[[:space:]]+UnityEngine\.AddressableAssets' 2>/dev/null | wc -l | tr -d ' ')

kv_write "$OUT" "resources_load_calls"     "$N_RES_LOAD"
kv_write "$OUT" "resources_load_calls_raw" "$N_RES_LOAD_RAW"
kv_write "$OUT" "resources_folder_files"   "$N_RES_FILES"
kv_write "$OUT" "asset_reference_decls"    "$N_ASSETREF"
kv_write "$OUT" "load_asset_async_calls"   "$N_LOADASYNC"
kv_write "$OUT" "instantiate_async_calls"  "$N_INSTASYNC"
kv_write "$OUT" "addressables_using_files" "$N_ADDR_USING"

echo "asset_loading: Resources.Load(effective)=$N_RES_LOAD raw=$N_RES_LOAD_RAW, Resources/files=$N_RES_FILES, AssetReference=$N_ASSETREF" >&2
