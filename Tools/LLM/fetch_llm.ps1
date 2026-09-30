# Downloads the local language model the guard-mimic prototype uses, and llama.cpp's server that
# runs it, into Assets/StreamingAssets/LLM/ (shipped with the game as-is). No account needed.
#   - llama.cpp server, Windows CPU build (MIT): github.com/ggml-org/llama.cpp, release b11295
#   - Qwen2.5 0.5B Instruct, 4-bit GGUF (Apache-2.0): huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF
# The model (491 MB) is too big for GitHub, so it is not committed; this script puts it back.
# Files already present are skipped. Run from anywhere:
#   powershell -ExecutionPolicy Bypass -File Tools\LLM\fetch_llm.ps1
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dest = Join-Path $repo 'Assets\StreamingAssets\LLM'
New-Item -ItemType Directory -Force $dest | Out-Null

$release = 'b11295'
$zipName = "llama-$release-bin-win-cpu-x64.zip"
$zipUrl = "https://github.com/ggml-org/llama.cpp/releases/download/$release/$zipName"
$model = 'qwen2.5-0.5b-instruct-q4_k_m.gguf'
$modelUrl = "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/$model"

$server = Join-Path $dest 'llama-server.exe'
if (Test-Path $server) {
    Write-Host "llama-server.exe already present"
} else {
    $zip = Join-Path $env:TEMP $zipName
    Write-Host "Downloading $zipName (about 19 MB)..."
    Invoke-WebRequest -Uri $zipUrl -OutFile $zip -UseBasicParsing
    $unpacked = Join-Path $env:TEMP "llama-$release"
    if (Test-Path $unpacked) { Remove-Item -Recurse -Force $unpacked }
    Expand-Archive -Path $zip -DestinationPath $unpacked
    # The server and the libraries it loads; the other tools in the zip are not needed.
    Get-ChildItem $unpacked -Recurse -Include 'llama-server.exe', '*.dll' | Copy-Item -Destination $dest
    Write-Host "llama-server.exe and its DLLs copied"
}

$modelPath = Join-Path $dest $model
if (Test-Path $modelPath) {
    Write-Host "$model already present"
} else {
    Write-Host "Downloading $model (about 491 MB)..."
    Invoke-WebRequest -Uri $modelUrl -OutFile "$modelPath.part" -UseBasicParsing
    Move-Item "$modelPath.part" $modelPath
    Write-Host "$model downloaded"
}

Write-Host "OK: $dest"
Get-ChildItem $dest -File | Where-Object { $_.Extension -ne '.meta' } | ForEach-Object { '{0,10:N1} MB  {1}' -f ($_.Length / 1MB), $_.Name }
