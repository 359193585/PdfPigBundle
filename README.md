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





# 注意 Note：
- 🛡️ macOS 版程序没有数字签名，如果你从github release 下载了该 tar.gz 包，解压后，macOS会系统会自动给该文件添加一个名为 com.apple.quarantine 的扩展属性，即“来源不明”的标签。The macOS version of the program is not digitally signed. If you downloaded the tar.gz package from the GitHub Release, after extraction, macOS will automatically add an extended attribute named com.apple.quarantine to the file, which is the "unknown origin" tag.

- 当你尝试运行带有此标签的应用时，Gatekeeper 会进行更严格的安全检查。When you try to run an application with this tag, Gatekeeper will perform stricter security checks.
- 对于未通过 Apple 公证（Notarization）的应用，系统就会弹出“已损坏，无法打开”的提示。For applications that have not passed Apple Notarization, the system will pop up a prompt saying "is damaged and can't be opened".
- 这个问题本质上是安全机制导致的“假损坏”，而非文件真的坏了。This issue is essentially "false damage" caused by the security mechanism, not that the file is actually broken.
- 你可以移除隔离属性，在终端中，针对单个 .app 文件，使用 xattr 命令移除该属性。You can remove the quarantine attribute by using the xattr command in the terminal for a single .app file to remove this attribute.
```
xattr -d com.apple.quarantine /path/to/PDFMerger.app
```
