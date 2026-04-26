#!/bin/sh
git config core.hooksPath .githooks
for target in .claudeignore .copilotignore .cursorignore; do
  cp .aiignore "$target"
done
echo "Done. Git hooks active. AI ignore files generated."
