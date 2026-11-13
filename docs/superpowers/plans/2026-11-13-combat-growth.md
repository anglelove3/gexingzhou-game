# 战斗成长试玩实施计划

> For agentic workers REQUIRED SUB-SKILL use superpowers:executing-plans in this session. Steps use checkboxes for tracking.

**Goal** 实现已审核的独立战斗成长试玩，并完成Windows发行与GitHub同步。

**Architecture** 沿用现有Domain纯C#规则、JSON配置、Godot静态场景与C#绑定。战斗进度使用独立目录与事务保存，战斗控制和原故事在各自场景读取输入。

**Tech Stack** Godot 4.7.2 .NET、C#、.NET 8、Windows x64。

**Spec** `docs/superpowers/specs/2026-11-13-战斗成长试玩-design.md`，用户已于本会话确认开始制作。

## Global Constraints

- 唯一工作目录 `D:/游戏创作/顾行舟`，分支 `feature/vs01`，本会话原生执行。
- 保留用户project.godot编辑、故事schema4与真实存档，战斗使用 `user://saves/combat-v1`。
- 5级、9装备、6波奖励、3类普通敌人与领队，数值按已审规格初测。
- 可运行内容变更更新完整Windows试玩，先验证新下载再归档撤旧，保留标签和证据。

## Review Focus

- 保存故障与重复结算应保留候选并只发放一次奖励，任务1验证。
- 暂停/失焦期间动作停止，恢复需重新按键，任务3验证。
- 同帧多目标、二连、死亡与击倒交叠应按攻击实例结算，任务2验证。
- 装备/成长面板与战斗实际数值一致，任务1/3验证。
- 完整首段与独立战斗往返应保持各自进度，任务3/5验证。

## Task 1 配置、成长、装备与独立存档

Create `content/combat/combat.json`、`code/Domain/Combat/CombatCatalog.cs`、`CombatProfile.cs`、`CombatRepository.cs`，测试 `tests/DomainChecks/Checks/CombatChecks.cs`，注册到Program。

Interfaces `CombatCatalog.Parse(string)` / `LoadDefault`由调用者传JSON，`CombatProgress.New(CombatCatalog)`，`CombatProgress.Reward(string,CombatCatalog)` 返回新快照，`CombatCatalog.Stats(CombatProgress)` 返回最终属性，`CombatRepository.Load()/Save(CombatProgress)` 提供结果与备份恢复。

- [ ] 先写成长阈值、伤害11/12/15、九装备、六波去重、损坏/未来版及保存失败测试，运行filter Combat观察失败。
- [ ] 实现JSON校验、属性推导、固定奖励与原子保存，运行 `dotnet run --project tests/DomainChecks -- --filter Combat`，预期全绿。
- [ ] 提交这一可测试数据层。

## Task 2 攻防状态、敌人协作与统计

Create `code/Domain/Combat/CombatFighter.cs`、`CombatMatch.cs`、`CombatStatistics.cs`，Extend CombatChecks。

Interfaces `CombatMatch(CombatCatalog,CombatProgress,string waveId)`，`Step(CombatInput)` 固定60Hz，公开Player/Enemies/Events/Statistics/Result给呈现层，`CombatFighter`保存脚底位置/面朝/状态/动作时间/生命耐力/攻击实例。

- [ ] 先写范围与纵深、耗耐力/恢复、三连、格挡方向、蓄力破防、闪避窗、单次命中、领队二次击倒、进攻资格测试，观察失败。
- [ ] 实现固定步状态转换、碰撞约束、敌人决策和单次尝试统计，测试通过。
- [ ] 提交可独立模拟的战斗规则。

## Task 3 可玩入口、装备与战斗界面

Create `scenes/combat/CombatDemo.tscn`、`scripts/Combat/CombatDemo.cs`、`CombatArena.cs`与UI分文件，Modify Boot.tscn/BootMenu.cs/export_presets.cfg。测试 `scripts/Testing/SmokeHarness.Combat.cs`与外置PCK驱动。

Interfaces CombatDemo加载目录/配置，管理准备、战斗、暂停和结算，Arena仅呈现CombatMatch。装备列表读取同一Catalog，Settings复用现有场景。主菜单提供独立入口。

- [ ] 先写真实键盘进入、选择装备、攻防输入、失焦暂停、奖励与模式往返回归，观察入口缺失失败。
- [ ] 实现场景与控制器，耗时只累计有效战斗步，保存失败留结算重试，所有控件设焦点循环。
- [ ] 执行新增引擎套件与领域回归，预期通过。提交可玩版本。

## Task 4 美术、声音和可读性

Create `assets/art/combat-v1/`与来源许可记录，动作使用独立栅格图集。新增命中/格挡/挥击短音效，保留现有音量与停音流程。补配置说明、理论收益与本次统计导出。

- [ ] 捕获准备/装备/单挑/围攻/领队/结算，核对脚底、朝向、动画阶段、减动效与字体缩放。
- [ ] 接入制作并保存来源的场地、角色动作、装备图集，补判定与表现一致性回归。
- [ ] GPU实际运行与音频生命周期回归通过，提交。

## Task 5 最终审查、回归与发行

Modify README/20试玩说明/10更新日志/00交接，Create `docs/development/26_战斗成长试玩验收与发行.md`，Extend运行驱动。

- [ ] 新上下文只读审查全部实现，对重要发现补红绿回归并修复。
- [ ] 全领域、全部引擎、工具、退出、冷源码导入、独立导出、实际PCK完整六波成长与原故事运行，检查实际渲染与原生EXE冷解压。
- [ ] 补真实参数测算及自动化调优条件，逐文件核对ZIP，提交源码与验收并上传GitHub。
- [ ] 备份旧ZIP/目录/发行元数据，上传新ZIP、验证大小/哈希/公开下载后撤旧，记录北京时间与回执，最终核对远端HEAD及用户配置哈希。

具体测量结果以实际日志为准，真人阅读、操作手感及异机验证分别记录。
