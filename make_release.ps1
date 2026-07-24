param(
    [string]$Version = "",
    [switch]$CI
)

#
#   请在 PowerShell 7 中运行此脚本
#   如没有 PowerShell 7，请先安装 PowerShell 7
#   或者修改脚本，使得它符合你当前的 PowerShell 版本
#
#   用 Resolve-Path 命令的可用性，探测 VS 初始化ps 环境是否完整）
if (-not (Get-Command Resolve-Path -ErrorAction SilentlyContinue)) {
    Write-Host "检测到 Resolve-Path 不可用，正在修复..." -ForegroundColor Yellow
    Import-Module Microsoft.PowerShell.Management -Force -ErrorAction SilentlyContinue
}

# 项目文件夹名
$PROJ_FOLDER = "PdfPigBundle"

# 发布产物名称（用户看到的名称）
$PROJ_NAME = "PDFMerger"


if ($Version) {
    # 如果指定了版本号，则使用该版本号
    $version = $Version
    Write-Host "使用指定版本: $version" -ForegroundColor Cyan
}
else {
    # 读取和写入 Patch 号 (防止 PS7 文件锁)
    $patchFile = Resolve-Path "version_patch.txt" -ErrorAction SilentlyContinue
    $patch = 0
    if (Test-Path $patchFile) {
        $patch = [int](Get-Content $patchFile)
    }
    $patch++
    Set-Content $patchFile $patch
    $major = 1
    $minor = 2
    $version = "$major.$minor.$patch"
    $assemblyVersion = "$major.$minor.$patch.0"
    [System.IO.File]::WriteAllText((New-Item -Path "version_patch.txt" -Force), $patch.ToString())
    Write-Host "自动递增版本: $version" -ForegroundColor Cyan
}

Write-Host "=== 发布 $PROJ_NAME $version ===" -ForegroundColor Green

#  清理旧包
if (Test-Path  "publish") { Remove-Item -Recurse -Force  "publish" }


#  运行时设置
# $runtimes = @( "win-x64")
# $runtimes = @( "osx-arm64")
# $runtimes = @( "linux-x64")
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

    # 1 准备基础参数
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
        
    # 2 纯净 Base 版
    $baseLaunchArgs = $baseArgs + @(
        "--self-contained", "false", 
        "-p:SelfContained=false", 
        "-p:PublishSelfContained=false", 
        "-p:PublishSingleFile=true",
        "-p:PublishReadyToRun=false", 
        "--output", $baseOutput
    )
    # 3 Bundled 版
    $bundledLaunchArgs = $baseArgs + @(
        "--self-contained", "true", 
        "-p:SelfContained=true", 
        "-p:PublishSelfContained=true", 
        "-p:PublishSingleFile=true",                        
        "-p:IncludeNativeLibrariesForSelfContained=true",  
        "-p:PublishReadyToRun=false",
        "--output", $bundledOutput
    )
    #  发布
    if ($rid -notlike "osx-*") {
        Write-Host "开始发布 Base 版..." -ForegroundColor Cyan
        dotnet @baseLaunchArgs
    }

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

    # 在打包前，删除所有 .pdb 文件
    Get-ChildItem -Path $baseOutput -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force
    Get-ChildItem -Path $bundledOutput -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force


    #  win 的直接打包为 ZIP ，linux 和 mac 的单独处理
    if ($rid -like "win-*") {
        Compress-Archive -Path "$baseOutput\*" -DestinationPath $baseZip -Force
        Compress-Archive -Path "$bundledOutput\*" -DestinationPath $bundledZip -Force
    }
}

# ===========================================================================
if ($CI) {
    Write-Host "CI环境，跳过WSL调用"
}
else {
    #  获取脚本所在目录的 WSL 路径（小写盘符，用于 cd）
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $drive = $ScriptDir[0].ToString().ToLower()
    $wslScriptDir = "/mnt/$drive" + $ScriptDir.Substring(2) -replace '\\', '/'
    Write-Host "项目根目录 (WSL): $wslScriptDir"

    # ===============================
    Write-Host "`n=== 本地环境,调用独立的 linux 打包脚本 ===" -ForegroundColor Cyan

    # 确保 .sh 有执行权限（在 WSL 中）
    wsl chmod +x "$wslScriptDir/PackageLinuxApp.sh" 2>$null
    # 在 WSL 中切换到项目根目录，然后执行脚本（传递版本号）
    wsl bash -c "cd '$wslScriptDir' && ./PackageLinuxApp.sh $version"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️ linux 打包脚本执行失败，退出码: $LASTEXITCODE" -ForegroundColor Red
    }
    else {
        Write-Host "✅ linux 打包完成" -ForegroundColor Green
    }

    # ===============================
    Write-Host "`n=== 本地环境,调用独立的 macOS 打包脚本 ===" -ForegroundColor Cyan
    
    # 确保 PackageMacApp.sh 有执行权限（在 WSL 中）
    wsl chmod +x "$wslScriptDir/PackageMacApp.sh" 2>$null

    # 在 WSL 中切换到项目根目录，然后执行脚本（传递版本号）
    wsl bash -c "cd '$wslScriptDir' && ./PackageMacApp.sh $version"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️ macOS 打包脚本执行失败，退出码: $LASTEXITCODE" -ForegroundColor Red
    }
    else {
        Write-Host "✅ macOS 打包完成" -ForegroundColor Green
    }
}


Write-Host "`n=== 所有平台发布完成 ===" -ForegroundColor Green
