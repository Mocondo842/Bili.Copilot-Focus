# Fork build report

| 项 | 值 |
|---|---|
| run id | 44134252104 (#66) |
| ref | focus/derec |
| commit | ea36b4fef529abca46a6bace3ae690c391e77e84 |
| 版本 | 3.0.0.5 |
| job 状态 | success |
| msbuild 退出码 | `MSBUILD_EXIT=0` |
| 产物 | BiliCopilot.UI_3.0.0.5_x64.msix (142.9 MB), Microsoft.WindowsAppRuntime.1.8.msix (39 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB), Microsoft.WindowsAppRuntime.1.8.msix (41 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB) |

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
== verify D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\BiliCopilot.UI_3.0.0.5_x64.msix (142.9 MB) ==
AppxSignature.p7x = present, 1614 bytes


Verifying: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\BiliCopilot.UI_3.0.0.5_x64.msix


Signature Index: 0 (Primary Signature)
Hash of file (sha256): 4150505841585043736BE87FA0C74EE2C6540DF0185CF7FE6802C674489C22926B237FE3DB26A48541584344066B677586DCAD87D0BD90E2C3DC7A01D15F214FBD3534E66ED7EB1DCF9B036141584354820F8F21D0A80BF20F8ED003BAD6030AD79D251AC41926A15CEE8DE527C2ED084158424D9BD285A38E760090E7C7573CEABFEE8958C865958EEBFA8E5B58E885CE41252F415843491D4C36365E1CE614432374D4CEED3B5B33928AE05129073333E7331DF420DCC0

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
draft release id = 448963030
local  sha256 = D5BC20625D8A70EB2E68F8E4CCF90C793C1C2FE8C7D761C78E9052952F7F36B7
remote sha256 = D5BC20625D8A70EB2E68F8E4CCF90C793C1C2FE8C7D761C78E9052952F7F36B7
```

## collect / release transcript（各 60 行）

```
**********************
PowerShell transcript start
Start time: 20261117172132
Username: runnervmm7ud1\runneradmin
RunAs User: runnervmm7ud1\runneradmin
Configuration Name: 
Machine: runnervmm7ud1 (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\5d293938-aa4d-4e11-87e9-fee0bbc64969.ps1'
Process ID: 10116
PSVersion: 7.6.9
PSEdition: Core
GitCommitId: 7.6.9
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
app: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\BiliCopilot.UI_3.0.0.5_x64.msix
dep(x64): D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\Dependencies\x64\Microsoft.WindowsAppRuntime.1.8.msix
--- FinalPackages ---

Name                                    Length
----                                    ------
BiliCopilot-Focus_3.0.0.5_x64.zip    191664313
BiliCopilot.UI_3.0.0.5_x64.msix      149878426
focus-sideload.cer                         808
Install-Focus.cmd                         1283
Install-Focus.ps1                         9822
Microsoft.WindowsAppRuntime.1.8.msix  43025663
安装说明.txt                              3967
**********************
PowerShell transcript end
End time: 20261117172138
**********************
```

```
**********************
PowerShell transcript start
Start time: 20261117172139
Username: runnervmm7ud1\runneradmin
RunAs User: runnervmm7ud1\runneradmin
Configuration Name: 
Machine: runnervmm7ud1 (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\d2597d8c-8d3d-42f8-aeaa-a1dc3410db76.ps1'
Process ID: 1256
PSVersion: 7.6.9
PSEdition: Core
GitCommitId: 7.6.9
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************

**********************
PowerShell transcript end
End time: 20261117172149
**********************
```

## FinalPackages 清单

```
BiliCopilot-Focus_3.0.0.5_x64.zip  182.8 MB
BiliCopilot.UI_3.0.0.5_x64.msix  142.9 MB
focus-sideload.cer  0 MB
Install-Focus.cmd  0 MB
Install-Focus.ps1  0 MB
Microsoft.WindowsAppRuntime.1.8.msix  41 MB
安装说明.txt  0 MB
```

## 拉取本次产物（草稿发布；gh 2.103+ 已支持按 tag 下载草稿，一行即可，整行复制执行）

```powershell
gh release download focus-3.0.0.5 --repo Mocondo842/Bili.Copilot-Focus --pattern "*.zip" --clobber
```

## 交付脚本行尾/编码体检

```
scripts/Install-Focus.cmd : CRLF=49 裸LF=0 前三字节=64,101,99
scripts/Install-Focus.ps1 : CRLF=199 裸LF=0 前三字节=239,187,191
scripts/fork-install-guide.txt : CRLF=0 裸LF=69 前三字节=239,187,191
```

## 构建日志尾部（250 行）

```
MSBuild version 18.11.1-1.26513.3+940033be3 for .NET Framework

  BiliCopilot.UI.ResourceGenerator -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\AnyCPU\Release\BiliCopilot.UI.ResourceGenerator\bin\BiliCopilot.UI.ResourceGenerator.dll
  BiliCopilot.UI.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI.Models\bin\BiliCopilot.UI.Models.dll
  BiliCopilot.Visor.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.Visor.Models\bin\BiliCopilot.Visor.Models.dll
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI\bin\BiliCopilot.UI.dll
  Generating native code
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.set_27_SymbolIcon_UseSegoeMetrics(object,object)' will always throw because: Missing method 'Void FluentIcons.WinUI.SymbolIcon.set_UseSegoeMetrics(Boolean)'
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.get_27_SymbolIcon_UseSegoeMetrics(object)' will always throw because: Missing method 'Boolean FluentIcons.WinUI.SymbolIcon.get_UseSegoeMetrics()'
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\BiliCopilot.UI_3.0.0.5_x64.msix
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_3.0.0.5_x64_Test\BiliCopilot.UI_3.0.0.5_x64.appxsym

```
