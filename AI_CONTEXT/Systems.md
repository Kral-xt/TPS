# 系统说明

## ConfigSystem

配置脚本与配置实例统一位于 `Assets/_Game/Config/<Module>`。

- `GameConfigManager` 挂载于 `SampleScene/GameConfigManager`，集中持有 Audio、Weapon、Player、Enemy、BulletTime、Camera 与 CameraFeedback 配置引用。
- `PlayerConfigProvider`、`CameraConfigProvider` 与 `BulletTimeConfigProvider` 从 `GameConfigManager` 获取配置。
- `Weapon1001Presentation` 与 `CameraFeedbackManager` 从同一入口获取武器和镜头反馈配置。
- `ZombieEnemy.prefab` 保留对 `Zombie_Default.asset` 的序列化引用。
- Controller 只保存运行状态和配置缓存，不维护默认平衡值。
- 配置系统不再通过 `Resources.Load` 读取 ScriptableObject。

## PlayerSystem

- Presentation：玩家输入、动画、残影和 `TpsPrototypePlayerController`。
- Application：生命、等级、闪避、移动编排和精准闪避检测。
- Domain：玩家属性模型、移动规则、战斗规则、状态和事件。
- Config：`Assets/_Game/Config/Player/PlayerConfig.cs`。
- 数据流：`Input -> Presentation Controller -> Application Service -> Domain Rules/Model -> Presentation Event Refresh`。
- `PlayerDodgeInput` 在 `LeftAlt` 按下时优先使用 WASD 的摄像机平面方向；没有方向输入时使用角色当前 `Transform.forward`，并继续统一调用 `PlayerDodgeController.TryStartDodge`。
- 单键 ALT 不读取摄像机朝向，也不复制冷却、能量、无敌、残影或 Bullet Time 逻辑；这些规则仍由现有闪避应用流程处理。

## EnemySystem

- `EnemyController` 负责移动、攻击流程与 Animator 表现。
- `EnemyHealth` 实现 Domain 层伤害合约并处理死亡流程。
- `EnemyConfig` 为 ScriptableObject，不再作为 MonoBehaviour 挂载。
- `ZombieEnemy.prefab` 的 Controller 与 Health 共用 `Zombie_Default.asset`。

## WeaponSystem

- `Weapon1001Presentation` 负责输入、武器视觉和射击表现。
- `WeaponShootHandler` 负责射击用例、冷却、屏幕中心射线和命中调度；射击方向不再执行目标吸附修正。
- `CameraAimAssistController` 复用 `WeaponAimAssistResolver` 搜索目标，仅在玩家瞄准时驱动第三人称摄像机的 yaw/pitch 平滑偏转。
- 瞄准目标点优先使用敌人 `HeadShot` 节点，其次才回退到 AimPoint、HeadPoint、ChestPoint 或碰撞体中心。
- `WeaponConfigProvider` 从 `GameConfigManager` 提供瞄准吸附配置，`AimAssistStrength` 保持 0～1 语义。
- 伤害目标依赖 `TPS.Combat.Domain.IDamageable`，不直接依赖具体敌人类型。
- FireVFX 与 HitVFX 继续通过基础设施对象池管理。

## BulletTimeSystem

- Domain：`BulletTimeState`。
- Application：`BulletTimeController`。
- Presentation：输入、音频、后处理和运行时 Bootstrap。
- Config：`Assets/_Game/Config/BulletTime/BulletTimeConfig.cs`。
- 常驻资源与精准闪避资源保持独立，BattleView 只绑定常驻资源。
- `BulletTimeRuntimeBootstrap` 在场景加载后创建 Controller、输入、VFX、AudioSource 与 SFX 池，进入子弹时间时不创建表现组件。
- `BulletTimeAudioController` 只监听 State/Source/Energy 事件并调用 `IGameAudioService`，不再创建 AudioSource。
- `BulletTimeSource.Normal` 暂停 Gaming BGM 并恢复 BulletTime BGM；退出时反向 Pause/UnPause，双方都保留播放位置。
- 独立 `PerfectDodge` 不操作 BulletTime BGM；Normal 运行中切换到 `PerfectDodge` 时仅暂停 BulletTime BGM，不重置位置。
- 大型子弹时间 BGM 使用 Compressed In Memory + Preload Audio Data + Load In Background；短音效使用 Decompress On Load + Preload Audio Data + Load In Background。
- 音频数据常驻是消除首次进入 IO/解压峰值的有意内存取舍，禁止在 Entering 状态内同步加载或创建 AudioSource。

## StartupSystem

