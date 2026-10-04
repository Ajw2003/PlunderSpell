"""
Can the guards' walk map climb a newel (spiral) stair of a given size? (#197, 2026-10-03)

    python Tools/ArtBible/castle/newel_walkmap_check.py

Models the stair the way the nav tiles see it: a 0.5 m grid of cells (CastleNavTile.CellSize), each
taking the height of the tread under its centre, and a walk between 4-neighbours allowed only when
their heights differ by 0.45 m or less (CastleNavTile.StepHeight). Prints, for each stair-space
width, whether the head is reachable from the foot. A model, not the baker: re-check the real module
with Tools/Unity/eval/stair_survey.cs once it is built.

Result on 2026-10-03: the art bible's drum (stair space 2.4 m across) fails, because around so small a
circle the next cell is 3-4 treads higher. 3.0 m across (drum about 4.6 m outside) passes at 15
degrees a tread.
"""
import math

CELL, STEP = 0.5, 0.45            # CastleNavTile.CellSize, CastleNavTile.StepHeight
NEWEL = 0.12                      # newel radius
RISER = 0.187                     # the newel sheet's riser (docs/art/data/high.json, spiral-stair)


def climbable(space_across, rise, tread_deg=15.0):
    """(head reachable from foot, cells reached, cells in the stair)."""
    r_out = space_across / 2
    risers = round(rise / RISER)
    r_h = rise / risers
    cells = {}
    n = int(r_out / CELL) + 2
    for i in range(-n, n):
        for j in range(-n, n):
            x, y = (i + .5) * CELL, (j + .5) * CELL
            if not NEWEL < math.hypot(x, y) < r_out:
                continue
            tread = int((math.degrees(math.atan2(-x, y)) % 360) // tread_deg)     # clockwise from north
            if tread < risers:
                cells[(i, j)] = (tread + 1) * r_h
    foot = min(cells, key=cells.get)
    head = max(cells, key=cells.get)
    seen, todo = {foot}, [foot]
    while todo:
        i, j = todo.pop()
        for m in ((i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)):
            if m in cells and m not in seen and abs(cells[m] - cells[(i, j)]) <= STEP:
                seen.add(m)
                todo.append(m)
    return head in seen, len(seen), len(cells)


def main():
    for rise, what in ((4.30, "up, ward to keep"), (3.30, "down, ward to crypt")):
        for across in (2.4, 3.0, 3.6):
            ok, reached, total = climbable(across, rise)
            print(f"{what}: stair space {across} m across -> head reachable {ok} ({reached}/{total} cells)")


if __name__ == "__main__":
    main()
