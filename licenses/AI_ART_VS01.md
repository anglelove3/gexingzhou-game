# VS01 AI美术来源记录

日期：2026-10-04。工具：Codex内置image_gen；工具没有返回具体模型名称或版本，不声称调用了某个已知“最新模型”。本文件是来源记录，不是CC0或独占版权授权书。

用户明确允许AI生图，并确认本轮首段美术替换方向。仅引用已有虚构男主造型图和本轮生成的虚构图；未上传女主真人照片、家庭资料或故事DOCX。三处场景为原创生成构图，不从参考游戏提取素材。现有男主概念图的历史逐图来源记录尚不完整，商业发行前应补齐该参考的权利来源，并审核所有资产；AI生成这一事实本身不保证独占版权或完成商业法律审计。

生成PNG原封不动保存，游戏通过AtlasTexture使用测量后的局部区域，未在外部软件抠图或修改原始文件。中间不合格版本没有接入项目，最终七文件及SHA256见assets/manifest.json。没有使用未经核实的网络下载资源，没有把系统字体文件打包分发。重要文字由Godot渲染，不在AI图片里内嵌。

男主曾做一次行走姿态定向修订，NPC曾做一次透明背景定向修订。八个男主帧有AI生成的细微形体变化，当前为可运行v1，不称已完成人工逐像素终稿；后续允许迭代。NPC的深层故事不由这些外观图定稿。

## 最终提示集

### community-v1.png

生成文件：exec-2e53e6da-772d-49af-831a-1761bc43cfb4.png。采用内置工具，项目资源文件：community-v1.png。

```text
Use case: stylized-concept
Asset type: final raster background for a 2D side-scrolling narrative game, NOT a concept board or mockup.
Primary request: a beautifully finished pixel-inspired hand-painted Jiangsu Taixing resettlement-community neighborhood panorama. Wide landscape 3:1 image. Strict straight-on side elevation, orthographic camera, coherent fine pixel-cluster shading and crisp edges, muted blue-gray palette, warm small window lights, intimate ordinary Chinese town life rather than a ruined or dystopian city. Modern modest six-storey apartment buildings behind a community gate, weathered stone boundary walls, electrical wires, parked unbranded electric scooters, planters and a humble bus-stop bench. One old blank road sign near 10% width; gate and safe car stopping zone near25%; bench near60%; opening toward local shop street near93%. Foreground continuous unobstructed horizontal walkway with its surface exactly at78% image height, pavement continuing below to bottom. Ground path flat all the way across. Buildings behind walkway; no elevated stairs blocking path. Late afternoon autumn subdued natural light and detailed lived-in wear. No people baked into background: characters are separate transparent sprites. No UI, typography, text, fake Chinese characters, watermarks, logos, brands, borders, or separate panels. Do not copy an existing game's artwork. Polished cohesive shipping-game environment.
```

### street-v1.png

生成文件：exec-16987cd4-53d7-443a-89b7-aa7d0a626c89.png。采用内置工具，项目资源文件：street-v1.png。

```text
Use case: stylized-concept
Asset type: final raster side-scrolling narrative game background.
Input images: Image1 is STYLE reference only; do not duplicate its gate. Produce a new companion environment for the same town.
Primary request: polished wide 3:1 panorama of an ordinary Jiangsu Taixing small-town shop street, orthographic straight-on side view. Pixel-inspired crisp clustered hand-painted illustration, same muted gray-blue warm-amber late-afternoon palette and worn lived-in material detail as reference. Continuous horizontal pavement walking surface at78% image height. Apartment facades and modest convenience-store shutters behind walkway, unbranded parked scooters, utility meters, cables, small cardboard boxes tucked out of path. Left end connects to community, central convenience-shop facade has a blank sign, right end leads to a duck-blood vermicelli soup shop with warm lighting and blank wooden sign. Clearly distinguish three shopfronts, keep path unobstructed and flat. No characters, text, pseudo-letters, logos, watermarks, UI, panels, dramatic fantasy, disaster ruin or copied-game art.
```

### soup-v1.png

生成文件：exec-900baeb0-bb5c-45bf-83cf-3ed3bd3ac72c.png。采用内置工具，项目资源文件：soup-v1.png。

```text
Use case: stylized-concept
Asset type: final raster background for the soup-shop level of a side-scrolling narrative game.
Input images: Image1 is only a style/color reference for the town, not an edit target.
Primary request: wide 3:1 interior panorama, strict orthographic straight-on side elevation of a humble Jiangsu duck-blood vermicelli soup shop. Pixel-inspired crisp clustered hand-painted game illustration, beautiful detailed believable worn surfaces, warm amber light against muted blue-gray tiles, ordinary intimate neighborhood realism. Continuous floor line at78% height, lower strip floor unobstructed. Far-left open entrance at8% width. Old blank wooden wall sign20%. A table and two stools behind the walking lane at46%, no food or humans baked into scene. Simple blank menu-board73%, service counter83% with bowls stacked and cooking steam behind it, stainless steel work area towardright. Wall-mounted fan, light wear and oil patina, not filthy or ruined. Furniture side-on, no tilted top-down camera. Upper wall with ventilation and simple hanging lamps. No people, words, pseudo-Chinese, logos, brands, UI or watermarks.
```