- Presentation：`GameStartupPresenter` 绑定 Panel 事件和进度显示；`StartGameView` 与 `LoadingView` 不直接调用场景或音频 API。
- Application：`GameStartFlow` 调度菜单音乐停止、异步场景加载、Loading 关闭和游戏音乐启动。
- Application Abstractions：`IGameAudioService`、`ISceneLoadService` 与 `GameSceneNames` 提供跨层契约。
- Infrastructure：`UnitySceneLoadManager` 封装 `LoadSceneAsync`；`GamePanelLoaderPool` 通过 `GameUIPrefabRegistry` 加载 UIKit Prefab。
- 启动数据流：`StartupScene -> StartGameView -> LoadingView -> SampleScene -> BattleView`。
- 加载进度数据流：`UnitySceneLoadManager -> GameStartFlow -> GameStartupPresenter -> LoadingView.UpdateProgress`；LoadingView 同步更新 Slider 与 `TMP_Text` 百分比。
- StartupScene 序列化绑定 AudioConfig，并提供启用的 MainCamera；GameAudioManager 在场景 Awake 阶段先于 StartGameView 完成初始化。
- `GameUIRuntimeEnvironment` 创建并复用运行时 UIRoot、UIKit 层级与 EventSystem，正式启动和直接运行 SampleScene 共用。

## AudioSystem

- Config：`AudioConfig` 集中保存 Menu/Gaming/BulletTime BGM、子弹时间提示、闪避、武器、敌人与 UI Clip/Volume，以及 AudioMixer Pitch 设置。
- Application Abstractions：`IGameAudioService` 提供语义播放接口，`GameAudio` 保存当前运行时实现；业务模块不访问 AudioSource。
- Infrastructure：`GameAudioManager` 预创建主 BGM、BulletTime BGM、SFX、UI 四个 AudioSource，并在启动时预热所有已配置 Clip。
- Menu/Gaming 共用主 BGM Source；BulletTime 使用独立常驻 Source。普通退出只调用 `Pause()`，再次进入调用 `UnPause()`，且通过主 BGM Pause/UnPause 保证互斥。
- StartupScene 通过序列化引用提供 AudioConfig；直接运行 SampleScene 时从 GameConfigManager 获取同一配置。
- 菜单阶段启用管理器上的临时 AudioListener，进入 SampleScene 前禁用，避免与 Main Camera AudioListener 重复。

## CameraSystem

- `TpsPrototypeCameraController` 从 `CameraConfig` 读取跟随、碰撞、灵敏度和 FOV 参数。
- `CameraFeedbackManager` 统一调度震动、后坐、FOV、位移和其他镜头反馈。
- 相机表现层可读取玩家状态，但不得修改玩家业务规则。

## UISystem

- QFramework Designer 自动生成文件不承载自定义业务逻辑。
- `BattleView.cs` 监听玩家属性和子弹时间事件刷新 UI。
- `BattleRuntimeBootstrap` 负责战斗 UI 的运行时接入，并复用统一 `GamePanelLoaderPool` 与 `GameUIRuntimeEnvironment`。
- `StartGameView` 只发布开始/设置点击事件；`LoadingView` 通过 `UpdateProgress` 同步显示归一化 Slider 与整数百分比文字，不读取 `AsyncOperation`。
- UI 不直接计算生命、经验或子弹时间规则。

## EmojiSystem

- Presentation：`EmojiBar` 只负责按钮与 Animator/CanvasGroup 表现；`EmojiBarController` 负责 T 键、显隐流程、Cursor 和输入模式；`PlayerEmojiController` 负责玩家 Emoji Trigger 与打断状态。
- Infrastructure：`PlayerInputGate` 提供 Gameplay/UI 全局输入门控，武器、相机、移动、跳跃、闪避和子弹时间输入均读取该状态。
- 运行时绑定：`BattleView` 通过 Designer 的 EmojiBar 引用绑定 UI，并在 `PlayerAttributeRuntimeBootstrap.EnsureFor` 后注入 `IPlayerEmojiController`；玩家重生后重新绑定。
- 数据流：`T/Emoji Button -> EmojiBarController -> EmojiBar / IPlayerEmojiController -> Player Animator`。
- Animator：EmojiBar 使用 `Hidden/Show/Visible/Hide`；玩家使用 `Emoji1-6` 和 `StopEmoji`，六个 Emoji Clip 循环播放。
- 生命周期：EmojiBar GameObject 保持 Active，默认由 CanvasGroup 隐藏；监听器按实例精确注册和注销，不使用 `RemoveAllListeners`。

## PoolSystem

- 统一实现位于 `Assets/_Game/Infrastructure/Pool`。
- Presentation 的残影、命中特效、开火特效和远程敌人子弹通过池接口获取与回收。
- 运行时禁止在高频表现路径中持续 Instantiate/Destroy。

## RangedEnemySystem

