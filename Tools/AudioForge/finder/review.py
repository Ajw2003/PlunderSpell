"""Writes docs/generated/audio-review/index.html: one self-contained page to audition the search's candidates.

    python Tools/AudioForge/audioforge.py review [glob ...] [--top 5] [--all] [--root NAME ...]

Default: every sound still synthesised as a stand-in (the coverage list); a glob narrows it, --all takes every
sound in the manifest. For each sound the page shows its brief, the file the build made now, and the top
candidates from the library index, each with a player that plays only its window, its score and the audit flags
of that window. Keep, Reject and a note are saved in the browser (localStorage); "Export decisions" writes
review-decisions.json, which `audioforge.py apply` turns into picks.csv. No server: open the page from the
checkout (the audio links point at the library files and at Assets/_Project/Audio).
"""

import argparse
import fnmatch
import html
import json
import os
import shlex
import sys
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import quote

from forge.build import OUT, REPO, load_picks
from forge.categories import category
from forge.preview import STYLE as BASE_STYLE

from .coverage import placeholder_rows
from .search import Library, length_limits, manifest_rows, query_for

PAGE = REPO / "docs" / "generated" / "audio-review" / "index.html"
LOOP_XFADE = 0.5

STYLE = """
.toolbar{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:8px 0}
.toolbar button,.cand button,.cur button{font:inherit;padding:3px 10px;border-radius:6px;border:1px solid var(--line);background:var(--panel);color:var(--ink);cursor:pointer}
button.keep.on{background:var(--verd);color:#0b1712;border-color:var(--verd)}button.reject.on{background:var(--madder);color:#1b0b05;border-color:var(--madder)}
button.play.on{background:var(--gold);color:#1d1600;border-color:var(--gold)}
.sound{padding:10px 0;border-bottom:1px solid var(--line)}.sound.done{opacity:.55}
.cur,.cand{display:grid;grid-template-columns:auto minmax(0,1fr) auto;gap:4px 10px;align-items:center;padding:4px 0}
.cand{border-top:1px dotted var(--line)}.meta{font-size:13px;color:var(--dim);word-break:break-all}
.score{font-family:ui-monospace,Menlo,monospace;font-size:13px}.bar{display:inline-block;height:6px;background:var(--lapis);border-radius:3px;vertical-align:middle;margin-left:6px}
.flags{color:var(--madder);font-size:12px}.note{grid-column:2/-1;width:100%;padding:3px 6px;background:var(--panel);color:var(--ink);border:1px solid var(--line);border-radius:5px;font:inherit;font-size:13px}
#out{width:100%;height:160px;background:var(--panel);color:var(--ink);border:1px solid var(--line);font:12px ui-monospace,Menlo,monospace}
.q{color:var(--dim);font-size:13px}.tag.hit{color:var(--verd);border-color:var(--verd)}
"""

