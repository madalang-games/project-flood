param(
  [Parameter(Mandatory = $true)]
  [string]$LogFile,

  [Parameter(Mandatory = $true)]
  [string]$StepName,

  [Parameter(Mandatory = $true)]
  [string]$ScriptPath,

  [Parameter(Mandatory = $true)]
  [string]$BatchPath
)

$ErrorActionPreference = 'Stop'

function Write-RunLog {
  param([Parameter(Mandatory = $true)][string]$Message)
  $Message | Tee-Object -FilePath $LogFile -Append
}

try {
  $resolvedScript = (Resolve-Path -LiteralPath $ScriptPath).Path
  $resolvedBatch = (Resolve-Path -LiteralPath $BatchPath).Path
  $nodeCommand = (Get-Command node -ErrorAction Stop).Source

  Write-RunLog "[batch] RUN step=$StepName script=$resolvedScript"

  $previousErrorActionPreference = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try {
    & $nodeCommand $resolvedScript 2>&1 | ForEach-Object {
      $_.ToString() | Tee-Object -FilePath $LogFile -Append
    }
  }
  finally {
    $ErrorActionPreference = $previousErrorActionPreference
  }

  $exitCode = $global:LASTEXITCODE
  if ($null -eq $exitCode) {
    $exitCode = 0
  }

  if ($exitCode -ne 0) {
    Write-RunLog "[batch] ERROR step=$StepName exit_code=$exitCode"
    Write-RunLog "[batch] ERROR script=$resolvedScript"
    Write-RunLog "[batch] ERROR batch=$resolvedBatch"
    exit $exitCode
  }

  Write-RunLog "[batch] OK step=$StepName exit_code=0"
  exit 0
}
catch {
  $position = $_.InvocationInfo.PositionMessage -replace "`r?`n", ' '
  $scriptStack = $_.ScriptStackTrace -replace "`r?`n", ' | '

  Write-RunLog "[batch] EXCEPTION step=$StepName"
  Write-RunLog "[batch] EXCEPTION script=$ScriptPath"
  Write-RunLog "[batch] EXCEPTION batch=$BatchPath"
  Write-RunLog "[batch] EXCEPTION type=$($_.Exception.GetType().FullName)"
  Write-RunLog "[batch] EXCEPTION message=$($_.Exception.Message)"
  if ($position) {
    Write-RunLog "[batch] EXCEPTION position=$position"
  }
  if ($scriptStack) {
    Write-RunLog "[batch] EXCEPTION stack=$scriptStack"
  }
  exit 1
}
