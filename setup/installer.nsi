!define APP_NAME "EditorDbf"
!define COMP_NAME "INIDEP"
!define COMP_DESC "Instituto Nacional de Investigación y Desarrollo Pesquero"
!define INSTALL_DIR "EditorDbf"
!define VERSION "1.0.0"

; Configuración del instalador
; Se definen los iconos ANTES de incluir MUI2.nsh para evitar errores de "already defined"
!define MUI_ICON "../EditorDbf.App/Assets/app_icon.ico"
!define MUI_UNICON "../EditorDbf.App/Assets/app_icon.ico"

; Configuración de la página de instalación
!include "MUI2.nsh"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "Spanish"

Section "Install"
    SetOutPath "$INSTDIR"
    
    ; Copiar todos los archivos del directorio de publicación
    ; Se asume que los archivos están en la carpeta 'publish' (generada por el .bat)
    File /r "publish\*"

    ; Generar el desinstalador
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Crear acceso directo en el menú inicio
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\EditorDbf.App.exe"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
    
    ; Crear acceso directo en el escritorio
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\EditorDbf.App.exe"
SectionEnd

Section "Uninstall"
    ; Eliminar archivos y carpetas
    Delete "$DESKTOP\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
    RMDir "$SMPROGRAMS\${APP_NAME}"
    
    ; Eliminar el desinstalador mismo
    Delete "$INSTDIR\Uninstall.exe"
    
    RMDir /r "$INSTDIR"
SectionEnd

OutFile "EditorDbf_Setup.exe"
Name "${APP_NAME}"
InstallDir "$PROGRAMFILES\${INSTALL_DIR}"
ShowInstDetails show
ShowUninstDetails show
