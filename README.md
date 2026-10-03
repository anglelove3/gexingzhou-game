# 葛行舟 · VS01首段试玩

这是Godot 4.7.2 .NET + C#的**编辑器可运行灰盒原型**，不是完整游戏，也不是无需引擎的Windows EXE。项目目录沿用用户指定的“顾行舟”，角色名仍为“葛行舟”。额外游戏插件不是当前依赖。

## 开始试玩

1. 用Godot **4.7.2 .NET版**导入本目录的`project.godot`。需要.NET SDK 8（本机锁定8.0.425，可使用较新的8.0补丁）。普通Godot版不能运行C#。
2. 在项目根目录终端执行`dotnet build GeXingzhou.csproj`，等编译完成；Godot按F5运行，选择“开始新游戏”。已有存档不会无提示被抹掉。
3. A/D或←→移动，Shift加快；金色目标附近E互动。Tab手机，Esc关闭当前界面或打开设置。选择用方向键/Tab切焦点，Enter确认；对白用E推进。F5保存手动槽，F9读手动槽。

首次可先在小区走走。张大炮语音、来电和到场按可操作时间推进；看手机、读对白时暂停计时。不接电话也能见到他。路线是小区见面→便利店交喜糖→汤店→桌面硬币回忆→返回现实→明天的伴郎安排。回忆中Esc退出不丢已推硬币，重看不会改写历史。

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

已接入首段任务、行动对白、回忆、自动/手动存档、可读设置与默认关闭的本地事件记录。所有画面明确标为“原型美术”，暂无音乐或真人配音，已有概念图没有冒充逐帧动画。没有AI生成对白、Android包或后三日完整内容，也不保证商业收入或求职薪资。

Windows独立包尚未生成：本机缺匹配的.NET导出模板。`tools/export-windows.ps1`会先验证并拒绝缺模板，不把编辑器运行算作EXE通过；生成后还须实际试玩和检查打包资源。脚本不自动安装插件、下载模板或上传记录。

具体证据、未验收项目与第一个学习练习见`docs/development/04_试玩验收记录.md`和`05_学习练习与下一步.md`。真实存档在Godot的user://saves/vs01，测试用独立test-output目录，不动玩家存档。
