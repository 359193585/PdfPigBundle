# 📄 PDF 合并工具 (PDF Merger)

一款简洁、免费、跨平台的 PDF 合并工具，支持保留原始书签目录结构，并自动生成文件名为一级目录。无广告、无弹窗、无水印，专为高效办公和信创环境打造。

---

## ✨ 功能特点

- **多文件合并**：支持任意数量的 PDF 文件合并为一个。
- **书签保留与增强**：
  - 如果源文件已有书签，合并后保留完整层级结构。
  - 以 **源文件名** 作为一级书签目录，原书签自动降级为其子节点（方便快速定位）。
- **元数据设置**：可自定义输出文件的作者、标题、主题等信息。
- **重复文件智能过滤**：可选择忽略重复添加的文件。
- **实时进度反馈**：图形界面显示合并进度。
- **跨平台支持**：Windows、macOS（Intel + Apple Silicon）、Linux（x64 + ARM64）。
- **纯离线运行**：无需联网，数据不上传，保障隐私安全。
- **干净无打扰**：无广告、无会员、无使用限制。

---

## 🖥️ 系统要求

- **操作系统**：
  - Windows 10 / 11
  - macOS 10.15+ (Intel / Apple Silicon)
  - Linux (Ubuntu 20.04+, 统信 UOS, 麒麟 KOS, 等)
- **CPU 架构**：x64、ARM64
- 无需安装 .NET 运行时（自包含版本）

---

## 🚀 使用方法

### 图形界面 (GUI)
  
<div align="center">
    <img width="802" height="632" alt="image" src="https://github.com/user-attachments/assets/46d6d923-1358-41b3-83a1-35ca7ad5ce8a" />
</div>


1. 下载对应平台的压缩包，解压后运行 `PdfMerger` 可执行文件。
2. 主界面：
   - 点击 **“添加文件”** 或直接将 PDF 文件拖入窗口。
   - 支持多选，支持多次添加。
   - 可调整文件顺序（上移/下移）。
   - 输出路径默认自动生成，也可手动选择。
3. 点击 **“开始合并”**，进度条实时显示。
4. 合并完成后，可打开输出文件所在文件夹。


# 📦 下载与安装
## 预编译包
- 前往 Releases 页面下载适合您系统的压缩包：

  - PdfMerger-win-x64.zip – Windows 64位

  - PdfMerger-osx-x64.zip – macOS Intel

  - PdfMerger-osx-arm64.zip – macOS Apple Silicon

  - PdfMerger-linux-x64.zip – Linux 64位

  - PdfMerger-linux-arm64.zip – Linux ARM64 (信创设备)

- 解压后，直接双击 PdfMerger 即可运行（Linux/macOS 可能需要授予执行权限）。

# 🛠️ 开发者指南
## 环境要求
- .NET SDK 8.0 或更高版本

- Avalonia UI 框架

- 构建 使用项目的powershell脚本和wsl 虚拟机的 shell脚本

## 🤝 贡献
- 欢迎提出 Issue 或 Pull Request。

- 报告 Bug 请附上系统信息和复现步骤。

- 希望新增功能或改进，请先开 Issue 讨论。

## 📄 许可证
本项目采用 MIT 许可证，允许自由使用、修改、分发，包括商业用途。

## 🧩 技术栈
Avalonia UI – 跨平台 .NET GUI 框架

PDFsharp – PDF 处理核心

.NET 8 – 运行时与 SDK

# ❓ FAQ
Q: 合并后的 PDF 目录书签无法在 Chrome 或 Edge 浏览器中显示？
A: 部分浏览器可能不支持 PDF 书签，请使用 Adobe Acrobat Reader 或 PDF Expert 等专业阅读器查看。

Q: 可以合并加密的 PDF 吗？
A: 目前不支持加密 PDF，未来可能加入密码输入功能。

Q: 软件需要联网吗？
A: 完全离线，无需联网，不会上传任何文件。

Q: 为什么我的 Apple Silicon Mac 无法运行？
A: 请下载 -osx-arm64 版本，确保您使用的是对应架构的包。

📧 联系
如有商务合作或定制需求，请通过 GitHub Issues 联系。


# 注意 Note：
- 🛡️ macOS 版程序没有数字签名，如果你从github release 下载了该 tar.gz 包，解压后，macOS会系统会自动给该文件添加一个名为 com.apple.quarantine 的扩展属性，即“来源不明”的标签。The macOS version of the program is not digitally signed. If you downloaded the tar.gz package from the GitHub Release, after extraction, macOS will automatically add an extended attribute named com.apple.quarantine to the file, which is the "unknown origin" tag.

- 当你尝试运行带有此标签的应用时，Gatekeeper 会进行更严格的安全检查。When you try to run an application with this tag, Gatekeeper will perform stricter security checks.
- 对于未通过 Apple 公证（Notarization）的应用，系统就会弹出“已损坏，无法打开”的提示。For applications that have not passed Apple Notarization, the system will pop up a prompt saying "is damaged and can't be opened".
- 这个问题本质上是安全机制导致的“假损坏”，而非文件真的坏了。This issue is essentially "false damage" caused by the security mechanism, not that the file is actually broken.
- 你可以移除隔离属性，在终端中，针对单个 .app 文件，使用 xattr 命令移除该属性。You can remove the quarantine attribute by using the xattr command in the terminal for a single .app file to remove this attribute.
```
xattr -d com.apple.quarantine /path/to/PDFMerger.app
```