### player-v1.png

生成文件：exec-3d0e34f5-89db-4d36-b078-da8835f7cb4f.png。采用内置工具，项目资源文件：player-v1.png。

```text
Use case: precise-object-edit
Asset type: corrected usable walk animation sheet with actual transparent background.
Input images: Image1 is EDIT TARGET, the established protagonist's8-frame4columns2rows atlas. Preserve top-rowfour idle figures, clothing identity, head hair denim vest chain boots, exact4x2grid and crisp hand-painted pixel-inspired game style. Keep alpha transparent no backdrop.
Primary request: change ONLY bottom-row WALK poses to ensure a genuine full four-phase looping walk: column1 right leg extended forward/left backward wide CONTACT pose; column2 PASSING pose with support leg almost vertical under hips and other knee bent forward, BOOTS CLOSE TO EACH OTHER horizontally, feet not spread apart; column3 LEFT leg extended forward/right backward opposite wide CONTACT pose; column4 PASSING pose with other support leg almost vertical under hips and opposite knee bent forward, BOOTS CLOSE TO EACH OTHER horizontally. Arms swing opposite legs. Passing frames MUST have narrow foot spread, not repeated lunge poses. Same full-body size/side-facingRIGHTview and common foot baseline inside all cells. No writing, checkerboard, grids, borders, background shadows or extra figures. Preserve original sheet aspect2:1.
```

### npcs-v1.png

生成文件：exec-d2be3bf5-c7ef-48f4-b195-b80cbcd02b31.png。采用内置工具，项目资源文件：npcs-v1.png。

```text
Use case: background-extraction
Asset type: final alpha-transparent six-frame NPC sprite atlas.
Input images: Image1 is the EDIT TARGET. Change ONLY background and background alpha. Preserve all SIX characters' exact faces, outfits, body proportions, limb poses, stools, candy packet, apron, exact pixel positions and image dimensions1536x1024. Remove ALL brown/black background glow, dark fog, gradients, backdrop shading, floor shadows or halo around silhouettes. Make every background pixel FULLY transparent alpha0 with clean crisp antialiased figure edges. Not a painted checkerboard. Do not restyle or reposition characters, do not change framing or introduce labels. Keep the3-column2-row sprite layout unchanged.
```

### memory-v1.png

生成文件：exec-8c97a1c3-88c3-4763-abcb-71e282914276.png。采用内置工具，项目资源文件：memory-v1.png。

```text
Use case: stylized-concept
Asset type: finished16:9game background for an intimate interactive soup-table memory scene.
Input images: Image1 is the soup shop's art style and setting reference only.
Primary request: beautiful hand-painted pixel-inspired nostalgic early-morning Jiangsu soup-shop table close-up, warm amber faded wood and blue-gray wall tile palette. Large worn laminated wooden tabletop occupies bottom75% of image, continuous subtle detailed surface with enough visual quiet for separate game coin and bowl sprites. At top25% slightly softly distant soup-shop wall, a blank paper menu and warm low morning light through window. Seen from seated diner's natural eye level looking gently down on table. Simple scratched wood, modest ordinary clean surfaces, a little steam/air haze at top but nothing hiding objects. A second empty wooden chair just visible at far edge. NO people, hands, bowls, food, coins, writing, logos, grids, panels or UI baked into background. No cinematic photo, same illustrated game shading as reference. This is the playable scene background not a screen mockup.
```

### props-v1.png

生成文件：exec-55770722-d495-4930-9b96-d6719b345344.png。采用内置工具，项目资源文件：props-v1.png。

```text
Use case: stylized-concept
Asset type: final alpha-transparent six-object prop sprite atlas,3columns2rows in6equal SQUARE cells, wide3:2canvas.
Primary request: six finished game objects with distinct useful silhouettes for an ordinary Chinese hometown narrative game. Matching crisp hand-painted pixel-inspired shading with muted colors and warm highlights, each fully contained and centered in own cell with generous transparent padding, fixed camera perspective consistent within objects. Top row left a single worn silver coin seen nearly face-on with subtle abstract relief but NO denomination or words; top middle single subtly warm brass coin same scale and view with different simple abstract relief, NO words; top right ceramic white-blue bowl full of steaming duck-blood vermicelli soup with noodles, dark red duck-blood cubes and scallion garnish, three-quarter tabletop view. Bottom left small red tied wedding-candy gift pouch; bottom middle old unbranded black smartphone straight-on with blank deep-blue screen, wear around edge; bottom right paired wooden chopsticks resting on a small white ceramic rest. No people, background, floor cast shadows, halo, labels, numbers, text, logos, gridlines or mockup. Real alpha transparency, not painted checkerboard. EXACTLY6objects in regular3x2grid.
```
# V2 场景润色资源（2026-10-04）

新增旧手机和对白图框来自内置 image_gen，未使用私人照片、品牌Logo或外部素材。PNG及九宫格裁切保存于本项目 assets/art/vs01-v2 和 assets/ui/vs01-v2。完整提示、来源及SHA256见 docs/project/2026-10-04_场景润色资源记录.json。工具未披露具体模型版本；不把模型版本或独占版权作为保证。商用发行前仍需审核内容、标识和平台要求，系统字体不再分发。

