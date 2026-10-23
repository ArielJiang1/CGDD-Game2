# Blockout Kit：Watch · Peek · Act · Hide（白盒工具）

生成一套白盒模块 Prefab，外加一个起步用的场景：**Ch.1 The Lesson（教室）+ 走廊 + Ch.4 The Cafeteria（食堂）+ Metrics Zoo**。

## 使用步骤

1. 双击 `BlockoutKit.unitypackage`（或者拖进 Project 窗口），然后点 **Import**。
2. 菜单 **Blockout > 1. Build Module Kit (Prefabs)**：模块会生成到 `Assets/Blockout/Prefabs`。
3. 菜单 **Blockout > 2. Build School Level Scene**：场景会保存到 `Assets/Blockout/Scenes/Blockout_School.unity`。
4. 导入课程给的 `FlyCamera.unitypackage`，把 `StudentTool/FlyCamera` 拖进场景，按 **Play** 并录屏。
5. 截图：展开 `ScreenshotCameras`，**一次只启用一个**相机，在 Game 视图里截图，截完再关掉。

> 步骤 3 会新建一个场景。这个场景归**负责在 Unity 里搭场景的那位同学**所有，另一位同学不要同时编辑它。

## 用玩家的尺寸"走"一遍（作业里强调的重点）

书上说："*Do NOT just fly around in the editor view.*" 所以：

- 在 `Corridor` 下启用 `Player_Walker`，同时**禁用 FlyCamera**（它会关掉其他所有相机），然后按 Play，并在 Game 画面里点一下。
- 操作：**按住右键**移动鼠标转视角（或单击锁定鼠标，Esc 释放）；WASD 移动，Shift 跑，Space 跳。
- **坐椅子**：看着一把椅子（屏幕中央有准星），出现 `[E] Sit` 时按 **E** 坐下，再按 **E** 站起来。
  - 每把椅子只有一个座位点；坐下后眼高 = **1.25 m，和所有坐着的 NPC 眼罩高度一致**。
  - 有同学坐着的椅子会显示 `Seat taken`。你自己的黄色座位可以坐，坐下时黄色占位小人会隐藏。
  - 坐着时身体不动，头最多左右转 110°。
- Walker 的碰撞体是 1.0 × 1.8 m，站立眼高 1.5 m（书中 Unity 的 metrics：1.5–1.7 m）。
- 左下角的状态框会显示当前眼高、是否着地、输入系统等信息。
- 坐到后排角落，看老师在教室里走一圈：前排同学能不能挡住他的视线？

## 颜色语言（参考 David Shaver 在 GDC 2018 的演讲 "Invisible Intuition"）

| 颜色 | 含义 |
|---|---|
| 浅灰 / 米白 | 地板 / 墙 |
| **蓝色** | **Cover（掩体）**：凡是能挡住观察者视线的东西都用蓝色，包括同学、书堆、储物柜、柱子 |
| **黄色** | 玩家（座位、比例人） |
| **红色** | 权威/观察者（老师、摄像头），红色聚光灯和地上的扇形 = 他们的视线 |
| 绿色 | 地标（黑板，从两扇门都能看到） |
| 橙色 | 老师巡逻路线的路点、标尺 |
| 米色 | 普通家具 |

人形模型上的青色"眼罩"条标出的是**眼睛高度和视线方向**。

## 尺寸（来源：The Level Design Book 的 *Metrics* 章节，Unity 部分）

| 项目 | 数值 |
|---|---|
| 网格 | 1 m（模块占地 2 m）→ 编辑器的 Grid Snapping 设为 1 或 2 |
| 玩家 | 1.0 × 1.8 m，眼高 1.6 m，坐姿 1.25 m |
| 墙 | 高 3.0 m（约为比例人身高的 167%），厚 0.2 m |
| 门 | 1.25 × 2.5 m，就是一个缺口，不做门框 |
| 窗 | 窗台高 1.0 m，窗口上沿 2.2 m |
| 走廊 | 4 m（书中最小值是 2 m） |
| 教室过道 | 1.3 / 1.6 m（有意比走廊窄，老师走过时会离你很近） |

所有数值都集中在 `Editor/BlockoutMetrics.cs`。改完之后重新运行 **1. Build Module Kit**，已经放进场景的 Prefab 会自动更新。

## 灯光（参考 Robert Yang 在 GDC 2018 的 "How to Light a Level"）

- 白盒阶段关掉灯光烘焙：**Window > Rendering > Lighting**，取消勾选 **Auto Generate**。所有灯光都已经设为 Realtime。
- 天花板是 *Shadows Only*（看不见，但会投下阴影），所以阳光只能从西边的窗户照进来，FlyCamera 从上往下拍也能看到室内。
- 老师和摄像头身上的红色聚光灯 = 视线；灯光的**阴影**就是掩体后面的盲区。
- 如果用 URP：聚光灯的阴影需要在 URP Asset 里开启 **Additional Lights > Cast Shadows**。

## Pivot 约定（之后用 Blender 模型替换时要保持一致）

- `Floor_* / Podium_* / Ceiling_*`：pivot 在最小角 (0,0)，顶面 y=0
- `Wall_*`：pivot 在墙的起点、墙体中线上、地面高度；墙沿 +X 方向延伸
- 其他所有模块：pivot 在底部中心，正面朝 **+Z**
- 单位：1 Blender 单位 = 1 m。导出 FBX 时选 *Apply Transform*，前向 −Z / 上方向 Y 的设置按 Unity 的要求来

## 老师巡逻

`Watcher_Teacher/Patrol_Waypoints` 下有 WP0–WP6：

- 移动或旋转路点，可以改变老师**走到哪里、在那里看向哪里**。
- `waitSeconds` 控制在该路点停留的时间。WP0 = 背对全班写板书（**安全窗口**），WP1 = 扫视全班（**危险**）。
- 在 `TeacherPatrol` 上调 `walkSpeed`，可以改变整个节奏。

## 这个场景只是初稿

> "99% of the time, your blockout will not survive a playtest." — The Level Design Book

走一遍，改一改，再走一遍。可以问自己的问题：
- 坐在你的座位上，老师在哪些位置能看到你的手？
- 安全窗口太长还是太短？
- 前面的同学够不够挡住老师的视线？
- 在食堂里，柱子后面的影子是不是真的能当安全区？
