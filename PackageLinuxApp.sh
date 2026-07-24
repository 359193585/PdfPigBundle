#!/bin/bash
# 用法:
# ./PackageLinuxApp.sh 1.2.3
set -e
VERSION=$1
if [ -z "$VERSION" ]; then
    echo "❌ 缺少版本号"
    exit 1
fi
APP_NAME="PDFMerger"
OUTPUT_DIR="./publish"
echo "📌 版本号: $VERSION"

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
        echo "设置执行权限"
        chmod +x "$DIR/$APP_NAME"
        if [ -f "$DIR/$APP_NAME.desktop" ]; then
            chmod +x "$DIR/$APP_NAME.desktop"
        fi
        TAR="$DIR.tar.gz"
        echo "生成 $TAR"
        tar czf "$TAR" \
            -C "$DIR" .
        echo "🧹 删除目录 "$DIR""
        rm -rf "$DIR"
    done
done
echo "=========================================="
echo "🎉 所有架构打包完成！"
echo "产物位置: $OUTPUT_DIR"
ls -lh "$OUTPUT_DIR"/$APP_NAME.$VERSION.linux-*.tar.gz