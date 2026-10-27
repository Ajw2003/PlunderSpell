"""Builds the fear-entities plan page from the house-rules handover template.

The template is copied whole; only its PAGE block and its <title> are replaced, with the content of
page.json beside this script. Nothing else in the page is written by hand.

Run from the repo root:
    python docs/plans/fear-entities/build_page.py [path/to/handover-page.html]
With no argument it uses the newest handover-page.html in the installed house-rules plugin.
"""
import glob
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
START = "const PAGE = {"
END = "// ---- End of PAGE."


def find_template():
    if len(sys.argv) > 1:
        return sys.argv[1]
    pattern = os.path.join(os.environ["APPDATA"], "Claude", "**", "templates", "handover-page.html")
    found = sorted(glob.glob(pattern, recursive=True), key=os.path.getmtime)
    if not found:
        sys.exit("No handover-page.html found under %APPDATA%/Claude; pass its path as the argument.")
    return found[-1]


def main():
    template_path = find_template()
    template = open(template_path, encoding="utf-8").read()
    page = json.load(open(os.path.join(HERE, "page.json"), encoding="utf-8"))

    start, end = template.find(START), template.find(END)
    if start < 0 or end < start:
        sys.exit(f"The PAGE block markers were not found in {template_path}; the template changed shape.")
    block = "const PAGE = " + json.dumps(page, indent=2, ensure_ascii=False) + ";\n"
    out = template[:start] + block + template[end:]
    out, titles = re.subn(r"<title>.*?</title>", "<title>" + page["title"] + "</title>", out, count=1)
    if titles != 1:
        sys.exit("The template has no <title> tag to fill.")

    target = os.path.join(HERE, "fear-entities-in-game.html")
    open(target, "w", encoding="utf-8").write(out)
    print(f"built {target} from {template_path}")


if __name__ == "__main__":
    main()
