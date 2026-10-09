# 葛行舟｜二维汤店资源来源

日期：2026-10-09。使用内置 image_gen；不是CLI/API后备。素材用于已批准汤店样板，不是商用权利完备认证。上线前仍须审核肖像、内容、平台要求与适用许可。

原PNG与透明通道保留；Godot AtlasTexture仅选择显示区域，不修改源像素。尺寸、哈希和接触点记录在docs/project/2026-10-09_探索资源记录.json及assets/manifest.json。运行场景只引用工程内assets/art/vs01-v4的正式文件。

## soup-bg

提示（原文）：

Use case: precise-object-edit. Asset type: production 2D Godot narrative game background, soup-background.png. Input image 1 is the edit target and warm painterly style reference (existing soup shop). Preserve the worn Jiangsu neighborhood duck-blood vermicelli soup shop walls, blue lower ceramic tiles, empty old wooden sign at left wall, blank framed menu near center-right, wall fan, hanging lamps, exposed pipes and warm autumn light through open doorway on the far left. Recompose as a clean 16:9 landscape room, ideally 1920x1080, reference world 960x540. Critical: back wall meets the floor along y=360 of 540; bottom THIRD is an open, continuous walkable stone floor showing moderate depth, viewed orthographically with slight elevated frontal viewpoint, not deep converging perspective. Remove ALL floor furniture from this background: dining table, chairs, stools, crates, potted plants, cooking counter, equipment shelves, all bowls and freestanding kitchen furniture. They will be separate sprites, so no duplicates or baked obstacles anywhere on floor. Keep upper-wall ventilation hood and pipes. Leave right room area for separate counter, central floor for small table/chairs. Door opening can show warm quiet street but no characters. No people, no text, no logos, no watermark, no UI. Match existing detailed hand-painted semi-realistic illustration, muted beige, smoky teal, warm amber daylight. Maintain scene as ordinary humble shop, not fancy cafe.

## soup-table

提示（原文）：

Use case: stylized-concept. Asset type: production transparent 2D game prop soup-table.png. Primary request: one modest low rectangular wooden dining table from an ordinary Jiangsu duck-blood vermicelli soup shop. Warm painterly semi-realistic raster illustration, smoky brown wood with worn edges, subtle fine brush texture, gold sunlight from upper left, same humble autumn neighborhood atmosphere. Composition: single table centered, frontal orthographic view with a slightly elevated view of tabletop, almost horizontal front tabletop edge; four short ordinary legs. Table is low normal seated dining height, not a tall bar table. Width about 2.5 times total height. Intended world placement is width110,height46, tabletop at y389 and legs bottom435 in a960x540 game with96px standing people. No stools or chairs, no dishes or tabletop objects, no people, no environment, no text, logos or watermark. Entire table fully visible including legs with generous transparent margins. Genuinely transparent background outside wooden table and between legs, not white or black, no baked floor rectangle or broad drop shadow.

## soup-chair

提示（原文）：

Use case: stylized-concept. Asset type: production 2D transparent game prop soup-chair.png. One humble ordinary low wooden soup-shop chair, no upholstery, worn warm brown wood, simple flat wooden seat and low backrest, four sturdy legs. Frontal orthographic slightly elevated viewpoint matched to a side-on 2D narrative room. Total height approximately50 world pixels and seat height30 above ground for a96px standing human; NOT bar stool, NOT high chair, NOT ornate. Slim seat width26-30 world pixels, short normal chair legs. Single centered complete chair, no table, people, environment, objects, text, logos or watermark. Detailed warm hand-painted semi-realistic illustration with subtle small-scale wood wear, upper-left warm amber daylight and muted shadows. Genuinely transparent background and gaps between legs, generous margins, no floor or large shadow.

## soup-counter

提示（原文）：

Use case: stylized-concept. Asset type: transparent 2D game prop soup-counter.png. A single simple broad freestanding stainless-steel serving counter from a humble Jiangsu duck-blood vermicelli soup shop, worn light gray metal panels with modest warm grime, two soup cooking pots, stack of white ceramic bowls, chopstick holder on top. Frontal orthographic slightly elevated view, upper-left warm amber autumn light, detailed hand-painted semi-realistic game illustration matched to worn beige walls and smoky teal tile. Wide counter approximately 2.4 times total height, normal waist-height of a96px adult when placed in world. No wall, no floor, no vent hood, no surrounding tables/chairs, no people, no labels, no text/logos/watermark. Completely isolated whole counter, including bottom, generous transparent margins. Genuine transparent background, no black or white background, no rectangle shadow.

