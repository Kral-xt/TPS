# 项目架构

项目类型：TPS（第三人称射击）
技术栈：Unity 6 + QFramework

## 目录规范

业务脚本统一位于 `Assets/_Game`，先按架构层分组，再按业务模块分组：

```text
Assets/_Game
├── Presentation
│   ├── Player
│   ├── Enemy
│   ├── Weapon
│   ├── Camera
│   ├── BulletTime
│   ├── Combat
│   ├── Inventory
│   ├── UI
│   └── VFX
├── Application
│   ├── Player
│   ├── Weapon
│   ├── BulletTime
│   ├── Combat
│   ├── Inventory
│   └── Abstractions
├── Domain
│   ├── Player
│   ├── Enemy
│   ├── Camera
│   ├── BulletTime
│   ├── Combat
│   └── Inventory
├── Infrastructure
│   ├── Audio
│   ├── Config
│   ├── Pool
│   ├── AssetLoading
│   ├── Persistence
│   └── HotUpdate
└── Config
    ├── Audio
    ├── Player
    ├── Enemy
    ├── Weapon
    ├── Camera
    ├── BulletTime
    └── Item
```

ScriptableObject 配置脚本与配置实例统一位于 `Assets/_Game/Config/<Module>`。运行时通过场景中的 `GameConfigManager` 序列化引用获取配置，不再使用 `Resources.Load` 加载配置。

`Assets/Resources` 只存放必须通过 Resources 运行时加载的 Prefab、纹理、材质、音频、动画与 VFX，禁止存放业务 `.cs` 和 ScriptableObject 配置实例。

## 分层职责

### Presentation

负责 MonoBehaviour、输入、UI、动画、相机和 VFX 表现。只调用 Application 入口或读取只读状态，不实现核心数值规则。

### Application

负责用例编排、状态流转和系统调度。通过接口访问基础设施，不直接依赖具体资源加载、存档和对象池实现。

### Domain

负责状态模型、游戏规则、计算和跨模块合约，尽量不依赖 Unity API，不包含 MonoBehaviour。

### Infrastructure

负责配置加载、对象池、资源加载、存档和热更新适配。实现 Application 声明的基础设施接口。

## 依赖方向

`Presentation -> Application -> Domain`

`Infrastructure -> Application abstractions / Domain contracts`

运行时 Bootstrap 负责把具体基础设施实现接入应用流程。禁止 Domain 反向依赖 Presentation、Application 或 Infrastructure。

## 配置规范

- 玩家配置：`Assets/_Game/Config/Player`
- 敌人配置：`Assets/_Game/Config/Enemy`
- 相机配置：`Assets/_Game/Config/Camera`
- 子弹时间配置：`Assets/_Game/Config/BulletTime`
- 武器配置：`Assets/_Game/Config/Weapon`
- 统一入口：`Assets/_Game/Infrastructure/Config/GameConfigManager.cs`
- 音频配置：`Assets/_Game/Config/Audio/AudioConfig.cs`

Controller 只缓存运行时状态与读取后的配置值，不再声明默认平衡数值。配置资产名称和位置在本次迁移中尽量保持不变，以保护序列化引用。

## 迁移约束

本次目录迁移保留现有命名空间和脚本 `.meta` GUID，避免 Prefab、Scene 和 ScriptableObject 丢失引用。后续若调整命名空间，应作为独立任务执行并重新验证所有序列化资源。
