# Playtest: the Lair and the Market, in the Editor

**2026-10-07.** For the owner, to try every new Lair and Market feature by hand on branch `claude/lair-market`
(#306, #328, #334). Each step says what to do and what you should see. About 20 minutes, solo. A short co-op
section is at the end.

## Before you start

- Use the Unity Editor whose window title says **PlunderSpell-lair** (the branch's folder,
  `C:\Users\aj\Desktop\GameDev\PlunderSpell-lair`), not the one titled PlunderSpell.
- Open `Assets/_Project/Scenes/RaidScene.unity` if it is not already the open scene.
- Save slot 1 was reset on 2026-10-07: debt 500, no coins, no pile. The game starts in slot 1.

## Controls

| Key | Does |
|---|---|
| W A S D, mouse | Walk, look |
| E | Use what you look at: the ledger table, the Market door |
| Esc | In the Lair room: open the Lair screen |
| Left mouse (hold) | Grab and carry a piece or a coin pouch |
| Right mouse | Throw what you hold |
| 1 / 2 / 3 | Haggle at a counter: *Plus* (more) / *Satis* (enough, sell) / *Vale* (farewell) |
| V (hold) + speak | Say *Plus*, *Satis* or *Vale* instead of 1 / 2 / 3 |
| F5 | Call extraction early (developer key) |

## 1. The Lair room

1. Press **Play**, then **Play Solo** on the main menu.
   **You should see:** the vaulted cellar, standing just inside the portal arch, facing into the room: the hearth on
   the left, the ledger table with its candle ahead, the century dial (brass rings), the Market door in the back wall.
2. Walk around.
   **You should see:** you cannot walk through the table, the dial, the walls or the strongboxes.
3. Walk to the ledger table, look at the table top or the open book, press **E**.
   **You should see:** the Lair screen: Owed 500, a Purses column for I, the four Ages, and **‹ Back to the Room**.
4. Click **‹ Back to the Room**.
   **You should see:** the room again, standing exactly where you were at the table (not back at the portal).
5. Press **Esc**.
   **You should see:** the Lair screen again. Click **‹ Back to the Room**.

## 2. The Market door and back

1. Walk to the low arched door in the back wall, look at it, press **E**.
   **You should see:** a lantern-lit yard at night: the well in the middle, four stalls round it (the Goldsmith's
   striped awning, the Pardoner's booth, the Antiquarian's cabinet, the Fence's cart), a pale figure behind each
   counter. You face the well.
2. Turn round and walk out through the gap in the low wall behind you.
   **You should see:** the Lair again, standing just inside the Market door, facing into the room.

## 3. A raid, and the haul comes home

1. Walk into the portal arch (the stone arch with the green stones on its threshold).
   **You should see:** the castle; you stand beside the extraction pad (the portal's glow on the floor).
2. Grab one or two pieces of loot near you (hold left mouse) and drop them on the pad. Stand on the pad yourself, or
   press **F5**.
   **You should see:** after the countdown (or at once with F5), you are back in the Lair, at the portal.
3. Look down and ahead.
   **You should see:** the pieces you carried, lying on the flagstones about a metre in front of you.
   *Known:* the pile sits low in the view; look down a little.
4. Open the ledger (E at the table).
   **You should see:** Owed is still 500 (plus 50 for the raid): carrying loot home no longer pays the debt; selling
   it does. The last-raid line says what you carried home.

## 4. Selling

1. Pick up a piece from the pile, carry it through the Market door (look at the door, **E**, still holding it).
   **You should see:** you arrive in the Market still holding the piece.
2. Carry it to the Goldsmith (the striped awning) and drop it on his counter. Stand within 3 m.
   **You should see:** a line above the counter: "The Goldsmith: N coin." He pays more for metal, the Pardoner for
   holy pieces, the Antiquarian for curios and arms; the Fence pays the same for anything but haggles least.
3. Press **1** (*Plus*) a few times.
   **You should see:** each time either a higher figure, or a refusal ("Too much. Do not push me."). Too many
   refusals and he will not buy that piece tonight.
4. Press **2** (*Satis*).
   **You should see:** "N coin, counted out. Done." The piece goes, and a **coin pouch** lies on the counter.
5. Try **3** (*Vale*) on another piece, then put it back on the same counter.
   **You should see:** "Farewell"; when you put it back, his new opening offer is 10% lower. He remembers.
6. Try the voice: hold **V**, say "Plus", release.
   **You should see:** the same as pressing 1. (Untested with a real microphone until now: please report how well
   it hears you.)

## 5. Coins home, and the Collector

1. Grab the coin pouch, walk out through the Market's gate still holding it.
   **You should see:** you arrive in the Lair still holding the pouch.
2. Drop it into the first strongbox (the four chests in front of the ledger table; yours is the first, I).
   **You should see:** the pouch disappears. At the ledger (E): Purse I shows the coins; Owes shows your share.
3. Set out: walk into the portal (or **Set Out** on the Lair screen).
   **You should see:** in the castle. Come straight back (F5).
4. Open the ledger.
   **You should see:** a line "The Collector takes N from I."; your purse went down by what he took; Owed went down
   by the same, plus 50 for the raid; Paid shows N.

## 6. Co-op (optional)

Host from the main menu (**Host Co-op**) and have a friend join, or run the automated check, which does all of the
above with the Editor as host and a built copy as client:

```bash
bash /c/Users/aj/Desktop/GameDev/PlunderSpell-lair/Tools/Unity/coop_lair_check.sh playtest
```

It plays in hidden save slot 99, never your slots, and ends with `PASS all Lair checks`.

**You should see (with a friend):** each of you arrives at your own spot in the Lair; each has their own strongbox
(the friend's is the second, II); the Collector takes an equal share of the debt from each purse; your friend's
Lair screen shows the same ledger as yours.

## Known gaps

- The vendors are stand-in figures and their lines are plain floating text.
- The pile lies low in the view on arrival.
- Keys 1 / 2 / 3 and the microphone have not been pressed or spoken by a person yet: this playtest is the first time.
- Everything else here passed automated checks solo and in two-player co-op on 2026-10-07.
