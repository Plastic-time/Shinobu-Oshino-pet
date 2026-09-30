# GPT 宠物版 · 忍野忍

在支持 Pets 的 ChatGPT Work 环境中使用的忍野忍动画宠物。与独立桌宠版共用角色造型，包含 9 种动作和 16 向视线。

![动画预览](../previews/all-states.gif)

## 下载与使用

1. [下载 GPT 版发布包](https://github.com/Plastic-time/Shinobu-Oshino-pet/releases/tag/v1.0.0) 并解压。
2. 在支持自定义宠物的 Pets 工作流中，提供 `assets/spritesheet.png`，请求使用这张精灵图创建并选择“忍野忍”。
3. 需要查看帧布局或动画时长时，使用包内的 `sprite-layout.json`。

可提供给 Pets 的请求示例：

> 使用附件中的精灵图创建并选择一个名叫“忍野忍”的宠物。这是已经排好布局的 Pets v2 精灵图，包含 9 种动作和 16 向视线。

此版需要可用的 Pets 功能及自定义宠物工作流。仅下载 PNG 不会自动启用宠物；具体可用入口以你的客户端为准。

## 包含内容

- `assets/spritesheet.png`：透明精灵图。
- `sprite-layout.json`：每行动作、帧数及逐帧时长。
- `previews/`：GIF 动画、MP4、全部帧与视线方向预览。
- `SHA256SUMS`：文件校验值。

精灵图为 1536 × 2288 px，8 列 × 11 行，每格 192 × 208 px，共 73 个有效帧。视线角度从正上方开始，顺时针排列。

非官方同人素材，角色及原作相关权利属于各自权利人。

[返回首页](../README.md) · [独立桌宠版](../desktop/USAGE.md)
