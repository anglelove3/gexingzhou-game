# Godot 与 VS Code 联合编辑：第一轮练习
更新：2026-10-10。工程唯一根目录 E:\游戏创作\顾行舟；Godot.NET4.7.2、C#、.NET8。

## 两个工具分别负责什么

Godot负责场景位置、图片、容器布局、主题、SpriteFrames、碰撞与锚点；VS Code负责C#输入、状态机、剧情事件和存档协调。静态节点保存在.tscn，脚本Ready获取它们，不再重建主要世界或UI。不要把场景中的节点删掉后在C#里随意重建来绕开错误。

本地树是磁盘场景，可编辑并保存；运行时出现的远程树是当前游戏进程的实例，不是服务器或联网工程。远程修改通常仅影响本次运行，停止后不会自动保存。@名字是代码自动创建、未命名节点的自动名称，不是远程资源。主要节点本轮已命名；变长剧情选项和临时付款反馈仍允许运行时创建。

## 练习1：修改坐姿位置

停止游戏，打开scenes/world/CommunityGate.tscn，本地树找到DepthLayers/Props/Bench。它是Bench.tscn实例，根位置(960,414)，SeatAnchor局部(0,-34)代表世界臀部(960,380)，StandAnchor局部(0,26)代表世界脚底(960,440)。展开后选SeatAnchor（需要时开启可编辑子节点），先记录原值再将X增加5；同时将Navigation/Anchors/SeatAnchor的X增加5，保持两个Marker一致。

保存场景后，在项目根目录运行 `& ./tools/bake-navigation.ps1`，将打印路径 test-output/navigation-bake/navigation.json 的结果复制到 content/vs01/navigation.json，再运行 `& ./tools/bake-navigation.ps1 -Check`。校验成功后F5完整运行，走到附近按E；角色先合法移动到站位再坐下，不瞬移穿过家具。不要删除导出引用。练习结束恢复两个Marker并重新导出、检查；仅改图片或仅改一个锚点会被拒绝载入。

学习点：.tscn组织与导出属性、CharacterBody2D脚圆碰撞、四向向量归一化、Y排序、C#状态机与代次回调、有界Dijkstra就位、只读日志投影和v3旧档迁移。未加入大数据集群或在线AI服务；当前练习优先掌握可测试的算法与工程基础。

场景移动NPC时，保留Id和ActionId。张大炮Cannon初始隐藏/不参与互动，到场由存档状态控制；不要用节点是否存在判断他是否到了。

旧路牌的图像/轮廓在DepthLayers/Props/OldSign/Visual，按脚底Y与角色排序；根OldSign是互动热点，VisualTarget和HitPolygon引用前者。移动路牌时同时保持这两个OldSign的世界位置一致，不用固定Z覆盖排序。形状至少三点、有限且非退化，发现ID必须已知；删掉Shape或破坏引用会在复制升级写v3之前拒绝候选，不会靠运行时重建补洞。

## 练习2：修改手机或对白外观

打开scenes/ui/Phone.tscn或Dialogue.tscn：编辑器中完整显示，运行Main时默认隐藏，由Tab/对白控制显示。
手机Frame已改为场景内StyleBoxFlat细长机身；浅灰聊天区与白/绿气泡都可以编辑，不再引用旧phone.tres金属九宫格。旧资源保留作历史素材。Phone根节点导出HandsetWidth/HandsetMaxHeight调整最大宽高，运行时按窗口留边适配，不改聊天字号。CloseButton的font_focus_color必须保持深色，不能只改普通字色。Dialogue的Panel仍为轻字幕，按字号和坐姿安全区调整高度/位置。改布局后跑PhoneChat、ExplorationUi和EditableUi。
姓名、正文、提示是独立节点；对白正文位于Panel/Content/Body/Text，可滚动，提示不随正文滚走。手机消息列表是Frame/Content/MessageScroll/Messages，邀请正文在Incoming/Bubble/Text；Outgoing是接听记录而非虚构聊天回复，CallNotice是来电/未接提示。头像使用原NPC图集的AtlasTexture区域，不另存真人照片。接听/收起仍是真实Button。容器控件应改父容器/边距，不和容器抢offset。界面数据由PhoneController.Open绑定；运行时不会重建一套静态UI。

## 练习3：修改一条C#提示并构建

新增“今日记事”编辑练习：停止运行后打开scenes/ui/Journal.tscn，本地树可见Panel/Content下Title、Tabs三个Button、BodyScroll/Body/Text与Empty、CloseButton。在Godot改Paper/Tab/Selected/Focus样式，保持深色焦点字、滚动区和关闭按钮可达；正文继承用户字号，不能通过缩字容纳。Main.tscn预存Journal实例及HUD/JournalButton，按钮在暂停左侧（32字号最小宽164，因此实际预存宽180，保留12间距）。C#在scripts/JournalController.cs绑定节点，在code/Domain/JournalProjection.cs只读投影已完成事实，不往SaveCodec增加日志字段。改后跑Journal、EditableUi、PhoneChat及CaptureJournal；入口J，仅稳定自由探索且未坐下时可用。手机Task节点只提示收起后按J，不允许叠层。

焦点边界也是界面行为：JournalController为四个按钮设置Tab前后环及上下左右方向邻居，避免Godot默认空间搜索跳到日志背后的HUD。修改按钮结构后，须保持边界并运行Journal的DirectionalFocus真实按键检查；仅检查Tab环不够。

