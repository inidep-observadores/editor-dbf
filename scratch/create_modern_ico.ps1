$pngPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.png"
$icoPath = "D:\Desarrollo\_INIDEP\OBS\EditorDbf\EditorDbf.App\Assets\app_icon.ico"

if (Test-Path $pngPath) {
    $pngBytes = [System.IO.File]::ReadAllBytes($pngPath)
    
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # IconDir (6 bytes)
    $bw.Write([uint16]0) # Reserved
    $bw.Write([uint16]1) # Type (Icon)
    $bw.Write([uint16]1) # Count

    # IconDirEntry (16 bytes)
    # Suponiendo que la imagen es de 256x256 o similar. 
    # Para PNG-in-ICO, 0 significa 256.
    $bw.Write([byte]0)   # Width
    $bw.Write([byte]0)   # Height
    $bw.Write([byte]0)   # Color count
    $bw.Write([byte]0)   # Reserved
    $bw.Write([uint16]1)  # Planes
    $bw.Write([uint16]32) # BPP
    $bw.Write([uint32]$pngBytes.Length) # Data size
    $bw.Write([uint32]22) # Offset (6 + 16)

    $bw.Write($pngBytes)
    $bw.Flush()

    [System.IO.File]::WriteAllBytes($icoPath, $ms.ToArray())
    $bw.Close()
    $ms.Close()
    Write-Host "ICO created successfully using PNG-in-ICO format."
} else {
    Write-Error "PNG not found."
}
