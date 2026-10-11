# 2026-10-11 试玩润色美术来源

本轮三份原始栅格素材由内置 ImageGen 生成，用于本游戏：秋日菜单近景、透明自行车盆栽/晒衣架、主角侧/前/背三方向行走素材。人物参考既有本项目AI角色图，未使用现实照片或他人素材。

资源路径、尺寸、SHA256和提示集见 `docs/project/2026-10-11_试玩润色资源记录.json` 及 `assets/manifest.json`。生成日期为2026-10-11，北京时间。

`assets/art/vs01-v6-polish/player-walk.png` 是生成原图 `player-walk-source.png` 的生产图集：`tools/build-walk-atlas.mjs` 按透明间隙提取24个姿态，规范每帧高度400、画布256×432与脚底基准(128,412)，未补画新人物。旧中立待机姿态继续来自player-depth-v4。

画风为像素感半写实栅格插画，不能宣称人工逐像素绘制或已经达到真人步态质量。AI生成素材的正式商业发布仍须单独复核来源与发布条款。

界面滑块小圆点采用Godot原生径向GradientTexture2D，落叶采用Polygon场景图形，无第三方图标依赖。初版slider-knob.svg保留为项目原创矢量源稿，运行主题无需加载或先导入它。