VS Code打开整个根目录，不只打开一个.cs。打开scripts/MainView.cs，在_Process中把显示文字“Tab 手机”暂改为“Tab 旧手机”，不要改事件ID或Flow枚举。
按Ctrl+Shift+B选择“构建游戏”，成功后Godot F5运行核对。完成后恢复文字，再构建。首次构建可用终端dotnet build GeXingzhou.csproj；无需安装额外游戏插件。
本机已检测到VS Code命令 F:\Vscode\vscode ben ti\Microsoft VS Code\bin\code.cmd；换设备请重新检查路径，不把这个路径写入公开包。Godot的C#外部编辑器在编辑器设置中选择VS Code；菜单名称/位置以本机设置为准。当前工具无法操作原生Godot设置窗口，未替你更改机器级偏好。

## F5 / F6边界和休息操作

Godot F5运行完整Boot→Main，有GameSession和交互协调。F6仅运行当前子场景：编辑器可看布局，但Phone/Dialogue在运行初态会隐藏，单独子场景不等于完整玩法试玩。VS Code F5默认含义是调试，不等于Godot运行；本轮提供的是构建任务，不声称已安装调试扩展。

长椅E：真实坐下，再默认焦点“坐一会儿”。↑↓选择，E/Enter或鼠标确认；Tab始终打开手机。“来一根”是有限动作，播放完回到坐稳；不会自动循环，本轮没有buff或烟草库存。
菜单开着时Esc先收起，菜单关闭时再Esc起身；Tab打开手机冻结动作，回来保留原阶段/帧进度，切场清理暂态。坐一会儿可以触发首次故乡观察；Esc取消对白不消耗线索。
F5保存合法世界脚底；坐起时保存站立安全落点，就位未完成时保存当前脚底。F9成功加载后站立，失败保持原动作。休息暂态不写入schema3，旧schema1/2只读升级。减少动效版本省略烟雾帧，但保留阶段和归一化进度。

## 汤店二维探索：同源编辑练习

停止游戏，打开scenes/world/SoupShop.tscn本地树。DepthLayers包含静态后景/Props/Actors；Foreground只负责前景遮挡；Navigation/GroundBoundary和Obstacles是脚圆可走区域，Anchors包含entry/exit/stand/safe/memory_return。NPC/主角脚点在Actors，左椅面在Props/Chairs/LeftChair/SeatSurface。椅面是视觉基准，不是把角色缩小后塞进家具；不可把碰撞脚点跟着臀部抬高。

移动家具图片时同步其Navigation障碍；改变合法地面/障碍/站位后：

1. `./tools/bake-navigation.ps1 -GodotExe '实际引擎路径'`导出到test-output/navigation-bake/navigation.json，不自动覆盖正式内容。
2. 比对该文件与content/vs01/navigation.json，确认是预期作者修改后复制替换正式导航JSON。
3. 加`-Check`再跑；忘导出会在验证和运行入口诊断不一致，阻止交互，而非偷偷沿用旧碰撞。
4. VS Code构建C#，跑DepthMovement/DepthSeat/ExplorationUi，再全量verify。练习后恢复原场景和JSON并再次Check。

三个可见热点在Props/Sign、Menu、CounterNote；Visual/HitPolygon决定可点击区域，根脚位置决定靠近E范围。保持soup.sign/menu/note发现ID与“先坐，汤马上好。”原文。UI点击/前景遮住时不能穿透观察，取消不能提前记发现。动画Frames/seat_pivot/contact_point metadata对应正式图像，换图后必须重新检查臀/脚/手接触点，而不是只改数值。

同源导航还检查实际物理配置：DepthLayers与Actors保持原点、无旋转且scale=(1,1)；Player保持未旋转/未缩放，collision_layer=2、collision_mask=1；地面边界不得禁用，障碍和边界collision_layer=1。角色脚圆启停由ApplyNavigation按世界模式决定。不要只改外观而不改导航，也不要关闭检查来隐藏不一致。

本轮菜单绑定路径从Menu改为MenuScroll/Menu，以便32字号双槽恢复错误可滚动。Main/Notice是保存失败/切场失败的非阻塞文字层，不代替剧情Dialogue。回忆场景的Table/Coin/DeliveryArea等必需具名节点在切入之前验证；改名时同步C#绑定和回归测试。

## 学到的知识与求职证据

这轮实际涉及C#属性/导出引用、信号与回调、状态机、焦点/输入优先级、资源依赖、等比例视域数学、回归测试与版本管理。RestStateMachine的纯逻辑测试可以不启动Godot学习算法边界和幂等性。
本地行为记录仍默认关闭，可作为后续数据清洗、SQL分析的学习来源；当前不是大数据平台，也没有接入AI生成剧情。先完成一段可演示玩法，再逐步学习数据分析或AI模块，不把未实现能力写进求职简历。

## 已验证与需要你确认的部分

自动检查已覆盖：实例未加入树时的预存节点；修改Inspector等价属性后Ready不覆盖；真实GUI鼠标命中/键盘/动画信号；三种世界视域和保存恢复。实际渲染截图与报告在08_场景润色验收记录.md。
原生Godot编辑器的手工拖动、资源保存、双击C#启动VS Code，以及嵌入/独立窗口人工试玩尚需你完成上述练习确认。脚本检查不冒充这些手工验收。
