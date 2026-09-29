"""AudioForge's one command. Runs from any folder.

    python3 Tools/AudioForge/audioforge.py manifest     # manifest_source.py -> manifest.csv
    python3 Tools/AudioForge/audioforge.py build [glob] # manifest -> Assets/_Project/Audio
    python3 Tools/AudioForge/audioforge.py check        # verify coverage, levels, loops, licences
    python3 Tools/AudioForge/audioforge.py promote <file> <name>_<NN> --licence "..."
    python3 Tools/AudioForge/audioforge.py ai [glob] [--dry-run] [--auto-promote]
    python3 Tools/AudioForge/audioforge.py preview      # docs/generated/audio-preview/index.html
    python3 Tools/AudioForge/audioforge.py index        # CLAP index of the library roots (finder/roots.json)
    python3 Tools/AudioForge/audioforge.py find "text" [--top N] [--for <sound>]
    python3 Tools/AudioForge/audioforge.py coverage     # which placeholders a library covers
    python3 Tools/AudioForge/audioforge.py review [glob]  # docs/generated/audio-review/index.html
    python3 Tools/AudioForge/audioforge.py apply review-decisions.json

(On Windows, `python` instead of `python3`.) See Tools/AudioForge/README.md.
"""

import runpy
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

COMMANDS = {
    "build": "forge.build",
    "check": "forge.check",
    "promote": "forge.promote",
    "ai": "forge.ai_elevenlabs",
    "preview": "forge.preview",
    "audit": "forge.audit",
    "index": "finder.index",
    "find": "finder.search",
    "coverage": "finder.coverage",
    "review": "finder.review",
    "apply": "finder.apply",
}


def main():
    if len(sys.argv) < 2 or sys.argv[1] in ("-h", "--help"):
        print(__doc__)
        return 0
    command, args = sys.argv[1], sys.argv[2:]
    if command == "manifest":
        sys.argv = [str(HERE / "manifest_source.py")] + args
        runpy.run_path(str(HERE / "manifest_source.py"), run_name="__main__")
        return 0
    if command not in COMMANDS:
        print(f"unknown command {command!r}\n{__doc__}")
        return 2
    module = __import__(COMMANDS[command], fromlist=["main"])
    return module.main(args) if command not in ("check",) else module.main()


if __name__ == "__main__":
    # The CSVs and briefs are UTF-8 (they hold characters like the arrow in "alarm -> stirred"). Python on
    # Windows opens files as cp1252 unless told otherwise, which crashed the manifest writer partway and
    # left a truncated manifest.csv. Restart once in UTF-8 mode rather than patch every open() call.
    if not sys.flags.utf8_mode:
        import subprocess
        sys.exit(subprocess.call([sys.executable, "-X", "utf8", str(Path(__file__).resolve())] + sys.argv[1:]))
    sys.exit(main())
