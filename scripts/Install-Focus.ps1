param(
    [switch]$FromCmd
)

# Bili.Copilot-Focus 侧载安装器（本 fork 自带，替代上游 Install.ps1）
#
# 用法（三选一）：
#   1. 双击同目录的 Install-Focus.cmd          ← 推荐，窗口无论如何都会停住
#   2. 右键本文件 →「使用 PowerShell 运行」     ← 若执行策略拦住脚本会闪退，改用 1
#   3. 管理员 PowerShell 里（先 cd 进本目录，或写完整路径）：
#        powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Focus.ps1
#
# 与上游 Install.ps1 的区别：非管理员时请求提权但原窗口不消失；提权窗口 -NoExit；
# 证书用 certutil 静默导入；依赖已有更高版本时给友好提示而不是报错；顶层 try/catch
# 兜住任何意外，保证窗口不会静默闪掉。

$ErrorActionPreference = 'Stop'

function Write-Line([string]$text, [string]$color = 'Gray') {
    Write-Host $text -ForegroundColor $color
}

function Stop-WithPause([int]$code) {
    if ($FromCmd) { exit $code }
    Write-Host ''
    Write-Host '按任意键关闭此窗口…' -ForegroundColor DarkGray
    try { $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') } catch { Start-Sleep -Seconds 10 }
    exit $code
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 取 msix 里的 Identity Name/Version（msix 就是 zip；读不到就返回 $null，不影响流程）
function Get-MsixIdentity([string]$path) {
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
        $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
        try {
            $entry = $zip.Entries | Where-Object { $_.FullName -eq 'AppxManifest.xml' } | Select-Object -First 1
            if (-not $entry) { return $null }
            $reader = New-Object System.IO.StreamReader($entry.Open())
            try { $xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $match = [regex]::Match($xml, '<Identity[^>]*Name="([^"]+)"[^>]*Version="([^"]+)"')
            if (-not $match.Success) { return $null }
            return [pscustomobject]@{ Name = $match.Groups[1].Value; Version = [version]$match.Groups[2].Value }
        }
        finally { $zip.Dispose() }
    }
    catch { return $null }
}

function Get-InstalledVersion([string]$name) {
    try {
        $pkg = Get-AppxPackage -Name $name -ErrorAction Stop | Sort-Object -Property Version -Descending | Select-Object -First 1
        if ($pkg) { return [version]$pkg.Version }
    }
    catch { }
    return $null
}

try {
    try { $Host.UI.RawUI.WindowTitle = 'Bili.Copilot-Focus 安装器' } catch { }

    Clear-Host
    Write-Line '=== Bili.Copilot-Focus 安装器 ===' 'Cyan'
    Write-Line ("PowerShell {0}（{1}）" -f $PSVersionTable.PSVersion, $PSVersionTable.PSEdition) 'DarkGray'

    # 脚本所在目录：$PSScriptRoot 最可靠，再退回 $MyInvocation 与当前目录
    $dir = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($dir)) { $dir = Split-Path -Parent $MyInvocation.MyCommand.Path }
    if ([string]::IsNullOrWhiteSpace($dir)) { $dir = (Get-Location).Path }
    Set-Location -LiteralPath $dir
    Write-Line "安装目录：$dir" 'DarkGray'
    Write-Line ''

    if (-not (Test-IsAdmin)) {
        Write-Line '需要管理员权限。正在请求提权——请在 UAC 弹窗里点「是」。' 'Yellow'
        try {
            $argList = "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$PSCommandPath`""
            Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs -ErrorAction Stop
            Write-Line '已发起提权请求：新开的窗口会继续安装，本窗口可以关掉了。' 'Yellow'
            Write-Line '没看到 UAC 弹窗时，它多半藏在别的窗口后面；' 'Yellow'
            Write-Line '也可以改为：右键本文件 →「以管理员身份运行」，或双击 Install-Focus.cmd。' 'Yellow'
        }
        catch {
            Write-Line "提权失败：$($_.Exception.Message)" 'Red'
            Write-Line '请改为双击 Install-Focus.cmd，或在管理员 PowerShell 里运行本脚本。' 'Yellow'
        }

        # 2 = 已请求提权、安装将在新窗口继续（Install-Focus.cmd 会据此给出正确提示）
        Stop-WithPause 2
    }

    Write-Line '权限：管理员 ✓' 'Green'
    Write-Line ''

    # ── 1/3 信任证书 ────────────────────────────────────────────────────────
    $cer = Get-ChildItem -LiteralPath $dir -Filter '*.cer' -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cer) {
        Write-Line "[1/3] 信任签名证书：$($cer.Name)"
        $certOut = & certutil.exe -f -addstore Root "$($cer.FullName)" 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Line ($certOut | Out-String) 'DarkGray'
            Write-Line '      证书导入失败；若之前已信任过同一张证书可以忽略这一步。' 'Yellow'
        }
        else {
            Write-Line '      已加入「受信任的根证书颁发机构」' 'Green'
        }
    }
    else {
        Write-Line '[1/3] 没找到 .cer，跳过证书导入（未签名的包将无法安装）' 'Yellow'
    }

    # ── 2/3 依赖（Windows App Runtime 等）──────────────────────────────────
    $deps = @(Get-ChildItem -LiteralPath $dir -Filter '*.msix' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like 'Microsoft*' })
    if ($deps.Count -gt 0) {
        Write-Line "[2/3] 安装依赖（$($deps.Count) 个）"
        foreach ($dep in $deps) {
            $identity = Get-MsixIdentity $dep.FullName
            $installed = if ($identity) { Get-InstalledVersion $identity.Name } else { $null }
            if ($installed -and $identity -and $installed -ge $identity.Version) {
                Write-Line "      $($dep.Name)：系统里已有更高版本（$installed），跳过" 'DarkGray'
                continue
            }

            Write-Host "      $($dep.Name) … " -NoNewline
            try {
                Add-AppxPackage -Path $dep.FullName -ForceApplicationShutdown -ErrorAction Stop
                Write-Line '成功' 'Green'
            }
            catch {
                Write-Line '跳过' 'Yellow'
                Write-Line '         系统里多半已有更高版本的运行时，这不影响后面的安装。' 'DarkGray'
                Write-Line "         原文：$($_.Exception.Message)" 'DarkGray'
            }
        }
    }
    else {
        Write-Line '[2/3] 没找到依赖包（系统已装 Windows App Runtime 时可以忽略）' 'DarkGray'
    }

    # ── 3/3 主应用 ──────────────────────────────────────────────────────────
    $app = Get-ChildItem -LiteralPath $dir -Filter '*.msix' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike 'Microsoft*' } | Select-Object -First 1
    if (-not $app) {
        Write-Line '[3/3] 没找到主应用的 msix，安装终止。' 'Red'
        Stop-WithPause 1
    }

    $appIdentity = Get-MsixIdentity $app.FullName
    Write-Line "[3/3] 安装应用：$($app.Name)"
    if ($appIdentity) {
        $installedApp = Get-InstalledVersion $appIdentity.Name
        if ($installedApp) { Write-Line "      当前已装版本：$installedApp；本包版本：$($appIdentity.Version)" 'DarkGray' }
    }

    $installed = $false
    try {
        Add-AppxPackage -Path $app.FullName -ForceApplicationShutdown -ErrorAction Stop
        $installed = $true
    }
    catch {
        Write-Line "      常规安装失败：$($_.Exception.Message)" 'Yellow'
        Write-Line '      尝试强制覆盖安装（-ForceUpdateFromAnyVersion）…'
        try {
            Add-AppxPackage -Path $app.FullName -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction Stop
            $installed = $true
        }
        catch {
            Write-Line "      仍然失败：$($_.Exception.Message)" 'Red'
        }
    }

    Write-Line ''
    if ($installed) {
        Write-Line '安装完成：开始菜单里搜索「哔哩助理」即可启动。' 'Green'
        Write-Line '（本 fork 已把「侧边导航栏设置」与「搜索推荐」从设置界面移除，导航只剩「动态」；' 'DarkGray'
        Write-Line ' 播放器设置里的「不使用 P2P」默认已开启，用于避开 PCDN 节点导致的无声。）' 'DarkGray'
        Stop-WithPause 0
    }
    else {
        Write-Line '安装未完成，常见原因：' 'Red'
        Write-Line '  · 证书没进「受信任的根证书颁发机构」：双击包内 focus-sideload.cer → 安装证书 → 本地计算机 → 受信任的根证书颁发机构；'
        Write-Line '  · 机器上已经装了同版本（本包版本见上面那行）；同版本需要先卸载再装，或等更高版本；'
        Write-Line '  · 系统版本低于 Windows 10 2004（内部版本 19041）。'
        Stop-WithPause 1
    }
}
catch {
    Write-Line ''
    Write-Line '安装器遇到未预期的错误：' 'Red'
    Write-Line ($_ | Out-String) 'Red'
    Write-Line '把这段原文发给作者即可定位。' 'Yellow'
    Stop-WithPause 1
}
