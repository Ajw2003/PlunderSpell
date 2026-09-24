# Regenerates the synthetic voice clips SpeechRecognitionTests runs through the real Vosk model.
#
# Every spell word, said correctly (OK_) and mispronounced (MF_), in both of Windows' built-in
# English voices, as 16 kHz 16-bit mono WAV - the format VoskVoiceInputService feeds the model.
# These are a regression floor, not a substitute for a real person at a microphone: text-to-speech
# pronounces Latin its own way. Run from any folder:
#   powershell -ExecutionPolicy Bypass -File Tools\VoiceFixtures\generate.ps1

$outDir = Join-Path $PSScriptRoot "clips"
New-Item -ItemType Directory -Force $outDir | Out-Null
Add-Type -AssemblyName System.Speech

$format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(16000,
    [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
    [System.Speech.AudioFormat.AudioChannel]::Mono)

$clips = [ordered]@{
    "OK_IGNIS" = "Ignis";                 "MF_IGNIS" = "Agnis"
    "OK_FRANGO" = "Frango";               "MF_FRANGO" = "Franco"
    "OK_LEVO" = "Levo";                   "MF_LEVO" = "Lavo"
    "OK_AURUMVOCO" = "Aurum voco";        "MF_AURUMVOCO" = "Aurum boco"
    "OK_TONITRUS" = "Tonitrus";           "MF_TONITRUS" = "Tonitus"
    "OK_SOMNUS" = "Somnus";               "MF_SOMNUS" = "Sonus"
    "OK_CADAVERSURGE" = "Cadaver surge";  "MF_CADAVERSURGE" = "Cadver surge"
    "OK_PORTA" = "Porta";                 "MF_PORTA" = "Prota"
}

foreach ($voice in @("David", "Zira")) {
    foreach ($name in $clips.Keys) {
        $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
        $synth.SelectVoice("Microsoft $voice Desktop")
        $synth.Rate = -1
        $synth.SetOutputToWaveFile((Join-Path $outDir "$($name)_$voice.wav"), $format)
        $synth.Speak($clips[$name])
        $synth.Dispose()
    }
}

Write-Output "Wrote $((Get-ChildItem $outDir -Filter *.wav).Count) clips to $outDir"
