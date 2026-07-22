#
#   请在 PowerShell 7 中运行此脚本
#   如没有 PowerShell 7，请先安装 PowerShell 7
#   或者修改脚本，使得它符合你当前的 PowerShell 版本
#

# 项目文件夹名
$PROJ_FOLDER = "PdfPigBundle"

# 发布产物名称（用户看到的名称）
$PROJ_NAME = "PDFMerger"

# 版本号文件
$patchFile = Resolve-Path "version_patch.txt" -ErrorAction SilentlyContinue

# 1. 读取和写入 Patch 号 (防止 PS7 文件锁)
$patch = 0
if ($patchFile -and (Test-Path $patchFile)) {
    $patch = [int]([System.IO.File]::ReadAllText($patchFile).Trim())
}
$patch++
[System.IO.File]::WriteAllText((New-Item -Path "version_patch.txt" -Force), $patch.ToString())

# 2. 版本号定义
$major = 1
$minor = 1
$version = "$major.$minor.$patch"
$assemblyVersion = "$major.$minor.$patch.0"

Write-Host "=== 发布 $PROJ_NAME $version ===" -ForegroundColor Green

# 3. 清理旧包
if (Test-Path  "publish") { Remove-Item -Recurse -Force  "publish" }


# 4. 按照运行时发布
#$runtimes = @( "win-x64")
$runtimes = @( "osx-arm64")
#$runtimes = @( "linux-x64")
# $runtimes = @( "linux-x64", "linux-arm64")
# $runtimes = @("win-x64", "linux-x64", "linux-arm64","osx-arm64")
$runtimes = @("win-x64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")

