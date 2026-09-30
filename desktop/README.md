# Windows 桌宠源码

`Shinobu-Oshino-pet.exe` 是独立的 Windows Forms 程序。它使用 Win32 `UpdateLayeredWindow` 绘制逐像素透明窗口，把精灵图嵌入 EXE，运行时不需要额外图片文件。

## 从源码构建

在 Windows PowerShell 中，从仓库根目录运行：

```powershell
powershell -NoProfile -File .\desktop\build.ps1
```

成品位于 `desktop/bin/Shinobu-Oshino-pet.exe`。脚本使用 Windows .NET Framework 随附的 C# 编译器，不下载依赖、不修改执行策略、不需要管理员权限。

如果本机策略阻止脚本执行，可以审阅脚本后在你信任的开发环境构建，无需永久更改系统策略。

## 运行测试

```powershell
powershell -NoProfile -File .\desktop\build.ps1 -Test
```

测试会短暂打开一个专用展示窗口和桌宠，验证真实 Win32 绘制、原生点击/双击/右键事件、焦点、拖动、缩放、GDI 句柄释放及退出。另有视线方向、动画时长、负坐标屏幕边界、设置往返与损坏恢复检查。

测试只将专用展示窗口的截图保存到构建目录，作为 `desktop-preview.png`；临时设置测试使用随机临时目录并在结束后清理。测试程序不随面向用户的下载包发布。

## 文件

| 文件 | 用途 |
| --- | --- |
| `Program.cs` | 单实例启动与错误提示 |
| `PetForm.cs` | 托盘菜单、拖动、视线、帧缓存与窗口生命周期 |
| `PetModel.cs` | 动画时长、角度换算、边界与设置 |
| `Native.cs` | 透明窗口调用及 GDI 资源释放 |
| `app.manifest` | 普通用户运行及 DPI 设置 |
| `tests/Tests.cs` | 数值、文件与原生窗口测试 |

动画速度使用单调时钟计算。空闲时每 40 ms 检查一次状态，画面不变时不重新提交位图；已经绘制的帧按大小缓存，缩放和退出时释放。暂停仍保留拖动和托盘操作。

位置按屏幕物理像素保存，支持负坐标显示器；缩放保留角色脚下的位置，再限制到可用桌面内。当前采用系统 DPI 感知，在不同缩放率的多显示器之间切换时，可使用菜单手动调整角色大小。混合 DPI 和显示器热插拔仅做了代码路径与边界检查，尚未在多台实体显示器上验证。

## 本机数据与卸载

运行时仅写入 `%LOCALAPPDATA%\ShinobuOshinoPet\settings.txt`。删除程序即可卸载；如需清除偏好，退出程序后再删除该设置目录。不创建启动项、服务或计划任务。

## 实现参考

- [Microsoft：UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow)
- [Microsoft：窗口特性与透明窗口](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features)
- [Microsoft：Windows 中的 .NET Framework](https://learn.microsoft.com/en-us/dotnet/framework/install/on-windows-and-server)

许可范围见 [LICENSE](LICENSE)。MIT 仅适用于本目录中的代码、构建脚本、测试和 manifest，不包括 `pet.ico`、精灵图或其他角色媒体。
