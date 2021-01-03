# Required:
# Install-Module -Name Invoke-MsBuild -RequiredVersion 2.6.2

$ErrorActionPreference = "Stop"
$location = Get-Location
$now = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$solution = "$location\IronBank.sln"
$outputdir = "$location\releases"
$outputfile = "$outputdir\${now}.zip"

Write-Host "Building $solution"
Write-Host "To $outputfile"

Invoke-MsBuild -Path ".\IronBank.sln" -MsBuildParameters "/target:Clean;Build /property:Configuration=Release"
New-Item -ItemType "Directory" -Path "$outputdir" -Force
Compress-Archive -Path ".\SubModule.xml",".\bin" -DestinationPath "$outputfile" -CompressionLevel "Optimal"
