param(
    [ValidateRange(0, 100)]
    [double]$MinimumLineCoverage = 80
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$results = Join-Path $repository ('TestResults/run-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$project = Join-Path $PSScriptRoot 'TodoLearn.Tests/TodoLearn.Tests.csproj'
$settings = Join-Path $PSScriptRoot 'coverage.runsettings'

dotnet test $project --settings $settings --collect 'XPlat Code Coverage' --results-directory $results --logger 'trx;LogFileName=tests.trx'
if ($LASTEXITCODE -ne 0) { throw 'NUnit tests failed.' }

$reports = @(Get-ChildItem -LiteralPath $results -Directory | ForEach-Object {
    Get-ChildItem -LiteralPath $_.FullName -Filter 'coverage.cobertura.xml' -File
})
if ($reports.Count -ne 1) { throw 'Expected one Cobertura coverage report.' }
[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName
$invariantCulture = [System.Globalization.CultureInfo]::InvariantCulture
$lines = [double]::Parse($coverage.coverage.'line-rate', $invariantCulture) * 100
$branches = [double]::Parse($coverage.coverage.'branch-rate', $invariantCulture) * 100
if ([int]$coverage.coverage.'lines-valid' -eq 0) { throw 'No application code was measured.' }

Write-Output ('Application logic coverage: {0:F2}% lines, {1:F2}% branches.' -f $lines, $branches)
Write-Output ('Report: ' + $reports[0].FullName)
if ($lines -lt $MinimumLineCoverage) {
    throw "Line coverage is below the required $MinimumLineCoverage%."
}
