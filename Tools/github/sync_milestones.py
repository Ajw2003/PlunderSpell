"""Creates the roadmap's milestones on GitHub and assigns each open issue to its milestone.

Usage: python Tools/github/sync_milestones.py [--dry-run]
The milestones and their issues mirror docs/2-roadmap/Roadmap.md; change that first, then this.
Safe to re-run: an existing milestone with the same title is updated rather than duplicated.
"""
import json
import subprocess
import sys

ROADMAP = 'docs/2-roadmap/Roadmap.md'

# (title, weight, state, one-line description, acceptance, issues)
MILESTONES = [
    ('M0 — Fork clean, cut gravity', 5, 'closed',
     'Delete the planetary Gravity assembly and restore world gravity.',
     'Crates and players fall along -Y; thrown items still deal velocity-scaled damage; '
     'no reference to the gravity module remains.', []),
    ('M1 — Prove the voice', 10, 'open',
     'Whether shouting at your own computer feels like power or embarrassment.',
     'Over 90% top-1 recognition across four accents on the 40-word lexicon, under 150 ms from '
     'word-end to effect, misfires land as jokes; measured with real microphones.', [125, 50]),
    ('M2 — The vertical slice', 20, 'open',
     'One castle, one century, four words, four players, the whole loop from Lair to Lair.',
     'Four real players complete a raid together through the built game, and loot they carry '
     'out changes what the Lair shows next time.',
     [143, 140, 110, 106, 152, 134, 155, 169, 171, 172, 158, 164, 154, 127, 131, 51, 167, 168,
      170, 55, 335, 336, 337, 338, 339, 341]),
    ('M3 — Open the other Ages', 15, 'open',
     'Each era brings its own rooms, loot, enemies and weapons.',
     'A different era gives a measurably different raid, every era builds from its own room set '
     'with none borrowed, and a person has played all four side by side.',
     [151, 17, 135, 150, 126, 38, 41]),
    ('M4 — The Lair and the Market', 15, 'open',
     'The fourth pillar: a Lair you walk around, and a market where purchases stay bought.',
     'Across three sessions with relaunches, a player pays their debt, spends the rest at a '
     'stall, and finds the purchase and the hoard still there in the Lair.',
     [30, 31, 33, 130, 26, 27, 28, 29, 108, 340]),
    ('M5 — The household is awake', 10, 'open',
     'Enemies notice, react and hunt; the player can creep past by crouching.',
     'In a recorded solo raid per era: guards react to hits, investigate thrown objects, lose '
     'and search for a crouching player, never stand frozen over 10 s, and hear crouched steps '
     'at most half as far as walking ones.', [160, 163, 153, 103]),
    ('M6 — The castle fights back', 10, 'open',
     'Working doors and stairs, real hazards, and a vault boss.',
     'On five seeds per era every door and stair can be used, and each raid has a working '
     'hazard of its era.', [111, 44, 45, 35, 43]),
    ('M7 — Final art and performance pass', 15, 'open',
     'Animation, audio, art and performance, done once after the systems settle.',
     'Every swing, throw, hit, cast and death is seen and heard with no sliding or T-posing '
     'enemy; a newcomer installs on a PC and a Steam Deck, joins by Steam invite and finishes '
     'a raid within the frame budget.',
     [11, 141, 52, 42, 48, 16, 34, 40, 58, 59, 121, 23, 32, 54, 165, 166, 56, 57]),
]


def gh(*args, payload=None):
    result = subprocess.run(['gh', *args], input=json.dumps(payload) if payload else None,
                            capture_output=True, text=True, encoding='utf-8')  # titles hold an em dash
    if result.returncode != 0:
        raise SystemExit(f'gh {" ".join(args)} failed: {result.stderr.strip()}')
    return result.stdout


def main():
    dry = '--dry-run' in sys.argv
    existing = {m['title']: m['number'] for m in json.loads(
        gh('api', 'repos/{owner}/{repo}/milestones?state=all&per_page=100'))}
    for title, weight, state, line, acceptance, issues in MILESTONES:
        body = {'title': title, 'state': state,
                'description': f'Weight {weight}/100. {line}\n\nAcceptance: {acceptance}\n\n'
                               f'Definition of done: {ROADMAP}'}
        if dry:
            print(f'would set "{title}" ({state}) and assign {len(issues)} issues')
            continue
        if title in existing:
            number = existing[title]
            gh('api', '-X', 'PATCH', f'repos/{{owner}}/{{repo}}/milestones/{number}', '--input', '-',
               payload=body)
            verb = 'updated'
        else:
            number = json.loads(gh('api', '-X', 'POST', 'repos/{owner}/{repo}/milestones',
                                   '--input', '-', payload=body))['number']
            verb = 'created'
        for issue in issues:
            gh('issue', 'edit', str(issue), '--milestone', title)
        print(f'{verb} "{title}" ({state}), assigned {len(issues)} issues')


if __name__ == '__main__':
    main()
