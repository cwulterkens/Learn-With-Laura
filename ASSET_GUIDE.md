# Learn With Laura Asset Guide

## Chosen visual direction

Learn With Laura uses a warm, painterly storybook style grounded in late-19th-century Midwestern materials, light, and environments. The current 1536x1024 classroom, subject, menu, and Laura's Trail backgrounds establish the target look. Pixel art, flat geometric sprites, anime-influenced characters, photorealism, and direct imitation of television-production imagery are outside this direction.

Artwork should feel inviting to a young learner without presenting invention as historical evidence. Educational labels, dates, equations, map names, and other factual text belong in authored UI, not inside generated images.

## Technical standards

### Backgrounds

- Author at 3072x2048 or larger when practical; export at 1536x1024.
- Export as 8-bit sRGB RGB PNG unless transparency is required.
- Preserve the 3:2 composition and test at the project's supported aspect ratios.
- Keep the central content zone calm and readable. Place distinctive props and landmarks mainly in the side thirds.
- Do not bake text, logos, answer clues, people, or modern objects into reusable lesson backgrounds.
- Use warm natural light, walnut and honey wood, parchment neutrals, prairie gold, muted pine green, and restrained blue accents.
- Avoid malformed tools, impossible architecture, duplicated objects, illegible pseudo-writing, or maps that imply false geographic facts.

### Characters and interactive objects

- Character cutouts: 512x768 RGBA PNG, transparent background, consistent scale, age, clothing, light direction, and facial design across poses.
- Reward and subject icons: 256x256 RGBA PNG.
- HUD and supply icons: 96x96 RGBA PNG.
- Interactive objects must have clean silhouettes, generous transparent hit padding, and recognizable shapes at half-size.
- Maintain a character model sheet before generating additional Laura, Mary, Ma, Pa, Carrie, or other recurring figures.

### User interface

- Nine-slice button textures: 560x96 RGBA PNG using the current 24-pixel horizontal and 18-pixel vertical slice margins.
- Provide normal, hover, pressed, disabled, and keyboard-focus states. Focus must remain clearly visible at 100% and 200% scale.
- Keep labels as live text for readability, localization, and assistive technology.
- Use bundled Atkinson Hyperlegible Regular for primary UI text and Bold for titles rather than relying on operating-system fallbacks.
- Verify ordinary text at WCAG 2.1 AA contrast and never communicate correct, incorrect, locked, or completed state by color alone.

### Audio

- Active music WAV imports use `edit/loop_mode=1`, `edit/loop_begin=0`, and `edit/loop_end=-1` for full-file forward looping.
- Confirm every music seam by listening on headphones after import; loop configuration cannot repair a poor source seam.
- Preserve the existing Music and SFX volume controls and avoid clipping.
- Short UI and answer cues should remain distinct, gentle, and intelligible at low volume.
- Do not add voice, music, or effects without recording or generation provenance and release terms in `assets/ASSET_MANIFEST.csv`.

## Asset status

`assets/ASSET_MANIFEST.csv` is the authoritative per-file inventory. Its Godot import mode is intentionally set to `Skip File (not exported)` because ordinary CSV files are otherwise interpreted as localization tables. Godot-generated `.import` files are metadata and are not listed as source assets.

### Active painterly and interface set

- Branding and splash: `assets/app_icon.png`, `assets/logo.png`, `assets/wulterkens_credits.png`.
- Shared environments: `assets/menu_wallpaper.png`, `assets/classroom.png`, `assets/science_lab_bg.png`, `assets/social_studies_bg.png`.
- Laura's Trail environments: `assets/laura_big_woods.png`, `assets/laura_prairie.png`, `assets/laura_plum_creek.png`, `assets/laura_schoolhouse.png`, `assets/laura_town.png`.
- Interface: the four files under `assets/ui/`.
- Typography: `assets/fonts/AtkinsonHyperlegible-Regular.otf` for primary UI text and `assets/fonts/AtkinsonHyperlegible-Bold.otf` for titles, distributed under SIL Open Font License 1.1 in `assets/fonts/OFL.txt`.
- Music: `assets/mainMenu_loop.wav`, `assets/menu_music.wav`, `assets/classroom_music.wav`, `assets/recess_music.wav`, `assets/laura_trail_theme.wav`.
- Wired effects: `assets/sfx_success.wav`, `assets/sfx_wrong.wav`, and `assets/sfx_award.wav` are used by both learning modes; `assets/sfx_select.wav` supplies distinct pitched clues for Laura's Trail memory patterns.

### Active generated subject backgrounds

The following generated images match the chosen style and are used for subject-specific lessons:

- `assets/math_classroom_bg.png`
- `assets/english_reading_bg.png`
- `assets/history_study_bg.png`
- `assets/geography_map_bg.png`

Their tool, date, output identifiers, reference image, and concise prompts are recorded in the manifest.

### Active branded application icon

`assets/app_icon.png` is a deterministic local crop and resize of `assets/logo.png` and is configured as the application icon. It inherits the logo's unknown rights status and still requires the same release-rights review as its source. The root `icon.svg` is an inactive stock Godot placeholder.

### Legacy or quarantine set

Do not add these to new scenes without an explicit art and rights review:

- All root-level `assets/button_*.png` files: incompatible baked-text pixel styles.
- `assets/teacher.png`: incompatible opaque-background pixel/anime style.
- `assets/family_sprite_sheet.png`: placeholder figures without usable animation.
- `assets/award_ribbons.png`: placeholder art that does not cover the badge catalog.
- `assets/wulterkens_credits.mov`: redundant, unused derivative of the active PNG splash.
- `assets/learn_with_laura_menu_theme.mid`: unimported source with undocumented authorship.

The root `icon.svg` is an inactive stock Godot placeholder and must not be restored as release branding.

## Historical and television-source boundaries

- Record whether each depiction is based on documented life, the Little House books, the television adaptation, or original frontier-themed invention.
- Do not merge television-only events or visual details into biographical claims about Laura Ingalls Wilder.
- Generated backgrounds are atmosphere, not primary historical sources. Verify buildings, clothing, tools, maps, dates, and place-specific details against documented references before attaching factual captions.
- Do not use television screenshots, promotional photography, actor likenesses, copied show logos, recognizable production designs, or show music without documented permission or a completed rights review.
- Avoid using a living person's likeness or an identifiable modern property as an uncredited visual reference.
- Preserve citations for factual source material separately from the asset's copyright or generation provenance; these are different review questions.

## Provenance and review workflow

For every new or replaced source asset:

1. Add the file to `assets/ASSET_MANIFEST.csv` in the same change.
2. Record the human creator or generation tool, date, output identifier, concise prompt or source URL, reference images, license or terms, and intended runtime status.
3. Mark unknown information as `Unknown`; do not infer ownership from file metadata, a download source, or the absence of a copyright notice.
4. Complete visual QA at native size for anatomy, object integrity, period details, readable negative space, and style consistency.
5. Complete rights and historical review before changing a file from pending or quarantine to active.
6. Test the imported asset in Godot at every supported resolution, including keyboard focus and muted-audio behavior where applicable.

Generated output identifiers document provenance but do not by themselves replace a release-rights review.

Atkinson Hyperlegible is the only third-party package currently documented as rights-cleared in this guide. Its attribution is recorded in `THIRD_PARTY_NOTICES.md`; this statement does not establish provenance for any other asset.
