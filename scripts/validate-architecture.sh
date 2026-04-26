#!/bin/bash
#
# validate-architecture.sh
# Validates architecture boundaries via JustGo architecture tests.
# Runs `dotnet test tests/JustGo.ArchitectureTests` and blocks deployment
# if any module boundary violations are detected. Designed for PostToolUse hooks.
#

set -e

echo "🔍 Running architecture boundary tests..."

if dotnet test tests/JustGo.ArchitectureTests --no-build --verbosity minimal; then
    echo "✅ Architecture tests passed"
    exit 0
else
    exit_code=$?
    echo "❌ Architecture tests FAILED" >&2
    
    # Return blocking error to agent
    cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "PostToolUse",
    "decision": "block",
    "reason": "Architecture boundary violations detected. Run 'dotnet test tests/JustGo.ArchitectureTests' to review."
  }
}
EOF
    
    exit 2  # Blocking error
fi
