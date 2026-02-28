$schemasPath = "./core/models"
$flatcPath   = "./tools/flatc"
$tsOutput    = "./frontend/edgars-face/src/generated"
$pyOutput    = "./backend/api/generated"

Write-Host "=== FlatBuffers Compilation ===" -ForegroundColor Cyan
Write-Host ""

# Clean output folders
foreach ($outputPath in @($tsOutput, $pyOutput) | Select-Object -Unique) {
    if (Test-Path $outputPath) {
        Write-Host "Cleaning: $outputPath" -ForegroundColor Magenta
        Remove-Item -Path "$outputPath/*" -Recurse -Force
    } else {
        Write-Host "Creating: $outputPath" -ForegroundColor Magenta
        New-Item -Path $outputPath -ItemType Directory -Force | Out-Null
    }
}

Write-Host ""

# Ensure Python output is recognized as a package
$initFile = Join-Path $pyOutput "__init__.py"
$initContent = "# Auto-generated package marker. Do not edit manually.`n# This file is recreated by the FlatBuffers build script to ensure Python treats this directory as a package.`n"
Set-Content -Path $initFile -Value $initContent -NoNewline
Write-Host "Created: $initFile" -ForegroundColor Magenta

Get-ChildItem -Path $schemasPath -Filter "*.fbs" | ForEach-Object {
    $name = $_.BaseName

    Write-Host "[$name] " -ForegroundColor Yellow -NoNewline
    Write-Host "Compiling TypeScript..."
    & $flatcPath --ts --ts-no-import-ext --gen-object-api -o $tsOutput $_.FullName

    Write-Host "[$name] " -ForegroundColor Yellow -NoNewline
    Write-Host "Compiling Python..."
    & $flatcPath --python --gen-object-api -o $pyOutput $_.FullName
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
