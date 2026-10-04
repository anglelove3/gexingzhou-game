# GitHub 与跨设备继续开发

更新：2026-10-05。独立仓库：`anglelove3/gexingzhou-game`，私有；原来的`anglelove3/fangguoge-game`不改动。此工程的开发分支沿用`feature/vs01`，不要用另一个游戏的目录或分支覆盖它。

## 另一台 Windows 电脑首次准备

需要 Git、Godot 4.7.2 **.NET 版**、.NET 8 SDK、VS Code。`global.json`要求8.0.425并允许同一8.0功能带的更新补丁；如果SDK无法匹配，先查看`dotnet --list-sdks`，安装匹配版本，不随意改工程基线。

私有仓库需要登录拥有权限的GitHub账号。可以用Git Credential Manager的浏览器登录，或安装GitHub CLI后执行以下命令；不要将密码、令牌发给Codex或写进仓库。

```powershell
gh auth login --hostname github.com --git-protocol https --web
gh auth setup-git
```

确认`E:\游戏创作\顾行舟`还不存在。若那里已有文件，先停下核对，不要删除、覆盖或在里面重复clone。若另一台电脑没有E盘，请先与你确认专门的游戏目录，不将项目散放到桌面或临时目录。

```powershell
git clone --branch feature/vs01 https://github.com/anglelove3/gexingzhou-game.git "E:\游戏创作\顾行舟"
Set-Location -LiteralPath "E:\游戏创作\顾行舟"
git status --short
dotnet restore GeXingzhou.csproj
dotnet build GeXingzhou.csproj
dotnet run --project tests/DomainChecks
```

用Godot.NET导入这个目录的`project.godot`，等待资源导入与C#构建，按F5运行完整游戏。VS Code打开整个工程目录，Ctrl+Shift+B构建。普通Godot版不能运行本游戏的C#代码。

本机Godot路径、VS Code路径不会自动适用于另一台电脑。完整检查时显式传入那台机器实际的Godot控制台程序：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1 -GodotExe '那台电脑的Godot.NET控制台程序绝对路径'
```

也可以按`tools/Common.ps1`读取规则配置本地`tools/environment.local.json`的GodotExe、DotnetExe、NugetSource；这个文件被忽略，不上传。第一次restore需要网络或已有完整NuGet缓存。

## 每次换设备的顺序

在开始开发的电脑上先查看`git status`，确认没有未保存改动，再`git pull --ff-only`。如果pull提示分叉或本地修改，先停止处理原因，不使用强推或`reset --hard`抹掉工作。

结束前仅暂存本次实际修改的文件，使用`git diff --cached --name-only`和`git diff --cached`检查，再提交并推送。不要为了上传而盲目把私人资料加到assets/docs中。

```powershell
git status
git diff --cached --name-only
git diff --cached
git commit -m "简要说明这次修改"
git push
```

提交之前需要自己用`git add -- 具体文件路径`暂存相应文件。commit保存本地版本，push才会同步GitHub；另一台电脑随后pull取得这些提交。不要同时在两台电脑修改同一分支后跳过pull，遇到冲突一起核对处理。该仓库默认分支也是开发分支，当前无需额外合并请求。

## 交给另一台电脑上的 Codex

GitHub同步工程代码、正式游戏素材和已提交的设计/开发文档，不会自动同步本聊天的完整历史。可以先发送：

> 请在当前克隆的葛行舟工程继续开发。先完整阅读00_阅读与协作说明.md、README.md、docs/development/09_GitHub与跨设备接续.md、07联合编辑教程及08验收记录，再按任务需要阅读已批准设计和实施计划。检查实际Git分支、未提交改动和本机Godot.NET/.NET环境。使用Godot+C#和VS Code，主要节点保存在.tscn，保留用户修改。先告诉我当前状态，再依据我的最新试玩反馈继续，不重问此前238轮已确定的问题；原生编辑器手工验收仍待我确认。所有项目文件集中于我确认的游戏目录。

## 不同步的内容

原始故事Word、私人照片/人物参考归档、references、builds、.superpowers、.godot、bin/obj、test-output、日志、本机配置和运行存档不上传。游戏正式PNG、源码、测试和已提交文档会上传；部分设计文档包含故事设定，不要把私有仓库随意改为公开或授权给不需要的人。

因此另一台电脑不会自动有原始故事集、历史测试截图或试玩存档。需要文学原件时，由用户通过私密方式单独提供；不要删除本机原件。需要测试截图时重新运行捕获工具。需要独立EXE时仍须匹配导出模板，本仓库不是成品发行包。
