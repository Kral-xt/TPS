# TPSDemo1

TPSDemo1 是一个基于 Unity 6 开发的第三人称射击（TPS）原型项目，当前重点验证角色移动、战斗、武器、敌人、子弹时间、UI 和背包等游戏系统的组合与运行流程。

项目仍处于原型开发阶段，部分目录同时包含第三方插件示例和演示资源。正式运行入口以 `Assets/Scenes/StartupScene.unity` 为准，主场景为 `Assets/Scenes/SampleScene.unity`。

## 功能概览

- 第三人称角色移动、跳跃、冲刺和蹲伏
- 摄像机跟随、瞄准观察和反馈效果
- 武器、攻击、战斗和敌人系统
- 子弹时间（Bullet Time）玩法支持
- 启动、加载、菜单、战斗和背包 UI
- 背包物品分类、数量拆分、物品详情和列表复用
- 物品配置、玩家配置、武器配置和敌人配置
- 音频、VFX、对象池、场景加载和数据持久化基础设施
- GM 调试视图，用于测试背包数据和 UI 流程

## 技术栈

- Unity `6000.0.59f2`
- Universal Render Pipeline（URP）版本 `17.0.4`
- Input System `1.14.2`
- Cinemachine `3.1.7`
- AI Navigation `2.0.13`
- ProBuilder `6.1.2`
- Visual Effect Graph `17.0.4`
- QFramework 相关工具链（UIKit、ResKit 等）
- Unity MCP（开发辅助工具）

## 快速开始

### 环境要求

- Unity Hub
- Unity `6000.0.59f2`
- Git
- Git LFS

### 获取项目

项目使用 Git LFS 管理 PSD、PSB 和 UnityPackage 等大文件，首次克隆前请先初始化 Git LFS：

```bash
git lfs install
git clone https://github.com/Kral-xt/TPS.git
cd TPS
```

使用 Unity Hub 添加项目目录，并选择 Unity `6000.0.59f2` 打开。项目依赖清单位于 `Packages/manifest.json` 和 `Packages/packages-lock.json`。首次打开时等待 Package Manager 完成依赖解析和资源导入。

### 运行场景

1. 打开 `Assets/Scenes/StartupScene.unity`，运行完整启动流程。
2. 需要直接调试主场景时，打开 `Assets/Scenes/SampleScene.unity`。
3. UI 独立测试场景位于 `Assets/Scenes/TestUIPanels/`。

Build Settings 当前已配置 `StartupScene` 和 `SampleScene`。

## 默认操作

| 操作              | 键位            |
| ----------------- | --------------- |
| 移动              | `WASD` / 方向键 |
| 视角              | 鼠标移动        |
| 攻击              | 鼠标左键        |
| 互动              | `E`             |
| 跳跃              | `Space`         |
| 蹲伏              | `C`             |
| 冲刺              | `Left Shift`    |
| 切换上一项/下一项 | `1` / `2`       |
| 打开背包          | `B`             |
| 打开 GM 调试视图  | `G`             |
| 关闭窗口或取消    | `Escape`        |

角色和 UI 输入动作的基础定义位于 `Assets/InputSystem_Actions.inputactions`。部分系统快捷键由运行时调试逻辑提供。

## 项目结构

```text
Assets/
├── _Game/
│   ├── Presentation/      UI、输入、动画、相机和 VFX 表现
│   ├── Application/       流程编排、状态流转和系统调度
│   ├── Domain/             游戏规则、状态模型和数值计算
│   ├── Infrastructure/     配置、资源、对象池、音频和持久化
│   └── Config/             各游戏模块的配置脚本和配置资源
├── XGame/Runtime/          业务 UI 运行时代码
├── XGameAssets/             UI 模块 Prefab、动画和表现资源
├── Resources/               运行时必须动态加载的资源
├── Scenes/                  启动、主流程和 UI 测试场景
├── Scripts/                 辅助脚本和 GM 调试入口
└── Plugins/                 第三方插件和项目工具

Packages/                    Unity 包依赖
ProjectSettings/             Unity 项目、输入、标签和构建设置
AI_CONTEXT/                  项目架构、模块依赖和开发上下文
```

## 架构约束

项目业务代码按以下方向组织依赖：

```text
Presentation -> Application -> Domain
Infrastructure -> Application abstractions / Domain contracts
```

- Presentation 负责 UI、特效、动画、相机和输入表现，不承载核心业务规则。
- Application 负责流程控制、系统调度和状态流转。
- Domain 负责游戏规则和数值计算，尽量不依赖 Unity API。
- Infrastructure 负责配置、资源加载、对象池、持久化和 Unity 适配。
- `Assets/Resources` 主要存放运行时必须动态加载的表现资源，不用于存放业务脚本和常规配置实例。

详细架构和模块依赖见 `AI_CONTEXT/ProjectArchitecture.md`、`AI_CONTEXT/Systems.md` 和 `AI_CONTEXT/ModuleDependency.json`。

## Git 说明

以下目录由 Unity 自动生成，不应提交：

- `Library/`
- `Build/`
- `Logs/`
- `UserSettings/`
- `Temp/`
- `Obj/`

项目已配置 `.gitignore` 和 `.gitattributes`。PSD、PSB、UnityPackage 使用 Git LFS 管理，提交或克隆大资源时请确认本机已安装并启用 Git LFS。

## 当前状态

项目目前以系统原型和功能验证为主，背包、物品详情、列表复用和 GM 调试流程已有运行验证记录。正式发布前仍需补充完整的玩法流程、资源整理、性能测试、平台构建测试和版本发布说明。

## 许可证

当前仓库尚未声明统一的开源许可证。项目内部分资源和第三方插件可能有独立授权条款，使用或再分发前请分别确认其许可证要求。
