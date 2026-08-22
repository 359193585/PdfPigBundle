#!/bin/bash
# 用法:
# ./PackageLinuxApp.sh 1.2.3

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
NC='\033[0m' # No Color

echo "liunx app publishing (both arm64 and x86 )..."
set -e

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "❌ 缺少版本号"
    exit 1
fi
echo "📌 版本号: $VERSION"

PROJECT_DIR_NAME="PDFMerger"   # 项目文件夹名（用于定位源代码和发布路径）
APP_NAME="PDFMerger"
OUTPUT_DIR="./publish"


# ---------- 查找发布目录 ----------

PUBLISH_BASE="./publish"
if [ ! -d "$PUBLISH_BASE" ]; then
    echo "❌ 发布目录不存在: $PUBLISH_BASE"
    exit 1
fi

if ! ls "$PUBLISH_BASE"/${APP_NAME}.*.linux-*-bundled 1>/dev/null 2>&1; then
    echo "❌ 未找到任何 linux-*-bundled 目录，请先运行 dotnet publish"
    exit 1
fi

# ---------- 输出目录 ----------
OUTPUT_DIR="./publish"
mkdir -p "$OUTPUT_DIR"


for RID in linux-x64 linux-arm64
do
    echo "=========================================="
    echo "📦 === 打包 $RID ==="
    for TYPE in "" "-bundled"
    do
        DIR="$OUTPUT_DIR/$APP_NAME.$VERSION.$RID$TYPE"
        if [ ! -d "$DIR" ]; then
            echo "跳过不存在目录 $DIR"
            continue
        fi
        
        # ==========  为 Linux 添加 .desktop 文件和图标 ==========
        # 复制 .desktop 文件
        DESKTOPSOURCE="$PROJECT_DIR_NAME/BuildAssets/Linux/$APP_NAME.desktop" 
        if [ -f "$DESKTOPSOURCE" ]; then
          cp "$DESKTOPSOURCE" "$DIR/"
          echo -e "${YELLOW} 已复制 $APP_NAME.desktop 到 Linux 发布目录 ${NC}"
        else
          echo -e "${YELLOW} 警告: 未找到 $APP_NAME.desktop 文件，Linux 桌面集成将不完整 ${NC}" 
        fi

     
        # 复制图标文件（使用 Assets/icon.png）
        ICON_SOURCE="$PROJECT_DIR_NAME/Assets/icon.png"
        if [ -f "$ICON_SOURCE" ]; then
          cp "$ICON_SOURCE" "$DIR/"
          echo -e "${YELLOW} 已复制 icon.png 到 Linux 发布目录${NC}"
        else
          echo -e "${YELLOW} 警告: 未找到 Assets\icon.png，Linux 图标可能无法显示 ${NC}"
        fi

        echo "设置执行权限"
        chmod +x "$DIR/$APP_NAME"
        if [ -f "$DIR/$APP_NAME.desktop" ]; then
            chmod +x "$DIR/$APP_NAME.desktop"
        fi
        TAR="$DIR.tar.gz"
        echo "生成 $TAR"
        tar -czf "$TAR" -C "$DIR" .
        
        # 清理原始目录    
        echo "🧹 原始目录 $DIR 已清理"
        rm -rf "$DIR"
    done
done

echo "=========================================="
echo "🎉 所有架构打包完成！"
echo "产物位置: $OUTPUT_DIR"
ls -lh "$OUTPUT_DIR"/$APP_NAME.$VERSION.linux-*.tar.gz
