# Makes a stand-in base set of guard voice lines with Windows' built-in text-to-speech, the same
# approach Tools/VoiceFixtures/generate.ps1 uses for the spell words. One WAV per line of
# record-lines.json, named exactly like a human take (vo_<age>_base_<situation>_<NN>.wav), so a real
# recording dropped in later simply replaces it. They sound like a computer: they exist so every guard
# situation has contextually right speech from day one. Run from any folder:
#   powershell -ExecutionPolicy Bypass -File Tools\GuardVoice\make_tts_base.ps1
#
# Text-to-speech cannot snore, so asleep lines are made by make_snores.py, which this script runs.

param(
    [string[]]$Ages = @("bronze", "high", "late", "powder")
)

$ErrorActionPreference = "Stop"
$outRoot = Join-Path $PSScriptRoot "takes-tts"
$lines = Get-Content (Join-Path $PSScriptRoot "record-lines.json") -Raw -Encoding UTF8 | ConvertFrom-Json
Add-Type -AssemblyName System.Speech

# 22.05 kHz: the two desktop voices are not better than this, and the game resamples anyway.
$format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050,
    [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
    [System.Speech.AudioFormat.AudioChannel]::Mono)

# Alternating voices gives variety; the game's per-guard pitch and timbre disguise does the rest.
$voices = @("Microsoft David Desktop", "Microsoft Zira Desktop")

# Delivery per situation, using the tone table the recording script gives a human performer. SAPI's
# prosody keywords are coarse, so "shout" is approximated: high, fast, as loud as it goes, stressed.
$tones = @{
    murmur  = @{ Rate = "slow";   Pitch = "low";    Volume = "soft";   Stress = $false }
    alert   = @{ Rate = "medium"; Pitch = "medium"; Volume = "medium"; Stress = $false }
    chase   = @{ Rate = "fast";   Pitch = "high";   Volume = "loud";   Stress = $true }
    search  = @{ Rate = "slow";   Pitch = "low";    Volume = "medium"; Stress = $false }
    lost    = @{ Rate = "slow";   Pitch = "low";    Volume = "soft";   Stress = $false }
    attack  = @{ Rate = "fast";   Pitch = "high";   Volume = "loud";   Stress = $true }
    hurt    = @{ Rate = "fast";   Pitch = "high";   Volume = "medium"; Stress = $true }
    death   = @{ Rate = "slow";   Pitch = "medium"; Volume = "medium"; Stress = $false }
    grabbed = @{ Rate = "fast";   Pitch = "high";   Volume = "medium"; Stress = $true }
    thrown  = @{ Rate = "slow";   Pitch = "x-high"; Volume = "loud";   Stress = $true }
}

# What to say for a line that has no words (or only a scream) in the script.
function Get-Spoken($line) {
    $text = ([string]$line.text).Trim()
    $direction = ([string]$line.direction).ToLowerInvariant()
    if ($text -match '^A{4,}H?') { return @{ Text = "Aaaaaaaaah!"; Treatment = "scream spoken as a long Aaah" } }
    if ($text.Length -gt 0) { return @{ Text = $text; Treatment = "spoken as written" } }
    if ($direction -match 'hum')      { return @{ Text = "Hmm, hm hm hmm. Hm hm hm, hmm."; Treatment = "hum spoken as hmm" } }
    if ($direction -match 'grunt')    { return @{ Text = "Unngh!"; Treatment = "grunt spoken" } }
    if ($direction -match 'muffled')  { return @{ Text = "Mmmph! Mm, mm mmph!"; Treatment = "muffled protest spoken as mmph" } }
    if ($direction -match 'snore')    { return $null }
    throw "No way to speak line $($line.file) (text empty, direction '$direction'): add a case to Get-Spoken."
}

function ConvertTo-Ssml($spoken, $tone) {
    # An em dash is a cut-off in the script; as speech it reads better as a pause.
    $clean = [System.Security.SecurityElement]::Escape(($spoken -replace '[—–]', ', '))
    if ($tone.Stress) { $clean = "<emphasis level=`"strong`">$clean</emphasis>" }
    return "<speak version=`"1.0`" xml:lang=`"en-US`" xmlns=`"http://www.w3.org/2001/10/synthesis`">" +
           "<prosody rate=`"$($tone.Rate)`" pitch=`"$($tone.Pitch)`" volume=`"$($tone.Volume)`">$clean</prosody></speak>"
}

$report = New-Object System.Collections.Generic.List[object]
foreach ($age in $lines.ages) {
    if ($Ages -notcontains $age.id) { continue }
    $dir = Join-Path $outRoot $age.id
    New-Item -ItemType Directory -Force $dir | Out-Null
    foreach ($line in $age.lines) {
        $spoken = Get-Spoken $line
        if ($null -eq $spoken) {
            $report.Add([pscustomobject]@{ file = $line.file; voice = ""; spoken = ""; treatment = "snore: made by make_snores.py" })
            continue
        }

        $tone = $tones[[string]$line.situation]
        if ($null -eq $tone) { throw "No tone for situation '$($line.situation)' ($($line.file))." }
        # A line marked quiet in the script stays soft even in a louder situation ("quietly" alerts).
        if ($line.loudness -eq "quiet") { $tone = @{ Rate = $tone.Rate; Pitch = $tone.Pitch; Volume = "soft"; Stress = $false } }

        $voice = $voices[1 - (([int]$line.number) % 2)]
        $path = Join-Path $dir $line.file
        $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
        try {
            $synth.SelectVoice($voice)
            $synth.SetOutputToWaveFile($path, $format)
            $synth.SpeakSsml((ConvertTo-Ssml $spoken.Text $tone))
        }
        finally {
            $synth.Dispose()
        }
        $report.Add([pscustomobject]@{ file = $line.file; voice = $voice; spoken = $spoken.Text; treatment = $spoken.Treatment })
    }
}

$report | Export-Csv (Join-Path $outRoot "tts-manifest.csv") -NoTypeInformation -Encoding UTF8
Write-Output "Wrote $(($report | Where-Object { $_.voice -ne '' }).Count) text-to-speech clips to $outRoot"

python (Join-Path $PSScriptRoot "make_snores.py")
if ($LASTEXITCODE -ne 0) { throw "make_snores.py failed with exit code $LASTEXITCODE" }
