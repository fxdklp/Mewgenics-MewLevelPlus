# MewLevelPlus

《喵喵的结合》(Mewgenics) 的一个 mod：让猫在 **10 级之后仍能继续获得法术 / 被动**，而不是只能拿属性提升。

## 功能

10 级之后，升级屏不再只有属性，而是按下面的规则刷新：

| 时机 | 显示内容 |
|---|---|
| 升级屏刚弹出（系统刷新） | 1 号位 = 升级已有被动 / 新被动替换；2 号位 = 升级已有法术 / 新法术替换；3 号位 = 新被动替换；4 号位 = 新法术替换 |
| 点 1 次「重掷」 | 纯属性 |
| 点 2 次「重掷」 | 又变回「被动 / 法术」 |
| …以此类推 | 每次重掷在「法术/被动」与「属性」之间交替 |

其中 1、2 号位会自动判断：

- **1 号位**：猫还有**未满级的被动**（1 级被动）→ 给出「升级被动到 2 级」选项；被动已全满级 → 给出「新被动替换」选项。
- **2 号位**：猫还有**未满级的法术**（名字不带 `2` 后缀）→ 给出「升级法术」选项；法术已全满级 → 给出「新法术替换」选项。

## 技术栈

- 基于 [Mewgenics Mod Framework](https://www.nexusmods.com/mewgenics/mods/183)（C# SDK + NativeAOT）
- 目标框架 `net8.0`，用 NativeAOT 编译成原生 DLL
- 通过 hook 游戏生成升级选项的函数 `0x3803b0`，改写选项的 `param` 参数来实现

## 目录结构

```
MewLevelPlus/
├── mewgenics-mod/
│   ├── src/
│   │   ├── Mod.cs              # 核心逻辑（hook 生成选项 + 改写 param）
│   │   ├── Exports.cs          # 加载器入口点
│   │   └── MewLevelPlus.csproj # 项目文件
│   └── build.sh                # 一键编译 + 部署脚本
└── docs/
    └── 可行性调研报告.md        # 逆向调研过程与结论
```

## 构建

前置条件：

1. **.NET SDK 9**（编译 NativeAOT 需要）
2. **Visual Studio 2022 Build Tools**（C++ 工作负载，NativeAOT 链接器需要）
3. **Mewgenics Mod Framework SDK**：从 [Nexus Mods 下载](https://www.nexusmods.com/mewgenics/mods/183)，把 `MewgenicsModSdk.dll` 放到 `mewgenics-mod/sdk/` 目录（该 SDK 不随仓库分发）

然后：

```bash
cd mewgenics-mod
bash build.sh
```

> `build.sh` 里硬编码了本机 MSVC / Windows SDK 的版本路径，若你的版本不同，请按需修改。

## 安装

1. 下载 Mewgenics Mod Framework 的 `version.dll` 加载器，放到游戏根目录（`...\Mewgenics\version.dll`）
2. 把编译出的 `MewLevelPlus.dll` 放到游戏的 `Mods/` 文件夹
3. 用 **Mewtator** 启动游戏（`version.dll` 注入与 Steam overlay 有时序冲突，Steam 直接启动可能失败，属正常现象）
4. 进游戏按 **F10** 管理 mod

## 已知限制

- 直接修改被动槽名（加 `+` 或 `2` 后缀）会导致图标/描述丢失甚至白屏，已废弃该方案。
- 被动升级（1→2 级）通过游戏原生 `rewardKind=9` 的「升级被动」选项实现，点击后由游戏自己处理升级，安全可靠。

## 参考

- [Skills & Passives First](https://www.nexusmods.com/mewgenics/mods/458)（开源 MIT）：提供了 `rewardKind` 枚举的关键参考（kind 8=升级法术、9=升级被动、10=混合升级、13=新被动）。源码见 <https://github.com/yun-666-666/mewgenics-skills-passives-first>。
- [Mewgenics Mod Framework](https://www.nexusmods.com/mewgenics/mods/183)：本 mod 使用的 C# SDK。

## 许可

MIT License
