#!/bin/bash
cd "$CLAUDE_PROJECT_DIR/.claude/hooks"
cat | node --import tsx skill-activation-prompt.ts || exit 0
