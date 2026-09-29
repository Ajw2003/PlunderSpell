# Audio tooling: what to download to make and review sounds and music

Researched 2026-09-29 at the owner's request: find skills, plugins or servers that would make the
sound-effect and music work more tailored, and the review of it better. **The current menu music is
out of scope: the owner calls it perfect.** Nothing below was installed; this is what was found and
what fits.

## What is true on this machine now

- No `ELEVENLABS_API_KEY` is set. `Tools/AudioForge/forge/ai_elevenlabs.py` already calls
  ElevenLabs' `/v1/sound-generation`, but the real endpoint has never been called (its README says
  so).
- `python` here lacks `soundfile`, `pyloudnorm` and `librosa`, so AudioForge's `build` and `audit`
  cannot run on this Windows machine yet. `numpy` and `scipy` are present. `ffmpeg`, `node`, `npx`
  and `uvx` are installed.
- GPU: NVIDIA GeForce RTX 5070, 12 GB. Local text-to-audio models can run here.
- The plugin catalog and the skill catalog turned up nothing built for game sound design.

## What fits, best first

| Option | What it gives | Fit | Cost and terms |
|---|---|---|---|
| **ElevenLabs skills** (`elevenlabs/skills`: `sound-effects`, `music`, `voice-isolator`, `setup-api-key`; MIT; install `npx skills add elevenlabs/skills`) | Claude knows the API's parameters and prompt style. Sound effects 0.5–22 s, a `loop` flag for a seamless loop, `prompt_influence` | **Best.** It is the source AudioForge already targets for the 38 AI-sourced sounds, and 93 sounds in the manifest are loops | Needs an API key. Commercial licence for sound effects and music starts at the Starter plan, US$6 a month (pricing page). The music docs say "cleared for ... gaming"; the plan terms page read did not restate that, so the Eleven Music model-specific terms were not seen |
| **ElevenLabs MCP server** (`uvx elevenlabs-mcp`, MIT, needs `ELEVENLABS_API_KEY`) | Generates and saves files from a chat: speech, voice design, audio isolation. The tool list for sound effects and music was not confirmed from the page | Good for quick tries by ear | Same key |
| **Mirelo MCP** (hosted, `mirelo.ai/mcp`) | Sound effects from text or from video, **extend a clip, and replace one region of a clip** | Good for tailoring: fix the last 300 ms of a take instead of rerolling it. Music is not exposed | Install: `claude mcp add --transport http mirelo https://mcp.mirelo.ai/mcp`, then `/mcp` and log in. Plans from free (5,000 credits) to Creator €20 and Studio €99 a month. Commercial-use terms and clip length are not on its MCP or pricing pages; ask them before shipping anything from it |
| **Ludo MCP** (`claude mcp add ludo https://mcp.ludo.ai/mcp`) | Music, sound effects and voices, MP3 only | Lower: a generalist, MP3 only, licence not stated | Pro plan $50 a month plus credits; 60-second timeout |
| **Stable Audio Open** (local, Hugging Face) | Up to 47 s of sound effects and field recordings on the RTX 5070, no per-file cost | Good for bulk foley variants | Free under US$1M annual revenue; a commercial licence is needed above that. Weaker at music |

Not useful: the `game-audio` skill in `opusgamelabs/game-creator` (Web Audio for browser games,
not Unity); `claude-music` (Apple Music remote control); `Music Catalog Diligence` (finance);
`transcribe-so`, `QkConvert`. The `mcpmarket.com` skills "Sound Effects Expert" and "Game Audio
Design" were opened: the first (by `willsigmon`, 7 stars) is a wrapper over the same ElevenLabs API
plus Meta AudioCraft with no Unity content, so `elevenlabs/skills` covers it; the second (by
`claudiodearaujo`) is advice on layering and adaptive music with no generating or reviewing tool
and no Unity, FMOD or Wwise integration. Both are third-party and unaudited.

## Two things in our own files to know

1. **`mus_title_loop` is marked `final = M` (AI music) in `Tools/AudioForge/manifest.csv`**, so the
   plan would replace the menu music the owner likes (`MusicDirector` plays `mus_title_loop` on the
main menu, `MusicDirector.cs:69`, so this is the track heard there). It is currently synthesised
   (`music:theme variant=title`, "The Herald's Overture"). Keep it: change its `final` to the
   synthesis being final (as the `G` rows are) in `manifest_source.py` and regenerate the manifest.
2. **`ai_elevenlabs.py` does not use the newer sound-effects model or the `loop` flag**, so it
   cannot make the 93 loops. It needs a small change before a real run.

## Review

Nothing off the shelf reviews game sound. What exists is ours: `audioforge.py audit` (measures
harshness, level, length; a proxy, not listening) and `audioforge.py preview` (a page with a player
per sound). The gap is a listening loop that ends in a decision. Proposal, not built: a review page
that plays each take beside its brief, records keep or reject and a note per take, and writes a file
that `promote` reads; plus the in-game category cycle from `audio.md` §7.3 so a sound can be heard
in the game's own acoustics.

## Steam

Pre-generated AI sound effects and music must be declared in Steamworks' AI content survey, with
what was generated and how, and the developer must confirm it is reviewed and does not copy
anyone's work. `ai/log.csv` in AudioForge is the record of every prompt.