- `RangedZombieEnemyController` 只锁定 `DetectionRange` 内的玩家，保持敌人根节点位置不变，并仅水平旋转 `Model` 视觉根节点。
- `RangedZombieEnemyConfig` 继承 `EnemyConfig`，复用生命、经验、攻击伤害、攻击冷却和旋转速度，并新增索敌距离、弹速、命中半径、弹道寿命与动画事件备用延迟。
- 攻击优先由现有 Zombie Attack 动画事件调用 `EnemyAnimationEventRelay` 释放弹道；若动画事件未触发，则按配置延迟执行一次备用释放，单次攻击不会重复发射。
- `RangedBullet` 使用 `SphereCastNonAlloc` 处理高速弹道，命中玩家后通过 `IIdentifiedAttackDamageable` / `IDamageable` 进入 `PlayerHealthController`，不直接写玩家生命值。
- 子弹复用 `Assets/Resources/VFX/Prefab/FireVFX.prefab`，使用独立 `PoolObjectType.Bullet` 池；远程池实例运行时染为红色并添加红色 `TrailRenderer`，不修改玩家 FireVFX 资源。
- `RangedZombieEnemy.prefab` 位于 `Assets/_Game/Prefabs/Enemy`，嵌套复用 Android Zombie FBX 和现有 Animator，保留 `EnemyHealth`、身体/爆头碰撞与受击反馈，不包含 `EnemyController` 或 `NavMeshAgent`。
- `RangedBullet` 以 `ProjectileType.EnemyBullet` 注册到 `ProjectileDodgeRegistry`；玩家闪避期间由 `PerfectDodgeDetector` 统一执行距离、接近方向、预计最近接时间与碰撞半径判断，敌人控制器不直接触发 Bullet Time。
- `RangedZombieEnemyConfig.CanTriggerDodge` 控制弹道是否参与精准闪避。成功闪避的弹道保持原速度和轨迹继续飞行，但忽略该玩家的碰撞伤害，回池、禁用或销毁时注销检测。
- 同一次闪避可标记全部满足命中预测条件的敌方弹道，Bullet Time 仅触发一次；注册表在 Unity Subsystem 初始化时清空，兼容关闭 Domain Reload 的运行模式。

## GrayboxLevelSystem

- 独立测试场景位于 `Assets/Scenes/TpsParkourGraybox.unity`，由 `SampleScene` 复制后保留玩家、TPS 相机、战斗 UI、配置和对象池运行环境。
- 新场景只在自身停用旧 `Building` 与 `Enemy` 根节点，不影响原 `SampleScene`，且未加入 Build Settings。
- 地图根节点为 `GrayboxLevel`，下分 `01_MovementTutorial`、`02_WallRunTraining`、`03_VerticalCombat`、`04_HighSpeedChallenge`、`05_FinalCombat`、`EnemyTestPoints` 与 `LevelMarkers`。
- 地图沿 +Z 方向线性推进，包围盒约 100m x 18.5m x 758.5m，有效路线从 Z=-30m 延伸到 Z=728.5m；五阶段之间保留独立移动和节奏缓冲距离。
- 尺寸基于 `PlayerAttributeConfig.asset`：冲刺速度 12m/s、墙跑最长 1.25s、跳跃高度 2m、二段跳高度 1.8m；连续墙跑面长 10-14m，段间距约 4-6m。
- 颜色约定：灰色为地面/结构，青色为 `Wall` 墙跑面，绿色为 `Climb`/`Cross` 穿越点，黄色为空中平台，红色为战斗掩体，蓝色地标为高速主路线。
- 新地图复用现有 `ZombieEnemy.prefab`，敌人压力按 0、2、7、3、12 递增，共布置 24 个地面与高台测试点；`GrayboxLevel` 持有独立 `NavMeshSurface` 和烘焙数据。

## 核心交互

`玩家输入 -> Player/Weapon Presentation -> Application -> Domain -> 事件 -> UI/VFX`

`武器命中 -> IDamageable -> EnemyHealth -> Combat events -> 经验/子弹时间/UI`

`配置资产 -> GameConfigManager/Prefab 序列化引用 -> Provider/Controller 初始化缓存 -> 运行时流程`

## EnemyWeakPointSystem

- Domain contract: `HitPartType`, `HitPartInfo`, and `IHitPartResolver` live in `Assets/_Game/Domain/Combat`.
- `EnemyHealth` owns collider-to-hit-part resolution. `HeadShot` returns the enemy-configured temporary critical chance bonus; normal non-trigger colliders remain `Body`.
- Weapon raycasts include triggers, but only triggers accepted by `IHitPartResolver` participate in hit selection.
- A weak-point trigger may override the same enemy's body collider, while walls and other targets continue to block the shot.
- `PlayerCombatAttributeController` applies the bonus only to the current damage roll and never writes it back to player attributes.
- Zombie default head-shot critical chance bonus is configured by `EnemyConfig.headShotBonusCriticalChance` at `0.5`.

