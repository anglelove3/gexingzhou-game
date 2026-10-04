# Godot 与 VS Code 联合编辑：第一轮练习
更新：2026-10-05。工程唯一根目录 E:\游戏创作\顾行舟；Godot.NET4.7.2、C#、.NET8。

## 两个工具分别负责什么

Godot负责场景位置、图片、容器布局、主题、SpriteFrames、碰撞与锚点；VS Code负责C#输入、状态机、剧情事件和存档协调。静态节点保存在.tscn，脚本Ready获取它们，不再重建主要世界或UI。不要把场景中的节点删掉后在C#里随意重建来绕开错误。

本地树是磁盘场景，可编辑并保存；运行时出现的远程树是当前游戏进程的实例，不是服务器或联网工程。远程修改通常仅影响本次运行，停止后不会自动保存。@名字是代码自动创建、未命名节点的自动名称，不是远程资源。主要节点本轮已命名；变长剧情选项和临时付款反馈仍允许运行时创建。

## 练习1：修改坐姿位置

停止游戏，打开scenes/world/CommunityGate.tscn，本地树找到Bench。它是Bench.tscn实例，展开后选SeatAnchor（需要时开启可编辑子节点）。先记录原值，再将SeatAnchor的X增加5，保存场景，F5完整运行，走到长椅按E。
主角坐稳后的位置应跟随锚点；碰撞体仍留在StandAnchor地面位置。请只移动SeatAnchor，不要随意删除其导出引用。练习后恢复原值并保存。可先单独打开Bench.tscn查看原锚点：SeatAnchor(0,-30)、StandAnchor(0,0)。

场景移动NPC时，保留Id和ActionId。张大炮Cannon初始隐藏/不参与互动，到场由存档状态控制；不要用节点是否存在判断他是否到了。

## 练习2：修改手机或对白外观

打开scenes/ui/Phone.tscn或Dialogue.tscn：编辑器中完整显示，运行Main时默认隐藏，由Tab/对白控制显示。
手机Frame使用assets/ui/vs01-v2/phone.tres，Dialogue的Panel使用dialogue.tres。这是StyleBoxTexture九宫格，不是烘焙文字；可改ContentMargin或换边框纹理。先记录值，将对白左内容边距30改为34，保存资源，F5触发对白核对，再恢复。
姓名、正文、提示是独立节点；对白正文位于Panel/Content/Body/Text，可滚动，提示不随正文滚走。手机消息位于Frame/Content/MessageScroll/Messages；接听/收起按钮仍是真实Button。容器管理的控件位置应改父容器/边距，而不是和容器抢offset。

## 练习3：修改一条C#提示并构建

VS Code打开整个根目录，不只打开一个.cs。打开scripts/MainView.cs，在_Process中把显示文字“Tab 手机”暂改为“Tab 旧手机”，不要改事件ID或Flow枚举。
按Ctrl+Shift+B选择“构建游戏”，成功后Godot F5运行核对。完成后恢复文字，再构建。首次构建可用终端dotnet build GeXingzhou.csproj；无需安装额外游戏插件。
本机已检测到VS Code命令 F:\Vscode\vscode ben ti\Microsoft VS Code\bin\code.cmd；换设备请重新检查路径，不把这个路径写入公开包。Godot的C#外部编辑器在编辑器设置中选择VS Code；菜单名称/位置以本机设置为准。当前工具无法操作原生Godot设置窗口，未替你更改机器级偏好。

## F5 / F6边界和休息操作

Godot F5运行完整Boot→Main，有GameSession和交互协调。F6仅运行当前子场景：编辑器可看布局，但Phone/Dialogue在运行初态会隐藏，单独子场景不等于完整玩法试玩。VS Code F5默认含义是调试，不等于Godot运行；本轮提供的是构建任务，不声称已安装调试扩展。

长椅E：真实坐下，再默认焦点“坐一会儿”。↑↓选择，E/Enter或鼠标确认；Tab始终打开手机。“来一根”是有限动作，播放完回到坐稳；不会自动循环，本轮没有buff或烟草库存。
菜单开着时Esc先收起，菜单关闭时再Esc起身；Tab打开手机暂停动作，回来坐稳，切场清理暂态。坐一会儿可以触发首次故乡观察；Esc取消对白不消耗线索。
F5保存站立安全落点；F9加载后站立。休息暂态不写入schema1旧档。减少动效版本省略烟雾帧，但保留动作和状态反馈。

## 学到的知识与求职证据

这轮实际涉及C#属性/导出引用、信号与回调、状态机、焦点/输入优先级、资源依赖、等比例视域数学、回归测试与版本管理。RestStateMachine的纯逻辑测试可以不启动Godot学习算法边界和幂等性。
本地行为记录仍默认关闭，可作为后续数据清洗、SQL分析的学习来源；当前不是大数据平台，也没有接入AI生成剧情。先完成一段可演示玩法，再逐步学习数据分析或AI模块，不把未实现能力写进求职简历。

## 已验证与需要你确认的部分

自动检查已覆盖：实例未加入树时的预存节点；修改Inspector等价属性后Ready不覆盖；真实GUI鼠标命中/键盘/动画信号；三种世界视域和保存恢复。实际渲染截图与报告在08_场景润色验收记录.md。
原生Godot编辑器的手工拖动、资源保存、双击C#启动VS Code，以及嵌入/独立窗口人工试玩尚需你完成上述练习确认。脚本检查不冒充这些手工验收。
