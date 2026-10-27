# Plays every speakable take in takes-tts/ into Windows' own speech recogniser and compares what it
# heard with what the take was meant to say. A rough stand-in for listening: it says whether the words
# are recognisable, not whether the tone is right. Invented or archaic words (the Bronze Age sheet is
# Greek-sounding) will score low by design. Run from any folder:
#   powershell -ExecutionPolicy Bypass -File Tools\GuardVoice\check_intelligibility.ps1

$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot "takes-tts"
$rows = Import-Csv (Join-Path $root "tts-manifest.csv") -Encoding UTF8 | Where-Object { $_.voice -ne "" }
Add-Type -AssemblyName System.Speech

function Get-Words($text) {
    return @(($text.ToLowerInvariant() -replace "[^a-z' ]", " ") -split "\s+" | Where-Object { $_ -ne "" })
}

$engine = New-Object System.Speech.Recognition.SpeechRecognitionEngine((New-Object System.Globalization.CultureInfo("en-US")))
$engine.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))

$perAge = @{}
foreach ($row in $rows) {
    $age = ($row.file -split "_")[1]
    $engine.SetInputToWaveFile((Join-Path (Join-Path $root $age) $row.file))
    $heard = ""
    try {
        $result = $engine.Recognize()
        if ($null -ne $result) { $heard = $result.Text }
    }
    catch {
        Write-Warning "$($row.file): recogniser error: $($_.Exception.Message)"
    }

    $want = Get-Words $row.spoken
    $got = Get-Words $heard
    $hits = @($want | Where-Object { $got -contains $_ }).Count
    $score = if ($want.Count -gt 0) { $hits / $want.Count } else { 1 }
    if (-not $perAge.ContainsKey($age)) { $perAge[$age] = New-Object System.Collections.Generic.List[double] }
    $perAge[$age].Add($score)
    "{0,4:P0}  {1}  said '{2}'  heard '{3}'" -f $score, $row.file, $row.spoken, $heard
}

Write-Output ""
foreach ($age in @("bronze", "high", "late", "powder")) {
    if ($perAge.ContainsKey($age)) {
        $mean = ($perAge[$age] | Measure-Object -Average).Average
        Write-Output ("{0,-7} mean words recognised: {1:P0} over {2} clips" -f $age, $mean, $perAge[$age].Count)
    }
}
$engine.Dispose()
