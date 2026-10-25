# Fork build report

| 项 | 值 |
|---|---|
| run id | 40196227810 (#42) |
| ref | focus/derec |
| commit | 222caecbcd3daf565a91c90a5f4ec1e45f92e647 |
| 版本 | 2.2511.2.919 |
| job 状态 | success |
| msbuild 退出码 | `MSBUILD_EXIT=0` |
| 产物 | BiliCopilot.UI_2.2511.2.919_x64.msix (142.4 MB), Microsoft.WindowsAppRuntime.1.8.msix (39 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB), Microsoft.WindowsAppRuntime.1.8.msix (41 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB) |

## 错误行（最多 80 条）

```
(无匹配的错误行)
```

## 早期步骤与签名校验诊断（diag.log 尾部 150 行）

```
== guard ==
fork_check: OK (all anchors present)
提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。
== probe ==

WindowsProductName             WindowsVersion
------------------             --------------
Windows Server 2025 Datacenter 2009


dotnet = C:\Program Files\dotnet\dotnet.exe
gh = C:\Program Files\GitHub CLI\gh.exe
7z = C:\ProgramData\Chocolatey\bin\7z.exe
msbuild = C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe
signtool = C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe
pfx present = True
== verify D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\BiliCopilot.UI_2.2511.2.919_x64.msix (142.4 MB) ==
AppxSignature.p7x = present, 1614 bytes


Verifying: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\BiliCopilot.UI_2.2511.2.919_x64.msix


Signature Index: 0 (Primary Signature)
Hash of file (sha256): 4150505841585043A5D8BED461549E1F7CA7369E233C3F104ACA78BB6C1D7905272C34A95F0C372641584344D7EEED6D03BFB8C8F07D14C7B9D1D3D8F615B726962618BAF3A0F144F2A788024158435412ECAECF8D7389A7D4C11B306ED2CB6BDB3E25062A34E8BE4387DF12BDD64C924158424D114CE7641E6B4A5A3C1708E5968261198D15192AD567CFFB08CD3F6D76E105AF415843497DD00A71257B15D0EDC708B6B54F23AE2D75683FDD1F872D249F55F7DFACC296

Signing Certificate Chain:
    Issued to: Richasy

    Issued by: Richasy

    Expires:   Tue Sep 30 10:16:43 2036

    SHA1 hash: 0F8D01AF8309E8EBA72D886C8442263979628799


File is not timestamped.


Number of files successfully Verified: 0

Number of warnings: 0

Number of errors: 1

SignTool Error: A certificate chain processed, but terminated in a root
	certificate which is not trusted by the trust provider.
== 草稿资产回拉验收 ==
draft release id = 423415884
local  sha256 = 10E0301EB41205EACD281EBC3FC841977518A0DB2312D77BF6107CCF71625A33
remote sha256 = 10E0301EB41205EACD281EBC3FC841977518A0DB2312D77BF6107CCF71625A33
```

## collect / release transcript（各 60 行）

```
**********************
PowerShell transcript start
Start time: 20261025131857
Username: runnervmdaln5\runneradmin
RunAs User: runnervmdaln5\runneradmin
Configuration Name: 
Machine: runnervmdaln5 (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\7f5ba29d-3f32-4c49-888c-0a869b890871.ps1'
Process ID: 2780
PSVersion: 7.6.7
PSEdition: Core
GitCommitId: 7.6.7
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
app: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\BiliCopilot.UI_2.2511.2.919_x64.msix
dep(x64): D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\Dependencies\x64\Microsoft.WindowsAppRuntime.1.8.msix
--- FinalPackages ---

Name                                      Length
----                                      ------
BiliCopilot-Focus_2.2511.2.919_x64.zip 191061481
BiliCopilot.UI_2.2511.2.919_x64.msix   149273987
focus-sideload.cer                           808
Install-Focus.cmd                           1283
Install-Focus.ps1                           9822
Microsoft.WindowsAppRuntime.1.8.msix    43025663
安装说明.txt                                4036
**********************
PowerShell transcript end
End time: 20261025131904
**********************
```

```
**********************
PowerShell transcript start
Start time: 20261025131904
Username: runnervmdaln5\runneradmin
RunAs User: runnervmdaln5\runneradmin
Configuration Name: 
Machine: runnervmdaln5 (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\08564153-217c-40e4-861c-579457e745be.ps1'
Process ID: 3324
PSVersion: 7.6.7
PSEdition: Core
GitCommitId: 7.6.7
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
同名草稿 release 已存在（id=423400076），删除后重建


**********************
PowerShell transcript end
End time: 20261025131917
**********************
```

## FinalPackages 清单

```
BiliCopilot-Focus_2.2511.2.919_x64.zip  182.2 MB
BiliCopilot.UI_2.2511.2.919_x64.msix  142.4 MB
focus-sideload.cer  0 MB
Install-Focus.cmd  0 MB
Install-Focus.ps1  0 MB
Microsoft.WindowsAppRuntime.1.8.msix  41 MB
安装说明.txt  0 MB
```

## 拉取本次产物（草稿发布；gh 2.103+ 已支持按 tag 下载草稿，一行即可，整行复制执行）

```powershell
gh release download focus-2.2511.2.919 --repo Mocondo842/Bili.Copilot-Focus --pattern "*.zip" --clobber
```

## 交付脚本行尾/编码体检

```
scripts/Install-Focus.cmd : CRLF=49 裸LF=0 前三字节=64,101,99
scripts/Install-Focus.ps1 : CRLF=199 裸LF=0 前三字节=239,187,191
scripts/fork-install-guide.txt : CRLF=69 裸LF=0 前三字节=239,187,191
```

## 构建日志尾部（250 行）

```
MSBuild version 18.11.1-1.26513.3+940033be3 for .NET Framework

  BiliCopilot.UI.ResourceGenerator -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\AnyCPU\Release\BiliCopilot.UI.ResourceGenerator\bin\BiliCopilot.UI.ResourceGenerator.dll
  BiliCopilot.Visor.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.Visor.Models\bin\BiliCopilot.Visor.Models.dll
  BiliCopilot.UI.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI.Models\bin\BiliCopilot.UI.Models.dll
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI\bin\BiliCopilot.UI.dll
  Generating native code
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.set_27_SymbolIcon_UseSegoeMetrics(object,object)' will always throw because: Missing method 'Void FluentIcons.WinUI.SymbolIcon.set_UseSegoeMetrics(Boolean)'
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.get_27_SymbolIcon_UseSegoeMetrics(object)' will always throw because: Missing method 'Boolean FluentIcons.WinUI.SymbolIcon.get_UseSegoeMetrics()'
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\BiliCopilot.UI_2.2511.2.919_x64.msix
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.919_x64_Test\BiliCopilot.UI_2.2511.2.919_x64.appxsym

```
