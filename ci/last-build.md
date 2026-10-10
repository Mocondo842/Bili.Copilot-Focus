# Fork build report

| 项 | 值 |
|---|---|
| run id | 38019406701 (#37) |
| ref | focus/derec |
| commit | 44639a58021230cf9ad79107650a29cce5aac8ea |
| 版本 | 2.2511.2.916 |
| job 状态 | success |
| msbuild 退出码 | `MSBUILD_EXIT=0` |
| 产物 | BiliCopilot.UI_2.2511.2.916_x64.msix (142.1 MB), Microsoft.WindowsAppRuntime.1.8.msix (39 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB), Microsoft.WindowsAppRuntime.1.8.msix (41 MB), Microsoft.WindowsAppRuntime.1.8.msix (20.7 MB) |

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
== verify D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\BiliCopilot.UI_2.2511.2.916_x64.msix (142.1 MB) ==
AppxSignature.p7x = present, 1614 bytes


Verifying: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\BiliCopilot.UI_2.2511.2.916_x64.msix


Signature Index: 0 (Primary Signature)
Hash of file (sha256): 4150505841585043EA7448A15708F0E101AF37A637491C023571145E445A0D18EB0C1F079440283141584344D4D48373084AA9EB8A0E8C68B4DB8E53D17DB96CD51C9063C9AC5FAB4DDE65F34158435412ECAECF8D7389A7D4C11B306ED2CB6BDB3E25062A34E8BE4387DF12BDD64C924158424DB13DFE43BBACD819DB3739362A08776079037070199F5D51DFF3E1B6496556C241584349A7D9061A3EB0BBAF39978E362CB981F5A1374A4FA05F897A6B15F025568E5142

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
== 回拉验收 ==
local  sha256 = 0C711D2FA8B3FD745ED8DE49969B6BC9BD06C2E12961DA2F5089DCD395B6CA24
remote sha256 = 0C711D2FA8B3FD745ED8DE49969B6BC9BD06C2E12961DA2F5089DCD395B6CA24
```

## collect / release transcript（各 60 行）

```
**********************
PowerShell transcript start
Start time: 20261010031523
Username: runnervmfi6oq\runneradmin
RunAs User: runnervmfi6oq\runneradmin
Configuration Name: 
Machine: runnervmfi6oq (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\dcaa5942-2ee8-437d-9543-76c9102938d8.ps1'
Process ID: 3756
PSVersion: 7.6.6
PSEdition: Core
GitCommitId: 7.6.6
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
app: D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\BiliCopilot.UI_2.2511.2.916_x64.msix
dep(x64): D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\Dependencies\x64\Microsoft.WindowsAppRuntime.1.8.msix
--- FinalPackages ---

Name                                      Length
----                                      ------
BiliCopilot-Focus_2.2511.2.916_x64.zip 190831997
BiliCopilot.UI_2.2511.2.916_x64.msix   149047227
focus-sideload.cer                           808
Install-Focus.cmd                           1283
Install-Focus.ps1                           9822
Microsoft.WindowsAppRuntime.1.8.msix    43025663
安装说明.txt                                4036
**********************
PowerShell transcript end
End time: 20261010031531
**********************
```

```
**********************
PowerShell transcript start
Start time: 20261010031532
Username: runnervmfi6oq\runneradmin
RunAs User: runnervmfi6oq\runneradmin
Configuration Name: 
Machine: runnervmfi6oq (Microsoft Windows NT 10.0.26100.0)
Host Application: C:\Program Files\PowerShell\7\pwsh.dll -command . 'D:\a\_temp\7b6bbf13-461a-4f79-8ad7-e4b23823955e.ps1'
Process ID: 8540
PSVersion: 7.6.6
PSEdition: Core
GitCommitId: 7.6.6
OS: Microsoft Windows 10.0.26100
Platform: Win32NT
PSCompatibleVersions: 1.0, 2.0, 3.0, 4.0, 5.0, 5.1, 6.0, 7.0
PSRemotingProtocolVersion: 2.4
SerializationVersion: 1.1.0.1
WSManStackVersion: 3.0
**********************
release not found

**********************
PowerShell transcript end
End time: 20261010031545
**********************
```

## FinalPackages 清单

```
BiliCopilot-Focus_2.2511.2.916_x64.zip  182 MB
BiliCopilot.UI_2.2511.2.916_x64.msix  142.1 MB
focus-sideload.cer  0 MB
Install-Focus.cmd  0 MB
Install-Focus.ps1  0 MB
Microsoft.WindowsAppRuntime.1.8.msix  41 MB
安装说明.txt  0 MB
```

## 交付脚本行尾/编码体检

```
scripts/Install-Focus.cmd : CRLF=49 裸LF=0 前三字节=64,101,99
scripts/Install-Focus.ps1 : CRLF=199 裸LF=0 前三字节=239,187,191
scripts/fork-install-guide.txt : CRLF=69 裸LF=0 前三字节=239,187,191
```

## 构建日志尾部（250 行）

```
MSBuild version 18.10.1-1.26427.6+3cd27c13e for .NET Framework

  BiliCopilot.UI.ResourceGenerator -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\AnyCPU\Release\BiliCopilot.UI.ResourceGenerator\bin\BiliCopilot.UI.ResourceGenerator.dll
  BiliCopilot.Visor.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.Visor.Models\bin\BiliCopilot.Visor.Models.dll
  BiliCopilot.UI.Models -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI.Models\bin\BiliCopilot.UI.Models.dll
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\src\_build\x64\Release\BiliCopilot.UI\bin\BiliCopilot.UI.dll
  Generating native code
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.set_27_SymbolIcon_UseSegoeMetrics(object,object)' will always throw because: Missing method 'Void FluentIcons.WinUI.SymbolIcon.set_UseSegoeMetrics(Boolean)'
  ILC: Method '[Richasy.MpvKernel.WinUI]Richasy.MpvKernel.WinUI.MpvKernel_WinUI_XamlTypeInfo.XamlTypeInfoProvider.get_27_SymbolIcon_UseSegoeMetrics(object)' will always throw because: Missing method 'Boolean FluentIcons.WinUI.SymbolIcon.get_UseSegoeMetrics()'
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\BiliCopilot.UI_2.2511.2.916_x64.msix
  BiliCopilot.UI -> D:\a\Bili.Copilot-Focus\Bili.Copilot-Focus\AppPackages\BiliCopilot.UI_2.2511.2.916_x64_Test\BiliCopilot.UI_2.2511.2.916_x64.appxsym

```
