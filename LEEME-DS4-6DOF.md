# DualShock 4 6DOF en el Virtual Device Manager

Este documento describe lo que se ha añadido al proyecto
**PSMoveServiceEx - Virtual Device Manager 13.14.9** para que el mando
DualShock 4 funcione en SteamVR con seguimiento 6DOF, usando el driver y el
servicio que ya estaban probados en el proyecto `PSMoveServiceEx`.

---

## 0. Instalación rápida (paquete ya compilado)

En la sección **Releases** de este repositorio está el paquete
`PSMoveServiceEx-DS4-13.14.9.zip`, que ya incluye todo lo necesario:

- `VirtualDeviceManager\` — el VDM con el driver `psmoveserviceex` dentro.
- `PSMoveService\` — el servicio y el config tool (0.27.8.0 con soporte DS4).
- `INSTALAR.bat` — escribe la ruta del servicio en `settings.ini`.

Pasos:

1. Descomprime el zip donde quieras (por ejemplo `C:\PSMoveServiceEx-DS4`).
2. Ejecuta `INSTALAR.bat`.
3. Abre `VirtualDeviceManager\PSMSVirtualDeviceManager.exe`.
4. **Virtual Motion Tracker → Management → Install Drivers**.
5. En **Virtual Trackers** añade tu webcam y marca **Autostart**. El
   **Virtual tracker count** se ajusta solo (el VDM lo sube a los slots que
   necesitan tus cámaras).
6. Arranca PSMoveServiceEx desde el VDM y después SteamVR.

No hace falta un segundo repositorio: el paquete y la release salen de este
mismo proyecto.

---

## 1. Qué se ha añadido

### 1.1. El driver de SteamVR (nuevo)

```
PSMSVirtualDeviceManager\driver\psmoveserviceex\
    driver.vrdrivermanifest
    bin\win64\driver_psmoveserviceex.dll
    resources\input\legacy_bindings_psmove_ds4.json
    resources\input\psmove_ds4_profile.json
    resources\localization\localization.json
    resources\settings\default.vrsettings
