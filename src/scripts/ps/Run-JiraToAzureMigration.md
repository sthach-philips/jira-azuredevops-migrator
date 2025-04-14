# Running Jira to Azure DevOps Migration Script

## Overview
This script facilitates the migration process between Jira and Azure DevOps by exporting data from Jira or importing configurations into Azure DevOps. It supports dynamic inputs through parameters for flexibility and security.

## Requirements
- PowerShell 5.0 or higher.
- Valid tokens:
  - Azure DevOps Personal Access Token (PAT).
  - Jira user email.
- Executable files for Jira export and Azure DevOps import:
  - `jira-export.exe`
  - `wi-import.exe`
- Migration configuration file (`config-scrum-philips.json`).

## Parameters
The script accepts the following parameters:

| Parameter        | Description                                               | Required | Default Value                           |
|------------------|-----------------------------------------------------------|----------|-----------------------------------------|
| `-pat`           | Azure DevOps Personal Access Token (PAT).                 | Yes      | N/A                                     |
| `-userEmail`     | Jira user email address.                                  | Yes      | N/A                                     |
| `-exportUrl`     | URL for Jira instance.                                    | No       | `https://biotel.atlassian.net`          |
| `-importUrl`     | URL for Azure DevOps instance.                            | No       | `https://dev.azure.com/phm-caremgmt/`   |
| `-exportExePath` | Path to the Jira export executable (`jira-export.exe`).   | No       | `C:\path\to\jira-export.exe`            |
| `-importExePath` | Path to the Azure DevOps import executable (`wi-import.exe`).| No       | `C:\path\to\wi-import.exe`              |
| `-configPath`    | Path to the migration configuration file.                 | No       | `C:\repos\jira-azuredevops-migrator\exec\workspace\configuration\config-scrum-philips.json` |
| `-force`         | Optional flag to add the `--force` parameter.             | No       | Disabled                                |
| `-isExport`      | Flag to determine whether to export (`true`) or import (`false`).| No    | Export enabled (`true`).                |

## Usage Examples

### 1. Run Export from Jira
To export data from Jira, use the following command:
```powershell
.\MigrationScript.ps1 -pat "your_azure_pat" -userEmail "your_email@domain.com" -isExport
```

### 2. Run Import to Azure DevOps
To import data into Azure DevOps, use the following command:
```powershell
.\MigrationScript.ps1 -pat "your_azure_pat" -userEmail "your_email@domain.com" -isExport:$false
```

### 3. Force Migration
To enable the `--force` flag, add the `-force` parameter:
```powershell
.\MigrationScript.ps1 -pat "your_azure_pat" -userEmail "your_email@domain.com" -force -isExport:$false
```

### 4. Customize Executable Paths and Configuration
If you need to specify custom paths for the executables or configuration file, include the relevant parameters:
```powershell
.\MigrationScript.ps1 -pat "your_azure_pat" -userEmail "your_email@domain.com" -exportExePath "D:\tools\jira-export.exe" -importExePath "D:\tools\wi-import.exe" -configPath "D:\config\custom-config.json"
```

## Notes
- Ensure that environment variables for sensitive data are securely managed if used instead of direct input.

```powershell
$env:AZURE_DEVOPS_PAT = "your_real_azure_pat"
$env:JIRA_USER_EMAIL = "your_real_email"
```   
- The script dynamically builds arguments for execution, allowing flexibility for custom configurations.
- Check for correct permissions in Azure DevOps and Jira before running the script.