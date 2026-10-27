# Changelog page template

A changelog page in the shape of the Changes section of the visual roadmap
(`docs/roadmap-visual/roadmap.html`, published at https://claude.ai/artifact/6LzKZXSuHpiVjtTnkA8E5s).
The owner asked on 2026-10-27 to keep that section as the template for a changelog page every repo should have.

Each entry is a row you tap to open. The row shows a tag, a title and one line. Opened, it shows the detail
and one real picture, or a before-and-after picture with a slider.

| Tag | Shown as | Use it for |
|---|---|---|
| `keep` | Kept | Built and checked. Title starts "Built <date>:". |
| `chg` | Changed | Something that works differently now. Give it a before-and-after picture. |
| `new` | New | Being built now ("Now, #N: ...") or coming next ("Next, #N: ..."). |

## Files

- `changelog.template.json`: the blank to copy. Every `<...>` is a placeholder.
- `changelog.example.json` and `changelog.example.html`: the template filled from PlunderSpell's roadmap,
  23 September to 27 October 2026, and the page it builds. Its pictures are in `changelog.example-pics/`.

## Making a changelog

1. Copy `changelog.template.json` to `docs/changelog/changelog.json` in the repo.
2. Replace every placeholder, and add or remove rows. Newest goes last, so the page reads in order.
3. Point each `src` at an existing screenshot or render in the repo, by its repo-relative path. Name that
   file in the caption. Don't generate new pictures. Recapture only when nothing exists, and ask the owner
   first.
4. Build it with the house-rules page builder, from the repo root:
   `python3 <house-rules plugin>/scripts/make_page.py build docs/changelog/changelog.json --out docs/changelog/changelog.html`
   The builder copies each picture into `docs/changelog/changelog-pics/`, and refuses a row marked
   `{"visual": true}` that has no picture.
5. Publish `changelog.html` with its `changelog-pics/` files, then commit all three.

Built unfilled, the template is refused, because its picture paths are placeholders. That is on purpose.
