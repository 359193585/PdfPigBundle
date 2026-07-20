# 基于开源的pdfpig，c# avalonia 框架开发的一个跨平台的桌面端程序，完成多个pdf文档的快速合并

- 文件支持拖放/目录选择加载
- 支持调整文件顺序
- 输出文件路径可选，默认为需合并的第一个pdf文件的目录

  
<div align="center">
    <img width="802" height="632" alt="image" src="https://github.com/user-attachments/assets/46d6d923-1358-41b3-83a1-35ca7ad5ce8a" />
</div>



# 注意 Note：
- 🛡️ macOS 版程序没有数字签名，如果你从github release 下载了该 tar.gz 包，解压后，macOS会系统会自动给该文件添加一个名为 com.apple.quarantine 的扩展属性，即“来源不明”的标签。The macOS version of the program is not digitally signed. If you downloaded the tar.gz package from the GitHub Release, after extraction, macOS will automatically add an extended attribute named com.apple.quarantine to the file, which is the "unknown origin" tag.

- 当你尝试运行带有此标签的应用时，Gatekeeper 会进行更严格的安全检查。When you try to run an application with this tag, Gatekeeper will perform stricter security checks.
- 对于未通过 Apple 公证（Notarization）的应用，系统就会弹出“已损坏，无法打开”的提示。For applications that have not passed Apple Notarization, the system will pop up a prompt saying "is damaged and can't be opened".
- 这个问题本质上是安全机制导致的“假损坏”，而非文件真的坏了。This issue is essentially "false damage" caused by the security mechanism, not that the file is actually broken.
- 你可以移除隔离属性，在终端中，针对单个 .app 文件，使用 xattr 命令移除该属性。You can remove the quarantine attribute by using the xattr command in the terminal for a single .app file to remove this attribute.
```
xattr -d com.apple.quarantine /path/to/PdfPigBundle.app
```
