Add-Type -AssemblyName System.Drawing
$inputPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.png"
$outputPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon_fixed.png"

if (Test-Path $inputPath) {
    $img = [System.Drawing.Image]::FromFile($inputPath)
    $img.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $img.Dispose()
    Write-Host "PNG re-saved as a standard PNG."
} else {
    Write-Error "Source not found."
}