```

Es el mismo driver que ya estaba instalado a mano en
`SteamVR\drivers\psmoveserviceex`, pero ahora viaja dentro del proyecto del
VDM y se copia solo a `bin\x64\Release\driver\psmoveserviceex` en cada
compilación (el `.vbproj` ya copia la carpeta `driver` completa).

El driver es autocontenido: no necesita ninguna DLL extra ni el runtime de
Visual C++.

### 1.2. Botones Install / Uninstall Drivers

En **Virtual Motion Tracker → Management**, los enlaces
*Install Drivers* / *Uninstall Drivers* ahora gestionan **los dos drivers**:

| Driver | Para qué sirve |
| --- | --- |
| `psmsex` | Trackers virtuales (VMT) del VDM. Ya existía. |
| `psmoveserviceex` | DualShock 4 6DOF: pose, sticks, botones, LED y vibración. **Nuevo.** |

Comportamiento del botón de instalación:

1. Comprueba que los dos drivers están completos antes de tocar nada.
2. Si SteamVR ya tiene una copia en `SteamVR\drivers\<nombre>` (como la
   instalación manual que ya funcionaba), **no la duplica**: informa de que ya
   está instalada y no la registra otra vez. Si esa copia es más antigua que la
   del VDM, lo avisa.
3. Si hay otra copia registrada en `openvrpaths.vrpath` desde otra carpeta,
   ofrece borrarla (diálogo *Outdated driver found*).
4. Registra la copia del VDM en `external_drivers` solo cuando hace falta.

El botón de desinstalar quita los registros del VDM e indica si queda alguna
copia dentro de `SteamVR\drivers` (esa hay que borrarla a mano si se quiere
eliminar del todo, porque está en una carpeta protegida de Program Files).

> Importante: SteamVR no puede estar abierto al instalar o desinstalar. El
> propio botón lo comprueba y avisa.

### 1.3. Cambios de código (VB.NET)

| Archivo | Cambio |
| --- | --- |
| `Classes\Service\ClassVmtConst.vb` | Constantes `PSM_DRIVER_NAME`, `PSM_DRIVER_ROOT_PATH` y `PSM_DRIVER_FILE` para el driver del DS4. |
| `Classes\Service\ClassOpenVRConfig.vb` | Nuevo `GetRuntimePath()`, que lee la ruta de SteamVR desde `openvrpaths.vrpath` para detectar copias en `SteamVR\drivers`. |
| `UCVirtualMotionTracker\UCManagement\UCVmtManagement.vb` | Instalación/desinstalación conjunta de `psmsex` + `psmoveserviceex`, con detección de duplicados y de versiones antiguas. |
| `UCVirtualMotionTracker\UCManagement\UCVmtManagement.Designer.vb` | Los enlaces pasan a llamarse *Install Drivers* / *Uninstall Drivers*. |
| `UCVirtualTrackers\UCVirtualTrackerItem.vb` | El hilo del named pipe espera a que el servicio esté conectado y reporte el tracker antes de conectar, y deja de escribir en el log si el tracker está deshabilitado. |

No se ha tocado nada más del VDM: ni el resto de la interfaz, ni los archivos
`.ini`, ni su driver `psmsex`.

---

## 2. Lo que NO se incluye aquí (y por qué)

**El servicio `PSMoveService.exe` no se duplica en esta carpeta.** El VDM no
trae servicio propio: es un gestor que arranca el ejecutable que le indiques.
Duplicar el servicio aquí volvería a crear el problema que ya nos costó
encontrar: un `.exe` viejo en una carpeta y el código nuevo en otra, con el
mando descartando en silencio los paquetes de LED y vibración.

Usa siempre el servicio compilado del proyecto PSMoveServiceEx:

```
C:\Users\Eliias\Documents\Proyecto predeterminado\PSMoveServiceEx\dist\Win64\Release\bin\PSMoveService.exe
```

En el VDM: **Start Page → PSMoveService location** y selecciona ese
`PSMoveService.exe`. El VDM guarda la ruta en su `settings.ini`
(`Settings → PSMoveServiceLocation`).

---

## 3. Cómo usarlo

1. Compila el VDM en Release (ver sección 5).
2. Arranca `PSMSVirtualDeviceManager\bin\x64\Release\PSMSVirtualDeviceManager.exe`.
3. Comprueba que el VDM apunta al `PSMoveService.exe` correcto y arráncalo
   desde el propio VDM. En esta compilación ya viene configurado en
   `bin\x64\Release\settings.ini`.
4. Con SteamVR cerrado, pulsa **Install Drivers** una vez. Debe informar:
   * `psmsex: registered`
   * `psmoveserviceex: already installed in SteamVR\drivers` (en este equipo,
     porque la copia manual sigue estando) o `registered` en un equipo limpio.
5. Enciende el DS4 por Bluetooth y abre SteamVR. El mando debe aparecer como
   controlador con posición y orientación.

### Controles del DS4 en SteamVR

| Entrada | Efecto |
| --- | --- |
| Stick izquierdo | Movimiento / joystick normal. |
| Stick derecho | `/input/thumbstick2` (giro con la vista, snap turn, etc.). |
| R3 (pulsar stick derecho) | `/input/thumbstick2/click`. |
| Options, toque corto (< 250 ms) | Click normal, se emite **al soltar**. |
| Options, pulsación larga | Recentra la orientación. No abre el menú de SteamVR. |
| LED / vibración | Los gestiona el servicio (config tool o el juego). |

La posición pasa por un filtro One Euro por eje con rechazo de saltos de 4 cm
para reducir el temblor en reposo sin añadir retardo apreciable. La orientación
usa un filtro adaptativo que se guía por la velocidad angular del giroscopio
que reporta el servicio: en reposo corta a 0,2 Hz (la barra de luz se promedia
y apenas tiembla) y al girar el mando sube hasta 15 Hz, así que responde al
movimiento en unas decenas de milisegundos.

---

## 4. Avisos importantes

- **Color personalizado vs. color de seguimiento.** Un color fijo en la barra
  anula el seguimiento óptico mientras está activo (el servicio usa el color
  para localizar el mando). Para jugar, deja el color de seguimiento
  (magenta). Los colores son para calibrar y probar.
- **Vibración y color comparten reporte.** Si un juego hace vibrar el mando
  mientras hay un color personalizado puesto, el color vuelve al de
  seguimiento. Es el comportamiento actual de la API del servicio, no del
  driver.
- **SteamVR y drivers duplicados.** No tengas a la vez el driver en
  `SteamVR\drivers\psmoveserviceex` y registrado desde la carpeta del VDM: son
  el mismo driver y SteamVR intentaría cargarlo dos veces. El botón de
  instalación ya evita ese caso, pero tenlo en cuenta si mueves carpetas a
  mano.
- **El driver `psmsex` es de terceros y va compilado.** El VDM no incluye su
  código fuente, así que el soporte DS4 no se puede meter *dentro* de
  `psmsex`. Va como driver independiente, que es justo lo que ya funcionaba.

---

## 5. Compilar

El proyecto original apunta a **.NET Framework 4.6.1**. En este equipo solo
están instalados los ensamblados de referencia de 4.8 (la carpeta `v4.6.1`
está incompleta), así que la compilación de verificación se hizo con:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe" `
    PSMSVirtualDeviceManager.sln /t:Build /p:Configuration=Release /p:Platform=x64 `
    /p:TargetFrameworkVersion=v4.8
```

