# 安装 BepInEx 前置

[返回首页](../README.md) · [English](README.en.md) · [CheatMenu 安装](../docs/INSTALL.md#cheatmenu)

**仅 CheatMenu 需要此前置，美术工具无需安装。** BepInEx 由 **BepInEx 团队及其贡献者**开发。本站提供下载助手，加载器原包从官方服务器获取。

| 需要的版本 | 需要的类型 |
|---|---|
| 6.0.0-be.788+5b766a3 | Unity.IL2CPP-win-x64（不是 Mono，也不是 x86） |

## 推荐：双击下载助手

1. [下载 01-BepInEx-Download-788.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/01-BepInEx-Download-788.zip)，完整解压到一个普通文件夹。
2. 双击其中的 **`下载BepInEx前置.cmd`**，保持它与 `Download-BepInEx.ps1` 在同一文件夹。助手会下载官方包并自动核对 SHA-256；它不会安装或修改游戏。
3. 成功后，打开**与该 CMD 同级的 `Downloads` 文件夹**。这是助手自己的子文件夹，不是 Windows 用户目录中的“下载”。
4. 找到 `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip`。下一步安装的是**这个新下载的 ZIP**。

```text
你解压下载助手的文件夹/
├─ 下载BepInEx前置.cmd
├─ Download-BepInEx.ps1
└─ Downloads/
   └─ BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip
```

**只解压 01 下载助手不会装好前置。** 不要直接在 ZIP 预览窗口里运行 CMD。

## 安装刚下载的 BepInEx 原包

1. 关闭游戏。在 Steam 中选择游戏 → 管理 → 浏览本地文件。
2. 打开刚下载的 BepInEx ZIP，将**内部全部内容**解压到游戏根目录，使下列文件与游戏 EXE 同级，不要额外套一层压缩包名称文件夹。
3. 启动游戏一次，等待首次初始化完成，进入主菜单后退出。首次初始化可能联网下载依赖，比平时启动慢；若失败，先查看 [FAQ](../docs/FAQ.md#loader)。
4. 检查 `BepInEx/interop/Assembly-CSharp.dll` 已生成，再按 [CheatMenu 安装步骤](../docs/INSTALL.md#cheatmenu)放入插件。

```text
游戏目录/
├─ The Rogue Prince of Persia.exe
├─ winhttp.dll
├─ doorstop_config.ini
├─ dotnet/
└─ BepInEx/
   ├─ core/
   └─ interop/                  ← 首次初始化后生成
      └─ Assembly-CSharp.dll
```

已正确安装同版本、同类型前置时可以跳过。若装有其他版本或其他加载器，请先记录其版本、备份相关配置并核对兼容性，不要直接覆盖未知安装。

## 下载失败时：手动获取

[直接下载官方固定版本 ZIP](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip) · [官方构建列表](https://builds.bepinex.dev/projects/bepinex_be)

核对文件名、架构及 SHA-256，再按上面的安装步骤解压。可在 PowerShell 中使用下列命令，将路径换成实际下载位置：

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'D:\Downloads\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip'
```

期望 SHA-256：

```text
F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A
```

校验不一致时不要安装。下载助手遇到同名但校验不同的文件会停止；可将助手解压到新的空文件夹后重新下载。

<details>
<summary>高级：指定下载目录</summary>

在助手文件夹打开 PowerShell 后运行：

```powershell
.\Download-BepInEx.ps1 -OutputDirectory 'D:\Downloads'
```

此参数会覆盖默认的“脚本旁 Downloads”位置。普通安装使用双击 CMD 即可。

</details>

## 上游与许可

[上游源码](https://github.com/BepInEx/BepInEx/tree/5b766a3) · [LGPL-2.1 许可证](https://github.com/BepInEx/BepInEx/blob/5b766a3/LICENSE) · [官方 IL2CPP 安装文档](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html)

下载助手由 **BrickZhou/青春啊砖在he边看月亮** 编写，采用本仓库 GPLv3；BepInEx 本身保留上游作者及许可。
