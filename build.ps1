$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pluginProject = Join-Path $projectRoot "src\Emby.BestEpisodes\Emby.BestEpisodes.csproj"
$testProject = Join-Path $projectRoot "tests\Emby.BestEpisodes.Tests\Emby.BestEpisodes.Tests.csproj"
$outputDirectory = Join-Path $projectRoot "dist"

dotnet restore $testProject
dotnet test $testProject --configuration Release --no-restore
dotnet build $pluginProject --configuration Release --no-restore

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
Copy-Item `
    (Join-Path $projectRoot "src\Emby.BestEpisodes\bin\Release\netstandard2.0\Emby.BestEpisodes.dll") `
    $outputDirectory `
    -Force

Write-Host "Plugin ready: $outputDirectory\Emby.BestEpisodes.dll"

