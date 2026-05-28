#!/usr/bin/env bash
# 모든 체크 순차 실행 후 리포트 생성. 개별 체크는 checks/NN_*.sh 로 단독 실행 가능.
set -eu
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_ARG="${1:-}"

# 00_setup은 항상 먼저 (파일 리스트 생성)
bash "$SCRIPT_DIR/checks/00_setup.sh" "$ROOT_ARG"

# 나머지 체크 — 순서 무관
for s in "$SCRIPT_DIR"/checks/[0-9]*.sh; do
    case "$(basename "$s")" in
        00_*) continue ;;
    esac
    bash "$s" "$ROOT_ARG"
done

bash "$SCRIPT_DIR/render_report.sh" "$ROOT_ARG"
