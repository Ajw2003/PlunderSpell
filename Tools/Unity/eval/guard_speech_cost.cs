var snore = UnityEngine.Resources.Load<UnityEngine.AudioClip>("GuardVoice/powder/vo_powder_base_asleep_01");
var shout = UnityEngine.Resources.Load<UnityEngine.AudioClip>("GuardVoice/powder/vo_powder_base_chase_01");
if (snore == null || shout == null) return "clips not found";
var profile = Plunderspell.Audio.GuardVoiceProfiles.For(1234, "knight");
string report = "";
foreach (var clip in new[] { shout, snore })
{
    var data = new float[clip.samples * clip.channels];
    clip.GetData(data, 0);
    Plunderspell.Audio.VoiceBank.VoiceDisguise.Render(data, clip.frequency, profile); // warm up the JIT
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Plunderspell.Audio.VoiceBank.VoiceDisguise.Render(data, clip.frequency, profile);
    sw.Stop();
    report += clip.name + " (" + clip.length.ToString("0.0") + " s): render " + sw.Elapsed.TotalMilliseconds.ToString("0.0") + " ms; ";
}
var renderer = new Plunderspell.Audio.GuardSpeechRenderer();
var t = System.Diagnostics.Stopwatch.StartNew();
UnityEngine.AudioClip ready;
bool got = renderer.TryGet(snore, profile, "probe/cost", out ready);
t.Stop();
report += "TryGet on the main thread for the snore: " + t.Elapsed.TotalMilliseconds.ToString("0.00") + " ms (ready immediately: " + got + ")";
return report;
