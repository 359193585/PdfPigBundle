#!/bin/bash
# run in wsl2 ubuntu 22.04 ,do not run in windows powershell
# 用法: ./PackageMacApp.ps1 [版本号]
# 如果不指定版本号，将从 arm64 发布目录中提取

echo "mac app publishing (both x64 and arm64)..."
set -e

APP_NAME="PdfPigBundle"
BUNDLE_ID="com.leison.pdfpigbundle"
VERSION=${1:-""}  # 可选参数，若留空则自动提取

PROJ_NAME="PdfPigBundle"

# 定义多个可能的发布目录（按优先级）
POSSIBLE_PATHS=(
    "./publish"
    "/mnt/e/Develop_Vs2022/${PROJ_NAME}/publish"
)

# 查找实际存在的发布基础目录
PUBLISH_BASE=""
for path in "${POSSIBLE_PATHS[@]}"; do
    if [ -d "$path" ] && ls "$path"/${PROJ_NAME}.*.osx-*-bundled 1>/dev/null 2>&1; then
        PUBLISH_BASE="$path"
        echo "✅ 找到发布目录: $PUBLISH_BASE"
        break
    fi
done

if [ -z "$PUBLISH_BASE" ]; then
    echo "❌ 未找到任何包含 ${PROJ_NAME}.*.osx-*-bundled 的发布目录"
    echo "请检查路径: ${POSSIBLE_PATHS[@]}"
    exit 1
fi

# 如果未指定版本号，从 arm64 发布目录中提取（优先），若没有则从 x64 提取
if [ -z "$VERSION" ]; then
    # 尝试 arm64
    ARM64_DIR=$(ls -td "$PUBLISH_BASE"/${PROJ_NAME}.*.osx-arm64-bundled 2>/dev/null | head -1)
    if [ -n "$ARM64_DIR" ]; then
        FOLDER_NAME=$(basename "$ARM64_DIR")
        if [[ $FOLDER_NAME =~ ${PROJ_NAME}\.([0-9]+\.[0-9]+\.[0-9]+)\.osx-arm64-bundled ]]; then
            VERSION="${BASH_REMATCH[1]}"
            echo "📌 从 arm64 目录提取版本号: $VERSION"
        fi
    fi
    # 如果仍未提取到，尝试 x64
    if [ -z "$VERSION" ]; then
        X64_DIR=$(ls -td "$PUBLISH_BASE"/${PROJ_NAME}.*.osx-x64-bundled 2>/dev/null | head -1)
        if [ -n "$X64_DIR" ]; then
            FOLDER_NAME=$(basename "$X64_DIR")
            if [[ $FOLDER_NAME =~ ${PROJ_NAME}\.([0-9]+\.[0-9]+\.[0-9]+)\.osx-x64-bundled ]]; then
                VERSION="${BASH_REMATCH[1]}"
                echo "📌 从 x64 目录提取版本号: $VERSION"
            fi
        fi
    fi
    if [ -z "$VERSION" ]; then
        echo "⚠️ 无法从任何目录提取版本号，使用默认 1.0.0"
        VERSION="1.0.0"
    fi
fi

echo "📌 版本号: $VERSION"

# 定义输出目录（Windows 发布目录）
OUTPUT_DIR="/mnt/e/Develop_Vs2022/${PROJ_NAME}/publish"
mkdir -p "$OUTPUT_DIR"

# 定义要打包的架构列表
ARCH_LIST=("arm64" "x64")

for ARCH in "${ARCH_LIST[@]}"; do
    echo "=========================================="
    echo "📦 开始打包 $ARCH 版本..."

    RID="osx-$ARCH"

    # 查找对应架构的 bundled 目录
    BUNDLED_DIR=$(ls -td "$PUBLISH_BASE"/${PROJ_NAME}.*.${RID}-bundled 2>/dev/null | head -1)
    if [ -z "$BUNDLED_DIR" ]; then
        echo "⚠️ 未找到 $RID 发布目录，跳过..."
        continue
    fi
    echo "📁 使用发布目录: $BUNDLED_DIR"

    # 创建临时工作目录（带架构后缀，避免冲突）
    WORK_DIR="$HOME/develop/${PROJ_NAME}/tmp_mac_pack_$ARCH"
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
    # rm -rf "$WORK_DIR"
    # echo "🧹 临时目录已清理"
done

echo "=========================================="
echo "🎉 所有架构打包完成！"
echo "产物位置: $OUTPUT_DIR"
ls -lh "$OUTPUT_DIR"/$APP_NAME.$VERSION.macos-*.app.tar.gz