## soup-pot

提示（原文）：

Use case: stylized-concept. Asset type: one transparent 2D game prop soup-pot.png. A modest terracotta flowerpot containing a bushy small green leafy indoor plant in a humble Jiangsu soup shop. Not cooking pot. Dense but readable leaves, worn brown earthenware with subtle cracks, upper-left warm amber autumn sunlight, muted teal-green leaves, warm painterly semi-realistic raster illustration, fine illustrative texture. Frontal slightly elevated orthographic view, fully visible single pot and plant, height about60px in a960x540 world with96px adults, pot footprint30px wide. Genuine transparent background, generous margins, no floor, large shadow, furniture, people, text, logos or watermark.


## cannon-seated

参考：assets/art/vs01-v1/npcs-v1.png，仅项目内既有角色身份/风格，不使用现实照片。

提示（原文）：

Use case: identity-preserve and background-extraction. Asset type: production transparent 2D game character cannon-seated.png. Input image1 is identity/pose reference: preserve ONLY the young man in beige jacket seated in the BOTTOM LEFT of the reference sheet, Zhang Dapao. Make one isolated full-body sprite of that exact man in that same relaxed seated pose, facing LEFT (toward protagonist), friendly confident ordinary adult, short tousled black hair, beige jacket over off-white shirt, dark gray jeans, black-and-white canvas shoes, arms resting naturally on thighs. Preserve adult realistic proportions, outfit, facial identity, illustration style and warm light. Remove the wooden chair/stool entirely from the sprite; leave the legs and soles fully intact with knees bent90degrees and soles naturally on floor, butt supported by an INVISIBLE low ordinary chair. No crouching on floor, no high bar stool pose. Genuine transparent background, no floor shadow rectangle, no other characters/chair/table/bowl, no text/logos/watermark. Single large centered complete character with generous margins, warm detailed hand-painted semi-realistic raster style. In game standing-equivalent height96px, seated head-to-foot about75px, butt30px above floor.

## player-depth

参考：assets/art/vs01-v1/player-v1.png，仅项目内既有角色身份/风格，不使用现实照片。

提示（原文）：

Use case: identity-preserve. Asset type: production transparent 2D character animation sprite sheet player-depth.png for Godot. Input image1 is protagonist identity/outfit/style anchor: same adult Chinese young man, messy black hair tied short low ponytail, dark worn denim jacket over black vest, dark jeans with silver wallet chain, black Martin boots, understated rock necklace, slightly down-and-out casual rock mood. Preserve this exact approved identity, clothes and body proportions; no redesign, no younger/chibi look. Produce ONE sprite sheet with exactly12 distinct full-body frames in a perfectly spaced4 COLUMNS x3 ROWS grid, ideally1536x1536, each equal cell with consistent head height, body width and sole baseline, generous transparent padding. ROW1: side view facing RIGHT, frames idle/left-leg-forward stride/passing stride/right-leg-forward stride. ROW2: FRONT toward viewer, frames idle/left-step/passing/right-step. ROW3: BACK facing away, frames idle/left-step/passing/right-step. Natural modest walking motion, alternating arms and feet; feet clearly leave different silhouettes across the cycle. Orthographic front/back/side 2D view, NOT isometric, same standing equivalent scale in all12 frames. Do not put more than12 people/frames, no partial bodies or overlapping cells. Warm hand-painted semi-realistic raster illustration consistent with anchor, restrained fine detail readable at96px character height in game. Genuine TRANSPARENT background outside all figures and between limbs, no floor/smoke/shadows/chair/props/text/gridlines/labels/logos/watermark. Each figure entirely visible including ponytail and boot soles.


## soup-table-front

提示（原文）：

