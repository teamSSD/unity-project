#!/bin/bash
# WebGL 빌드 산출물을 gh-pages 브랜치에 push해서 GitHub Pages 배포.
# 사전 조건: Unity Editor에서 Tools/Build/WebGL — Release 실행 완료.
#
# 사용:
#   ./scripts/deploy-gh-pages.sh                    # 최신 빌드 자동 선택
#   ./scripts/deploy-gh-pages.sh <build_folder>     # 특정 빌드 경로 지정

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

# ── 1. 빌드 소스 결정
if [ $# -ge 1 ]; then
  BUILD_SRC="$1"
else
  BUILD_SRC="$(ls -td Builds/WebGL/*/*/ 2>/dev/null | head -1 || true)"
fi

if [ -z "${BUILD_SRC:-}" ] || [ ! -d "$BUILD_SRC" ]; then
  echo "❌ 빌드 폴더 없음. Unity에서 Tools/Build/WebGL — Release 먼저 실행하거나 경로 인자 전달."
  exit 1
fi

# WebGL 산출물이 실제로 있는지 검증 (index.html + Build/ 하위)
if [ ! -f "$BUILD_SRC/index.html" ] || [ ! -d "$BUILD_SRC/Build" ]; then
  echo "❌ WebGL 산출물이 아님: $BUILD_SRC (index.html 또는 Build/ 없음)"
  exit 1
fi

BUILD_LABEL="$(basename $(dirname "$BUILD_SRC"))/$(basename "$BUILD_SRC")"
echo "📦 배포 소스: $BUILD_SRC"
echo "🏷  라벨: $BUILD_LABEL"

# ── 2. 현재 상태 안전 검증 (uncommitted 변경 있으면 warning)
if ! git diff-index --quiet HEAD --; then
  echo "⚠️  현재 브랜치에 uncommitted 변경 있음. (배포엔 영향 없음)"
fi

# ── 3. worktree로 gh-pages 별도 checkout — 현재 브랜치 안 건드림
WORKTREE_DIR="$REPO_ROOT/.gh-pages-worktree"
if [ -d "$WORKTREE_DIR" ]; then
  echo "🧹 기존 worktree 정리"
  git worktree remove --force "$WORKTREE_DIR" 2>/dev/null || rm -rf "$WORKTREE_DIR"
fi

if git show-ref --verify --quiet refs/heads/gh-pages || \
   git ls-remote --heads origin gh-pages | grep -q gh-pages; then
  # 기존 gh-pages 있으면 fetch 후 worktree
  git fetch origin gh-pages 2>/dev/null || true
  if git show-ref --verify --quiet refs/heads/gh-pages; then
    git worktree add "$WORKTREE_DIR" gh-pages
  else
    git worktree add -b gh-pages "$WORKTREE_DIR" origin/gh-pages
  fi
  echo "🌿 기존 gh-pages 브랜치 사용"
else
  # 최초 배포: orphan 브랜치 생성
  git worktree add --detach "$WORKTREE_DIR"
  (
    cd "$WORKTREE_DIR"
    git checkout --orphan gh-pages
    git rm -rf . 2>/dev/null || true
  )
  echo "🌱 gh-pages orphan 브랜치 신규 생성"
fi

# ── 4. worktree 내용 갱신
cd "$WORKTREE_DIR"
# 기존 파일 전부 지움 (구 빌드 잔재 제거). .git은 자동 유지.
find . -mindepth 1 -maxdepth 1 ! -name '.git' -exec rm -rf {} +

# 새 빌드 복사 (dot 파일까지)
cp -R "$BUILD_SRC"/. .

# ── 5. GH Pages 필수 파일
# .nojekyll — Jekyll 처리 방지 (`_`로 시작하는 파일 무시 회피)
touch .nojekyll

# ── 6. 커밋 + push
git add -A
if git diff --cached --quiet; then
  echo "ℹ️  변경 없음. 배포 skip."
else
  git commit -m "deploy: $BUILD_LABEL"
  git push origin gh-pages
  echo "✅ Push 완료 (origin/gh-pages)"
fi

# ── 7. worktree 정리
cd "$REPO_ROOT"
git worktree remove --force "$WORKTREE_DIR"

# ── 8. 안내
REPO_URL="$(git remote get-url origin | sed -E 's|.*github\.com[:/](.+)\.git$|\1|')"
PAGES_URL="https://$(echo "$REPO_URL" | cut -d/ -f1).github.io/$(echo "$REPO_URL" | cut -d/ -f2)/"

cat <<EOF

✅ 배포 완료.

🌐 예상 URL: $PAGES_URL

⚙️  최초 배포라면:
   GitHub → Settings → Pages → Source: "Deploy from a branch"
                            → Branch: gh-pages / (root)
   저장 후 1-2분 뒤 위 URL 활성화됨.

📌 재배포 시 URL 그대로. 브라우저 hard-refresh (Cmd+Shift+R) 권장 (구 캐시 방어).
EOF
