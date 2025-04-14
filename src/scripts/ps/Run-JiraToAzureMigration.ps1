param (
    [string]$pat,          # Azure DevOps token (parameterized)
    [string]$userEmail,    # Jira user email (parameterized)
    [string]$exportUrl = "https://biotel.atlassian.net", # Default Export URL
    [string]$importUrl = "https://dev.azure.com/phm-caremgmt/", # Default Import URL
    [string]$exportExePath = "C:\path\to\jira-export.exe", # Default path to export executable
    [string]$importExePath = "C:\path\to\wi-import.exe",   # Default path to import executable
    [string]$configPath = "C:\repos\jira-azuredevops-migrator\exec\workspace\configuration\config-scrum-philips.json", # Config file path
    [switch]$force,        # Optional flag for --force
    [switch]$isExport      # Flag to determine Export or Import operation
)

# Validate input tokens
if (-not $pat -or -not $userEmail) {
    Write-Error "Both --pat (Azure DevOps Token) and --userEmail (Jira Email) parameters are required."
    exit 1
}

# Build arguments dynamically
$arguments = @{}
$exePath = if ($isExport) {
    $arguments["-u"] = $userEmail
    $arguments["-p"] = $pat
    $arguments["--url"] = $exportUrl
    $arguments["--config"] = $configPath
    $exportExePath
} else {
    $arguments["--token"] = $pat
    $arguments["--url"] = $importUrl
    $arguments["--config"] = $configPath
    $importExePath
}

# Add --force flag if true
if ($force) {
    $arguments["--force"] = $null
}

# Convert arguments into a formatted string
$argumentsString = ($arguments.GetEnumerator() | ForEach-Object {
    if ($_.Value) { "$($_.Key) $($_.Value)" } else { "$($_.Key)" }
}) -join " "

# Run process and capture real-time output
Write-Host "Executing: $exePath $argumentsString"

$process = Start-Process -FilePath $exePath -ArgumentList $argumentsString -NoNewWindow -PassThru -RedirectStandardOutput "stdout.log" -RedirectStandardError "stderr.log"

# Stream live output
while (-not $process.HasExited) {
    Get-Content "stdout.log" -Wait | ForEach-Object { Write-Host $_ }
    Get-Content "stderr.log" -Wait | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
}

# Cleanup temp log files
Remove-Item "stdout.log", "stderr.log"
