# 葛行舟 · VS01首段试玩

这是Godot 4.7.2 .NET + C#的**编辑器可运行首段试玩**，已接入第一版正式栅格美术，不是完整游戏，也不是无需引擎的Windows EXE。项目目录沿用用户指定的“顾行舟”，角色名仍为“葛行舟”。额外游戏插件不是当前依赖。

2026-10-04本轮升级：世界、角色和主要UI具名节点已存入.tscn，本地可以编辑；手机/对白有新正式图框，背景按窗口比例覆盖、HUD缩成任务卡。长椅E有坐下动作，再由玩家选择“坐一会儿／来一根／起身”，默认不抽烟，本轮无buff。菜单开着Esc先收起，再Esc起身；Tab手机；坐姿F5保存安全站立位置，F9恢复站立。

最新证据与人工待办见[场景润色验收记录](docs/development/08_场景润色验收记录.md)，学习从[Godot与VSCode联合编辑](docs/development/07_Godot与VSCode联合编辑.md)开始。Godot F5运行完整游戏；VS Code Ctrl+Shift+B构建C#。新资源、完整生图提示及哈希在docs/project/2026-10-04_场景润色资源记录.json；v1原图保留。

## 开始试玩

跨电脑开发使用独立私有仓库[anglelove3/gexingzhou-game](https://github.com/anglelove3/gexingzhou-game)，开发分支`feature/vs01`。首次克隆、登录、环境准备及给另一台Codex的接续提示见[GitHub与跨设备接续](docs/development/09_GitHub与跨设备接续.md)。原始故事集、照片、存档和本机配置不随Git同步；原有fangguoge-game保持不变。

1. 用Godot **4.7.2 .NET版**导入本目录的`project.godot`。需要.NET SDK 8（本机锁定8.0.425，可使用较新的8.0补丁）。普通Godot版不能运行C#。
2. 在项目根目录终端执行`dotnet build GeXingzhou.csproj`，等编译完成；Godot按F5运行，选择“开始新游戏”。已有存档不会无提示被抹掉。
3. A/D或←→移动，Shift加快；金色标记附近E互动。Tab手机，Esc关闭当前界面或打开设置。剧情选择用方向键/Tab切焦点，Enter确认；休息菜单用↑↓选择，Tab始终打开手机；对白用E推进。F5保存手动槽，F9读手动槽。

首次可先在小区走走。张大炮语音、来电和到场按可操作时间推进；看手机、读对白时暂停计时。不接电话也能见到他。路线是小区见面→便利店交喜糖→汤店→桌面硬币回忆→返回现实→明天的伴郎安排。回忆中Esc退出不丢已推硬币，重看不会改写历史。

小区首次完整读完路牌观察，或坐稳选择“坐一会儿”后读完描述，会追加一句与邀请经历相符的心声；Esc取消不会消耗这次提示，单纯坐下或抽烟不会消费。回忆中“接过／等待／分享”分别对应不同的现实对白，三个选择不分对错，付款和后续安排相同。存档仍为schema 1 / vs01-0.1，旧档可继续。

## 开发与跨设备

VS Code打开整个项目目录。`content/vs01/parameters.json`存数值，`dialogues.json`存可修改手工对白，规则在`code/Domain`，Godot呈现在`scripts`及`scenes`。

```powershell
dotnet run --project tests/DomainChecks
powershell -NoProfile -ExecutionPolicy Bypass -File tests/ToolsChecks.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -GodotExe '你的Godot.NET控制台程序绝对路径'
```

`tools/environment.local.json`可填写GodotExe、DotnetExe、NugetSource，但它是本机配置，**不提交、不复制到分享包**。未配置时从PATH找Godot和dotnet；网络不可用可指定已有NuGet缓存。不需要改动引擎/SDK基线来绕过失败。

跨设备继续开发时，先让Codex阅读设计总案、实施计划、开发文档与`docs/project`决策记录，再检查实际Git/测试状态。分享试玩源码使用`tools/package-editor.ps1`生成`builds/editor-vs01-*`，包含测试以便学习，但排除原始故事文档、现实照片、人物概念图ZIP及本机配置。原目录含私人资料，不要直接整目录上传。

## 当前边界

已接入首段任务、行动对白、回忆、自动/手动存档、可读设置与默认关闭的本地事件记录。2026-10-04已用七张AI生成PNG替换三处场景、方块角色/NPC、回忆背景与硬币/汤碗，并更换菜单背景。男主实际使用四帧待机、四帧行走；原概念图仍仅作造型参考。素材在`assets/art/vs01-v1`，来源/哈希在`assets/manifest.json`，提示集在`licenses/AI_ART_VS01.md`，接续记录见`docs/project/2026-10-04_正式美术接入.md`。它们是像素感半写实栅格插画，不假称严格48×80人工像素终稿；动画仍可迭代。暂无音乐或真人配音。没有AI生成对白、Android包或后三日完整内容，也不保证商业收入或求职薪资。

Windows独立包尚未生成：本机缺匹配的.NET导出模板。`tools/export-windows.ps1`会先验证并拒绝缺模板，不把编辑器运行算作EXE通过；生成后还须实际试玩和检查打包资源。脚本不自动安装插件、下载模板或上传记录。

具体证据、未验收项目与第一个学习练习见`docs/development/04_试玩验收记录.md`和`05_学习练习与下一步.md`。真实存档在Godot的user://saves/vs01，测试用独立test-output目录，不动玩家存档。
