param(
    [string] $BackupFileName
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$backupRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'database/backups'))
$backupContainerRoot = '/var/opt/mssql/backups'
$sqlContainer = 'makeupart-sql'

function ConvertTo-BashQuoted([string] $Value) {
    return "'" + $Value.Replace("'", "'\''") + "'"
}

function Invoke-ContainerSql([string] $Query, [switch] $TableOutput) {
    $quotedQuery = ConvertTo-BashQuoted $Query
    $command = '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d master -b -Q ' + $quotedQuery
    if ($TableOutput) { $command += ' -h -1 -W -s "|"' }

    $output = @(& docker exec $sqlContainer /bin/bash -lc $command 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'SQL Server backup or restore command failed.' }
    return $output
}

if ([string]::IsNullOrWhiteSpace($BackupFileName)) {
    $BackupFileName = "MakeupArtDb-restore-check-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')).bak"
    if ($BackupFileName -notmatch '^[A-Za-z0-9._-]+\.bak$') { throw 'Generated backup file name was invalid.' }
    $containerPath = "$backupContainerRoot/$BackupFileName"
    Invoke-ContainerSql "BACKUP DATABASE [MakeupArtDb] TO DISK = N'$containerPath' WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT; RESTORE VERIFYONLY FROM DISK = N'$containerPath';" | Out-Null
}

if ($BackupFileName -notmatch '^[A-Za-z0-9._-]+\.bak$') { throw 'BackupFileName must be a plain .bak file name.' }
$backupHostPath = [System.IO.Path]::GetFullPath((Join-Path $backupRoot $BackupFileName))
if (-not $backupHostPath.StartsWith($backupRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Backup file must stay inside database/backups.'
}
if (-not (Test-Path -LiteralPath $backupHostPath -PathType Leaf)) { throw "Backup file not found: $BackupFileName" }
$containerBackupPath = "$backupContainerRoot/$BackupFileName"

$scratchDatabase = "MakeupArtRestoreCheck_$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))_$([Guid]::NewGuid().ToString('N').Substring(0, 6))"
$scratchFiles = [System.Collections.Generic.List[string]]::new()
$restoreVerified = $false

try {
    Invoke-ContainerSql "RESTORE VERIFYONLY FROM DISK = N'$containerBackupPath';" | Out-Null
    $fileList = Invoke-ContainerSql "RESTORE FILELISTONLY FROM DISK = N'$containerBackupPath';" -TableOutput
    $restoreFiles = foreach ($line in $fileList) {
        $parts = ([string]$line).Split('|')
        if ($parts.Length -ge 3 -and $parts[2].Trim() -in @('D', 'L')) {
            $logicalName = $parts[0].Trim()
            if ($logicalName -notmatch '^[A-Za-z0-9_-]+$') { throw 'Backup contains an unexpected logical file name.' }
            [PSCustomObject]@{ LogicalName = $logicalName; Type = $parts[2].Trim(); OriginalPath = $parts[1].Trim() }
        }
    }
    if (-not ($restoreFiles | Where-Object Type -eq 'D') -or -not ($restoreFiles | Where-Object Type -eq 'L')) {
        throw 'Backup file list did not contain both data and log files.'
    }

    $dataIndex = 0
    $logIndex = 0
    $moveClauses = foreach ($file in $restoreFiles) {
        if ($file.Type -eq 'L') {
            $suffix = "log$logIndex.ldf"
            $logIndex++
        } else {
            $extension = [System.IO.Path]::GetExtension($file.OriginalPath)
            if ($extension -notmatch '^\.(mdf|ndf)$') { $extension = if ($dataIndex -eq 0) { '.mdf' } else { '.ndf' } }
            $suffix = "data$dataIndex$extension"
            $dataIndex++
        }
        $target = "/var/opt/mssql/data/$scratchDatabase-$suffix"
        $scratchFiles.Add($target)
        "MOVE N'$($file.LogicalName)' TO N'$target'"
    }

    $moves = $moveClauses -join ', '
    Invoke-ContainerSql "RESTORE DATABASE [$scratchDatabase] FROM DISK = N'$containerBackupPath' WITH $moves, RECOVERY, CHECKSUM;" | Out-Null
    $counts = Invoke-ContainerSql "USE [$scratchDatabase]; SET NOCOUNT ON; SELECT CAST((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS varchar(12)) + '|' + CAST((SELECT COUNT(*) FROM dbo.Products) AS varchar(12));" -TableOutput
    $countLine = $counts | Where-Object { ([string]$_) -match '^\d+\|\d+$' } | Select-Object -Last 1
    if (-not $countLine) { throw 'Scratch restore verification query returned no counts.' }
    $values = ([string]$countLine).Split('|')
    $tableCount = [int]$values[0]
    $productCount = [int]$values[1]
    if ($tableCount -lt 1 -or $productCount -lt 1) { throw 'Scratch restore is missing the expected application data.' }
    $restoreVerified = $true
} finally {
    $drop = "IF DB_ID(N'$scratchDatabase') IS NOT NULL BEGIN ALTER DATABASE [$scratchDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$scratchDatabase]; END;"
    Invoke-ContainerSql $drop | Out-Null
    foreach ($file in $scratchFiles) {
        $remove = 'rm -f -- ' + (ConvertTo-BashQuoted $file)
        & docker exec $sqlContainer /bin/bash -lc $remove | Out-Null
    }
}

if (-not $restoreVerified) { throw 'Scratch restore did not complete.' }
Write-Output "Verified backup '$BackupFileName' by restoring it to a scratch database ($tableCount tables, $productCount products)."
