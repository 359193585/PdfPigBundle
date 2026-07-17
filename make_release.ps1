#
#   请在 PowerShell 7 中运行此脚本
#   如没有 PowerShell 7，请先安装 PowerShell 7
#   或者修改脚本，使得它符合你当前的 PowerShell 版本
#

# 添加项目名称变量, 发布程序名和项目路径，均依据此值
$PROJ_NAME = "PdfPigBundle"


# 1. 读取和写入 Patch 号 (防止 PS7 文件锁)
$patchFile = Resolve-Path "version_patch.txt" -ErrorAction SilentlyContinue
$patch = 0
if ($patchFile -and (Test-Path $patchFile)) {
    $patch = [int]([System.IO.File]::ReadAllText($patchFile).Trim())
}
$patch++
[System.IO.File]::WriteAllText((New-Item -Path "version_patch.txt" -Force), $patch.ToString())

# 2. 版本号定义
$major = 1
$minor = 0
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
    $runtimes = @("win-x64", "linux-x64", "linux-arm64","osx-x64","osx-arm64")

foreach ($rid in $runtimes) {
        Write-Host "`n=== 正在发布 $rid ===" -ForegroundColor Cyan
    # 4.1 定义输出文件夹
        $baseFolder    = "$PROJ_NAME.$version.$rid"
        $bundledFolder = "$baseFolder-bundled"
        $baseOutput    = "publish\$baseFolder"
        $bundledOutput = "publish\$bundledFolder"
        $baseZip       = "publish\$baseFolder.zip"
        $bundledZip    = "publish\$bundledFolder.zip"

    # 4.2 准备基础参数
        $baseArgs = @(
            "publish", ".\$PROJ_NAME\$PROJ_NAME.csproj", 
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

    # 4.4 打包 ZIP
        Compress-Archive -Path "$baseOutput\*" -DestinationPath $baseZip -Force
        Compress-Archive -Path "$bundledOutput\*" -DestinationPath $bundledZip -Force

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
