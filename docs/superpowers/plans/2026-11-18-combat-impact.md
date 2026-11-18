# 战斗打击感 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans。用户已选择本会话原生连续执行，并确认本轮范围，不另开会话或重复审批。

**Goal:** 打磨现有战斗的反馈层次、命中后闪避衔接、守势反应机会及最后一击展示，完成Windows发行。

**Architecture:** 领域层维护命中确认、停顿和规则参数，事件携带招式/方向/逻辑击退信息；Godot显示层独立处理短时效果。既有结算先提交保存，再延迟显示界面。

**Tech Stack:** Godot 4.7.2 .NET、C#、.NET 8。

**Spec:** docs/superpowers/specs/2026-11-18-combat-impact-design.md

## Global Constraints

唯一目录D:\游戏创作\顾行舟；沿用feature/vs01。保护用户project.godot（SHA256 3948D7E06CA66F1849ACB06838414AADA177E201573BD2B8291303CBBD22E51E）、schema4、combat-v1、真实档与历史证据。只提交明确文件；不发布测试缓存/本机配置/私人资料。

## Review Focus

- 终结展示期间关闭/失焦不重复奖励或丢失连战结果。
- 多目标、格挡、空挥、停顿缓冲不能绕过取消条件或重复消耗耐力。
- 减少动效与暂停后不遗留抖动或过期效果。
- 旧配置快照及成绩不受新参数默认值污染。
- 发行包使用已提交工程配置，完整依赖与公开下载哈希匹配。

### Task 1: 领域规则与兼容

Files: code/Domain/Combat/{CombatCatalog,CombatFighter,CombatMatch,CombatStatistics}.cs、content/combat/combat.json、tests/DomainChecks/{Program,Checks/CombatImpactChecks,Checks/CombatBuildChecks}.cs。

- [x] 先写受控规则回归，运行dotnet run --project tests/DomainChecks -- --filter CombatImpact，观察预期失败。
- [x] 命中确认限当前攻击；后摇4帧后TryDodge按原成本/冷却执行，统计DodgeCancels。事件追加可选MoveId/Facing/Displacement，不破坏旧构造。
- [x] 配置追加HitstopFrames（旧招式缺省按旧ID规则）、GuardFrames默认48；当前配置写2/3/4/5/7、72并更新balanceVersion。校验边界，旧进度版本不变。
- [x] 全量领域回归通过，记录实际取消与守势边界。Expected: RESULT全部通过。

### Task 2: 显示、音效与收尾

Files: scripts/Combat/CombatArena.cs、CombatDemo.cs、CombatDemo.Challenge.cs、scripts/AudioDirector.cs、scripts/Testing/SmokeHarness.CombatImpact.cs及现有普通/连战测试。

- [x] 先补即时保存且界面暂缓、保存失败立即可重试、按键取消与动效冻结的引擎检查，观察功能缺失失败。
- [x] 实现显示层短促效果；逻辑击退保持原量。PlayCue默认参数保持旧调用，槽位复用重置音高/音量；反馈同帧限量防止多目标叠音。
- [x] 结算立即调用原保存逻辑；成功后finish屏0.32/0.18秒，不推进领域钟；失败直接result。冻结失焦/暂停效果，切波清空；HUD固定。
- [x] 运行专项、普通/连战与30/60/120Hz检查；GPU20/32字号采图目视检查。Expected: GODOT_CHECKS_PASS。

### Task 3: 回归、数值记录与发行

Files: docs/design/08_战斗打击感与取消窗口调优.md、docs/development/29_战斗打击感验收与发行.md、交接/README/更新日志/20号说明。

- [x] 全量领域/引擎/工具；与上一版同条件矩阵对照，保留失败证据和真人待验边界。
- [x] 冷源码使用已提交project.godot；隔离编辑器APPDATA配置完成导入、验证、Windows导出。测试实际PCK故事/普通/连战与冷解压原生EXE。
- [x] 独立全变更审查，修复实际重要问题并补红绿回归。
- [x] 提交明确文件并上传feature/vs01；制作完整ZIP、逐文件冷解压核对、上传新Release并核对远端digest/下载。
- [x] 校验备份旧ZIP/目录/发行元数据后撤下旧Release，保留标签历史；补北京时间回执并上传。

## Execution ledger

- 2026-11-18：承接用户已审核范围，在唯一工作目录执行；不另建worktree，避免超出指定路径。原始工程配置哈希与HEAD a3f7a1b一致，用户输入修改仍单独保留。
- Task 1：199项规则通过；旧快照守势/重击格挡停顿保持，当前取消成本和敌方格挡2帧均有红绿证据。
- Task 2：20/32字号GPU五招式、实体Space取消、暂停冻结、暂停切换减少动效及音高/增益复用通过。独立审查四项均补复现修复；七种退出另抓到Godot动态调用不支持C#新增可选参数默认值，保留PlayCue单参数绑定并另设PlayImpactCue。
- Ruling：两项初标Minor（格挡遮盖实际命中、冷却时提示可取消并误记耐力不足）提升为Important——直接影响本轮核心反馈及可用性判断，均本轮修复；代价为同帧混合反馈优先表现实际命中。
- Ruling：显示层平滑展开既有击退，保持判定坐标和总位移——避免新增追击距离平衡变化；代价是短时显示脚底与逻辑位置有小幅差异，真人感受待验。
- Task 3：功能提交cd7c0af普通上传。根/冷源码199规则、53引擎/293资源，工具0失败、七种GPU退出及实际PCK/冷EXE通过；首次故事动画超时与默认配置导入等待如实保留。2026-11-18 15:18:12发布vs01-impact-20261118，163751245字节ZIP/207文件，远端digest与HTTP200核验；旧版已核对归档，仅新版公开。具体哈希和边界见29号回执。