Use case: precise-object-edit
Asset type: transparent front occlusion strip for an editable Godot 2D narrative game.
Input image 1 is the matching wooden table reference. Keep its exact worn warm-brown wood, perspective, golden edge lighting and front board identity. Primary request: isolate ONLY the narrow front horizontal lip and apron board of this table on a genuinely transparent background; absolutely no tabletop plane, no legs, no other objects. The strip is long and thin, a shallow curved bevel at the top plus the front plank. Render the strip centered, approximately 10:1 width to height with transparent padding. This is a separate foreground sprite that covers seated waist/upper legs, not heads. Pixel-inspired semi-realistic raster painting matching the reference; no text, watermark, shadows outside the wood, wall or floor.

## 参考及测量

## player-soup 与 player-soup-phone（2026-10-09）

模式：built-in image_gen，透明背景；参考为本项目player-depth.png，未使用真人照片。原始生成PNG像素不变，以AtlasTexture选取完整帧；总图末行截断的手机帧不使用，另生成手机三帧。起身按完整坐下关键帧逆序播放，减少动效复用关键帧、缩短过渡。坐稳臀部世界Y400、脚底约Y430；手机图以独立原生比例校准，家具与人物接触仍需正常速度真人验收。

player-soup提示原文：

Use case: stylized-concept. Image 1 identity reference for SAME male rock musician: black tied-back shaggy hair, dark worn denim jacket, black vest, jeans and wallet chain, black Martin boots. Production transparent 2D GAME SPRITES, square sheet EXACTLY FOUR equal columns and FOUR equal rows, 16 isolated figures. Camera fixed side view facing right. Painted pixel-textured realism matching reference. No furniture, no shadows, no text, no grid drawn. Each cell needs wide transparent padding; no body part touches or overlaps another cell. Keep physical body size identical in all frames. Upright height 90% of cell; seated head-to-foot height around 80% of upright, not miniature. Seated knees bend at 90 degrees, thigh level, shins vertical and LONG enough: butt-to-sole distance 32% of upright head-to-sole height, boots directly below knees. NO tucked or scrunched short legs. Boots on SAME 95% cell-height baseline in EVERY cell, including seated. Row1 four sit-down frames: upright, knee bend/lean forward, hips halfway down, final fully seated naturally. Row2 four eating frames seated, hand supports a bowl, utensil lifts to mouth then lowers. Row3 four setting-chopsticks frames seated, lift chopsticks, extend arm right to table-height, release, return arm. Row4 four checking-phone frames seated, reach pocket, lift phone, look at glowing screen, lower it. No extra figures, no cropped heads or feet. Genuine transparent alpha background.

player-soup-phone提示原文：

Use case: stylized-concept. Image 1 is approved identity reference. Production GAME ANIMATION atlas of SAME Chinese rock musician black tied hair, dark worn denim jacket, black vest, jeans wallet chain, Martin boots. Exactly THREE full-body SEATED phone-check poses in ONE horizontal row. Wide landscape 3:2 canvas. Each figure isolated on genuinely transparent alpha with generous gaps, fully visible head and boots, nothing cropped. Match painted pixel-realistic art. Side view facing right. Figure1 hand reaching jacket pocket. Figure2 both hands holding old phone and looking down at a softly glowing screen. Figure3 phone lowering toward pocket, ready to return to relaxed seated. Ordinary invisible LOW CHAIR: horizontal thighs, knees90°, vertical shins, boot soles flat beneath knees. Long lower legs: butt-to-sole 32% of the character's hypothetical upright height. Do NOT include any chair, table, background, floor, ground shadow, text or watermark. Identical physical body proportions and head size across poses, exact same sole baseline, generous transparent top and bottom margin. Three figures only, one row, no other poses.

实际尺寸/hash见探索资源记录。素材专为项目生成，商用前仍须审核人物、品牌与生成素材权益；不承诺完整商用许可。

背景参考既有soup-v1.png；人物参考既有player-v1.png与npcs-v1.png；桌沿参考本轮soup-table.png。未使用外部免费素材或真人照片。

八个PNG的实际尺寸、SHA256、使用方式见探索资源记录。主角整张源图保持透明，显示区域为原生AtlasTexture；脚底测量使用alpha>=26，统一scale=96/400，不随场景Y位置缩放。张大炮坐姿在场景里水平翻转朝向桌面。家具可微调非等比尺寸以配合占位；人物不非等比拉伸。


