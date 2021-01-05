# Required:
# Install-Module -Name Invoke-MsBuild -RequiredVersion 2.6.2

$ErrorActionPreference = "Stop"
$location = Get-Location
$version = Select-Xml -Path ".\SubModule.xml" -XPath "/Module/Version/@value"
  | Select-Object -Expand Node
  | Select-Object -Expand value
$solution = "$location\IronBank.sln"
$outputdir = "$location\releases"
$outputfile = "$outputdir\${version}.zip"

Write-Host "Building $solution"
Write-Host "To $outputfile"

Invoke-MsBuild -Path ".\IronBank.sln" -MsBuildParameters "/target:Clean;Build /property:Configuration=Release"
New-Item -ItemType "Directory" -Path "$outputdir" -Force
Compress-Archive -Path ".\SubModule.xml",".\bin" -DestinationPath "$outputfile" -CompressionLevel "Optimal"
