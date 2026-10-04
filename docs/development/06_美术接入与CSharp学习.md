# 本轮可以学习什么

先试玩，学习不阻塞游戏制作。本轮把表现层换成图像，任务规则仍在code/Domain，存档格式未变。

1. PNG是图片数据；Texture2D是引擎加载的纹理；Sprite2D是在世界里画纹理的节点。AnimatedSprite2D按SpriteFrames切换图像，PlayerController只根据已有速度选择idle/walk，不另造一份移动规则。
2. AtlasTexture把大图的一块当作贴图使用。Rect2四个数字是左上x/y及宽/高，不是右下坐标。AI图集并不天然等距，ArtAssets中的区域是测量数据。
3. C#对象初始化器按书写顺序设置属性。本轮实机发现：TextureRect先设Texture/Size，再设ExpandMode，原图最小尺寸可能已经夹大请求尺寸。已改为先IgnoreSize再赋纹理与尺寸，新增180×140汤碗上限及1280×720背景断言。可结合[Godot TextureRect文档](https://docs.godotengine.org/en/stable/classes/class_texturerect.html)理解。
4. 运行正常不代表发布包资源齐全。字符串动态加载的PNG要明确加入export_presets的include_filter；本轮增加真实引擎读取配置并匹配七路径的检查。完整EXE还需匹配模板和实际启动。
5. 硬币仍有大于可见图案的按钮区域。使用Button的icon与主题常量icon_max_width约束图标，保留Tab／方向键／E以及鼠标拖动的原规则，见[Godot Button文档](https://docs.godotengine.org/en/stable/classes/class_button.html)。

一个可选小练习：找到PlayerController里的SetAnimationSpeed，解释为什么idle为3、walk为8，并只把idle改为2。先运行tools/verify.ps1，再实际观察待机；这改变表现，不应改变走路速度、剧情计时或存档。看完后可恢复，不需要同时学习算法或大数据。
