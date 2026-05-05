#!/bin/bash

BRANCH=$(git branch --show-current 2>/dev/null)

case "$BRANCH" in
  feat/*|fix/*|chore/*) ;;
  *) exit 0 ;;
esac

command -v gh >/dev/null 2>&1 || exit 0

MERGED_PR=$(gh pr list --head "$BRANCH" --state merged --json number --jq '.[0].number' 2>/dev/null)

if [ -n "$MERGED_PR" ]; then
  echo "BLOCKED: $BRANCH was already merged (PR #$MERGED_PR)." >&2
  echo "Start a new branch before editing." >&2
  exit 2
fi

exit 0
