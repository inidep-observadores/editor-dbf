!define APP_NAME "EditorDbf"
!define COMP_NAME "INIDEP"
!define COMP_DESC "Instituto Nacional de Investigación y Desarrollo Pesquero"
!define INSTALL_DIR "EditorDbf"
!define VERSION "1.0.0"

; Configuración de la página de instalación
!include "MUI2.nsh"

!set INSERTMACRO MacroVariables
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "Spanish"

Var InstallDir

Section "Install"
    SetOutPath "$INSTDIR"
    
    ; Copiar todos los archivos del directorio de publicación
    ; Se asume que los archivos están en la carpeta 'publish_output' durante el proceso de compilación de NSIS
    File /r "publish_output\*"

    ; Crear acceso directo en el menú inicio
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\EditorDbf.App.exe"
    
    ; Crear acceso directo en el escritorio
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\EditorDbf.App.exe"
SectionEnd

Section "Uninstall"
    ; Eliminar archivos y carpetas
    Delete "$DESKTOP\${APP_NAME}.lnk"
    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    RMDir "$SMPROGRAMS\${APP_NAME}"
    
    RMDir /r "$INSTDIR"
SectionEnd

; Configuración del instalador
!insertmacro MUI_C dlg_header_image "Assets/app_icon.png"
!insertmacro MUI_C dlg_footer_image "Assets/app_icon.png"

!insertmacro MUI_LANGUAGE_Spanish

!insertmacro MUI_FUNCTION_FINISH
!insertmacro MUI_FUNCTION_UNINSTALL

Section "Main"
    ; Ejecutar sección de instalación
    Call Install
SectionEnd
