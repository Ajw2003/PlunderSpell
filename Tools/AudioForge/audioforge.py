"""AudioForge's one command. Runs from any folder.

    python3 Tools/AudioForge/audioforge.py manifest     # manifest_source.py -> manifest.csv
    python3 Tools/AudioForge/audioforge.py build [glob] # manifest -> Assets/_Project/Audio
    python3 Tools/AudioForge/audioforge.py check        # verify coverage, levels, loops, licences
    python3 Tools/AudioForge/audioforge.py promote <file> <name>_<NN> --licence "..."
    python3 Tools/AudioForge/audioforge.py ai [glob] [--dry-run] [--auto-promote]
    python3 Tools/AudioForge/audioforge.py preview      # docs/generated/audio-preview/index.html

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
    sys.exit(main())
