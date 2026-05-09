Add-Type -AssemblyName System.Drawing
$pngPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.png"
$icoPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.ico"

if (Test-Path $pngPath) {
    $bmp = New-Object System.Drawing.Bitmap($pngPath)
    # Redimensionar a 256x256 para asegurar calidad
    $resized = New-Object System.Drawing.Bitmap(256, 256)
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.DrawImage($bmp, 0, 0, 256, 256)
    $g.Dispose()
    
    $hIcon = $resized.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $stream = [System.IO.File]::Create($icoPath)
    $icon.Save($stream)
    $stream.Close()
    
    $resized.Dispose()
    $bmp.Dispose()
    Write-Host "ICO regenerated using standard GDI+ methods."
}
