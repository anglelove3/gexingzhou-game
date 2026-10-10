# GitHub 与跨设备继续开发

更新记录：2026-10-10（北京时间；实际提交时间见Git）。独立仓库：[anglelove3/gexingzhou-game](https://github.com/anglelove3/gexingzhou-game)，已按用户决定改为公开；原来的`anglelove3/fangguoge-game`不改动。此工程的开发分支及远端默认分支均为`feature/vs01`，不要用另一个游戏的目录或分支覆盖它。用户已授权本轮完成后正常推送，不强推、不另建合并请求。更新内容见[10_更新日志](10_更新日志.md)，同步成功必须核对远端提交等于本机HEAD，而不是只看本地commit。

## 另一台 Windows 电脑首次准备

本轮优先读13_小区四向探索验收与接续.md：小区四向已接入，便利店街仍横向；新版存档目录vs01-explore-v3。不要将另一设备旧档强行覆盖新版目录，使用游戏菜单明确复制升级，冻结旧导航验证v1/v2。原生键鼠、Godot手工编辑、新电脑重新构建和独立EXE仍需分别验收。所有新素材和测试都保存在本项目目录；公开源码包不带私人故事稿/照片、真实存档或测试日志。

2026-10-10较早日志轮次：微信式手机及独立今日记事已接入，详见12_今日记事验收与接续.md。J打开当前/经历/见闻，手机行程栏是收起后按J的轻提示，日志不可与坐姿/对白/手机/回忆叠加。日志只读、不增加奖励。本轮随后完成小区四向，当前操作以13记录为准；便利店街仍横向，不能误称全地图四向完成。

今日记事规格和实施计划均获用户确认，实施已开展；不要回到计划待审阅阶段。新设备先核对远端HEAD、实际测试/审查记录和本地修改，再安全pull --ff-only。跨设备不自动拥有原始照片/故事集或本聊天历史；不要整目录上传，源码包是编辑器工程而非独立EXE。

需要 Git、Godot 4.7.2 **.NET 版**、.NET 8 SDK、VS Code。`global.json`要求8.0.425并允许同一8.0功能带的更新补丁；如果SDK无法匹配，先查看`dotnet --list-sdks`，安装匹配版本，不随意改工程基线。

公开仓库通常无需登录即可浏览和克隆；推送修改仍需具有写入权限的GitHub账号认证。可以用Git Credential Manager的浏览器登录，或安装GitHub CLI后执行以下命令；不要将密码、令牌发给Codex或写进仓库。

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

> 请在当前克隆的葛行舟工程继续开发。先完整阅读00_阅读与协作说明.md、README.md、docs/development/12_今日记事验收与接续.md、10_更新日志、09_GitHub与跨设备接续和07联合编辑教程，再按任务需要阅读已批准设计和实施计划。检查实际Git分支、未提交改动和本机Godot.NET/.NET环境。使用Godot+C#和VS Code，主要节点保存在.tscn，保留用户修改。不要重做已完成的汤店四向样板、微信式手机和J今日记事。先告诉我当前状态，再依据我的最新试玩反馈继续，不重问此前238轮已确定的问题；另一设备原生编辑器手工验收仍待我确认。所有项目文件集中于我确认的游戏目录。

## 不同步的内容

原始故事Word、私人照片/人物参考归档、references、builds、.superpowers、.godot、bin/obj、test-output、日志、本机配置和运行存档不上传。游戏正式PNG、原创音源及生成脚本、源码、测试和已提交文档可随批准的push上传；公开仓库的这些内容任何人都能查看，后续每次提交仍检查隐私和素材来源。历史日期文档中“曾私有”的事实不改写；公开源码不自动授予第三方素材商业使用权。

当前主要节点在Godot本地场景可编辑，C#在VS Code维护状态与行为。2026-10-09二维探索样板完成后正常同步本分支；请追加阅读`docs/reviews/2026-10-09_二维探索汤店样板验收记录.md`、10月9日设计/计划和探索资源记录。新档schema 2 / vs01-0.2在user://saves/vs01-explore-v2，旧schema 1 / vs01-0.1只读复制升级，不自动回退；既有事件ID不变，不新增姿态字段。原生键盘、真实试听、另一台实际设备及独立EXE仍待补验。历史10月8日更新说明不代表最新存档版本。

因此另一台电脑不会自动有原始故事集、历史测试截图或试玩存档。需要文学原件时，由用户通过私密方式单独提供；不要删除本机原件。需要测试截图时重新运行捕获工具。需要独立EXE时仍须匹配导出模板，本仓库不是成品发行包。
