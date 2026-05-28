#!/usr/bin/env bash
# 2a_move.sh — phase2a_mapping.tsv 기반 git mv 실행
# 사용:
#   bash 2a_move.sh                  # 전체 이동
#   bash 2a_move.sh "Schema"         # asmdef prefix 필터 (Schema/Config, Schema/State, Schema/Interfaces)
#   bash 2a_move.sh "Schema/Config"  # 더 좁은 필터
#   bash 2a_move.sh --dry-run        # 실행 없이 미리보기
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
MAPPING="$SCRIPT_DIR/../plan/phase2a_mapping.tsv"

DRY=0
FILTER=""
for arg in "$@"; do
    case "$arg" in
        --dry-run) DRY=1 ;;
        *) FILTER="$arg" ;;
    esac
done

[ ! -f "$MAPPING" ] && { echo "ERROR: $MAPPING not found" >&2; exit 1; }

moved=0
skipped=0
errors=0

while IFS=$'\t' read -r src dst asmdef feature note; do
    [ -z "$src" ] && continue
    case "$src" in '#'*) continue ;; esac

    # asmdef 필터
    if [ -n "$FILTER" ]; then
        case "$asmdef" in
            "$FILTER"*) ;;
            *) continue ;;
        esac
    fi

    # 미분류 스킵
    [ "$dst" = "?" ] && { echo "SKIP (unclassified): $src" >&2; skipped=$((skipped + 1)); continue; }

    # 원본 존재 확인
    if [ ! -f "$ROOT/$src" ]; then
        echo "SKIP (not found): $src" >&2
        skipped=$((skipped + 1))
        continue
    fi

    # 이미 같은 위치
    if [ "$src" = "$dst" ]; then
        skipped=$((skipped + 1))
        continue
    fi

    if [ "$DRY" = "1" ]; then
        printf "DRY: %s\n     -> %s\n" "$src" "$dst" >&2
        moved=$((moved + 1))
        continue
    fi

    # 대상 폴더 생성
    mkdir -p "$ROOT/$(dirname "$dst")"

    # git mv (.cs + .meta)
    if git -C "$ROOT" mv "$src" "$dst" 2>/dev/null; then
        if [ -f "$ROOT/$src.meta" ]; then
            git -C "$ROOT" mv "$src.meta" "$dst.meta" 2>/dev/null || true
        fi
        moved=$((moved + 1))
    else
        echo "ERROR: git mv failed: $src -> $dst" >&2
        errors=$((errors + 1))
    fi
done < "$MAPPING"

echo "" >&2
if [ "$DRY" = "1" ]; then
    echo "DRY RUN: would move=$moved, skipped=$skipped" >&2
else
    echo "Done: moved=$moved, skipped=$skipped, errors=$errors" >&2
fi