foreach ($rid in $runtimes) {
    Write-Host "`n=== 正在发布 $rid ===" -ForegroundColor Cyan
    # 4.1 定义输出文件夹
    $baseFolder = "$PROJ_NAME.$version.$rid"
    $bundledFolder = "$PROJ_NAME.$version.$rid-bundled"
    $baseOutput = "publish\$baseFolder"
    $bundledOutput = "publish\$bundledFolder"
    $baseZip = "publish\$baseFolder.zip"
    $bundledZip = "publish\$bundledFolder.zip"

    # 4.2 准备基础参数
    $baseArgs = @(
        "publish", ".\$PROJ_FOLDER\$PROJ_FOLDER.csproj", 
        "-c", "Release", 
        "-r", $rid,
        "-p:Version=$version",
        "-p:AssemblyVersion=$assemblyVersion",
        "-p:FileVersion=$assemblyVersion",
        "-p:InformationalVersion=$version"
    )
    # 设置 Windows 应用程序图标（其他平台忽略此参数）
    $baseArgs += @("-p:ApplicationIcon=Assets/icon.ico")
        
    # 4.2.1 纯净 Base 版
    $baseLaunchArgs = $baseArgs + @(
        "--self-contained", "false", 
        "-p:SelfContained=false", 
        "-p:PublishSelfContained=false", 
        "-p:PublishSingleFile=true",
        "-p:PublishReadyToRun=false", 
        "--output", $baseOutput
    )
    # 4.2.2 Bundled 版
    $bundledLaunchArgs = $baseArgs + @(
        "--self-contained", "true", 
        "-p:SelfContained=true", 
        "-p:PublishSelfContained=true", 
        "-p:PublishSingleFile=true",                        
        "-p:IncludeNativeLibrariesForSelfContained=true",  
        "-p:PublishReadyToRun=false",
        "--output", $bundledOutput
    )
    # 4.3 发布
    Write-Host "开始发布 Base 版..." -ForegroundColor Cyan
    dotnet @baseLaunchArgs

    Write-Host "开始发布 Bundled 版..." -ForegroundColor Cyan
    dotnet @bundledLaunchArgs


    # ==========  为 Linux 添加 .desktop 文件和图标 ==========
    if ($rid -like "linux-*") {
        # 复制 .desktop 文件
        $desktopSource = "$PROJ_FOLDER\BuildAssets\Linux\$PROJ_NAME.desktop"
        if (Test-Path $desktopSource) {
            Copy-Item $desktopSource -Destination $baseOutput -Force
            Copy-Item $desktopSource -Destination $bundledOutput -Force
            Write-Host "已复制 $PROJ_NAME.desktop 到 Linux 发布目录" -ForegroundColor Yellow
        }
        else {
            Write-Host "警告: 未找到 $PROJ_NAME.desktop 文件，Linux 桌面集成将不完整。" -ForegroundColor Red
        }

        # 复制图标文件（使用 Assets/icon.png）
        $iconSource = "$PROJ_FOLDER\Assets\icon.png"
        if (Test-Path $iconSource) {
            Copy-Item $iconSource -Destination "$baseOutput\icon.png" -Force
            Copy-Item $iconSource -Destination "$bundledOutput\icon.png" -Force
            Write-Host "已复制 icon.png 到 Linux 发布目录" -ForegroundColor Yellow
        }
        else {
            Write-Host "警告: 未找到 Assets\icon.png，Linux 图标可能无法显示。" -ForegroundColor Red
        }
    }
    # ==========================================================


    # 4.4 打包 ZIP
    if ($rid -like "win-*") {
        Compress-Archive -Path "$baseOutput\*" -DestinationPath $baseZip -Force
        Compress-Archive -Path "$bundledOutput\*" -DestinationPath $bundledZip -Force
    }

    if ($rid -like "linux-*") {
        Write-Host "使用 tar 打包 Linux 版本（保留权限）..." -ForegroundColor Cyan

        # 1. 定义 tar 输出文件名
        $baseTar = "$baseOutput.tar.gz"
        $bundledTar = "$bundledOutput.tar.gz"

        # 2. 转换路径为 WSL 风格
        $wslBaseOutput = $baseOutput -replace '^([A-Z]):', '/mnt/$1' -replace '\\', '/'
        $wslBundledOutput = $bundledOutput -replace '^([A-Z]):', '/mnt/$1' -replace '\\', '/'
        $wslBaseTar = $baseTar -replace '^([A-Z]):', '/mnt/$1' -replace '\\', '/'
        $wslBundledTar = $bundledTar -replace '^([A-Z]):', '/mnt/$1' -replace '\\', '/'

        # 3. 设置执行权限
        wsl chmod +x "$wslBaseOutput/$PROJ_NAME"
        wsl chmod +x "$wslBundledOutput/$PROJ_NAME"
        wsl chmod +x "$wslBaseOutput/$PROJ_NAME.desktop"
        wsl chmod +x "$wslBundledOutput/$PROJ_NAME.desktop"

        # 4. 打包为 .tar.gz（保留权限）
        wsl tar -czf "$wslBaseTar" -C "$wslBaseOutput" .
        wsl tar -czf "$wslBundledTar" -C "$wslBundledOutput" .

        # 5. 删除未打包的目录
        Remove-Item -Recurse -Force $baseOutput, $bundledOutput

        Write-Host "✅ 已生成: $baseTar 和 $bundledTar" -ForegroundColor Green
    }

   


    # 4.5 GitHub Release
    # Write-Host "发布到 github ..." -ForegroundColor Cyan
    # $tag = "v$version"
    # git tag -f -m "Release $PROJ_NAME $version" $tag
    # git push origin $tag --force
    #
    # gh release create $tag `
    #      $baseZip `
    #      $bundledZip `
    #      --title "$PROJ_NAME $version" `
    #      --notes "Release of $PROJ_NAME $version with both self-contained and dependent builds."
}

# ===========================================================================
Write-Host "`n=== 正在调用 macOS 打包脚本 ===" -ForegroundColor Cyan

#  获取脚本所在目录的 WSL 路径（小写盘符，用于 cd）
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$drive = $ScriptDir[0].ToString().ToLower()
$wslScriptDir = "/mnt/$drive" + $ScriptDir.Substring(2) -replace '\\', '/'
Write-Host "项目根目录 (WSL): $wslScriptDir"

# 确保 PackageMacApp.sh 有执行权限（在 WSL 中）
wsl chmod +x "$wslScriptDir/PackageMacApp.sh" 2>$null

# 在 WSL 中切换到项目根目录，然后执行脚本（传递版本号）
wsl bash -c "cd '$wslScriptDir' && ./PackageMacApp.sh $version"

if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️ macOS 打包脚本执行失败，退出码: $LASTEXITCODE" -ForegroundColor Red
}
else {
    Write-Host "✅ macOS 打包完成" -ForegroundColor Green
    Remove-Item -Recurse -Force "publish\$PROJ_NAME.$version.osx-x64"
    Remove-Item -Recurse -Force "publish\$PROJ_NAME.$version.osx-arm64"
    Remove-Item -Recurse -Force "publish\$PROJ_NAME.$version.osx-x64-bundled"
    Remove-Item -Recurse -Force "publish\$PROJ_NAME.$version.osx-arm64-bundled"
}


Write-Host "`n=== 所有平台发布完成 ===" -ForegroundColor Green