SCRIPT = """
const KEY='plunderspell-audio-review-v1';
let state={};try{state=JSON.parse(localStorage.getItem(KEY)||'{}')}catch(e){state={}}
const save=()=>{try{localStorage.setItem(KEY,JSON.stringify(state))}catch(e){}};
const player=new Audio();player.preload='auto';let current=null,timer=null;
function stop(){if(timer){clearInterval(timer);timer=null}player.pause();if(current){current.classList.remove('on');current.textContent='play';current=null}}
function play(btn){
  if(current===btn){stop();return}
  stop();const start=parseFloat(btn.dataset.start||'0'),end=btn.dataset.end?parseFloat(btn.dataset.end):null;
  const go=()=>{player.currentTime=start;player.play().catch(e=>{btn.textContent='cannot play'});
    current=btn;btn.classList.add('on');btn.textContent='stop';
    timer=setInterval(()=>{if(player.ended||(end!==null&&player.currentTime>=end)){stop()}},15)};
  const url=new URL(btn.dataset.src,location.href).href;
  if(player.src!==url){player.src=url;player.addEventListener('loadedmetadata',go,{once:true});player.load()}else go();
}
document.addEventListener('click',e=>{
  const b=e.target.closest('button');if(!b)return;
  if(b.classList.contains('play'))return play(b);
  const c=b.closest('.cand');if(!c)return;
  if(b.classList.contains('keep')||b.classList.contains('reject')){
    const kind=b.classList.contains('keep')?'keep':'reject';const id=c.dataset.id;
    const cur=(state[id]||{}).decision;state[id]=Object.assign({},state[id],{decision:cur===kind?null:kind});
    paint(c);save();count();
  }
});
document.addEventListener('input',e=>{
  if(e.target.classList.contains('note')){const c=e.target.closest('.cand');
    state[c.dataset.id]=Object.assign({},state[c.dataset.id],{note:e.target.value});save()}
  if(e.target.id==='q'){const t=e.target.value.toLowerCase();
    document.querySelectorAll('.sound').forEach(s=>{s.style.display=s.dataset.k.includes(t)?'':'none'})}
});
function paint(c){const s=state[c.dataset.id]||{};
  c.querySelector('.keep').classList.toggle('on',s.decision==='keep');
  c.querySelector('.reject').classList.toggle('on',s.decision==='reject');
  c.querySelector('.note').value=s.note||'';
  const sound=c.closest('.sound');sound.classList.toggle('done',!!sound.querySelector('.keep.on'))}
function count(){let k=0,r=0;for(const id in state){if(state[id].decision==='keep')k++;if(state[id].decision==='reject')r++}
  document.getElementById('count').textContent=k+' kept, '+r+' rejected'}
function exportDecisions(){
  const out=[];
  document.querySelectorAll('.cand').forEach(c=>{const s=state[c.dataset.id]||{};
    if(!s.decision&&!(s.note&&s.note.trim()))return;
    const d=JSON.parse(c.dataset.info);d.decision=s.decision||null;d.note=(s.note||'').trim();out.push(d)});
  const doc={version:1,exported:new Date().toISOString(),decisions:out};
  const text=JSON.stringify(doc,null,2);
  document.getElementById('out').value=text;document.getElementById('outbox').open=true;
  try{const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([text],{type:'application/json'}));
    a.download='review-decisions.json';document.body.appendChild(a);a.click();a.remove()}catch(e){}
  return doc}
document.getElementById('export').addEventListener('click',exportDecisions);
document.getElementById('clear').addEventListener('click',()=>{if(confirm('Forget every Keep, Reject and note in this browser?')){state={};save();document.querySelectorAll('.cand').forEach(paint);count()}});
document.querySelectorAll('.cand').forEach(paint);count();
"""


def audio_url(path):
    """URL of an audio file relative to the page (spaces and the like escaped); file:// when on another drive."""
    try:
        rel = os.path.relpath(path, PAGE.parent).replace(os.sep, "/")
        return quote(rel)
    except ValueError:
        return Path(path).as_uri()


def recipe_for(hit, row, library):
    """The lib: recipe that reproduces this candidate in the build."""
    rel = hit["file"]
    parts = [f"lib:{hit['root']}/{shlex.quote(rel)}"]
    if hit["file_dur"] > hit["dur"] + 0.01:
        parts.append(f"start={hit['start']:g} end={hit['start'] + hit['dur']:g}")
    if row["loop"] == "1":
        parts.append(f"xfade={min(LOOP_XFADE, round(hit['dur'] / 4, 2)):g}")
    return " ".join(parts)


