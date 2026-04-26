git config core.hooksPath .githooks
foreach ($target in @(".claudeignore", ".copilotignore", ".cursorignore")) {
    Copy-Item .aiignore $target -Force
}
Write-Host "Done. Git hooks active. AI ignore files generated."
