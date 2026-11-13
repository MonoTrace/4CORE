# PIXELFX sheets for 4CORE

Effects are made with [pixelfx](https://github.com/patch-gg/pixelfx) (MIT) and baked into sprite sheets in
`Assets/Externals/PixelFX/Resources/PixelFX/` (`<key>.png` + `<key>.json`). The C# side loads them by key
(`PixelFx.Keys`), so to recolor, swap or add an effect, edit the clip list in `bake.mjs` and rebake:

    node PixelFXTools/bake.mjs ../pixelfx Assets/Externals/PixelFX/Resources/PixelFX [preview-folder]

| key | pixelfx effect | used for |
|---|---|---|
| core-break | explosions/plasma-explosion (red) | a core cell is destroyed |
| core-recover | pickups/collect-burst (red) | a core cell comes back |
| core-link | arcane/arcane-beam (grey, cropped) | texture of the core-to-player links, tinted red to blue |
| shield-break | impacts/parry (blue) | an enemy breaks a shield |
| missile-reflect | impacts/shield-block (blue) | a shield reflects a missile |
| player-hit | lightning/zap-hit (blue) | the player loses energy |
| missile / missile-reflected | projectiles/plasma-shot (magenta / blue) | the missile in flight |
| enemy-defeat-&lt;type&gt; | spawn/poof (enemy color) | an enemy is defeated |
| score-&lt;base&gt;x&lt;cores&gt; | ui/damage-number (gold) | world score popup |
| score-gain | pickups/collect-burst (gold) | HUD score goes up |

Textures under `Assets/Externals/PixelFX` are imported point-filtered and uncompressed by `PixelFxImport`
(menu: 4CORE > PixelFX > Reimport Sheets).
