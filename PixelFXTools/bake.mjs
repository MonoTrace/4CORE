// Bakes 4CORE's PIXELFX clips into sprite sheets (PNG + Aseprite-style JSON) for Unity.
//
//   git clone https://github.com/patch-gg/pixelfx ../pixelfx
//   node PixelFXTools/bake.mjs ../pixelfx Assets/Externals/PixelFX/Resources/PixelFX
//
// Each clip is [key, pixelfx effect id, options, crop?]. Keys are referenced from PixelFx.Keys in C#.
import { join } from "node:path";
import { mkdirSync, writeFileSync } from "node:fs";
const ROOTFX = process.argv[2], OUT = process.argv[3];
const { loadEffect, renderFrames, spriteSheet, contactSheet, save } = await import(join(ROOTFX, "tools/lib/fxlib.mjs"));
const { PAL } = await import(join(ROOTFX, "src/core/color.js"));

const CORE = ["#160306", "#420812", "#7a0c1c", "#c0142a", "#ff1a26", "#ff6a6a", "#ffbcbc", "#ffffff"];
const ENEMY = {
  straight:   ["#1a0408", "#4a0a14", "#8c1426", "#d4283c", "#ff3850", "#ff7a86", "#ffc4ca", "#ffffff"],
  diagonal:   ["#1e0c04", "#4a1e08", "#8a3a0c", "#d46410", "#ff7a1f", "#ffa860", "#ffd8b0", "#ffffff"],
  rightangle: ["#04180a", "#0a3a16", "#126628", "#1fa040", "#33ff6b", "#80ff9e", "#c8ffd6", "#ffffff"],
  zigzag:     ["#1e1604", "#4a3a08", "#8a6c0c", "#d4a816", "#ffe029", "#fff07a", "#fff8c4", "#ffffff"],
  missile:    PAL.neon,
};
const GREY = ["#101010", "#303030", "#505050", "#787878", "#a0a0a0", "#c8c8c8", "#e8e8e8", "#ffffff"];

const clips = [
  ["core-break", "explosions/plasma-explosion", { palette: CORE, accent: PAL.neon }],
  ["core-recover", "pickups/collect-burst", { palette: CORE }],
  ["shield-break", "impacts/parry", { palette: PAL.electric }],
  ["missile-reflect", "impacts/shield-block", { palette: PAL.electric }],
  ["player-hit", "lightning/zap-hit", { palette: PAL.electric }],
  ["missile", "projectiles/plasma-shot", { palette: PAL.neon }],
  ["missile-reflected", "projectiles/plasma-shot", { palette: PAL.electric }],
  // Greyscale so CoreLinkView can tint it red-to-blue; cropped to the beam body (no source flare or end spray)
  // and used as the LineRenderer's texture.
  ["core-link", "arcane/arcane-beam", { palette: GREY }, { x: 54, y: 46, w: 128, h: 28 }],
  ["score-gain", "pickups/collect-burst", { palette: PAL.gold }],
  ...Object.entries(ENEMY).map(([k, pal]) => [`enemy-defeat-${k}`, "spawn/poof", { palette: pal }]),
];
for (const base of [10, 20, 30, 40]) for (let cores = 1; cores <= 4; cores++)
  clips.push([`score-${base}x${cores}`, "ui/damage-number", { palette: PAL.gold, text: `${base}x${cores}` }]);

const PREVIEW = process.argv[4];   // optional folder for contact-sheet previews
if (PREVIEW) mkdirSync(PREVIEW, { recursive: true });
mkdirSync(OUT, { recursive: true });
function crop(fx, r, c) {
  const frames = r.frames.map((f) => {
    const out = new Uint8ClampedArray(c.w * c.h * 4);
    for (let y = 0; y < c.h; y++) out.set(f.clear.subarray(((c.y + y) * r.w + c.x) * 4, ((c.y + y) * r.w + c.x + c.w) * 4), y * c.w * 4);
    return { ...f, clear: out, color: out };
  });
  const meta = { ...fx.meta, origin: [0, c.h >> 1] };
  return [{ ...fx, meta, mod: { ...fx.mod, act: undefined } }, { ...r, w: c.w, h: c.h, frames }];
}

for (const [key, id, opts, cut] of clips) {
  let fx = await loadEffect(id);
  let r = renderFrames(fx, { actor: false, bg: "none", options: opts });
  if (cut) [fx, r] = crop(fx, r, cut);
  const s = spriteSheet(fx, r, key);
  save(join(OUT, `${key}.png`), s.png);
  writeFileSync(join(OUT, `${key}.json`), JSON.stringify(s.json));
  if (PREVIEW && !cut) save(join(PREVIEW, `${key}.png`), contactSheet(fx, renderFrames(fx, { actor: false, bg: "dark", options: opts }), { max: 12 }));
  console.log(key, id, r.n, "frames", `${r.w}x${r.h}`);
}
