# Batch-link Theme/textures/{ingredients,enemies}/*.png into content/*/*.tres Icon field.
# Usage: powershell -ExecutionPolicy Bypass -File tools\link_icons.ps1
# Idempotent: .tres files that already have an Icon are skipped.

$ErrorActionPreference = "Stop"
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0

function Link-Tres {
    param([string]$tresPath, [string]$pngPath)
    $import = "$pngPath.import"
    if (-not (Test-Path $import)) { Write-Warning "Missing import: $import"; return }
    $imp = Get-Content $import -Encoding UTF8 -Raw
    $m = [regex]::Match($imp, 'uid="(uid://[^"]+)"')
    if (-not $m.Success) { Write-Warning "No uid in import: $import"; return }
    $uid = $m.Groups[1].Value

    $c = Get-Content $tresPath -Encoding UTF8 -Raw
    if ($c -match 'Icon\s*=\s*ExtResource') { return }  # already linked

    $resPath = "res://" + ($pngPath -replace '\\', '/')
    $ext = "[ext_resource type=""Texture2D"" uid=""$uid"" path=""$resPath"" id=""1_icon""]`n"

    # Insert right after the first [gd_resource ...] line (keeps ext_resource before sub_resource)
    $nl = $c.IndexOf("`n")
    if ($nl -lt 0) { Write-Warning "Bad format: $tresPath"; return }
    $c = $c.Substring(0, $nl + 1) + $ext + $c.Substring($nl + 1)

    # Add Icon property right after the [resource] line
    $idx = $c.IndexOf("[resource]")
    if ($idx -lt 0) { Write-Warning "No [resource] section: $tresPath"; return }
    $mark = "[resource]"
    $c = $c.Substring(0, $idx + $mark.Length) + "`nIcon = ExtResource(""1_icon"")" + $c.Substring($idx + $mark.Length)

    [System.IO.File]::WriteAllText($tresPath, $c, $utf8)
    Write-Output "Linked: $tresPath <- $pngPath"
    $script:count++
}

# Ingredients
Get-ChildItem "content\ingredients\*.tres" | ForEach-Object {
    $png = "Theme\textures\ingredients\$($_.BaseName).png"
    if (Test-Path $png) { Link-Tres $_.FullName $png }
}

# Enemies (only those that already have a .tres)
Get-ChildItem "content\enemies\*.tres" | ForEach-Object {
    $png = "Theme\textures\enemies\$($_.BaseName).png"
    if (Test-Path $png) { Link-Tres $_.FullName $png }
}

Write-Output "Done. Linked $count total."
