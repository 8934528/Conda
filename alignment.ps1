# Adds VerticalAlignment="Center" to icon and TextBlock inside 
# horizontal StackPanels (MenuItem headers), preserving all other markup.

param([string]$BaseDir = ".")

function Fix-XamlFile {
    param([string]$FilePath)
    
    if (-not (Test-Path $FilePath)) {
        Write-Host "Skipping (not found): $FilePath"
        return
    }

    $content = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
    $original = $content

    # PackIconMaterial tags that lack VerticalAlignment="Center"
    $content = [regex]::Replace($content,
        '(<iconPacks:PackIconMaterial\b(?:(?!VerticalAlignment)[^>])*)(/>)',
        '$1 VerticalAlignment="Center"$2')

    # StackPanel Orientation="Horizontal" add VerticalAlignment="Center" if missing  
    $content = [regex]::Replace($content,
        '(<StackPanel Orientation="Horizontal")(?![^>]*VerticalAlignment)([^>]*>)',
        '$1 VerticalAlignment="Center"$2')

    # TextBlock tags that are DIRECT children in icon+text rows:
    # Pattern: <TextBlock Text="SomeLiteral" .../> on a single line, inside a menu header StackPanel
    # These will have no VerticalAlignment. I target TextBlocks with just Text= (simple label TextBlocks)
    # that sit right after an iconPacks line (so they're icon row labels)
    # Strategy: match <TextBlock Text="..." [other simple attrs]/> with no VerticalAlignment
    # that appear immediately after a PackIconMaterial line
    $content = [regex]::Replace($content,
        '([ \t]*<iconPacks:PackIconMaterial[^\n]+\n[ \t]*)(<TextBlock Text="[^"]*"(?:\s+(?:Foreground|FontSize|FontWeight|FontFamily|Margin)="[^"]*")*\s*/>)',
        {
            param($m)
            $iconLine = $m.Groups[1].Value
            $tbTag = $m.Groups[2].Value
            if ($tbTag -match 'VerticalAlignment') {
                return $m.Value
            }
            $fixed = [regex]::Replace($tbTag, '(\s*/>)$', ' VerticalAlignment="Center"$1')
            return $iconLine + $fixed
        })

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($FilePath, $content, [System.Text.Encoding]::UTF8)
        Write-Host "Fixed: $(Split-Path $FilePath -Leaf)"
    } else {
        Write-Host "No changes needed: $(Split-Path $FilePath -Leaf)"
    }
}

$viewsDir = Join-Path $BaseDir "Conda\UI\Views"
$xamlFiles = Get-ChildItem -Path $viewsDir -Filter "*.xaml" -File

foreach ($f in $xamlFiles) {
    Fix-XamlFile -FilePath $f.FullName
}

Write-Host "`nAll done."
