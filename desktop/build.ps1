param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin'), [switch]$Test)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler is required.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$sprite = Join-Path $PSScriptRoot '../assets/spritesheet-desktop.png'
$sources = @('Program.cs', 'PetModel.cs', 'Native.cs', 'PetForm.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$common = @('/nologo', '/optimize+', '/debug-', '/platform:anycpu', '/warnaserror+', '/codepage:65001',
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
    "/resource:$sprite,Shinobu.spritesheet.png", "/win32manifest:$PSScriptRoot/app.manifest", "/win32icon:$PSScriptRoot/pet.ico")
& $compiler @common '/target:winexe' "/out:$OutputDirectory/Shinobu-Oshino-pet.exe" @sources
if ($LASTEXITCODE -ne 0) { throw 'Desktop pet compilation failed.' }
if ($Test) {
    & $compiler @common '/target:exe' '/main:ShinobuPet.Tests' "/out:$OutputDirectory/ShinobuPet.Tests.exe" @sources (Join-Path $PSScriptRoot 'tests/Tests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & (Join-Path $OutputDirectory 'ShinobuPet.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Desktop pet tests failed.' }
}
Get-Item -LiteralPath (Join-Path $OutputDirectory 'Shinobu-Oshino-pet.exe') | Select-Object Name,Length
