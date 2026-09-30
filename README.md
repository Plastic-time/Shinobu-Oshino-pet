# 忍野忍 · Shinobu Oshino Pet

一个独立运行的 **Windows 桌宠**。采用《化物语》中忍野忍的头盔与护目镜造型：深绿头盔、金发金瞳、粉裙红结。

她会安静地待在你放下的位置，偶尔眨眼，跟着鼠标转动视线。点一下挥手，双击跳跃。无需账号、浏览器或其他客户端，全程离线。

An unofficial Shinobu Oshino desktop companion for Windows. A small, offline, transparent window with click interactions, dragging, gaze following and a system tray menu. She stays where you put her.

![Windows 桌宠实机预览，背景为测试展示窗口](previews/desktop-preview.png)

## 下载并运行

**[下载最新版 Windows 桌宠](https://github.com/Plastic-time/Shinobu-Oshino-pet/releases/latest)**

1. 下载 `Shinobu-Oshino-pet-windows-v2.0.0.zip`，解压到你喜欢的文件夹。
2. 双击 `Shinobu-Oshino-pet.exe`。第一次运行时，她会出现在主屏幕右下方。
3. 按住角色拖到合适的位置。右键角色或任务栏右下角的托盘图标可打开菜单。

支持 Windows 10 / 11，使用系统的 .NET Framework；无需安装 Python、Node.js 或额外软件包。程序和图片打包在同一个 EXE 中，无需管理员权限。已在 Windows 11 环境验证；其他系统尚未实机测试。

程序尚未进行代码签名，Windows 下载保护可能显示未知发布者。可以核对发布页的 SHA-256，或从源码自行构建；不需要关闭杀毒软件。

## 怎么陪她玩

| 操作 | 效果 |
| --- | --- |
| 单击 | 挥手；等待系统双击判定后播放 |
| 双击 | 跳一下，然后回到待机 |
| 按住左键拖动 | 改变位置，松手后记住位置 |
| 移动鼠标 | 16 向视线跟随，停止一会儿恢复正面 |
| 右键 → 其他动作 | 眨眼、原地跑、等待、思考等 |
| 右键 → 大小 | 100%、125%（默认）、150%、200% |
| 右键 → 暂停动画 | 暂停当前画面，仍可拖动；点击互动会恢复动画 |
| 托盘图标双击 | 回到主屏幕；找不到她时可用 |
| 右键 → 退出 | 关闭桌宠，同时移除托盘图标 |

默认固定在原地，不会自动走动。默认置顶，可在菜单里取消。透明区域不挡住后面的窗口；角色不会抢走正在输入的窗口焦点。移动到屏幕边缘时会保留在可用桌面内，断开显示器后会调整位置。

设置只保存在 `%LOCALAPPDATA%\ShinobuOshinoPet\settings.txt`，包括位置、大小、视线、置顶和暂停选项。不自动设置开机启动，不联网，不读取你的文档或键盘输入。

## 动画预览

| 日常动作 | 视线跟随 |
| --- | --- |
| ![日常动作](previews/all-states.gif) | ![视线循环](previews/look-loop.gif) |

[观看 MP4](previews/all-states.mp4) · [全部帧预览](previews/contact-sheet.png) · [16 向视线对照](previews/look-directions.png)

## 源码与素材

- [Windows 程序源码、构建与测试](desktop/README.md)：C# / Windows Forms / Win32 逐像素透明窗口，不使用第三方运行依赖。
- [透明精灵图](assets/spritesheet.png)：1536 × 2288 px，8 列 × 11 行，每格 192 × 208 px。
- **9 种动作、16 向视线，共 73 帧**；[布局与逐帧时长](sprite-layout.json)。
- `running-right`、`running-left` 是跑步动作；`running` 是思考动作。桌宠菜单里的跑步在原地播放。

角色图像由 AI 图像生成工具生成，经提取、对齐、去底色和组装。部分相邻视线方向较接近，尤其 337.5° 的左向分量较弱，详见 [素材质量摘要](quality-summary.json)。

## 检查与隐私

独立桌宠已验证透明渲染、点击、双击、拖动、缩放、暂停、屏幕边界、右键退出、资源释放及损坏设置恢复。实机预览仅截取专用测试窗口，不包含个人桌面。

公开文件排除了私人制作工程、账号 ID、临时上传链接、个人目录和登录凭据。程序未加入联网、遥测、自动更新、广告或全局键盘钩子。

检查范围与限制见 [SECURITY.md](SECURITY.md)、[隐私扫描结果](privacy-audit.json) 和 [程序测试结果](desktop/validation.json)。发布包附 SHA-256 校验值。

## 角色与许可

这是非官方同人项目。忍野忍及《物语》系列相关权利属于各自权利人，与原作权利方没有官方合作或背书。

[桌宠程序源码采用 MIT 许可](desktop/LICENSE)。**角色图片、图标、动画和预览不适用该代码许可**，本仓库不授予第三方角色或商标的授权。参考用动画设定图不在发布范围内。角色背景参考：[《物语》系列官网](https://www.monogatari-series.com/)。
