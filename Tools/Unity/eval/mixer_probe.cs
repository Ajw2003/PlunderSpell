var bank = UnityEngine.Resources.FindObjectsOfTypeAll<Plunderspell.Audio.SoundBank>()[0];
var m = bank.Mixer;
float a, b, c, d; m.GetFloat("MasterVolume", out a); m.GetFloat("MusicVolume", out b); m.GetFloat("SfxVolume", out c); m.GetFloat("UiVolume", out d);
int playing = 0; string names = "";
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
    if (s.isPlaying) { playing++; if (names.Length < 120) names += (s.clip != null ? s.clip.name : "?") + ";"; }
float sm = UnityEngine.PlayerPrefs.GetFloat("Settings.MasterVolume", 1f), sx = UnityEngine.PlayerPrefs.GetFloat("Settings.MusicVolume", 1f), sf = UnityEngine.PlayerPrefs.GetFloat("Settings.SfxVolume", 1f);
return "mixer dB master=" + a.ToString("0.00") + " music=" + b.ToString("0.00") + " sfx=" + c.ToString("0.00") + " ui=" + d.ToString("0.00")
  + " | expected dB master=" + Plunderspell.Audio.AudioLevels.LinearToDb(sm).ToString("0.00") + " music=" + Plunderspell.Audio.AudioLevels.LinearToDb(sx).ToString("0.00") + " sfx=" + Plunderspell.Audio.AudioLevels.LinearToDb(sf).ToString("0.00")
  + " | listener=" + UnityEngine.AudioListener.volume + " | sourcesPlaying=" + playing + " " + names;
