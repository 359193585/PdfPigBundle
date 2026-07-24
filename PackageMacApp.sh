#!/bin/bash
# run in wsl2 ubuntu 22.04 ,do not run in windows powershell
# 用法: ./PackageMacApp.sh 版本号

echo "mac app publishing (both arm64 and x86 )..."
set -e

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "❌ 缺少版本号"
    exit 1
fi

PROJECT_DIR_NAME="PdfPigBundle"   # 项目文件夹名（用于定位源代码和发布路径）
APP_NAME="PDFMerger"              # 最终应用名称（用户看到的名称）
BUNDLE_ID="com.leison.pdfmerger"  # Bundle ID 

echo "📌 版本号: $VERSION"

# ---------- 查找发布目录 ----------

PUBLISH_BASE="./publish"
if [ ! -d "$PUBLISH_BASE" ]; then
    echo "❌ 发布目录不存在: $PUBLISH_BASE"
    exit 1
fi

if ! ls "$PUBLISH_BASE"/${APP_NAME}.*.osx-*-bundled 1>/dev/null 2>&1; then
    echo "❌ 未找到任何 osx-*-bundled 目录，请先运行 dotnet publish"
    exit 1
fi

# ---------- 输出目录 ----------
OUTPUT_DIR="./publish"
mkdir -p "$OUTPUT_DIR"

# ---------- 打包各架构 ----------
ARCH_LIST=("arm64" "x64")

for ARCH in "${ARCH_LIST[@]}"; do
    echo "=========================================="
    echo "📦 开始打包 $ARCH 版本..."

    RID="osx-$ARCH"

    # 查找对应架构的 bundled 目录
    BUNDLED_DIR=$(ls -td "$PUBLISH_BASE"/${APP_NAME}.*.${RID}-bundled 2>/dev/null | head -1)
    if [ -z "$BUNDLED_DIR" ]; then
        echo "⚠️ 未找到 $RID 发布目录，跳过..."
        continue
    fi
    echo "📁 使用发布目录: $BUNDLED_DIR"

    # 创建临时工作目录
    WORK_DIR="$HOME/develop/${PROJECT_DIR_NAME}/tmp_mac_pack_$ARCH"
    rm -rf "$WORK_DIR"
    mkdir -p "$WORK_DIR"

    APP_DIR="$WORK_DIR/$APP_NAME.app"
    MACOS_DIR="$APP_DIR/Contents/MacOS"
    RESOURCES_DIR="$APP_DIR/Contents/Resources"

    mkdir -p "$MACOS_DIR"
    mkdir -p "$RESOURCES_DIR"

    echo "📦 正在复制发布产物..."
    cp -r "$BUNDLED_DIR"/* "$MACOS_DIR/"

    # 查找并复制图标
    ICON_SOURCE=""
    if [ -f "$MACOS_DIR/Assets/icon.icns" ]; then
        ICON_SOURCE="$MACOS_DIR/Assets/icon.icns"
    elif [ -f "$MACOS_DIR/icon.icns" ]; then
        ICON_SOURCE="$MACOS_DIR/icon.icns"
    fi

    ICON_FILE_NAME=""
    if [ -n "$ICON_SOURCE" ]; then
        cp "$ICON_SOURCE" "$RESOURCES_DIR/"
        ICON_FILE_NAME=$(basename "$ICON_SOURCE")
        echo "✅ 已找到图标: $ICON_FILE_NAME"
    else
        echo "⚠️ 未找到图标文件"
    fi

    # 生成 Info.plist
    cat > "$APP_DIR/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>CFBundleIconFile</key>
    <string>$ICON_FILE_NAME</string>
</dict>
</plist>
EOF

    # 赋予执行权限
    chmod +x "$MACOS_DIR/$APP_NAME"

    # 打包为 .tar.gz（保留权限）
    TAR_NAME="$OUTPUT_DIR/$APP_NAME.$VERSION.macos-$ARCH.app.tar.gz"
    tar -czf "$TAR_NAME" -C "$WORK_DIR" "$APP_NAME.app"

    echo "✅ 已生成: $TAR_NAME"
    echo "📦 最终产物: $TAR_NAME"

    # 清理临时目录（可选）
    rm -rf "$WORK_DIR"
    echo "🧹 临时目录 $WORK_DIR 已清理"

    # 清理原始目录
    rm -rf "$BUNDLED_DIR"
    echo "🧹 原始目录 $BUNDLED_DIR 已清理"
done

echo "=========================================="
echo "🎉 所有架构打包完成！"
echo "产物位置: $OUTPUT_DIR"
ls -lh "$OUTPUT_DIR"/$APP_NAME.$VERSION.macos-*.app.tar.gz


