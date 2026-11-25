$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$outDir = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$source = 'Program.cs','Settings.cs','StartupTask.cs','Editor.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
$output = Join-Path $outDir 'SCUNET_Config_Tool.exe'
$manifest = Join-Path $PSScriptRoot 'app.manifest'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 "/win32manifest:$manifest" "/out:$output" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll $source
if ($LASTEXITCODE -ne 0) { throw 'SCUNET build failed.' }
Write-Output $output
