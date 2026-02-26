$schemasPath = "./core/models"
$flatcPath   = "./tools/flatc"
$tsOutput    = "./frontend/edgars-face/generated"
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

Get-ChildItem -Path $schemasPath -Filter "*.fbs" | ForEach-Object {
    $name = $_.BaseName

    Write-Host "[$name] " -ForegroundColor Yellow -NoNewline
    Write-Host "Compiling TypeScript..."
    & $flatcPath --ts --ts-no-import-ext -o $tsOutput $_.FullName

    Write-Host "[$name] " -ForegroundColor Yellow -NoNewline
    Write-Host "Compiling Python..."
    & $flatcPath --python -o $pyOutput $_.FullName
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
