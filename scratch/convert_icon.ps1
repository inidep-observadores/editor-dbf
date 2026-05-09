Add-Type -AssemblyName System.Drawing
$pngPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.png"
$icoPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.ico"

if (Test-Path $pngPath) {
    $bmp = New-Object System.Drawing.Bitmap($pngPath)
    $icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
    $stream = [System.IO.File]::Create($icoPath)
    $icon.Save($stream)
    $stream.Close()
    $bmp.Dispose()
    Write-Host "Icon created successfully at $icoPath"
} else {
    Write-Error "Source PNG not found at $pngPath"
}