Si instalas el *Developer Pack* de .NET Framework 4.6.1, puedes compilar sin
el último parámetro y el proyecto queda exactamente como estaba.

Salida: `PSMSVirtualDeviceManager\bin\x64\Release\PSMSVirtualDeviceManager.exe`
(con `driver\psmsex` y `driver\psmoveserviceex` copiados al lado).

---

## 6. Verificación realizada

- El driver del DS4 compila (`driver_psmoveserviceex`) y es el mismo binario
  que ya funcionaba en SteamVR.
- El VDM compila en Release sin errores con los cambios de los archivos
  VB.NET.
- La carpeta `driver\psmoveserviceex` aparece correctamente en
  `bin\x64\Release\driver\` tras compilar.
- Se validó contra el binario compilado que `GetRuntimePath()` lee
  `C:\Program Files (x86)\Steam\steamapps\common\SteamVR` y que
  `GetDrivers()` no falla cuando `external_drivers` es `null`.
- El servicio (`dist\Win64\Release\bin\PSMoveService.exe`) se recompiló y se
  verificó con traza real: la barra pasa a azul, rojo y verde, y la vibración
  responde al 80 %. El fallo anterior era un `.exe` de las 21:23 anterior al
  último cambio del código del DS4.

Pendiente de probar por ti en el VDM: pulsar **Install Drivers** y confirmar
que el mensaje es el esperado, y jugar con el stick derecho y el recentrado
con Options.

---

## 7. Tracker virtual con webcam

El VDM puede usar una webcam normal como tracker virtual: captura la imagen y
la envía a PSMoveServiceEx por un *named pipe*. Para que el config tool lo
liste hay que tener en cuenta tres cosas:

1. **Slots virtuales en el servicio.** El servicio solo crea un tracker
   virtual si `virtual_tracker_count` es mayor que 0. El VDM lo ajusta solo:
   al abrir la página **Virtual Trackers** y al añadir una cámara lo sube a los
   slots que necesitan tus dispositivos (dos por cada cámara de PS4) y, si el
   servicio ya estaba arrancado, te ofrece reiniciarlo. Además, al lanzar el
   servicio le pasa `--min_virtual_tracker`, que cubre el caso de una
   instalación nueva en la que
   `%APPDATA%\PSMoveService\TrackerManagerConfig.json` todavía no existe.
   Nunca lo baja solo, porque los dispositivos con `Autostart=False` no se
   restauran al arrancar. También se puede fijar a mano en el combo o en el
   config tool (**Advanced Settings**).
2. **Tracker id.** En el VDM cada webcam se asigna a un *tracker id*. Los
   trackers virtuales se enumeran antes que las cámaras reales, así que con un
   slot virtual la PS3 Eye pasa a ser el tracker 1. El valor guardado en
   `devices.ini` es el índice del combo: `TrackerId=1` equivale al tracker 0.
3. **Autostart.** Con `Autostart=False` el VDM solo crea la entrada de la
   webcam al abrir la página **Virtual Trackers**; hasta entonces nadie
   alimenta el pipe y el config tool ve el tracker sin imagen. Con
   `Autostart=True` el VDM empieza a enviar fotogramas nada más arrancar.

La resolución del tracker virtual (`PS3EyeTrackerConfig_virtual_0.json`,
`640x480`) tiene que coincidir con la del VDM (**480p**), porque el nombre del
pipe incluye la resolución.

### Cambiar el tracker id

Al cambiar el *tracker id* de una webcam, el VDM ahora:

- comprueba en ese mismo momento cuántos slots virtuales hacen falta, sube
  `virtual_tracker_count` y muestra el aviso de reiniciar el servicio (antes
  solo se recalculaba al abrir el VDM o al añadir una cámara);
- reconecta el hilo del pipe al tracker nuevo en cuanto cambia el id, sin
  cerrar y abrir el VDM (antes seguía enviando fotogramas al pipe antiguo);
- crea el `PS3EyeTrackerConfig_virtual_N.json` del id nuevo con la resolución
  de la webcam, para que el servicio no use un tamaño de fotograma distinto;
- el combo **Virtual tracker count** ya no permite bajar de los slots que usan
  los dispositivos configurados.

Si el tracker no conecta, el aviso rojo dice ahora la causa concreta: servicio
parado, slot inexistente o servicio todavía arrancando.

### Arreglos necesarios en el servicio

- **Pipe creado tras la inicialización.** Antes el servicio creaba el named
  pipe del tracker virtual mientras abría los dispositivos. Si el VDM se
  conectaba a ese pipe en ese momento (por ejemplo al reiniciar el servicio con
  el VDM abierto), el servicio moría con una corrupción de heap
  (`0xc0000374`). Ahora el pipe se crea en el primer fotograma, cuando el
  servicio ya ha terminado de inicializarse.
- **Recurso de versión.** `PSMoveService.exe` no llevaba `VERSIONINFO`, así
  que el VDM leía `ProductVersion` vacío y avisaba siempre de una actualización
  a 0.27.8. Ahora el binario declara `0.27.8.0` (tomado de
  `src/psmoveprotocol/ProtocolVersion.h`) y el aviso desaparece.
- **Test de orientación del DS4.** El config tool dibujaba siempre el modelo
  del PS Move en *Test Orientation*, aunque el mando fuese un DualShock 4.
  Ahora usa el mismo `drawController` que el resto de pantallas y muestra el
  modelo DS4.

Comprobación rápida con el servicio arrancado y el VDM enviando vídeo:

```powershell
$dll = "...\PSMSVirtualDeviceManager\bin\x64\Release\PSMoveServiceExCAPI.dll"
$asm = [Reflection.Assembly]::LoadFrom($dll)
$svc = [Activator]::CreateInstance(
    $asm.GetType("PSMoveServiceExCAPI.PSMoveServiceExCAPI+Service"))
$svc.Connect(3000)
$asm.GetType("PSMoveServiceExCAPI.PSMoveServiceExCAPI+Trackers").
    GetMethod("GetTrackerList").Invoke($null, @()) |
    ForEach-Object { "{0} -> {1}" -f $_.m_Info.m_TrackerId, $_.m_Info.m_DevicePath }
$svc.Dispose()
```

Salida esperada: `0 -> VirtualTracker_0` y
`1 -> USB\VID_1415&PID_2000\b1_p9`.