## KillFeedbackSystem

- `DamageInfo.HitPart` carries the lethal hit part without mutating player attributes.
- `EnemyKilledEvent.IsHeadShot` exposes the confirmed lethal weak-point result to presentation listeners.
- `KillIconController` listens to the existing combat event, tracks the current battle kill streak, reuses the existing Animator, and maps normal kills to `KillIcon1` through `KillIcon6` while head-shot kills use `HeadShot`.
- The controller is attached to the existing `BattleView/KillIcon` node; no QFramework Designer file is modified.
- KillAudioConfig 在 Assets/_Game/Config/KillAudioConfig.asset 提供 Kill1-Kill8 与 HeadShot 音频槽位，并由 GameConfigManager 统一持有。
- KillIconController 先判断 `EnemyKilledEvent.IsHeadShot`，爆头反馈优先于连杀图标，但仍正常累计连杀数。
- KillIconController 在处理同一个 EnemyKilledEvent 时调用 IGameAudioService.PlayKillFeedback；GameAudioManager 使用现有 2D SFX AudioSource 播放并在启动时预热已配置的击杀音频。
- `StartupScene/GameAudioManager` 序列化绑定 KillAudioConfig；运行时同时保留从 `GameConfigManager` 延迟解析的兜底路径，避免启动顺序导致音频配置为空。

## ItemSystem

- 配置脚本位于 `Assets/_Game/Config/Item`，运行时加载器位于 `Assets/_Game/Infrastructure/Config/Item`，不在 Resources 中存放业务脚本。
- 基础物品配置资产位于 `Assets/Resources/Config/Item`，由独立 `ItemConfigManager` 首次访问时一次加载并按唯一 ID 建立只读查询索引。
- `ItemConfig` 保存 ID、图标、名称、描述、品阶和多标签类型；Description 由策划在自定义 Inspector 中填写，并通过配置 Provider 进入只读展示数据，不写入玩家存档。
- `ItemQualityConfig` 通过可序列化条目统一维护 1-5 品阶颜色；UI 通过 `GetQualityColor` 获取颜色，不写死品质色。
- 重复 ID 在 Inspector 中提示；运行时会将该 ID 的全部冲突项标记为无效，避免 Resources 加载顺序导致非确定查询结果。

## InventorySystem

- Domain：`PlayerInventoryModel` 统一维护 `ItemID -> Count`，拒绝非法添加、数量不足删除，并在数量归零时移除键；公开集合使用只读包装，外部不能绕过控制中心修改。
- Application：`PlayerInventoryService` 负责启动加载、增删查询与变更后自动保存；`InventoryDisplayService` 通过 `IItemConfigProvider` 把库存转换为按 ItemID 排序的只读 UI 展示数据。
- Infrastructure：`InventoryJsonStore` 负责存档；`InventoryItemConfigProvider` 适配 `ItemConfigManager`，集中提供物品配置与品阶颜色，缺失配置只记录一次并跳过显示。
- Presentation：`PlayerInventoryController` 提供库存入口、展示查询和变更通知；`BagDialogController` 监听 B/ESC、维护单实例、输入模式和跨窗口 ItemCellPool；`BagDialog` 只协调界面生命周期、分类选择和动画。
- UI 列表：`BagCategoryCache` 一次展开数量并建立六类索引；`BagItemLoader` 按 ScrollRect 可视行与缓存行虚拟化 Content，每帧最多创建 20 个 Cell；`ItemCellPool` 在滚动、分类切换和重新打开时复用实例，不再按库存总量创建对象。
- UI 详情：`ItemDetailController` 保证只显示一个详情并处理外部点击、快速切换和 Cell 回收；`ItemDetailView` 刷新品质色、图标、名称、多类型与描述，并播放 `ItemDetail@Show` / `ItemDetail@hide`。
- UI 资源：`BagDialog.prefab` 与 `ItemCell.prefab` 仍通过 `GameUIPrefabRegistry` 注册；ItemCell 原有 Btn 已拉伸覆盖 Cell，ItemDetail 显示时临时挂到 BagDialog 根节点以避免 Viewport 裁剪，关闭后归还原 Cell；Animator 继续使用 `Bag@Show` / `Bag@Hide`。
- 背包存档只保存物品 ID 和数量；名称、图标、品阶、类型与品质色均在刷新时从配置查询，不写入玩家存档。
- 数据流：`B -> BagDialogController -> UIKit/BagDialog -> PlayerInventoryController -> InventoryDisplayService -> IItemConfigProvider -> ItemConfigManager`；库存写入仍为 `掉落/使用 -> PlayerInventoryController -> PlayerInventoryService -> PlayerInventoryModel -> InventoryJsonStore -> inventory.json`。
