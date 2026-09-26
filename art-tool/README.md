# 美术资源工具 1.1.1

[首页与下载](../README.md#选择下载) · [English](README.en.md) · [安装步骤](../docs/INSTALL.md#art-tool) · [常见问题](../docs/FAQ.md#art)

作者：**BrickZhou/青春啊砖在he边看月亮** · GPL-3.0-only

`一键导出美术资源.exe` 负责导出，`打开美术资源库.cmd` 负责打开已有结果。玩家使用成品 EXE 无需额外安装 Python 或 BepInEx。

## 命令行用法

在 EXE 所在文件夹打开 PowerShell：

```powershell
& '.\一键导出美术资源.exe' --version
& '.\一键导出美术资源.exe' --no-open
& '.\一键导出美术资源.exe' --game 'D:\Game' --output 'D:\Images'
```

| 参数 | 用途 |
|---|---|
| `--game PATH` | 指定游戏目录 |
| `--output PATH` | 指定输出目录；默认在游戏目录的 ArtExports |
| `--no-open` | 导出完成后不自动打开浏览器 |
| `--force` | 强制重新导出 |
| `--match TEXT` | 筛选资源包；建议为筛选结果选择单独输出目录 |
| `--interactive` | 结束后等待按回车键 |
| `--version` | 显示工具版本与署名 |

双击 EXE 会在结束后保留窗口，按回车退出。查看 CMD 不会重新导出，且只打开自身旁的 `ArtExports/index.html`；自定义输出请直接打开该目录中的 HTML。

## 输出与范围

- 自定义输出须为空或已由本工具创建；游戏目录内部仅允许输出到 `ArtExports`。
- 图片包括 Sprite、Texture2D、Texture2DArray；浮点纹理同时保存原始数据与 8 位 PNG 预览。
- 源资源只读，导出前后校验哈希；报告在 `report.json`，失败记录在 `errors.json`。
- 不导出音频、模型、骨架动画或完整 Unity 工程。图片不包含游戏内灯光与材质合成。
- 游戏美术归原权利人所有，工具 GPL 许可不覆盖游戏资源。

## 开发与许可证

[构建及测试步骤](../docs/BUILD.md#art-tool) · [验证记录](../docs/VALIDATION.md)

第三方许可随程序提供，导出后位于 `ArtExports/third-party-licenses`；工具许可与署名文件为 `TOOL-LICENSE.txt` 和 `TOOL-COPYRIGHT.md`。发行包排除了未使用的 FMOD 原生音频库。
