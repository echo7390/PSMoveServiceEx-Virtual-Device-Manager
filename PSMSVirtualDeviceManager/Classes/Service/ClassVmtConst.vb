Public Class ClassVmtConst
    Public Const VMT_TRACKER_MAX As Integer = 16 'Max trackers we can have, reserve 10 for tracking references
    Public Const VMT_DRIVER_VERSION_EXPECT As String = "VMTX_019"

    Public Const VMT_IP As String = "127.0.0.1"
    Public Const VMT_PORT_RECEIVE As Integer = 34571
    Public Const VMT_PORT_SEND As Integer = 34570

    Public Const VMT_DEVICE_NAME As String = "PSMS_EX_"
    Public Const VMT_DEVICE_SERIAL As String = "/devices/psmsex/" & VMT_DEVICE_NAME
    Public Const VMT_DRIVER_NAME As String = "psmsex"
    Public Const VMT_DRIVER_ROOT_PATH As String = "driver\psmsex"
    Public Const VMT_DRIVER_FILE As String = "driver_psmsex.dll"

    ' DualShock 4 6DOF driver bundled with the manager. It is a separate SteamVR
    ' driver because it talks to PSMoveServiceEx directly (controller pose,
    ' thumbsticks, recenter button and light bar/rumble), while psmsex only
    ' publishes virtual motion trackers.
    Public Const PSM_DRIVER_NAME As String = "psmoveserviceex"
    Public Const PSM_DRIVER_ROOT_PATH As String = "driver\psmoveserviceex"
    Public Const PSM_DRIVER_FILE As String = "driver_psmoveserviceex.dll"
End Class
