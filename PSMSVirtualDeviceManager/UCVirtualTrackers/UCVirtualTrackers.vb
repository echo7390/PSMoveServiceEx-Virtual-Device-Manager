Public Class UCVirtualTrackers
    Public g_mFormMain As FormMain

    Private g_bIgnoreEvents As Boolean = False
    Private g_bInit As Boolean = False

    Public Sub New(_mFormMain As FormMain)
        g_mFormMain = _mFormMain

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.  

        UcInformation_WebcamPrivacy.m_ReadMoreAction = AddressOf OpenWindowsPrivacySettings

        Try
            g_bIgnoreEvents = True

            ComboBox_VirtualTrackerCount.Items.Clear()
            For i = 0 To ClassSerivceConst.PSMOVESERVICE_MAX_TRACKER_COUNT
                ComboBox_VirtualTrackerCount.Items.Add(CStr(i))
            Next
        Finally
            g_bIgnoreEvents = False
        End Try

        CreateControl()
    End Sub

    Public Sub Init()
        If (g_bInit) Then
            Return
        End If

        g_bInit = True

        Try
            ' Load all devices that autostart enabled.
            Dim mDeviceList As New List(Of ClassVideoInputDevices.ClassDeviceInfo)
            If (ClassVideoInputDevices.GetDevicesOfVideoInput(mDeviceList)) Then
                For i = 0 To mDeviceList.Count - 1
                    Dim bAutostart As Boolean = UCVirtualTrackerItem.ClassCaptureLogic.ClassConfig.CanDeviceAutostart(mDeviceList(i).m_Path)
                    If (Not bAutostart) Then
                        Continue For
                    End If

                    AddNewDevice(mDeviceList(i))
                Next
            End If
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try

        Try
            g_bIgnoreEvents = True

            LoadSettings()
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        Finally
            g_bIgnoreEvents = False
        End Try

        Try
            ' Give the service a slot for every configured video input device.
            If (EnsureVirtualTrackerSlotCount()) Then
                PromptRestartServiceForNewSlots()
            End If
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try
    End Sub

    ' Called when the user changes the tracker id of a video input device.
    ' The tracker id is the index of the virtual tracker inside PSMoveServiceEx,
    ' so a new id can need more virtual tracker slots than the service currently
    ' creates. Raise the stored count and ask for a service restart if it changed.
    ' Returns True when the stored virtual tracker count was raised, in which case
    ' a restart prompt has already been shown to the user.
    Public Function NotifyVirtualTrackerIdChanged() As Boolean
        Try
            If (EnsureVirtualTrackerSlotCount()) Then
                PromptRestartServiceForNewSlots()
                Return True
            End If
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLog(ex)
        End Try

        Return False
    End Function

    Private Sub PromptRestartServiceForNewSlots()
        ' During startup the service is usually not running yet (the user starts it
        ' from the VDM), so it will pick the new slot count up on its own. Only ask
        ' for a restart when the manager allows prompts (it is enabled once loading
        ' finished); the overlay hides itself again when the process is not running.
        If (g_mFormMain Is Nothing OrElse g_mFormMain.IsDisposed) Then
            Return
        End If

        g_mFormMain.PromptRestartPSMoveService()
    End Sub

    Private Sub OpenWindowsPrivacySettings()
        Try
            Process.Start("ms-settings:privacy-webcam")
        Catch ex As Exception
        End Try
    End Sub

    Class ClassVideoInputDevicesListViewItem
        Inherits ListViewItem
        Implements IDisposable

        Private g_UCVirtualTrackerItem As UCVirtualTrackerItem
        Private g_UCVirtualTrackers As UCVirtualTrackers
        Private g_mClassDeviceInfo As ClassVideoInputDevices.ClassDeviceInfo

        Public Sub New(_UCVirtualMotionTracker As UCVirtualTrackers, _DeviceInfo As ClassVideoInputDevices.ClassDeviceInfo)
            MyBase.New(New String() {"", "", "", ""})

            Dim sName As String = _DeviceInfo.m_Name
            For Each mDevice In ClassLibusbDriver.DRV_PS4CAM_KNOWN_CONFIGS
                Dim sHardwareId As String = String.Format("\USB#VID_{0}&PID_{1}", mDevice.VID, mDevice.PID)

                If (_DeviceInfo.m_Path.ToLowerInvariant.Contains(sHardwareId.ToLowerInvariant)) Then
                    sName = mDevice.sName
                    Exit For
                End If
            Next

            g_UCVirtualTrackers = _UCVirtualMotionTracker
            g_mClassDeviceInfo = New ClassVideoInputDevices.ClassDeviceInfo(
                _DeviceInfo.m_Index,
                sName,
                _DeviceInfo.m_Path,
                _DeviceInfo.m_CLSID)
            g_UCVirtualTrackerItem = New UCVirtualTrackerItem(g_UCVirtualTrackers, g_mClassDeviceInfo)
            g_UCVirtualTrackerItem.Init()

            UpdateItem(New ClassVideoInputDevices.ClassDeviceInfo(
                _DeviceInfo.m_Index,
                sName,
                _DeviceInfo.m_Path,
                _DeviceInfo.m_CLSID))
        End Sub

        Public Sub UpdateItem()
            UpdateItem(Nothing)
        End Sub

        Public Sub UpdateItem(mDeviceInfo As ClassVideoInputDevices.ClassDeviceInfo)
            Const LISTVIEW_SUBITEM_ID As Integer = 0
            Const LISTVIEW_SUBITEM_TRACKERID As Integer = 1
            Const LISTVIEW_SUBITEM_NAME As Integer = 2
            Const LISTVIEW_SUBITEM_PATH As Integer = 3

            'Is there any error?
            If (g_UCVirtualTrackerItem Is Nothing OrElse g_UCVirtualTrackerItem.IsDisposed OrElse g_UCVirtualTrackerItem.m_HasStatusError) Then
                Me.BackColor = Color.FromArgb(255, 192, 192)
            Else
                Me.BackColor = Color.FromArgb(255, 255, 255)
            End If

            If (g_UCVirtualTrackerItem Is Nothing OrElse g_UCVirtualTrackerItem.IsDisposed) Then
                Return
            End If

            Dim iIndex As Integer = -1
            Dim sName As String = ""
            Dim sPath As String = ""
            Dim sCLSID As String = ""

            If (mDeviceInfo Is Nothing) Then
                iIndex = g_mClassDeviceInfo.m_Index
                sName = g_mClassDeviceInfo.m_Name
                sPath = g_mClassDeviceInfo.m_Path.ToUpperInvariant
                sCLSID = g_mClassDeviceInfo.m_CLSID
            Else
                iIndex = mDeviceInfo.m_Index
                sName = mDeviceInfo.m_Name
                sPath = mDeviceInfo.m_Path.ToUpperInvariant
                sCLSID = mDeviceInfo.m_CLSID
            End If

            Me.SubItems(LISTVIEW_SUBITEM_ID).Text = CStr(iIndex)

            If (g_UCVirtualTrackerItem.g_mClassCaptureLogic IsNot Nothing AndAlso g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_Initialized) Then
                If (g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_IsPlayStationCamera AndAlso g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_PipePrimaryIndex > -1) Then
                    Me.SubItems(LISTVIEW_SUBITEM_TRACKERID).Text = String.Format("{0} & {1}",
                                                                             g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_PipePrimaryIndex,
                                                                             g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_PipeSecondaryIndex)
                Else
                    Me.SubItems(LISTVIEW_SUBITEM_TRACKERID).Text = CStr(g_UCVirtualTrackerItem.g_mClassCaptureLogic.m_PipePrimaryIndex)
                End If
            Else
                Me.SubItems(LISTVIEW_SUBITEM_TRACKERID).Text = "-1"
            End If

            Me.SubItems(LISTVIEW_SUBITEM_NAME).Text = sName
            Me.SubItems(LISTVIEW_SUBITEM_PATH).Text = sPath
        End Sub

        ReadOnly Property m_UCVirtualMotionTrackerItem As UCVirtualTrackerItem
            Get
                Return g_UCVirtualTrackerItem
            End Get
        End Property

        ReadOnly Property m_ClassDeviceInfo As ClassVideoInputDevices.ClassDeviceInfo
            Get
                Return g_mClassDeviceInfo
            End Get
        End Property

        Property m_Visible As Boolean
            Get
                If (g_UCVirtualTrackerItem Is Nothing OrElse g_UCVirtualTrackerItem.IsDisposed) Then
                    Return False
                End If

                Return (g_UCVirtualTrackerItem.Parent Is g_UCVirtualTrackers.Panel_Devices)
            End Get
            Set(value As Boolean)
                If (value) Then
                    If (g_UCVirtualTrackerItem Is Nothing OrElse g_UCVirtualTrackerItem.IsDisposed) Then
                        Return
                    End If

                    g_UCVirtualTrackerItem.Parent = g_UCVirtualTrackers.Panel_Devices
                    g_UCVirtualTrackerItem.Dock = DockStyle.Top
                    g_UCVirtualTrackerItem.Visible = True
                Else
                    g_UCVirtualTrackerItem.Parent = Nothing
                    g_UCVirtualTrackerItem.Visible = False
                End If
            End Set
        End Property

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    If (g_UCVirtualTrackerItem IsNot Nothing AndAlso Not g_UCVirtualTrackerItem.IsDisposed) Then
                        g_UCVirtualTrackerItem.Dispose()
                        g_UCVirtualTrackerItem = Nothing
                    End If
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

    ' The service only creates as many virtual tracker slots as
    ' 'virtual_tracker_count' says. When that number is lower than the amount of
    ' video input devices the user has configured, the extra devices have no slot
    ' (and no pipe) to connect to, which surfaces as "Virtual tracker not connected
    ' to PSMoveServiceEx" even though everything else is fine.
    '
    ' A fresh installation starts with 0 slots, so without this the first added
    ' camera never works until the user finds the slot count dropdown.
    '
    ' The count is only ever raised: lowering it could break setups whose devices
    ' are not restored at startup (Autostart disabled).
    '
    ' Returns the amount of virtual tracker slots the configured video input
    ' devices need. The service places the virtual slots after the physical
    ' trackers, so what matters is that every tracker id a device streams to
    ' exists: 'tracker id + slots in that device'.
    Public Function GetRequiredVirtualTrackerSlotCount() As Integer
        Try
            Dim iRequiredCount As Integer = 0
            Dim iDeviceSlotSum As Integer = 0
            Dim bAnyIndexResolved As Boolean = False

            For Each mItem As ListViewItem In ListView_VideoDevices.Items
                Dim mTrackerItem = TryCast(mItem, ClassVideoInputDevicesListViewItem)
                If (mTrackerItem Is Nothing) Then
                    Continue For
                End If

                Dim bIsPlayStationCamera As Boolean = IsPlayStation4Camera(mTrackerItem.m_ClassDeviceInfo)
                Dim iSlots As Integer = If(bIsPlayStationCamera, 2, 1)
                iDeviceSlotSum += iSlots

                Dim mCaptureLogic As UCVirtualTrackerItem.ClassCaptureLogic = Nothing
                If (mTrackerItem.m_UCVirtualMotionTrackerItem IsNot Nothing) Then
                    mCaptureLogic = mTrackerItem.m_UCVirtualMotionTrackerItem.g_mClassCaptureLogic
                End If

                If (mCaptureLogic IsNot Nothing AndAlso mCaptureLogic.m_PipePrimaryIndex >= 0) Then
                    bAnyIndexResolved = True
                    iRequiredCount = Math.Max(iRequiredCount, mCaptureLogic.m_PipePrimaryIndex + iSlots)
                End If
            Next

            ' Trackers whose id is not resolved yet are assumed to be packed from 0.
            If (Not bAnyIndexResolved) Then
                iRequiredCount = iDeviceSlotSum
            End If

            Return Math.Max(0, Math.Min(ClassSerivceConst.PSMOVESERVICE_MAX_TRACKER_COUNT, iRequiredCount))
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLog(ex)
            Return 0
        End Try
    End Function

    ' Returns True when the configuration was changed, in which case the service
    ' has to be restarted before the new slots exist.
    Public Function EnsureVirtualTrackerSlotCount() As Boolean
        Try
            Dim iRequiredCount As Integer = GetRequiredVirtualTrackerSlotCount()
            If (iRequiredCount <= 0) Then
                Return False
            End If

            ' Never create the service config from scratch: TrackerManagerConfig
            ' is only accepted by the service when it carries the expected
            ' 'version', so a file containing just this one key would be thrown
            ' away together with every other setting. When the service has not
            ' written its config yet, the slot count is passed on the command
            ' line instead (see UCStartPage.RunService).
            Dim sConfigPath As String = GetConfig()
            If (String.IsNullOrEmpty(sConfigPath) OrElse Not IO.File.Exists(sConfigPath)) Then
                Return False
            End If

            Dim mConfig As New ClassServiceConfig(sConfigPath)
            If (Not mConfig.LoadConfig()) Then
                Return False
            End If

            If (Not mConfig.ContainsKey("", "version")) Then
                Return False
            End If

            Dim iCurrentCount As Integer = mConfig.GetValue(Of Integer)("", "virtual_tracker_count", 0)
            If (iCurrentCount >= iRequiredCount) Then
                Return False
            End If

            mConfig.SetValue("", "virtual_tracker_count", iRequiredCount)
            mConfig.SaveConfig()

            ' Keep the dropdown in sync without writing the config back
            Try
                g_bIgnoreEvents = True
                ClassMathUtils.SetComboBoxSelectedIndexClamp(ComboBox_VirtualTrackerCount, iRequiredCount)
            Finally
                g_bIgnoreEvents = False
            End Try

            Return True
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLog(ex)
            Return False
        End Try
    End Function

    Private Shared Function IsPlayStation4Camera(mDeviceInfo As ClassVideoInputDevices.ClassDeviceInfo) As Boolean
        If (mDeviceInfo Is Nothing OrElse String.IsNullOrEmpty(mDeviceInfo.m_Path)) Then
            Return False
        End If

        Dim sPath As String = mDeviceInfo.m_Path.ToLowerInvariant()

        For Each mKnownDevice In ClassLibusbDriver.DRV_PS4CAM_KNOWN_CONFIGS
            Dim sHardwareId As String = String.Format("\USB#VID_{0}&PID_{1}", mKnownDevice.VID, mKnownDevice.PID).ToLowerInvariant()
            If (sPath.Contains(sHardwareId)) Then
                Return True
            End If
        Next

        Return False
    End Function

    Private Sub ComboBox_VirtualTrackerCount_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox_VirtualTrackerCount.SelectedIndexChanged
        If (g_bIgnoreEvents) Then
            Return
        End If

        Try
            ' Never allow less slots than the configured video input devices use,
            ' otherwise their tracker id would point to a tracker that does not
            ' exist in PSMoveServiceEx and the device would stay disconnected.
            Dim iRequiredCount As Integer = GetRequiredVirtualTrackerSlotCount()
            If (ComboBox_VirtualTrackerCount.SelectedIndex < iRequiredCount) Then
                g_bIgnoreEvents = True
                Try
                    ClassMathUtils.SetComboBoxSelectedIndexClamp(ComboBox_VirtualTrackerCount, iRequiredCount)
                Finally
                    g_bIgnoreEvents = False
                End Try

                MessageBox.Show(
                    String.Format("The configured video input devices use virtual tracker id up to {0}, so at least {1} virtual trackers are required.", (iRequiredCount - 1), iRequiredCount),
                    "Virtual tracker count too low",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
            End If

            Dim mConfig As New ClassServiceConfig(GetConfig())
            mConfig.LoadConfig()
            mConfig.SetValue("", "virtual_tracker_count", ComboBox_VirtualTrackerCount.SelectedIndex)
            mConfig.SaveConfig()

            g_mFormMain.PromptRestartPSMoveService()
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try
    End Sub

    Private Function GetConfig() As String
        Dim sConfigPath As String = ClassServiceConfig.GetConfigPath()
        If (sConfigPath Is Nothing) Then
            Return Nothing
        End If

        Return IO.Path.Combine(sConfigPath, "TrackerManagerConfig.json")
    End Function

    Private Sub LoadSettings()
        Try
            g_bIgnoreEvents = True

            Dim mConfig As New ClassServiceConfig(GetConfig())
            mConfig.LoadConfig()

            ClassMathUtils.SetComboBoxSelectedIndexClamp(ComboBox_VirtualTrackerCount, mConfig.GetValue(Of Integer)("", "virtual_tracker_count", 0))
        Finally
            g_bIgnoreEvents = False
        End Try
    End Sub

    Private Sub Button_DeviceAdd_Click(sender As Object, e As EventArgs) Handles Button_DeviceAdd.Click
        Try
            Using mForm As New FormVideoInputDeviceSelection()
                If (mForm.ShowDialog(Me) <> DialogResult.OK) Then
                    Return
                End If

                If (Not mForm.m_DialogResult.bIsValid) Then
                    Return
                End If

                AddNewDevice(mForm.m_DialogResult.mVideoInputDeviceItem)
            End Using
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try
    End Sub

    Private Sub ToolStripMenuItem_VideoReconnect_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem_VideoReconnect.Click
        Try
            If (ListView_VideoDevices.SelectedItems.Count < 1) Then
                Return
            End If

            Dim mAttachmentItem = DirectCast(ListView_VideoDevices.SelectedItems(0), ClassVideoInputDevicesListViewItem)
            If (mAttachmentItem Is Nothing) Then
                Return
            End If

            If (mAttachmentItem.m_UCVirtualMotionTrackerItem.g_mClassCaptureLogic IsNot Nothing AndAlso
                    Not mAttachmentItem.m_UCVirtualMotionTrackerItem.g_mClassCaptureLogic.m_Initialized) Then
                Throw New ArgumentException("Please wait until video input device is initialized.")
            End If

            mAttachmentItem.m_UCVirtualMotionTrackerItem.Dispose()

            ' Copy and get the new index. Should fail if -1
            Dim mNewDeviceInfo As New ClassVideoInputDevices.ClassDeviceInfo(
                mAttachmentItem.m_ClassDeviceInfo.GetIndexByPath(),
                mAttachmentItem.m_ClassDeviceInfo.m_Name,
                mAttachmentItem.m_ClassDeviceInfo.m_Path,
                mAttachmentItem.m_ClassDeviceInfo.m_CLSID)

            If (mNewDeviceInfo.m_Index < 0) Then
                Throw New ArgumentException("Could not find video input device.")
            End If

            AddNewDevice(mNewDeviceInfo)
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try
    End Sub

    Public Sub AddNewDevice(mDeviceInfo As ClassVideoInputDevices.ClassDeviceInfo)
        ' $TODO Does not work properly.
        'If (Not ClassVideoInputDevices.GetCameraAccessAllowed()) Then
        '    Throw New ArgumentException("Windows privacy settings have restricted access to video input devices! Enable access to video input devices in the Windows privacy settings.")
        'End If

        For Each mUCVirtualTrackerItem In GetAllDevices()
            If (mUCVirtualTrackerItem.m_DevicePath = mDeviceInfo.m_Path) Then
                Throw New ArgumentException("Device already in the list")
            End If
        Next

        ' Remove disposed devices
        For i = ListView_VideoDevices.Items.Count - 1 To 0 Step -1
            Dim mTrackerItem = DirectCast(ListView_VideoDevices.Items(i), ClassVideoInputDevicesListViewItem)
            If (mTrackerItem.m_ClassDeviceInfo.m_Path <> mDeviceInfo.m_Path) Then
                Continue For
            End If

            If (mTrackerItem.m_UCVirtualMotionTrackerItem Is Nothing OrElse mTrackerItem.m_UCVirtualMotionTrackerItem.IsDisposed) Then
                ListView_VideoDevices.Items.RemoveAt(i)
            End If
        Next

        Dim mItem = New ClassVideoInputDevicesListViewItem(Me, mDeviceInfo)
        ListView_VideoDevices.Items.Add(mItem)
        mItem.Selected = True

        ' A slot has to exist in the service before this device can stream to it,
        ' otherwise the tracker reports "not connected to PSMoveServiceEx".
        If (EnsureVirtualTrackerSlotCount()) Then
            PromptRestartServiceForNewSlots()
        End If
    End Sub

    Public Function GetAllDevices() As UCVirtualTrackerItem()
        Dim mTrackerList As New List(Of UCVirtualTrackerItem)

        For Each mItem As ListViewItem In ListView_VideoDevices.Items
            Dim mTrackerItem = DirectCast(mItem, ClassVideoInputDevicesListViewItem)
            If (mTrackerItem.m_UCVirtualMotionTrackerItem Is Nothing OrElse mTrackerItem.m_UCVirtualMotionTrackerItem.IsDisposed) Then
                Continue For
            End If

            mTrackerList.Add(mTrackerItem.m_UCVirtualMotionTrackerItem)
        Next

        Return mTrackerList.ToArray
    End Function

    Private Sub Timer_VideoInputDevices_Tick(sender As Object, e As EventArgs) Handles Timer_VideoInputDevices.Tick
        Timer_VideoInputDevices.Stop()

        Try
            If (Me.Visible) Then
                For Each mItem As ListViewItem In ListView_VideoDevices.Items
                    Dim mTrackerItem = DirectCast(mItem, ClassVideoInputDevicesListViewItem)
                    mTrackerItem.UpdateItem()
                Next
            End If
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLog(ex)
        End Try

        Timer_VideoInputDevices.Start()
    End Sub

    Private Sub ListView_VideoDevices_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView_VideoDevices.SelectedIndexChanged
        For Each mItem As ListViewItem In ListView_VideoDevices.Items
            Dim mTrackerItem = DirectCast(mItem, ClassVideoInputDevicesListViewItem)
            If (mTrackerItem.Selected) Then
                If (Not mTrackerItem.m_Visible) Then
                    mTrackerItem.m_Visible = True
                End If
            Else
                If (mTrackerItem.m_Visible) Then
                    mTrackerItem.m_Visible = False
                End If
            End If
        Next
    End Sub

    Private Sub ToolStripMenuItem_VideoRemove_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem_VideoRemove.Click
        Try
            If (ListView_VideoDevices.SelectedItems.Count < 1) Then
                Return
            End If

            Dim mAttachmentItem = DirectCast(ListView_VideoDevices.SelectedItems(0), ClassVideoInputDevicesListViewItem)

            If (mAttachmentItem.m_UCVirtualMotionTrackerItem.g_mClassCaptureLogic IsNot Nothing AndAlso
                    Not mAttachmentItem.m_UCVirtualMotionTrackerItem.g_mClassCaptureLogic.m_Initialized) Then
                Throw New ArgumentException("Please wait until video input device is initialized.")
            End If

            ListView_VideoDevices.Items.Remove(mAttachmentItem)
            mAttachmentItem.Dispose()
        Catch ex As Exception
            ClassAdvancedExceptionLogging.WriteToLogMessageBox(ex)
        End Try
    End Sub

    Private Sub CleanUp()
        For Each mItem As ListViewItem In ListView_VideoDevices.Items
            Dim mTrackerItem = DirectCast(mItem, ClassVideoInputDevicesListViewItem)

            mTrackerItem.Dispose()
        Next
        ListView_VideoDevices.Items.Clear()
    End Sub
End Class
