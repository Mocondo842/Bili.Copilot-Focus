# Bili.Copilot-Focus 侧载安装器（本 fork 自带，替代上游 Install.ps1）
#
# 与上游 Install.ps1 的区别：
#   1. 非管理员运行时会请求提权，但**原窗口不会立刻消失**——把"提权中/失败怎么办"留在屏幕上；
#   2. 提权后的窗口用 -NoExit 启动，装完/报错都停在那里；
#   3. 证书用 certutil 静默导入（不弹确认框），失败也只在窗口里提示、不再吞掉；
#   4. 主应用常规安装失败时会自动用 -ForceUpdateFromAnyVersion 重试一次；
#   5. 每一步都有 [n/3] 进度与结果，最后一定停下来等按键。
#
# 用法：右键本文件 →「使用 PowerShell 运行」；或管理员 PowerShell 里：
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-Focus.ps1

$ErrorActionPreference = 'Continue'
try { $Host.UI.RawUI.WindowTitle = 'Bili.Copilot-Focus 安装器' } catch { }

function Stop-WithPause([int]$code) {
    Write-Host ''
    Write-Host '按任意键关闭此窗口…' -ForegroundColor DarkGray
    try { $null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') } catch { Start-Sleep -Seconds 10 }
    exit $code
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Clear-Host
Write-Host '=== Bili.Copilot-Focus 安装器 ===' -ForegroundColor Cyan

# 脚本所在目录：$PSScriptRoot 在 -File 下可靠，退回 $MyInvocation 与当前目录
$dir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($dir)) { $dir = Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($dir)) { $dir = (Get-Location).Path }
Set-Location -LiteralPath $dir
Write-Host "安装目录：$dir"

if (-not (Test-IsAdmin)) {
    Write-Host ''
    Write-Host '需要管理员权限。正在请求提权——请在 UAC 弹窗里点「是」。' -ForegroundColor Yellow
    try {
        $argList = "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$PSCommandPath`""
        Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs -ErrorAction Stop
        Write-Host '已发起提权请求：新的窗口会继续安装。' -ForegroundColor Yellow
        Write-Host '如果没看到 UAC 弹窗（有时会藏在别的窗口后面），' -ForegroundColor Yellow
        Write-Host '请关掉本窗口，改为右键本文件 →「以管理员身份运行」。' -ForegroundColor Yellow
    } catch {
        Write-Host "提权失败：$($_.Exception.Message)" -ForegroundColor Red
        Write-Host '请关掉本窗口，改为右键本文件 →「以管理员身份运行」。' -ForegroundColor Yellow
    }

    Stop-WithPause 1
}

Write-Host '权限：管理员 ✓' -ForegroundColor Green
Write-Host ''

# ── 1/3 信任证书 ────────────────────────────────────────────────────────────
$cer = Get-ChildItem -LiteralPath $dir -Filter '*.cer' -File -ErrorAction SilentlyContinue | Select-Object -First 1
if ($cer) {
    Write-Host "[1/3] 信任签名证书：$($cer.Name)"
    $certOut = & certutil.exe -f -addstore Root "$($cer.FullName)" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($certOut | Out-String)
        Write-Host '      证书导入失败。若之前已经信任过同一个证书，可以忽略这一步。' -ForegroundColor Yellow
    }
    else {
        Write-Host '      已加入「受信任的根证书颁发机构」' -ForegroundColor Green
    }
}
else {
    Write-Host '[1/3] 没找到 .cer，跳过证书导入（未签名的包将无法安装）' -ForegroundColor Yellow
}

# ── 2/3 安装依赖（Windows App Runtime 等，文件名以 Microsoft 开头）────────────
$deps = Get-ChildItem -LiteralPath $dir -Filter '*.msix' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -like 'Microsoft*' }
if ($deps) {
    Write-Host "[2/3] 安装依赖（$(@($deps).Count) 个）"
    foreach ($dep in $deps) {
        Write-Host "      $($dep.Name) … " -NoNewline
        try {
            Add-AppxPackage -Path $dep.FullName -ForceApplicationShutdown -ErrorAction Stop
            Write-Host '成功' -ForegroundColor Green
        }
        catch {
            Write-Host "跳过（$($_.Exception.Message)）" -ForegroundColor Yellow
        }
    }
}
else {
    Write-Host '[2/3] 没找到依赖包（系统已装 Windows App Runtime 时可以忽略）' -ForegroundColor DarkGray
}

# ── 3/3 安装主应用 ──────────────────────────────────────────────────────────
$app = Get-ChildItem -LiteralPath $dir -Filter '*.msix' -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike 'Microsoft*' } | Select-Object -First 1
if (-not $app) {
    Write-Host '[3/3] 没找到主应用的 msix，安装终止。' -ForegroundColor Red
    Stop-WithPause 1
}

Write-Host "[3/3] 安装应用：$($app.Name)"
$installed = $false
try {
    Add-AppxPackage -Path $app.FullName -ForceApplicationShutdown -ErrorAction Stop
    $installed = $true
}
catch {
    Write-Host "      常规安装失败：$($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host '      尝试强制覆盖安装（-ForceUpdateFromAnyVersion）…'
    try {
        Add-AppxPackage -Path $app.FullName -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction Stop
        $installed = $true
    }
    catch {
        Write-Host "      仍然失败：$($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ''
if ($installed) {
    Write-Host '安装完成：开始菜单里搜索「哔哩助理」即可启动。' -ForegroundColor Green
    Write-Host '（本 fork 已把「侧边导航栏设置」与「搜索推荐」从设置界面移除，导航只剩「动态」；' -ForegroundColor DarkGray
    Write-Host ' 播放器设置里的「不使用 P2P」默认已开启，用于避开 PCDN 节点导致的无声。）' -ForegroundColor DarkGray
    Stop-WithPause 0
}
else {
    Write-Host '安装未完成，常见原因：' -ForegroundColor Red
    Write-Host '  · 证书没进「受信任的根证书颁发机构」：双击包内 focus-sideload.cer → 安装证书 → 本地计算机 → 受信任的根证书颁发机构；'
    Write-Host '  · 机器上已经装了同版本或更高版本（看 msix 文件名里的版本号）；'
    Write-Host '  · 系统版本低于 Windows 10 2004（19041）。'
    Stop-WithPause 1
}
