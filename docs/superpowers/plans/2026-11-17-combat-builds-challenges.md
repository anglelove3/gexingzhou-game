# 装备流派与连战实施计划

采用superpowers:executing-plans，在当前会话逐项原生实现，任务进度记入本计划对应的本地ledger。用户于2026-11-17要求继续开发，接续上一轮具体设计。

## 目标与结构

按[设计稿](../specs/2026-11-16-combat-builds-challenges-design.md)实现三种武器机制、六波生命继承挑战、独立成绩保存和装备对照测量。Godot 4.7.2 .NET、C#、.NET 8，沿用feature/vs01及当前工作目录。

领域层维护武器触发、挑战快照和记录，现有CombatDemo接入挑战准备/波间/结算。测试工具复用实际CombatMatch执行确定性矩阵，报告记录条件与局限。

## 全局约束

- 保留用户原project.godot，发行从独立源码副本采用Git中的项目配置。
- 故事schema4、combat-v1格式与RunId保护延续，九件装备、五级和六波ID保持。
- 首轮参数采用设计稿，源码与完整Windows试玩同步发布，旧包经备份核对后退休。
- 保留所有历史测试证据、标签、旧包和真实存档。

## 审查重点

- 旧配置缺少机制参数时采用零效果，旧档可读，配置及历史记录畸形输入受校验。
- 多目标及保护状态的回气/反击/破势统计只记有效触发，取整与耗耐力边界明确。
- 波间保存失败后复用候选和AttemptId，继续及退出路径保持一致。
- 另一窗口重开周目时挑战锁定快照继续，返回读取当前普通周目。
- 暂停、命中停顿、物理更新频率和进程中断不会产生额外奖励或错误成绩。

## 任务1　武器机制与统计

修改CombatCatalog、CombatFighter、CombatMatch、CombatStatistics及combat.json，新增CombatBuildChecks。

接口为CombatItem机制参数、CombatFighter反击状态、CombatStatistics有效伤害/总帧及触发计数，CombatMatch保持既有构造入口。

- [x] 写入配置/回气/破势/及时格挡/取整/保护与时钟失败检查，运行dotnet run --project tests/DomainChecks，保存RED输出。
- [x] 实现6耐力返还、1.20破势与额外4耐力、6帧格挡/90帧机会/120帧冷却/1.25反击，旧参数缺省零效果。
- [x] 运行全部领域检查，通过后提交本任务。

## 任务2　连战与成绩持久化

新增CombatChallenge.cs、CombatChallengeRepository.cs和对应检查。

接口为CombatChallenge.Start(catalog, progress, configHash)、CreateMatch()、FinishWave(match)、Continue()、End()，记录采用独立AttemptId、版本、快照及逐波统计。持久化接口Save(record)、LoadHistory()和RecoverBackup(attemptId)。

- [x] 写失败测试，覆盖旧通关档解锁、快照、生命继承、耐力回满、失败、退出、完成和记录故障重试。
- [x] 实现挑战状态机与原子文件保存，每次尝试独立锁及备份，验证损坏/过期/并发候选。
- [x] 运行全部领域检查，通过后提交。

## 任务3　实际界面与输入接入

修改CombatDemo及CombatDemo.tscn，新增CombatDemo.Challenge.cs和SmokeHarness.CombatChallenge.cs，沿用焦点与音频边界。

- [x] 添加实际场景入口、锁定条件、保存失败、跨周目及键盘六波检查，先运行确认失败。
- [x] 准备添加流派说明、连战入口与历史，结果添加触发统计及挑战继续/重试/退出。保证普通键盘路径和既有调用行为。
- [x] 跑Combat、CombatChallenge、CombatTiming与字号20/32的实际GPU检查，通过后提交。

## 任务4　装备矩阵与调优报告

新增tests/DomainChecks/CombatBalance.cs及对应命令分支，复用CombatAutomation及真实领域循环。

- [x] 写入确定性、行数、条件哈希和结果汇总检查。
- [x] 实现基线/机制、普通/连战、27装备×3策略×3站位，每波7200步上限，输出本地JSON明细和仓库Markdown总结。
- [x] 记录每组最多486尝试/2916波，分别报告胜利/失败/超时、有效伤害及触发次数，核对样本可追溯后提交。

## 任务5　审查、完整验证与发行

- [x] 新上下文只读审查本轮完整变更，问题补真实回归后修正。
- [ ] 根工程完整verify、工具及七种GPU退出，独立源码副本完整回归与导出。
- [ ] 实际发行PCK验证普通故事、战斗和连战，冷ZIP逐文件哈希与原生EXE运行检查。
- [ ] 更新使用说明、README、数值/验收/交接记录，提交上传源码及完整Windows ZIP。
- [ ] 核对GitHub大小/SHA256和下载，归档旧包/目录/元数据后撤旧，最终确认远端HEAD与唯一Release。