def main(argv=None):
    parser = argparse.ArgumentParser(prog="audioforge.py review")
    parser.add_argument("globs", nargs="*", help="sound-name globs (default: every placeholder sound)")
    parser.add_argument("--top", type=int, default=5)
    parser.add_argument("--all", action="store_true", help="every sound in the manifest, not only placeholders")
    parser.add_argument("--root", action="append", help="candidates only from this library root (repeatable)")
    args = parser.parse_args(argv)

    rows = manifest_rows()
    chosen = rows if args.all else placeholder_rows(rows)
    if args.globs:
        chosen = [r for r in rows if any(fnmatch.fnmatch(r["name"], g) for g in args.globs)]
    if not chosen:
        print("no sounds match")
        return 2
    library = Library(args.root)
    picks = load_picks()
    body = []
    n_cands = 0
    for row in chosen:
        query = query_for(row)
        hits = library.search(query, args.top, length_limits(row))
        cur = OUT / row["folder"] / f"{row['name']}_01.ogg"
        picked = " (a pick is set)" if (row["name"], 0) in picks else ""
        block = [f"<div class=sound data-k='{html.escape((row['name'] + ' ' + row['brief'] + ' ' + category(row)).lower())}'>",
                 f"<div><span class=name>{html.escape(row['name'])}</span> "
                 f"<span class=tag>{html.escape(category(row))}</span>"
                 f"{'<span class=tag>loop</span>' if row['loop'] == '1' else ''}"
                 f"<span class=tag>{row['variants']} variant{'s' if row['variants'] != '1' else ''}</span></div>",
                 f"<div class=brief>{html.escape(row['brief'])}</div>",
                 f"<div class=q>searched: {html.escape(query)}</div>"]
        if cur.exists():
            block.append(f"<div class=cur><button class=play data-src='{html.escape(audio_url(cur))}'>play</button>"
                         f"<div class=meta>now: {html.escape(row['name'])}_01.ogg{picked}</div><span></span></div>")
        if not hits:
            block.append("<div class=meta>no candidate passes this sound's length rule in the indexed libraries</div>")
        for hit in hits:
            n_cands += 1
            recipe = recipe_for(hit, row, library)
            info = dict(sound=row["name"], recipe=recipe, root=hit["root"], file=hit["file"], start=hit["start"],
                        dur=hit["dur"], score=round(hit["score"], 3))
            cid = f"{row['name']}|{hit['root']}/{hit['file']}|{hit['start']:g}"
            flags = library.flags(hit, row)
            end = hit["start"] + hit["dur"]
            window = "whole file" if hit["file_dur"] <= hit["dur"] + 0.01 else f"{hit['start']:g}-{end:g} s of {hit['file_dur']:g} s"
            block.append(
                f"<div class=cand data-id='{html.escape(cid)}' data-info='{html.escape(json.dumps(info))}'>"
                f"<button class=play data-src='{html.escape(audio_url(library.path(hit)))}' data-start='{hit['start']:g}' "
                f"data-end='{end:g}'>play</button>"
                f"<div><span class=score>{hit['score']:.3f}<span class=bar style='width:{max(2, int(hit['score'] * 120))}px'></span></span> "
                f"<span class=meta>{html.escape(hit['root'] + '/' + hit['file'])} &middot; {html.escape(window)} &middot; {hit['dur']:.2f} s</span>"
                + (f"<div class=flags>{html.escape('; '.join(flags))}</div>" if flags else "")
                + "</div><div><button class=keep>Keep</button> <button class=reject>Reject</button></div>"
                "<input class=note placeholder='note'></div>")
        block.append("</div>")
        body.append("".join(block))

    page = (f"<!doctype html><html lang=en><head><meta charset=utf-8>"
            f"<meta name=viewport content='width=device-width,initial-scale=1'><title>Plunderspell Sound Search Review</title>"
            f"<style>{BASE_STYLE}{STYLE}</style></head><body><main>"
            f"<h1>Plunderspell: sound candidates</h1>"
            f"<p class=sub>{len(chosen)} sounds, {n_cands} candidates from {', '.join(sorted({r['root'] for r in library.rows}))} "
            f"({len(library.file_ids)} files), searched with CLAP. Play plays only the window shown. Scores are cosine similarity: "
            f"a guide to what to listen to, not a verdict. Generated {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M')} UTC.</p>"
            f"<nav><input id=q placeholder='Filter by name, brief or category'></nav>"
            f"<div class=toolbar><button id=export>Export decisions</button><button id=clear>Clear all</button>"
            f"<span id=count></span></div>"
            f"<details id=outbox><summary>Exported JSON (also downloaded as review-decisions.json)</summary>"
            f"<textarea id=out readonly></textarea></details>"
            f"{''.join(body)}</main><script>{SCRIPT}</script></body></html>")
    PAGE.parent.mkdir(parents=True, exist_ok=True)
    PAGE.write_text(page, encoding="utf-8")
    print(f"wrote {PAGE.relative_to(REPO)} ({len(chosen)} sounds, {n_cands} candidates)")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
