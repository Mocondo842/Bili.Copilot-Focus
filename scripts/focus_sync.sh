#!/usr/bin/env bash
# 一键同步：把上游更新（经 fork 的 master）合进 focus/derec，守卫通过才推送。
#
# 前置：先在 GitHub 仓库页面点一次「Sync fork」，让 fork 的 master 对齐上游
#       （fork 无法通过 SSH 直接拉上游：deploy key 只授权本仓库）。
# 之后运行本脚本即可；任一步失败即中止，不会推送。
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== 1/5 拉取 fork =="
git fetch origin

echo "== 2/5 快进 master（只允许 ff，不允许在 master 上产生提交）=="
git checkout master
git merge --ff-only origin/master

echo "== 3/5 把 master 合进 focus/derec =="
git checkout focus/derec
git merge --no-edit master

echo "== 4/5 锚点守卫（缺任一锚点即失败，不推送）=="
bash scripts/fork_check.sh

echo "== 5/5 推送 =="
git push origin focus/derec
echo "同步完成：$(git log --oneline -1)"
