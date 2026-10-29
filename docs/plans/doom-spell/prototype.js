// Fatum (the Doom spell) visual prototype. One WebGL fragment shader draws the whole scene; a 2D
// canvas on top draws the boss bar, damage numbers and captions. Load it into any blank page:
// it builds its own stage and controls. capture.js uses window.renderAt(t) to record frames.
//
// TIMELINE must match the constants in the shader below and in compose_sound.py.
(function () {
'use strict';

const TIMELINE = { CHARGE: 0.0, RELEASE: 3.0, OPEN: 5.0, INHALE: 14.2, SILENCE: 15.0, DETONATE: 15.4, END: 21.0 };
const T = TIMELINE;

const PHASES = [
  { from: 0.0,  name: 'Incantation', text: 'The caster speaks the word. Their hands blacken and the torches lean toward them. A funeral bell tolls once.' },
  { from: 3.0,  name: 'The Omen rises', text: 'A small red-black orb leaves the hands and drifts up over the boss. A heartbeat starts. Everyone has 2 seconds to get clear.' },
  { from: 5.0,  name: 'The Maw opens', text: 'The orb tears into a red black hole. It drags loot, guards and anyone on the ground toward it, and crushes the boss for 9 seconds.' },
  { from: 14.2, name: 'The Inhale', text: 'It swallows the light, the torches and the sound. Everything goes still and dark.' },
  { from: 15.4, name: 'Doom', text: 'The hole collapses onto the boss: a blast, shattered gold and a final toll ringing through an empty hall.' },
];

// Heartbeats, in the same places as compose_sound.py, drive the vignette and the damage ticks.
const BEATS = [T.RELEASE + 0.5, T.RELEASE + 1.15, T.RELEASE + 1.65];
for (let b = T.OPEN + 1.0, gap = 1.1; b < T.INHALE; b += gap, gap = Math.max(0.42, gap * 0.9)) BEATS.push(b);
const HOLE_BEATS = BEATS.filter(b => b >= T.OPEN);

const BOSS_START_HP = 0.62;
const BOSS_HP_BEFORE_BLAST = 0.30;

const FRAGMENT_SHADER = `
precision highp float;
uniform vec2 uRes;
uniform float uT;
uniform float uBeat;

const float RELEASE = 3.0;
const float OPEN = 5.0;
const float INHALE = 14.2;
const float SILENCE = 15.0;
const float DETONATE = 15.4;

const float FLOOR_Y = -0.30;
const vec2 HOLE = vec2(0.30, 0.25);
const vec2 HANDS = vec2(-0.555, -0.135);

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float hash1(float n) { return fract(sin(n * 91.345) * 47453.5453); }
float noise(vec2 p) {
  vec2 i = floor(p), f = fract(p);
  vec2 u = f * f * (3.0 - 2.0 * f);
  return mix(mix(hash(i), hash(i + vec2(1, 0)), u.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), u.x), u.y);
}
float fbm(vec2 p) {
  float v = 0.0, a = 0.5;
  for (int i = 0; i < 5; i++) { v += a * noise(p); p = p * 2.03 + vec2(1.7, 9.2); a *= 0.5; }
  return v;
}
float sdBox(vec2 p, vec2 b) { vec2 d = abs(p) - b; return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0); }
float smin(float a, float b, float k) { float h = clamp(0.5 + 0.5 * (b - a) / k, 0.0, 1.0); return mix(b, a, h) - k * h * (1.0 - h); }
mat2 rot(float a) { float c = cos(a), s = sin(a); return mat2(c, -s, s, c); }
float easeOutBack(float x) { float c = 1.9; return 1.0 + (c + 1.0) * pow(x - 1.0, 3.0) + c * pow(x - 1.0, 2.0); }

// ---------- timeline-derived quantities ----------
float holeRadius(float t) {
  if (t < OPEN) return 0.0;
  if (t < OPEN + 0.9) return mix(0.02, 0.105, easeOutBack(clamp((t - OPEN) / 0.9, 0.0, 1.0)));
  if (t < INHALE) return mix(0.105, 0.125, (t - OPEN - 0.9) / (INHALE - OPEN - 0.9));
  if (t < SILENCE) return mix(0.125, 0.004, pow((t - INHALE) / (SILENCE - INHALE), 0.6));
  if (t < DETONATE) return 0.004;
  return 0.0;
}
float holePresence(float t) { return t < OPEN ? 0.0 : (t < DETONATE ? 1.0 : 0.0); }
float sceneLight(float t) {
  float l = 1.0;
  l *= mix(1.0, 0.75, smoothstep(0.0, RELEASE, t));
  l *= mix(1.0, 0.55, smoothstep(OPEN, OPEN + 2.0, t));
  l *= mix(1.0, 0.05, smoothstep(INHALE, SILENCE - 0.1, t));
  if (t > DETONATE) l = mix(0.05, 0.55, smoothstep(DETONATE, DETONATE + 0.4, t));
  return l;
}
vec2 orbPos(float t) {
  float u = clamp((t - RELEASE) / (OPEN - RELEASE), 0.0, 1.0);
  float e = u * u * (3.0 - 2.0 * u);
  vec2 ctrl = vec2(-0.35, 0.42);
  vec2 a = mix(HANDS, ctrl, e), b = mix(ctrl, HOLE, e);
  return mix(a, b, e) + vec2(0.0, 0.008 * sin(t * 9.0));
}
float bossLift(float t) {
  if (t < OPEN + 0.5) return 0.0;
  if (t < INHALE) return 0.075 * smoothstep(OPEN + 0.5, INHALE - 1.0, t);
  if (t < DETONATE) return mix(0.075, 0.11, smoothstep(INHALE, SILENCE, t));
  return mix(0.11, 0.0, clamp((t - DETONATE) / 0.55, 0.0, 1.0));
}
float casterPull(float t) { return 0.045 * smoothstep(OPEN + 0.5, INHALE, t); }

// ---------- the hall ----------
vec3 torch(vec2 p, vec2 at, float t, float lean, float alive, inout vec3 glow) {
  vec2 q = p - at;
  q.x -= lean * max(q.y, 0.0) * 1.6;
  float flick = 0.85 + 0.15 * noise(vec2(t * 9.0, at.x * 10.0));
  float shape = length(q * vec2(1.6, 0.8) - vec2(0.0, 0.03)) - 0.03 * flick;
  float flame = smoothstep(0.012, 0.0, shape) * alive;
  vec3 c = mix(vec3(1.0, 0.45, 0.1), vec3(1.0, 0.9, 0.6), smoothstep(0.0, -0.02, shape));
  c = mix(c, vec3(0.9, 0.1, 0.12), smoothstep(OPEN, OPEN + 3.0, t));
  glow += c * alive * flick * 0.0007 / (0.02 + dot(q, q) * 6.0);
  float bracket = step(sdBox(p - at + vec2(0.0, 0.035), vec2(0.008, 0.03)), 0.0);
  return c * flame + vec3(0.05) * bracket * (1.0 - flame);
}

vec3 hall(vec2 p, float t, out float isFloor) {
  vec3 col;
  isFloor = 0.0;
  if (p.y > FLOOR_Y) {
    vec2 b = p * vec2(10.0, 16.0);
    b.x += step(1.0, mod(floor(b.y), 2.0)) * 0.5;
    vec2 f = fract(b);
    float mortar = smoothstep(0.0, 0.07, f.x) * smoothstep(0.0, 0.07, f.y) * smoothstep(1.0, 0.93, f.x) * smoothstep(1.0, 0.93, f.y);
    float tone = 0.6 + 0.4 * hash(floor(b));
    col = vec3(0.115, 0.105, 0.12) * tone * (0.45 + 0.55 * mortar);
    col *= 0.75 + 0.5 * fbm(p * 7.0);
    float cx = mod(p.x + 0.29, 0.58) - 0.29;
    float column = smoothstep(0.004, 0.0, abs(cx) - 0.05);
    vec3 stone = vec3(0.16, 0.15, 0.16) * (0.55 + 0.45 * cos(cx * 26.0)) * (0.8 + 0.3 * fbm(p * vec2(4.0, 30.0)));
    col = mix(col, stone, column);
    float archY = 0.33 - 0.22 * sqrt(max(0.0, 1.0 - pow(cx / 0.24, 2.0)));
    float window = step(abs(cx), 0.17) * step(archY + 0.08, p.y) * step(p.y, 0.48) * (1.0 - column);
    col = mix(col, vec3(0.03, 0.035, 0.06) + vec3(0.02, 0.03, 0.06) * fbm(p * 12.0 + t * 0.1), window);
    col *= mix(1.0, 0.45, smoothstep(FLOOR_Y, 0.5, p.y));
    col += vec3(0.04) * smoothstep(0.012, 0.0, abs(p.y - FLOOR_Y - 0.01));
  } else {
    isFloor = 1.0;
    float depth = FLOOR_Y - p.y;
    vec2 fp = vec2(p.x / (depth * 2.2 + 0.18), 0.42 / (depth + 0.03));
    vec2 g = fract(fp * vec2(2.4, 1.0));
    float seam = smoothstep(0.0, 0.05, g.x) * smoothstep(1.0, 0.95, g.x) * smoothstep(0.0, 0.08, g.y) * smoothstep(1.0, 0.92, g.y);
    col = vec3(0.10, 0.092, 0.095) * (0.6 + 0.4 * hash(floor(fp * vec2(2.4, 1.0)))) * (0.5 + 0.5 * seam);
    col *= 0.7 + 0.5 * fbm(fp * 3.0);
    col *= mix(0.55, 1.0, smoothstep(0.0, 0.2, depth));
  }
  return col;
}

// ---------- the Gilded Colossus ----------
float bossSdf(vec2 q) {
  float d = sdBox(q - vec2(-0.05, 0.065), vec2(0.032, 0.065));
  d = min(d, sdBox(q - vec2(0.05, 0.065), vec2(0.032, 0.065)));
  d = smin(d, sdBox(q - vec2(0.0, 0.135), vec2(0.085, 0.028)), 0.02);
  float w = 0.07 + (q.y - 0.13) * 0.42;
  d = smin(d, max(abs(q.x) - w, abs(q.y - 0.235) - 0.095), 0.02);
  vec2 s = vec2(abs(q.x), q.y);
  d = smin(d, length(s - vec2(0.135, 0.305)) - 0.055, 0.02);
  d = smin(d, sdBox(s - vec2(0.17, 0.22), vec2(0.032, 0.085)), 0.02);
  d = smin(d, length(s - vec2(0.172, 0.125)) - 0.045, 0.015);
  d = smin(d, sdBox(q - vec2(0.0, 0.37), vec2(0.036, 0.04)) - 0.008, 0.015);
  for (int k = -1; k <= 1; k++) {
    float x = float(k) * 0.026;
    d = min(d, sdBox(q - vec2(x, 0.43 + 0.006 * (1.0 - abs(float(k)))), vec2(0.006, 0.026)));
  }
  return d;
}

vec2 bossLocal(vec2 p, float t) {
  float lift = bossLift(t);
  float shake = (t > OPEN && t < DETONATE) ? 0.004 * (noise(vec2(t * 31.0, 1.0)) - 0.5) : 0.0;
  vec2 q = p - vec2(HOLE.x + shake, FLOOR_Y + lift);
  if (t > DETONATE) {
    float fall = smoothstep(DETONATE + 0.3, DETONATE + 1.6, t);
    q.y += 0.06 * fall;
    q = rot(-0.55 * fall) * (q - vec2(0.12, 0.0)) + vec2(0.12, 0.0);
  }
  float stretch = 0.9 * smoothstep(OPEN + 1.0, SILENCE, t) * (t < DETONATE ? 1.0 : 0.0);
  q.y /= 1.0 + stretch * smoothstep(0.1, 0.45, q.y) * 0.5;
  q.x *= 1.0 + stretch * smoothstep(0.25, 0.45, q.y) * 0.4;
  return q;
}

vec4 boss(vec2 p, float t, vec3 holeLight, float light) {
  vec2 q = bossLocal(p, t);
  float d = bossSdf(q);
  if (d > 0.01) return vec4(0.0);
  float dead = step(DETONATE, t);
  float damage = smoothstep(OPEN, INHALE, t) * (1.0 - dead) + dead;
  float shattered = dead * step(0.62 - 0.1 * smoothstep(DETONATE, DETONATE + 1.0, t), fbm(q * 14.0 + 3.0));
  if (shattered > 0.5) return vec4(0.0);
  vec2 e = vec2(0.002, 0.0);
  vec2 n = normalize(vec2(bossSdf(q + e.xy) - bossSdf(q - e.xy), bossSdf(q + e.yx) - bossSdf(q - e.yx)) + 1e-5);
  float bevel = clamp(-d * 30.0, 0.0, 1.0);
  vec3 gold = vec3(0.78, 0.56, 0.2) * (0.35 + 0.65 * fbm(q * 40.0));
  gold *= mix(0.5, 1.0, bevel);
  float panel = smoothstep(0.004, 0.0, abs(fract(q.y * 18.0) - 0.5) - 0.46) * step(0.15, q.y);
  gold *= 1.0 - 0.35 * panel;
  float rim = pow(clamp(dot(n, normalize(HOLE - p)), 0.0, 1.0), 2.0) * (1.0 - bevel * 0.6);
  float torchRim = pow(clamp(dot(n, normalize(vec2(0.85, 0.05) - p)), 0.0, 1.0), 3.0) * 0.6;
  vec3 col = gold * (0.35 * light + 0.15) + vec3(1.0, 0.75, 0.35) * torchRim * light * 0.5 + holeLight * rim * 2.6;
  col *= mix(1.0, 0.25, dead * smoothstep(DETONATE + 0.5, DETONATE + 3.0, t));
  float crack = smoothstep(0.035 * damage, 0.0, abs(fbm(q * 22.0) - 0.5)) * damage;
  float crackHeat = dead > 0.5 ? exp(-(t - DETONATE) * 0.6) : 1.0;
  col += vec3(1.0, 0.18, 0.08) * crack * 1.6 * crackHeat;
  vec2 eq = vec2(abs(q.x) - 0.016, q.y - 0.378);
  float eye = smoothstep(0.008, 0.0, length(eq * vec2(1.0, 2.2)));
  vec3 eyeCol = mix(vec3(1.0, 0.85, 0.4), vec3(1.0, 0.1, 0.05), smoothstep(OPEN, OPEN + 2.0, t));
  col += eyeCol * eye * 2.0 * (1.0 - dead);
  return vec4(col, smoothstep(0.004, -0.002, d));
}

// ---------- the caster ----------
float casterSdf(vec2 q, float t) {
  float w = 0.05 - q.y * 0.17;
  float d = max(abs(q.x) - w, abs(q.y - 0.075) - 0.075);
  d = smin(d, length(q - vec2(0.006, 0.168)) - 0.027, 0.012);
  float raise = smoothstep(0.0, 0.5, t) * (1.0 - smoothstep(RELEASE + 0.3, RELEASE + 1.0, t));
  vec2 hand = mix(vec2(0.04, 0.07), vec2(0.055, 0.16), raise);
  vec2 sh = vec2(0.02, 0.13);
  vec2 pa = q - sh, ba = hand - sh;
  float hseg = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
  return smin(d, length(pa - ba * hseg) - 0.011, 0.01);
}

vec4 caster(vec2 p, float t, vec3 holeLight, float light, out vec3 handGlow) {
  float pull = casterPull(t);
  float lean = 0.25 * smoothstep(OPEN + 0.5, INHALE, t) * (t < DETONATE ? 1.0 : 0.0);
  vec2 q = p - vec2(-0.6 + pull, FLOOR_Y);
  q.x -= q.y * lean;
  float d = casterSdf(q, t);
  float charge = smoothstep(0.2, RELEASE, t) * (1.0 - smoothstep(RELEASE, RELEASE + 0.4, t));
  vec2 handPos = vec2(-0.6 + pull + 0.055 + 0.16 * lean, FLOOR_Y + 0.165);
  vec2 hq = p - handPos;
  float hr = length(hq);
  float swirl = fbm(vec2(atan(hq.y, hq.x) * 2.0 + t * 3.0, hr * 30.0 - t * 4.0));
  handGlow = vec3(1.0, 0.08, 0.06) * charge * (0.00024 / (hr * hr + 0.0008)) * (0.6 + swirl);
  handGlow -= vec3(0.45) * charge * smoothstep(0.03, 0.0, hr);
  float smoke = fbm(vec2(hq.x * 18.0, hq.y * 9.0 - t * 2.5)) * smoothstep(0.12, 0.0, length(hq * vec2(1.6, 0.5) - vec2(0.0, 0.05)));
  handGlow -= vec3(0.25, 0.3, 0.3) * smoke * charge;
  if (d > 0.01) return vec4(0.0);
  vec2 e = vec2(0.002, 0.0);
  vec2 n = normalize(vec2(casterSdf(q + e.xy, t) - casterSdf(q - e.xy, t), casterSdf(q + e.yx, t) - casterSdf(q - e.yx, t)) + 1e-5);
  float rim = pow(clamp(dot(n, normalize(HOLE - p)), 0.0, 1.0), 2.0);
  float handRim = pow(clamp(dot(n, normalize(handPos - p)), 0.0, 1.0), 2.0) * charge;
  vec3 robe = vec3(0.05, 0.035, 0.06) * (0.6 + 0.4 * fbm(q * 50.0)) * (0.5 + light);
  vec3 col = robe + holeLight * rim * 2.0 + vec3(1.0, 0.1, 0.06) * handRim * 0.8;
  return vec4(col, smoothstep(0.004, -0.002, d));
}

// ---------- everything that is not the hole itself ----------
vec3 scene(vec2 p, float t, float light, vec3 holeLight, float holeStrength) {
  float isFloor;
  vec3 col = hall(p, t, isFloor) * (0.25 + light);
  vec3 glow = vec3(0.0);
  float alive = 1.0 - smoothstep(INHALE, SILENCE - 0.2, t);
  float lean = 0.5 * holeStrength;
  col += torch(p, vec2(-0.87, 0.04), t, lean, alive, glow);
  col += torch(p, vec2(-0.29, 0.04), t, lean, alive, glow);
  col += torch(p, vec2(0.87, 0.04), t, -lean, alive, glow);
  col += glow * light * 3.0;
  float floorLight = isFloor * holeStrength * 0.35 / (0.15 + abs(p.x - HOLE.x) * 2.0);
  col += holeLight * floorLight * 0.4;
  if (isFloor > 0.5) {
    float streak = smoothstep(0.55, 0.85, fbm(vec2((p.x - HOLE.x) * 6.0 + sign(p.x - HOLE.x) * t * 1.8, p.y * 60.0)));
    col += vec3(0.25, 0.12, 0.1) * streak * holeStrength * 0.4;
  }
  vec4 b = boss(p, t, holeLight, light);
  col = mix(col, b.rgb, b.a);
  vec3 handGlow;
  vec4 c = caster(p, t, holeLight, light, handGlow);
  col = mix(col, c.rgb, c.a);
  return max(col + handGlow, vec3(0.0));
}

// ---------- debris pulled in and shrapnel blown out ----------
vec3 debris(vec2 p, float t, float R, inout float occl) {
  vec3 add = vec3(0.0);
  for (int i = 0; i < 64; i++) {
    float fi = float(i);
    float h1 = hash1(fi + 1.3), h2 = hash1(fi * 2.7 + 4.1), h3 = hash1(fi * 5.1 + 0.7), h4 = hash1(fi * 9.9 + 2.2);
    bool coin = h1 < 0.5;
    vec2 start = vec2(mix(-0.82, 0.86, h2), FLOOR_Y - 0.012 - h3 * 0.13);
    float tc = OPEN + 0.3 + h4 * 7.0;
    float dur = 2.2 + h3 * 2.4;
    float u = clamp((t - tc) / dur, 0.0, 1.0);
    vec2 pos = start;
    float size = coin ? 0.009 : 0.011 + h1 * 0.007;
    if (t > OPEN && t < tc) pos.y += 0.004 * abs(sin(t * 30.0 + fi));
    if (u > 0.0 && t < DETONATE) {
      if (u >= 1.0) continue;
      vec2 d0 = start - HOLE;
      float r0 = length(d0);
      float a0 = atan(d0.y, d0.x);
      float r = mix(r0, R * 0.9, pow(u, 1.3));
      float a = a0 + u * u * (5.0 + h2 * 5.0);
      pos = HOLE + vec2(cos(a), sin(a) * mix(1.0, 0.38, u)) * r;
      size *= 1.0 - 0.6 * u;
    }
    if (t > SILENCE && u > 0.0) continue;
    if (t > DETONATE) {
      float dt = t - DETONATE;
      vec2 dir = normalize(start - HOLE + vec2(0.0, 0.2));
      pos = start + dir * dt * (0.35 + h3 * 0.3) * exp(-dt * 0.8);
      pos.y = max(pos.y, FLOOR_Y - 0.15);
    }
    float m = smoothstep(size, size * 0.4, length(p - pos));
    vec3 c = coin ? vec3(0.95, 0.72, 0.25) * (0.5 + 0.8 * abs(sin(t * 12.0 + fi))) : vec3(0.22, 0.2, 0.2);
    c += vec3(1.0, 0.2, 0.1) * 0.6 * u;
    if (u > 0.0) occl = max(occl, smoothstep(size * 3.0, 0.0, length(p - pos)) * 0.35);
    add = mix(add, c, m);
    occl = max(occl, m);
  }
  if (t > DETONATE) {
    float dt = t - DETONATE;
    vec2 core = vec2(HOLE.x, FLOOR_Y + 0.25);
    for (int j = 0; j < 46; j++) {
      float fj = float(j);
      float a = hash1(fj * 3.3 + 1.0) * 6.2831;
      float sp = 0.25 + hash1(fj * 7.7) * 0.9;
      vec2 pos = core + vec2(cos(a), sin(a) * 0.8 + 0.3) * sp * (1.0 - exp(-dt * 2.2)) * 0.9;
      pos.y -= 0.06 * dt * dt;
      pos.y = max(pos.y, FLOOR_Y - 0.1 * hash1(fj));
      float life = exp(-dt * (0.5 + hash1(fj * 1.9)));
      float sz = 0.004 + 0.006 * hash1(fj * 8.1);
      float m = smoothstep(sz, sz * 0.3, length(p - pos));
      vec3 c = hash1(fj * 4.4) < 0.5 ? vec3(1.0, 0.7, 0.25) : vec3(1.0, 0.25, 0.08);
      add += c * m * life * 1.5;
      occl = max(occl, m * life);
    }
  }
  return add;
}

void main() {
  float t = uT;
  vec2 uv = (gl_FragCoord.xy - 0.5 * uRes) / uRes.y;

  float shakeAmt = 0.0;
  if (t > OPEN && t < OPEN + 0.8) shakeAmt += 0.012 * (1.0 - (t - OPEN) / 0.8);
  if (t > OPEN && t < INHALE) shakeAmt += 0.002 + uBeat * 0.003;
  if (t > DETONATE) shakeAmt += 0.03 * exp(-(t - DETONATE) * 2.5);
  vec2 p = uv + shakeAmt * vec2(noise(vec2(t * 40.0, 0.0)) - 0.5, noise(vec2(0.0, t * 40.0)) - 0.5);

  float R = holeRadius(t);
  float presence = holePresence(t);
  float holeStrength = presence * smoothstep(OPEN, OPEN + 1.0, t) * (1.0 - smoothstep(SILENCE - 0.2, SILENCE, t));
  float light = sceneLight(t);
  vec3 holeLight = vec3(0.9, 0.06, 0.08) * holeStrength * (0.8 + 0.4 * uBeat);

  vec2 d = p - HOLE;
  float r = length(d);

  // The detonation shockwave refracts the world as it passes.
  float wave = 0.0;
  if (t > DETONATE) {
    float s = (t - DETONATE) * 1.5;
    wave = exp(-abs(r - s) * 55.0) * exp(-(t - DETONATE) * 1.2);
    p += normalize(d + 1e-5) * wave * 0.03;
  }

  // Gravitational lensing: light near the hole is bent around it.
  vec2 sp = p;
  if (R > 0.0) {
    float k = 1.6 * R * R;
    sp = HOLE + d * (1.0 - k / max(r * r, R * R * 0.6));
    sp += vec2(-d.y, d.x) / max(r, 0.01) * 0.01 * holeStrength * smoothstep(0.5, 0.0, r);
  }

  vec3 col = scene(sp, t, light, holeLight, holeStrength);

  // A crimson, desaturated world while the hole lives.
  float grey = dot(col, vec3(0.299, 0.587, 0.114));
  col = mix(col, vec3(grey) * vec3(1.15, 0.55, 0.55), 0.55 * holeStrength);

  // The orb, rising from the caster's hands.
  if (t > RELEASE - 0.2 && t < OPEN + 0.3) {
    vec2 op = orbPos(t);
    float od = length(p - op);
    float orbR = 0.026 + 0.006 * sin(t * 14.0) + 0.014 * smoothstep(OPEN - 0.6, OPEN, t);
    float fadeIn = smoothstep(RELEASE - 0.2, RELEASE, t) * (1.0 - smoothstep(OPEN, OPEN + 0.3, t));
    float ang = atan(p.y - op.y, p.x - op.x);
    float flare = fbm(vec2(ang * 3.0 + t * 6.0, od * 60.0 - t * 8.0));
    col += vec3(1.0, 0.08, 0.06) * fadeIn * (0.00045 / (od * od + 0.0005)) * (0.5 + flare);
    col = mix(col, vec3(0.0), fadeIn * smoothstep(orbR * 0.75, orbR * 0.55, od));
    col += vec3(1.0, 0.25, 0.15) * fadeIn * exp(-abs(od - orbR * 0.7) * 400.0) * 1.2;
    float trail = 0.0;
    for (int k = 1; k < 12; k++) {
      vec2 tp = orbPos(t - float(k) * 0.06);
      trail += exp(-length(p - tp) * 180.0) * (1.0 - float(k) / 12.0);
    }
    col += vec3(0.6, 0.03, 0.05) * trail * fadeIn * 0.7;
    col -= vec3(0.08) * trail * fadeIn;
  }

  // Smoke spiralling into the hole.
  if (presence > 0.0 && R > 0.0) {
    float ang = atan(d.y, d.x);
    float logr = log(max(r, 1e-3));
    float spin = t * (t > INHALE ? 3.5 : 0.9);
    float swirl = fbm(vec2(ang * 2.0 + spin - 2.5 * logr, logr * 3.0 + t * (t > INHALE ? 6.0 : 1.5)));
    float reach = smoothstep(0.55, R * 1.2, r) * holeStrength;
    col = mix(col, vec3(0.02, 0.0, 0.01), smoothstep(0.45, 0.8, swirl) * reach * 0.8);
    col += vec3(0.5, 0.02, 0.04) * smoothstep(0.55, 0.9, swirl) * reach * 0.6;
  }

  // Accretion disk: the back half is hidden by the horizon, the front half crosses over it.
  vec2 dq = rot(0.12) * d;
  dq.y /= 0.3;
  float rq = length(dq);
  float Rs = max(R, 1e-3);
  float diskAng = atan(dq.y, dq.x);
  float diskBand = smoothstep(Rs * 1.35, Rs * 1.7, rq) * smoothstep(Rs * 3.6, Rs * 2.0, rq);
  float diskNoise = fbm(vec2(diskAng * 3.0 - t * 2.2 + 4.0 / max(rq / Rs, 0.5), rq / Rs * 2.5));
  float heat = smoothstep(Rs * 3.2, Rs * 1.4, rq);
  vec3 diskCol = mix(vec3(0.55, 0.02, 0.03), vec3(1.0, 0.55, 0.3), heat * heat) * (0.4 + 1.3 * diskNoise);
  float diskA = diskBand * presence * smoothstep(OPEN, OPEN + 0.6, t);
  if (d.y > 0.0) col += diskCol * diskA * 0.9;

  // Event horizon and photon ring.
  if (R > 0.0) {
    col = mix(col, vec3(0.0), smoothstep(R * 1.02, R * 0.96, r));
    float ring = exp(-abs(r - R * 1.08) / (0.0025 + R * 0.02));
    float ringNoise = 0.7 + 0.6 * fbm(vec2(atan(d.y, d.x) * 4.0 + t * 3.0, t));
    col += vec3(1.0, 0.18, 0.12) * ring * ringNoise * (1.2 + uBeat * 0.8) * presence;
    col += vec3(1.0, 0.75, 0.6) * exp(-abs(r - R * 1.08) / 0.0015) * 0.5 * presence;
    col += vec3(0.6, 0.0, 0.03) * (0.0012 / (abs(r - R) + 0.004)) * presence;
  }

  if (d.y <= 0.0) col += diskCol * diskA * 1.1;

  float occl = 0.0;
  vec3 deb = debris(p, t, R, occl);
  col = mix(col, deb, occl * 0.9) + deb * 0.15;

  // Silence: one red pinprick in the dark.
  if (t > INHALE && t < DETONATE) {
    float pin = 0.0006 / (r * r + 0.00004);
    col += vec3(1.0, 0.12, 0.08) * pin * smoothstep(INHALE + 0.4, SILENCE, t) * 0.4;
  }

  // Detonation flash, shockwave, embers, dust and a scorched floor.
  if (t > DETONATE) {
    float dt = t - DETONATE;
    float flash = exp(-dt * 5.0);
    col += mix(vec3(1.0, 0.25, 0.15), vec3(1.0, 0.95, 0.9), exp(-dt * 12.0)) * flash * 1.6;
    col += vec3(1.0, 0.3, 0.12) * wave * 1.4;
    float ember = smoothstep(0.75, 0.95, fbm(p * 9.0 + vec2(0.0, -dt * 0.4))) * exp(-dt * 0.4);
    col += vec3(1.0, 0.3, 0.05) * ember * 0.5;
    float dust = fbm(p * 3.0 + vec2(dt * 0.15, 0.0)) * smoothstep(0.0, 1.0, dt) * exp(-dt * 0.25);
    col = mix(col, vec3(0.16, 0.1, 0.09), dust * 0.35 * smoothstep(0.2, -0.3, p.y));
    float scorch = smoothstep(0.35, 0.0, length((p - vec2(HOLE.x, FLOOR_Y - 0.03)) * vec2(1.0, 4.0)));
    col += vec3(0.6, 0.12, 0.04) * scorch * exp(-dt * 0.35) * 0.4;
  }

  // A vignette that tightens with every heartbeat, film grain, gentle tone map.
  float vig = smoothstep(1.25, 0.25 - uBeat * 0.08 * holeStrength, length(uv * vec2(0.85, 1.1)));
  col *= mix(0.25, 1.0, vig);
  col += (hash(gl_FragCoord.xy + fract(t) * 100.0) - 0.5) * 0.025;
  col = col / (1.0 + col * 0.35);
  col = pow(max(col, 0.0), vec3(0.92));
  gl_FragColor = vec4(col, 1.0);
}
`;

const CSS = `
  .fatum { --bg:#0b0708; --panel:#160e10; --ink:#eadfd2; --muted:#a3958a; --blood:#c0152a; --ember:#ff6a3d; --line:#3a2328;
    background:var(--bg); color:var(--ink); font-family:'Spectral',Georgia,serif; max-width:1100px; margin:0 auto; padding:16px; }
  .fatum * { box-sizing:border-box; }
  .fatum h1 { font-family:'Eczar',Georgia,serif; letter-spacing:.04em; margin:4px 0 2px; font-size:clamp(26px,5vw,40px); }
  .fatum h1 span { color:var(--blood); }
  .fatum .sub { color:var(--muted); margin:0 0 14px; font-size:15px; }
  .fatum .stage { position:relative; width:100%; aspect-ratio:16/9; background:#000; border:1px solid var(--line); border-radius:6px; overflow:hidden; }
  .fatum .stage canvas { position:absolute; inset:0; width:100%; height:100%; display:block; }
  .fatum .controls { display:flex; flex-wrap:wrap; gap:8px; align-items:center; margin-top:10px; }
  .fatum button { font-family:'Overpass Mono',ui-monospace,monospace; font-size:13px; background:var(--panel); color:var(--ink); border:1px solid var(--line); border-radius:4px; padding:7px 11px; cursor:pointer; }
  .fatum button:hover { border-color:var(--blood); }
  .fatum button.on { background:var(--blood); border-color:var(--blood); color:#fff; }
  .fatum .scrub { flex:1 1 260px; display:flex; align-items:center; gap:8px; font-family:'Overpass Mono',monospace; font-size:12px; color:var(--muted); }
  .fatum input[type=range] { flex:1; accent-color:var(--blood); }
  .fatum .phases { display:grid; grid-template-columns:repeat(auto-fit,minmax(190px,1fr)); gap:10px; margin-top:18px; }
  .fatum .phase { background:var(--panel); border:1px solid var(--line); border-left:3px solid var(--blood); border-radius:4px; padding:10px 12px; font-size:14px; line-height:1.45; }
  .fatum .phase b { font-family:'Eczar',Georgia,serif; font-size:16px; display:block; }
  .fatum .phase .when { font-family:'Overpass Mono',monospace; font-size:11px; color:var(--muted); }
  .fatum .phase.active { border-left-color:var(--ember); background:#22120f; }
`;

function buildDom(root, audioSrc) {
  const style = document.createElement('style');
  style.textContent = CSS;
  document.head.appendChild(style);
  document.body.style.margin = '0';
  document.body.style.background = '#0b0708';
  root.className = 'fatum';
  root.innerHTML = `
    <h1><span>FATUM</span> — the Doom spell</h1>
    <p class="sub">A boss-only end-game spell, an homage to Doom in Mage Arena. Press play with sound on.</p>
    <div class="stage"><canvas class="gl" width="1280" height="720"></canvas><canvas class="hud" width="1280" height="720"></canvas></div>
    <div class="controls">
      <button class="play">▶ Play</button>
      <button data-jump="0">Casting</button>
      <button data-jump="7.5">Mid-cast</button>
      <button data-jump="14.2">Finish</button>
      <button class="sound on">Sound on</button>
      <div class="scrub"><input type="range" min="0" max="${T.END}" step="0.01" value="0"><span class="clock">0.00 s</span></div>
    </div>
    <div class="phases"></div>
    <audio preload="auto" src="${audioSrc}"></audio>`;
}

function start(root, options) {
  const opts = Object.assign({ audioSrc: 'doom_spell_full.ogg', width: 1280, height: 720 }, options || {});
  buildDom(root, opts.audioSrc);
  const glCanvas = root.querySelector('canvas.gl');
  const hud = root.querySelector('canvas.hud');
  glCanvas.width = hud.width = opts.width;
  glCanvas.height = hud.height = opts.height;
  const ctx = hud.getContext('2d');
  const gl = glCanvas.getContext('webgl', { preserveDrawingBuffer: true, antialias: false });
  if (!gl) throw new Error('WebGL is not available in this browser, so the prototype cannot draw.');

  function compile(type, src) {
    const s = gl.createShader(type);
    gl.shaderSource(s, src);
    gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error('Shader failed to compile: ' + gl.getShaderInfoLog(s));
    return s;
  }
  const program = gl.createProgram();
  gl.attachShader(program, compile(gl.VERTEX_SHADER, 'attribute vec2 a;void main(){gl_Position=vec4(a,0.,1.);}'));
  gl.attachShader(program, compile(gl.FRAGMENT_SHADER, FRAGMENT_SHADER));
  gl.linkProgram(program);
  if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error('Shader failed to link: ' + gl.getProgramInfoLog(program));
  gl.useProgram(program);
  gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
  const loc = gl.getAttribLocation(program, 'a');
  gl.enableVertexAttribArray(loc);
  gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);
  const uRes = gl.getUniformLocation(program, 'uRes');
  const uT = gl.getUniformLocation(program, 'uT');
  const uBeat = gl.getUniformLocation(program, 'uBeat');

  const beatPulse = t => Math.min(1, BEATS.reduce((v, b) => v + (t >= b ? Math.exp(-(t - b) * 7) : 0), 0));

  function bossHp(t) {
    if (t < T.OPEN) return BOSS_START_HP;
    if (t < T.DETONATE) {
      const ticks = HOLE_BEATS.filter(b => t >= b).length;
      return BOSS_START_HP - (BOSS_START_HP - BOSS_HP_BEFORE_BLAST) * ticks / HOLE_BEATS.length;
    }
    return Math.max(0, BOSS_HP_BEFORE_BLAST * (1 - (t - T.DETONATE) / 0.35));
  }

  // Shader coordinates (y up, height 1) to HUD pixels.
  const toScreen = (x, y) => [hud.width / 2 + x * hud.height, hud.height / 2 - y * hud.height];
  const px = n => n * hud.height / 720;

  function drawHud(t) {
    const W = hud.width, H = hud.height;
    ctx.clearRect(0, 0, W, H);
    const dim = t > T.INHALE && t < T.DETONATE ? 0.35 : 1;

    ctx.globalAlpha = dim;
    const bw = W * 0.5, bx = (W - bw) / 2, by = px(46);
    ctx.font = `600 ${px(22)}px Eczar, Georgia, serif`;
    ctx.textAlign = 'center';
    ctx.fillStyle = '#eadfd2';
    ctx.fillText(t > T.DETONATE + 0.4 ? 'THE GILDED COLOSSUS — DOOMED' : 'THE GILDED COLOSSUS', W / 2, by - px(12));
    ctx.fillStyle = 'rgba(0,0,0,.6)';
    ctx.fillRect(bx - 3, by - 3, bw + 6, px(14) + 6);
    const hp = bossHp(t);
    ctx.fillStyle = '#5a1018';
    if (hp > 0) ctx.fillRect(bx, by, bw * Math.min(BOSS_START_HP, hp + 0.04), px(14));
    ctx.fillStyle = t > T.OPEN ? '#c0152a' : '#d9a441';
    ctx.fillRect(bx, by, bw * hp, px(14));
    ctx.strokeStyle = '#d9a441';
    ctx.lineWidth = 1.5;
    ctx.strokeRect(bx - 3, by - 3, bw + 6, px(14) + 6);

    const [hx, hy] = toScreen(0.30, 0.10);
    HOLE_BEATS.forEach((b, i) => {
      const age = t - b;
      if (age < 0 || age > 1.1 || t > T.SILENCE) return;
      ctx.globalAlpha = Math.max(0, 1 - age / 1.1) * dim;
      ctx.font = `700 ${px(30 + i * 2)}px Eczar, Georgia, serif`;
      ctx.fillStyle = '#ff6a3d';
      ctx.fillText(`-${380 + i * 45}`, hx + px(((i * 37) % 9 - 4) * 14), hy - px(age * 70));
    });
    if (t > T.DETONATE && t < T.DETONATE + 3.2) {
      const age = t - T.DETONATE;
      ctx.globalAlpha = Math.min(1, 3.2 - age);
      ctx.font = `700 ${px(64 + 40 * Math.exp(-age * 6))}px Eczar, Georgia, serif`;
      ctx.lineWidth = px(6);
      ctx.strokeStyle = 'rgba(0,0,0,.8)';
      const y = hy - px(40 + age * 20);
      ctx.strokeText('-9,999  DOOM', hx, y);
      ctx.fillStyle = '#ff2a2a';
      ctx.fillText('-9,999  DOOM', hx, y);
    }

    if (t > 0.3 && t < 3.4) {
      const word = 'FATUM';
      const shown = word.slice(0, Math.min(word.length, Math.floor((t - 0.3) / 0.28) + 1));
      ctx.globalAlpha = Math.min(1, (3.4 - t) * 2);
      ctx.font = `700 ${px(46)}px Eczar, Georgia, serif`;
      ctx.fillStyle = '#c0152a';
      ctx.lineWidth = px(5);
      ctx.strokeStyle = 'rgba(0,0,0,.85)';
      const [cx, cy] = toScreen(-0.6, 0.02);
      ctx.strokeText(`“${shown}”`, cx, cy);
      ctx.fillText(`“${shown}”`, cx, cy);
    }

    const i = PHASES.findLastIndex(ph => t >= ph.from);
    ctx.globalAlpha = 0.9;
    ctx.textAlign = 'left';
    ctx.font = `${px(13)}px "Overpass Mono", monospace`;
    ctx.fillStyle = '#a3958a';
    ctx.fillText(`${i + 1} / ${PHASES.length}`, px(28), H - px(52));
    ctx.font = `600 ${px(26)}px Eczar, Georgia, serif`;
    ctx.fillStyle = '#eadfd2';
    ctx.fillText(PHASES[i].name, px(28), H - px(24));
    if (t > T.OPEN + 0.5 && t < T.INHALE) {
      ctx.textAlign = 'right';
      ctx.font = `${px(17)}px Spectral, Georgia, serif`;
      ctx.fillStyle = '#ff9a7a';
      ctx.fillText('You are being pulled in. Get off the ground.', W - px(28), H - px(24));
    }
    ctx.globalAlpha = 1;
  }

  function render(t) {
    gl.viewport(0, 0, glCanvas.width, glCanvas.height);
    gl.uniform2f(uRes, glCanvas.width, glCanvas.height);
    gl.uniform1f(uT, t);
    gl.uniform1f(uBeat, beatPulse(t));
    gl.drawArrays(gl.TRIANGLES, 0, 3);
    drawHud(t);
  }

  const audio = root.querySelector('audio');
  const playBtn = root.querySelector('.play');
  const soundBtn = root.querySelector('.sound');
  const scrub = root.querySelector('input[type=range]');
  const clock = root.querySelector('.clock');
  const phaseList = root.querySelector('.phases');
  phaseList.innerHTML = PHASES.map((ph, i) => {
    const to = (PHASES[i + 1] || { from: T.END }).from;
    return `<div class="phase"><span class="when">${ph.from.toFixed(1)}–${to.toFixed(1)} s</span><b>${i + 1}. ${ph.name}</b>${ph.text}</div>`;
  }).join('');

  let time = 0, playing = false, soundOn = true, lastFrame = 0;

  function setTime(t) {
    time = Math.max(0, Math.min(T.END, t));
    scrub.value = time;
    clock.textContent = `${time.toFixed(2)} s`;
    const i = PHASES.findLastIndex(ph => time >= ph.from);
    phaseList.querySelectorAll('.phase').forEach((el, k) => el.classList.toggle('active', k === i));
    render(time);
  }
  function play() {
    if (time >= T.END - 0.05) setTime(0);
    playing = true;
    playBtn.textContent = '❚❚ Pause';
    if (soundOn) { audio.currentTime = time; audio.play().catch(err => console.warn('Sound did not start:', err)); }
    lastFrame = performance.now();
    requestAnimationFrame(tick);
  }
  function pause() {
    playing = false;
    playBtn.textContent = '▶ Play';
    audio.pause();
  }
  function tick(now) {
    if (!playing) return;
    const followAudio = soundOn && !audio.paused && !audio.ended;
    setTime(followAudio ? audio.currentTime : time + (now - lastFrame) / 1000);
    lastFrame = now;
    if (time >= T.END) { pause(); return; }
    requestAnimationFrame(tick);
  }

  playBtn.onclick = () => (playing ? pause() : play());
  soundBtn.onclick = () => {
    soundOn = !soundOn;
    soundBtn.classList.toggle('on', soundOn);
    soundBtn.textContent = soundOn ? 'Sound on' : 'Sound off';
    if (!soundOn) audio.pause();
    else if (playing) { audio.currentTime = time; audio.play().catch(err => console.warn('Sound did not start:', err)); }
  };
  scrub.oninput = () => { setTime(parseFloat(scrub.value)); if (playing && soundOn) audio.currentTime = time; };
  root.querySelectorAll('[data-jump]').forEach(b => { b.onclick = () => { setTime(parseFloat(b.dataset.jump)); play(); }; });

  // capture.js calls this to draw one exact moment and read both layers back as one image.
  window.renderAt = t => { setTime(t); return true; };
  window.frameAt = (t, type, quality) => {
    setTime(t);
    const out = document.createElement('canvas');
    out.width = glCanvas.width; out.height = glCanvas.height;
    const c = out.getContext('2d');
    c.drawImage(glCanvas, 0, 0);
    c.drawImage(hud, 0, 0);
    return out.toDataURL(type || 'image/png', quality);
  };
  window.FATUM_TIMELINE = TIMELINE;
  setTime(0);
}

window.Fatum = { start, TIMELINE };
const auto = document.getElementById('fatum');
if (auto) start(auto);
})();
