"""Repoint references after docs files move (one-off for the 2026-10-11 docs/7-design move, kept as a record).

Every path-like token in tracked text files is resolved to the file it names. A token naming a
moved file is rewritten to the new path in the same style (repo-root or relative). Inside a moved
file, relative tokens are resolved against its old folder and rewritten for its new one.
Usage: python Tools/docs/relink_moves.py [--dry-run]
"""
import os
import re
import subprocess
import sys

MOVES = {
    "docs/plunderspell.md": "docs/7-design/plunderspell.md",
    "docs/plans/fear-pass.md": "docs/7-design/fear-pass.md",
    "docs/plans/diegetic-ui-lair-market.md": "docs/7-design/diegetic-ui-lair-market.md",
    "docs/plans/wizard-character.md": "docs/7-design/wizard-character.md",
}
TEXT = (".md", ".py", ".cs", ".html", ".txt", ".json", ".yml", ".sh")
TOKEN = re.compile(r"[A-Za-z0-9_./-]+\.(?:md|png|jpg|html|py|cs|json|mp4|svg)")


def norm(path):
    return os.path.normpath(path).replace("\\", "/")


def old_path_of(new_path):
    for old, new in MOVES.items():
        if new == new_path:
            return old
    return new_path


def resolve(token, file_old_dir, exists):
    """Return the repo path a token names (in the pre-move tree), or None."""
    candidates = [token] if token.startswith(("docs/", "Tools/", "Assets/")) else []
    candidates.append(norm(os.path.join(file_old_dir, token)))
    for candidate in candidates:
        candidate = norm(candidate)
        if candidate in exists:
            return candidate
    return None


def rewrite(token, target_old, file_new_dir):
    target_new = MOVES.get(target_old, target_old)
    if token.startswith(("docs/", "Tools/", "Assets/")):
        return target_new
    return norm(os.path.relpath(target_new, file_new_dir or "."))


def main():
    dry = "--dry-run" in sys.argv
    tracked = subprocess.run(["git", "ls-files"], capture_output=True, text=True, check=True).stdout.split("\n")
    tracked = [p for p in tracked if p]
    exists = {old_path_of(p) for p in tracked}  # the tree as it was before the move
    changed = 0
    for path in tracked:
        # Archives and the issue snapshot record the past as it was, so their paths stay.
        if not path.endswith(TEXT) or path.startswith(("Assets/PurrNet", "docs/archive/")) or path == "all_issues.json":
            continue
        is_moved = path in MOVES.values()
        old_dir = os.path.dirname(old_path_of(path))
        new_dir = os.path.dirname(path)
        try:
            with open(path, encoding="utf-8", newline="") as handle:
                text = handle.read()
        except (UnicodeDecodeError, OSError):
            continue

        def fix(match):
            token = match.group(0)
            target = resolve(token, old_dir, exists)
            if target is None:
                return token
            if target not in MOVES and not (is_moved and not token.startswith(("docs/", "Tools/", "Assets/"))):
                return token
            new_token = rewrite(token, target, new_dir)
            if new_token != token:
                print(f"{path}: {token} -> {new_token}")
            return new_token

        new_text = TOKEN.sub(fix, text)
        if new_text != text:
            changed += 1
            if not dry:
                with open(path, "w", encoding="utf-8", newline="") as handle:
                    handle.write(new_text)
    print(f"{changed} files {'would change' if dry else 'changed'}")


if __name__ == "__main__":
    main()
