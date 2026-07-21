/*
 * Copyright 2019-2025 Sony Corporation
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using SRD.Core;
using static SRD.Utils.SRDProjectSettings.BehaviorOptionWhenNoSRDisplay;

namespace SRD.Utils
{
    internal class SRDApplicationWindow
    {
        private struct Monitor
        {
            public string friendlyName;
            public Rect screenRect;
            public Rect workArea;
            public DisplayInfo displayInfo;
            public Display display;
            public int displayIndex;
            public IntPtr hWnd;
        }

        private static List<Monitor> _monitorList = new List<Monitor>();

        public static Dictionary<int, int> DeviceIndexToDisplayIndex = new Dictionary<int, int>();
        public static Dictionary<int, uint> DeviceIndexToSrdID = new Dictionary<int, uint>();
        private static int _numberOfConnectedDevices = 1;
        public static int NumberOfConnectedDevices
        {
            get
            {
                return _numberOfConnectedDevices;
            }
        }

        private static Monitor _SRDMonitor;
        private static Monitor _secondMonitor;
        private static bool _isSecondDisplayActivated = false;
        private static bool _isPreviewWindowInitialized = false;
        private static bool _isPreviewWindowMoving = false;

        private static IntPtr _mainWindowHandle = IntPtr.Zero;
        private static IntPtr _secondWindowHandle = IntPtr.Zero;

        public static IntPtr SRDWindow
        {
            get
            {
                var value = _mainWindowHandle;
                if (_SRDMonitor.displayIndex != 0 && IsPreviewWindowInitialized)
                {
                    value = _secondWindowHandle;
                }
                return value;
            }
        }
        public static IntPtr PreviewWindow
        {
            get
            {
                var value = _secondWindowHandle;
                if (_secondMonitor.displayIndex == 0)
                {
                    value = _mainWindowHandle;
                }
                return value;
            }
        }

        public static int SRDTargetDisplay
        {
            get
            {
                return _SRDMonitor.displayIndex;
            }
        }

        public static int PreviewWindowTargetDisplay
        {
            get
            {
                return _secondMonitor.displayIndex;
            }
        }

        public static bool IsPreviewWindowVisible
        {
            get
            {
                if (!IsPreviewWindowInitialized)
                {
                    return false;
                }
                return GetWindowStyle(PreviewWindow).HasFlag(User32.WindowStyle.WS_VISIBLE) && User32.GetWindowLongPtr(PreviewWindow, -8) == IntPtr.Zero;
            }
        }

        public static bool IsPreviewWindowInitialized
        {
            get
            {
                return _isPreviewWindowInitialized;
            }
        }

        public static Rect SRDScreenRect
        {
            get
            {
#if UNITY_EDITOR
                return new Rect(0, 0, Screen.width, Screen.height);
#else
                return GetWindowRect(SRDWindow);
#endif
            }
        }

        private static User32.WINDOWPLACEMENT _previewWindowPlacement = new User32.WINDOWPLACEMENT();
        public static Rect PreviewWindowRestoredRect
        {
            get
            {
                if (IsPreviewWindowVisible && !IsFullscreen(PreviewWindow))
                {
                    _previewWindowPlacement = GetWindowPlacement(PreviewWindow);
                }
                var rect = LPRECTToUnityRect(_previewWindowPlacement.rcNormalPosition);
                rect.position -= GetMonitor(PreviewWindow).workArea.position;
                return rect;
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void CheckDisplayConfig()
        {
            _mainWindowHandle = GetSelfWindowHandle();
            GetMonitors();
            MatchUnityDisplaysToMonitors();

            if (SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                _SRDMonitor = _monitorList.Find(m => m.displayIndex == 0);
            }
            else
            {
                var result = SRDCorePlugin.ShowNativeLog();
                if (result == SrdXrResult.SUCCESS)
                {
                    _SRDMonitor = EnumerateSRDisplays();
                }
                else
                {
                    var errorToMessage = new Dictionary<SrdXrResult, string>()
                    {
                        { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                        { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
                    };
                    var msg = errorToMessage.ContainsKey(result) ? errorToMessage[result] : SRDHelper.SRDMessages.UnknownError;

                    switch(SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay())
                    {
                    case RunWithDisabled:
                        Debug.Log(msg);
                        SRDSessionHandler.Instance.Active = false;
                        break;
                    case ExitWithError:
                    default:
                        SRDHelper.PopupMessageAndForceToTerminate(msg);
                        return;
                        break;
                    }
                }
            }

            if (_monitorList.Count >= 2)
            {
                _secondMonitor = _monitorList.Find(m => m.displayIndex != _SRDMonitor.displayIndex);
            }
        }
#endif

        private static class MonitorInfos
        {
            private static List<User32.MONITORINFOEX> _monitorInfos = null;

            [AOT.MonoPInvokeCallback(typeof(User32.MonitorEnumProc))]
            private static bool MonitorEnumProcCallback(IntPtr hMonitor, IntPtr hdc, ref User32.LPRECT prect, IntPtr data)
            {
                User32.MONITORINFOEX monitorInfo = new User32.MONITORINFOEX
                {
                    cbSize = (uint)Marshal.SizeOf<User32.MONITORINFOEX>()
                };

                if (User32.GetMonitorInfo(hMonitor, ref monitorInfo))
                {
                    _monitorInfos.Add(monitorInfo);
                }

                return true;
            }

            public static List<User32.MONITORINFOEX> Create()
            {
                List<User32.MONITORINFOEX> monitorInfos = new List<User32.MONITORINFOEX>();

                _monitorInfos = monitorInfos;
                User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProcCallback, IntPtr.Zero);
                _monitorInfos = null;

                return monitorInfos;
            }
        }

        private static void MatchUnityDisplaysToMonitors()
        {
            List<Monitor> sortedMonitors = new List<Monitor>();

            SetWindowPos(_mainWindowHandle, Vector2.zero, new Vector2(Display.main.systemWidth, Display.main.systemHeight));

            for (int i = 0; i < Display.displays.Length; i++)
            {
                Display d = Display.displays[i];
                d.Activate();

                var hWnd = i == 0 ? _mainWindowHandle : GetWindowHandleByTitle("Unity Secondary Display");

                Monitor m = GetMonitor(hWnd);
                m.display = d;
                m.displayIndex = i;
                m.hWnd = hWnd;

                sortedMonitors.Add(m);

                if (i > 0)
                {
                    User32.SetWindowText(hWnd, Application.productName);
                    User32.ShowWindow(hWnd, (int)User32.ShowCommand.SW_HIDE);
                }
            }

            _monitorList = sortedMonitors;

            Debug.Log($"{_monitorList.Count} monitor(s) detected");
            foreach (var m in _monitorList)
            {
                Debug.Log($"[{m.displayIndex}] {m.friendlyName} {m.screenRect}");
            }
        }

        private static void GetMonitors()
        {
            _monitorList.Clear();

            List<DisplayInfo> displayInfos = new List<DisplayInfo>();
            Screen.GetDisplayLayout(displayInfos);

            var monitorInfos = MonitorInfos.Create();

            uint pathCount, modeCount;
            var error = User32.GetDisplayConfigBufferSizes(User32.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
            if (error != 0)
            {
                Debug.LogError($"GetDisplayConfigBufferSizes({User32.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS}) failed with error code {error}");
                return;
            }

            var displayPaths = new User32.DISPLAYCONFIG_PATH_INFO[pathCount];
            var displayModes = new User32.DISPLAYCONFIG_MODE_INFO[modeCount];

            error = User32.QueryDisplayConfig(User32.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                ref pathCount, displayPaths, ref modeCount, displayModes, IntPtr.Zero);
            if (error != 0)
            {
                Debug.LogError($"QueryDisplayConfig({User32.QUERY_DEVICE_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS}) failed with error code {error}");
                return;
            }

            foreach (var path in displayPaths)
            {
                User32.DISPLAYCONFIG_SOURCE_DEVICE_NAME sourceName = new User32.DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header =
                        {
                            size = (uint)Marshal.SizeOf<User32.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                            adapterId = path.sourceInfo.adapterId,
                            id = path.sourceInfo.id,
                            type = User32.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME
                        }
                };

                error = User32.DisplayConfigGetDeviceInfo(ref sourceName);
                if (error != 0)
                {
                    Debug.LogError($"DisplayConfigGetDeviceInfo() for Source[{sourceName.header.adapterId.HighPart} / {sourceName.header.adapterId.LowPart}:{sourceName.header.id}] failed !");
                    continue;
                }

                User32.DISPLAYCONFIG_TARGET_DEVICE_NAME targetName = new User32.DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header =
                        {
                            size = (uint)Marshal.SizeOf<User32.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                            adapterId = path.targetInfo.adapterId,
                            id = path.targetInfo.id,
                            type = User32.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME
                        }
                };

                error = User32.DisplayConfigGetDeviceInfo(ref targetName);
                if (error != 0)
                {
                    Debug.LogError($"DisplayConfigGetDeviceInfo() for Target[{targetName.header.adapterId.HighPart} / {targetName.header.adapterId.LowPart}:{targetName.header.id}] failed !");
                    continue;
                }

                var monitorInfo = monitorInfos.Find(m => m.szDevice.Equals(sourceName.viewGdiDeviceName));

                var monitorScreenRect = LPRECTToUnityRect(monitorInfo.rcMonitor);
                SetWindowPos(_mainWindowHandle, monitorScreenRect.position, monitorScreenRect.size);
                
                Monitor monitor = new Monitor
                {
                    friendlyName = targetName.monitorFriendlyDeviceName,
                    screenRect = monitorScreenRect,
                    workArea = LPRECTToUnityRect(monitorInfo.rcWork),
                    displayInfo = Screen.mainWindowDisplayInfo,
                };

                _monitorList.Add(monitor);
            }
        }

        private static Monitor EnumerateSRDisplays()
        {
            DeviceIndexToDisplayIndex.Clear();
            DeviceIndexToSrdID.Clear();

            var appMultiDisplayMode = SRDProjectSettings.GetMutlipleDisplayMode();
            var appDeviceNum = SRDProjectSettings.GetNumberOfDevices();

            string warningMessage = string.Empty;

            SrdXrResult result = SRDCorePlugin.EnumerateMultiDisplayDevices(out var deviceList, out var runtimeMultiDisplayMode);

            if ((appMultiDisplayMode == SRDProjectSettings.MultiSRDMode.SingleDisplay) &&
                (result == SrdXrResult.ERROR_FUNCTION_UNSUPPORTED || result == SrdXrResult.ERROR_DEVICE_NOT_FOUND))
            {
                return SelectSRDisplay();
            }
            else if (result != SrdXrResult.SUCCESS)
            {
                var errorToMessage = new Dictionary<SrdXrResult, string>()
                {
                    { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                    { SrdXrResult.ERROR_DEVICE_NOT_FOUND, SRDHelper.SRDMessages.DisplayConnectionError},
                    { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
                    { SrdXrResult.ERROR_FUNCTION_UNSUPPORTED, SRDHelper.SRDMessages.FunctionUnsupportedError(requiredVersion: "2.4.0")},
                };
                var msg = errorToMessage.ContainsKey(result) ? errorToMessage[result] : SRDHelper.SRDMessages.UnknownError;

                switch(SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay())
                {
                case RunWithDisabled:
                    Debug.Log(msg);
                    SRDSessionHandler.Instance.Active = false;
                    break;
                case ExitWithError:
                default:
                    SRDHelper.PopupMessageAndForceToTerminate(msg);
                    break;
                }
                return _monitorList.Find(m => m.displayIndex == 0);
            }

            if (appMultiDisplayMode == SRDProjectSettings.MultiSRDMode.SingleDisplay)
            {
                switch (runtimeMultiDisplayMode)
                {
                    case SrdXrMultiDisplayMode.Single:
                    {
                        _numberOfConnectedDevices = 1;
                        break;
                    }
                    case SrdXrMultiDisplayMode.Clone:
                    {
                        _numberOfConnectedDevices = deviceList.Count;
                        break;
                    }
                    default:
                    {
                        _numberOfConnectedDevices = 1;
                        warningMessage += $"・The Multi Display Mode set in the project settings ({appMultiDisplayMode.InspectorName()}) does not match that of the Spatial Reality Display Settings ({runtimeMultiDisplayMode.InspectorName()}).\n";
                        break;
                    }
                }
            }
            else
            {
                if ((int)appMultiDisplayMode != (int)runtimeMultiDisplayMode)
                {
                    warningMessage += $"・The Multi Display Mode set in the project settings ({appMultiDisplayMode.InspectorName()}) does not match that of the Spatial Reality Display Settings ({runtimeMultiDisplayMode.InspectorName()}).\n";
                }
                if (deviceList.Count < appDeviceNum)
                {
                    warningMessage += $"・The number of Spatial Reality Displays detected ({deviceList.Count}) is less than the number set in the project settings ({appDeviceNum}).\n";
                }
                _numberOfConnectedDevices = Math.Min(deviceList.Count, appDeviceNum);
            }

            if (warningMessage != String.Empty)
            {
                SRDHelper.PopupWarningMessage($"This application may not run properly:\n{warningMessage}\n");
            }

            var numberOfDevices = Math.Min(deviceList.Count, appDeviceNum);

            var sortedDevices = new List<SrdXrDeviceInfo>(deviceList);
            switch (appMultiDisplayMode)
            {
                case SRDProjectSettings.MultiSRDMode.SingleDisplay:
                {
                    if (_numberOfConnectedDevices <= 1)
                    {
                        return SelectSRDisplay();
                    }
                    break;
                }
                case SRDProjectSettings.MultiSRDMode.MultiHorizontal:
                {
                    sortedDevices.Sort((d1, d2) =>
                    {
                        return d1.target_monitor_rectangle.left.CompareTo(d2.target_monitor_rectangle.left);
                    });
                    if (_numberOfConnectedDevices >= 3)
                    {
                        var devTmp = sortedDevices[0];
                        sortedDevices.RemoveAt(0);
                        sortedDevices.Insert(2, devTmp);
                    }
                    break;
                }
                case SRDProjectSettings.MultiSRDMode.MultiVertical:
                {
                    sortedDevices.Sort((d1, d2) =>
                    {
                        return d1.target_monitor_rectangle.top.CompareTo(d2.target_monitor_rectangle.top);
                    });
                    if (_numberOfConnectedDevices >= 3)
                    {
                        var devTmp = sortedDevices[0];
                        sortedDevices.RemoveAt(0);
                        sortedDevices.Insert(2, devTmp);
                    }
                    break;
                }
                case SRDProjectSettings.MultiSRDMode.MultiGrid:
                {
                    sortedDevices.Sort((d1, d2) =>
                    {
                        return (d1.target_monitor_rectangle.top + d1.target_monitor_rectangle.left / 2).CompareTo(d2.target_monitor_rectangle.top + d2.target_monitor_rectangle.left / 2);
                    });
                    break;
                }
            }

            Debug.Log($"{sortedDevices.Count} SRD device(s) detected");
            for (int i = 0; i < sortedDevices.Count; i++)
            {
                SrdXrDeviceInfo dev = sortedDevices[i];
                var monitor = _monitorList.Find(m => m.screenRect == SrdXrRectToUnityRect(dev.target_monitor_rectangle));
                Debug.Log($"[{dev.device_index}] {dev.product_id} {dev.device_serial_number} {SrdXrRectToUnityRect(dev.target_monitor_rectangle)}\n" +
                    $"\t=> {monitor.friendlyName} {monitor.screenRect} DisplayIndex = {monitor.displayIndex}");

                DeviceIndexToDisplayIndex.Add(i, monitor.displayIndex);
                DeviceIndexToSrdID.Add(i, (uint)deviceList.FindIndex(deviceInfo => deviceInfo.target_monitor_rectangle.Equals(dev.target_monitor_rectangle)));
            }

            Monitor srdMonitor;
            if (!DeviceIndexToDisplayIndex.ContainsValue(0))
            {
                srdMonitor = FitSRDDisplay(sortedDevices[0].target_monitor_rectangle);
                // Make sure the main SRDisplayManager uses Unity's main window as target display
                DeviceIndexToDisplayIndex[0] = 0;
            }
            else
            {
                srdMonitor = _monitorList.Find(m => m.screenRect.Equals(SrdXrRectToUnityRect(sortedDevices[0].target_monitor_rectangle)));
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.SetResolution((int)srdMonitor.screenRect.width, (int)srdMonitor.screenRect.height, true);
            }
            return srdMonitor;
        }

        private static Monitor SelectSRDisplay()
        {
            SrdXrResult result = SRDCorePlugin.SelectDevice(out var device);
            if (result == SrdXrResult.SUCCESS)
            {
                Debug.Log($"Selected device [{device.device_index}] {device.product_id} {device.device_serial_number}");
                return FitSRDDisplay(device.target_monitor_rectangle);
            }

            if (result == SrdXrResult.ERROR_USER_CANCEL)
            {
                Application.Quit();
            }
            else if (result != SrdXrResult.SUCCESS)
            {
                var errorToMessage = new Dictionary<SrdXrResult, string>()
                {
                    { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                    { SrdXrResult.ERROR_DEVICE_NOT_FOUND, SRDHelper.SRDMessages.DisplayConnectionError},
                    { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
                };
                var msg = errorToMessage.ContainsKey(result) ? errorToMessage[result] : SRDHelper.SRDMessages.UnknownError;

                switch(SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay())
                {
                case RunWithDisabled:
                    Debug.Log(msg);
                    SRDSessionHandler.Instance.Active = false;
                    break;
                case ExitWithError:
                default:
                    SRDHelper.PopupMessageAndForceToTerminate(msg);
                    break;
                }
            }
            return _monitorList.Find(m => m.displayIndex == 0);
        }

        private static Monitor FitSRDDisplay(SrdXrRect target)
        {
            var srdMonitor = _monitorList.Find(m => m.screenRect.Equals(SrdXrRectToUnityRect(target)));

            var position = new Vector2Int(target.left, target.top);
            var resolution = new Vector2Int(target.right - target.left, target.bottom - target.top);
            User32.GetWindowRect(_mainWindowHandle, out var rect);
            if (position.x != rect.left || position.y != rect.top ||
                resolution.x != (rect.right - rect.left) || resolution.y != (rect.bottom - rect.top) ||
                resolution.x != Screen.width || resolution.y != Screen.height)
            {
                User32.SetWindowPos(_mainWindowHandle, 0,
                                    position.x, position.y,
                                    resolution.x, resolution.y, 0x0040);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.SetResolution(resolution.x, resolution.y, true);
            }

            return srdMonitor;
        }

        private static Rect SrdXrRectToUnityRect(SrdXrRect r)
        {
            return new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }

        private static Rect LPRECTToUnityRect(User32.LPRECT lprect)
        {
            return new Rect(lprect.left, lprect.top, lprect.right - lprect.left, lprect.bottom - lprect.top);
        }

        private static class MyToplevelWindows
        {
            private static System.Diagnostics.Process _process = null;
            private static int _threadId = 0;
            private static string _title = null;
            private static StringComparison _stringComparison;
            private static IntPtr _hWnd = IntPtr.Zero;

            [AOT.MonoPInvokeCallback(typeof(User32.WndEnumProc))]
            private static bool WndEnumProcCallback(IntPtr hWnd, IntPtr lParam)
            {
                var threadId = User32.GetWindowThreadProcessId(hWnd, out var processId);
                if (processId != _process.Id) return true;
                if (threadId != _threadId) return true;

                var textLength = User32.GetWindowTextLength(hWnd) + 1;
                if (textLength <= 1) return true;

                var windowTitle = new StringBuilder(textLength);
                User32.GetWindowText(hWnd, windowTitle, windowTitle.Capacity);
                if (!windowTitle.ToString().Contains(_title, _stringComparison)) return true;

                _hWnd = hWnd;
                return false;
            }

            public static IntPtr GetWindowHandleByTitle(string title, StringComparison stringComparison)
            {
                var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                if (thisProcess.MainWindowTitle.Contains(title, stringComparison))
                {
                    return thisProcess.MainWindowHandle;
                }

                _process = thisProcess;
                _threadId = Kernel32.GetCurrentThreadId();
                _title = title;
                _stringComparison = stringComparison;
                _hWnd = IntPtr.Zero;

                User32.EnumWindows(WndEnumProcCallback, IntPtr.Zero);

                _threadId = 0;
                _process = null;
                _title = null;

                var hWnd = _hWnd;
                _hWnd = IntPtr.Zero;
                return hWnd;
            }
        }

        private static IntPtr GetWindowHandleByTitle(string title, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return MyToplevelWindows.GetWindowHandleByTitle(title, stringComparison);
        }

        private static Rect GetWindowRect(IntPtr hWnd)
        {
            User32.GetWindowRect(hWnd, out var rect);
            return LPRECTToUnityRect(rect);
        }

        private static User32.WINDOWPLACEMENT GetWindowPlacement(IntPtr hWnd)
        {
            var windowPlacement = new User32.WINDOWPLACEMENT
            {
                length = (uint)Marshal.SizeOf<User32.WINDOWPLACEMENT>()
            };
            User32.GetWindowPlacement(hWnd, ref windowPlacement);

            return windowPlacement;
        }

        private static Monitor GetMonitor(IntPtr hWnd)
        {
            var windowRect = GetWindowRect(hWnd);
            foreach (Monitor m in _monitorList)
            {
                if (m.screenRect.Contains(windowRect.center))
                {
                    return m;
                }
            }
            return default;
        }

        private static User32.WindowStyle WindowStyle(bool fullScreen)
        {
            return fullScreen ? User32.WindowStyle.WS_OVERLAPPED : User32.WindowStyle.WS_OVERLAPPEDWINDOW;
        }

        private static User32.WindowStyle GetWindowStyle(IntPtr hWnd)
        {
            return (User32.WindowStyle) User32.GetWindowLongPtr(hWnd, -16);
        }

        private static void SetWindowStyle(IntPtr hWnd, User32.WindowStyle style)
        {
            User32.SetWindowLongPtr(hWnd, -16, new IntPtr((long)style));
        }

        private static bool SetWindowPos(IntPtr hWnd, Vector2 pos, Vector2 size)
        {
            User32.SetWindowPos_Flags flags = User32.SetWindowPos_Flags.SWP_FRAMECHANGED;

            var success = User32.SetWindowPos(hWnd, 0, ((int)pos.x), ((int)pos.y), ((int)size.x), ((int)size.y), (int)flags);
            if (!success)
            {
                Debug.LogError($"SetWindowPos({hWnd}, 0, {(int)pos.x}, {(int)pos.y}, {(int)size.x}, {(int)size.y}, {flags}) failed!");
            }

            return success;
        }

        private static bool MoveWindowToDisplay(IntPtr hWnd, DisplayInfo targetDisplay, bool fullScreen, Vector2 pos, Vector2 size)
        {
            var success = false;
            var index = _monitorList.FindIndex(m => m.displayInfo.Equals(targetDisplay));
            if (index != -1)
            {
                var monitor = _monitorList[index];
                User32.SetWindowPos(hWnd, 0, (int)monitor.screenRect.x, (int)monitor.screenRect.y, (int)size.x, (int)size.y, (int)(User32.SetWindowPos_Flags.SWP_NOSIZE | User32.SetWindowPos_Flags.SWP_NOREDRAW | User32.SetWindowPos_Flags.SWP_NOACTIVATE));

                if (fullScreen)
                {
                    pos = Vector2.zero;
                }

                pos += (fullScreen ? monitor.screenRect.position : monitor.workArea.position);

                if (fullScreen)
                {
                    size = monitor.screenRect.size;
                }

                success = SetWindowPos(hWnd, pos, size);
            }

            return success;
        }

        public static IntPtr GetSelfWindowHandle()
        {
            var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
            var hWnd = User32.GetTopWindow(IntPtr.Zero);

            while (hWnd != IntPtr.Zero)
            {
                int processId;
                User32.GetWindowThreadProcessId(hWnd, out processId);
                if (processId == thisProcess.Id)
                {
                    if (GetWindowStyle(hWnd).HasFlag(User32.WindowStyle.WS_VISIBLE))
                    {
                        return hWnd;
                    }
                }
                hWnd = User32.GetWindow(hWnd, 2);
            }
            return IntPtr.Zero;
        }

        internal static Rect GetClientRect(IntPtr hWnd)
        {
            User32.GetClientRect(hWnd, out var rect);
            User32.LPPOINT topLeft = new User32.LPPOINT { x = rect.left, y = rect.top };
            User32.LPPOINT bottomRight = new User32.LPPOINT { x = rect.right, y = rect.bottom };
            User32.ClientToScreen(hWnd, ref topLeft);
            User32.ClientToScreen(hWnd, ref bottomRight);
            rect.left = topLeft.x;
            rect.top = topLeft.y;
            rect.right = bottomRight.x;
            rect.bottom = bottomRight.y;
            return LPRECTToUnityRect(rect);
        }

        public static DisplayInfo GetDisplayInfo(IntPtr hWnd)
        {
            return GetMonitor(hWnd).displayInfo;
        }

        public static bool IsFullscreen(IntPtr hWnd)
        {
            var screenRect = GetMonitor(hWnd).screenRect;
            return GetWindowRect(hWnd).Equals(screenRect) && !GetWindowStyle(hWnd).HasFlag(WindowStyle(false));
        }

        public static bool MovePreviewWindowToDisplay(DisplayInfo targetDisplay)
        {
            var index = _monitorList.FindIndex(m => m.displayInfo.Equals(targetDisplay));
            if (index != -1)
            {
                var monitor = _monitorList[index];
                if (GetMonitor(PreviewWindow).Equals(monitor))
                {
                    return true;
                }
                else
                {
                    var windowRect = GetWindowRect(PreviewWindow);
                    return SetWindowPos(PreviewWindow, windowRect.position + monitor.screenRect.position, windowRect.size);
                }
            }

            return false;
        }

        public static Vector3 GetSRDMousePos(Vector3 screenMousePos)
        {
            Vector3 srdMousePos = screenMousePos;

            if (IsPreviewWindowInitialized && PreviewWindow == _mainWindowHandle)
            {
                var previewClientRect = GetClientRect(PreviewWindow);
                srdMousePos += new Vector3(-_SRDMonitor.screenRect.xMin + previewClientRect.x, _SRDMonitor.screenRect.yMax - previewClientRect.y - _secondMonitor.display.renderingHeight, 0);
            }

            return srdMousePos;
        }

        private static Rect _previewWindowRectCache;
        private static void UpdateSecondDisplayParams()
        {
            if (!IsPreviewWindowInitialized || !IsPreviewWindowVisible)
            {
                return;
            }

            var windowRect = GetWindowRect(PreviewWindow);
            // Update display params to match the actual window size
            if (_previewWindowRectCache != windowRect)
            {
                var clientRect = GetClientRect(PreviewWindow);

                if (PreviewWindow == _mainWindowHandle)
                {
                    var targetPos = _SRDMonitor.screenRect.center - clientRect.center - _SRDMonitor.screenRect.position;
                    _SRDMonitor.display.SetParams((int)_SRDMonitor.screenRect.width, (int)_SRDMonitor.screenRect.height, (int)targetPos.x, (int)targetPos.y);

                    SetWindowPos(SRDWindow, _SRDMonitor.screenRect.position, _SRDMonitor.screenRect.size);
                }
                else
                {
                    var targetPos = clientRect.position - _secondMonitor.screenRect.position;
                    _secondMonitor.display.SetParams((int)clientRect.width, (int)clientRect.height, (int)targetPos.x, (int)targetPos.y);
                    SetWindowPos(PreviewWindow, windowRect.position, windowRect.size);
                }

                _previewWindowRectCache = windowRect;
            }
        }

        public static void SetPreviewWindowFullScreen(bool fullScreen)
        {
            var pos = PreviewWindowRestoredRect.position;
            var size = PreviewWindowRestoredRect.size;

            SetWindowStyle(PreviewWindow, WindowStyle(fullScreen));
            MoveWindowToDisplay(PreviewWindow, GetDisplayInfo(PreviewWindow), fullScreen, pos, size);

            User32.ShowWindow(PreviewWindow, (int)_previewWindowPlacement.showCmd);
        }

        public static void RestorePreviewWindow()
        {
            Debug.Log($"RestorePreviewWindow()");
            User32.SetParent(PreviewWindow, IntPtr.Zero);
            User32.ShowWindow(PreviewWindow, (int)_previewWindowPlacement.showCmd);
            SetWindowPos(PreviewWindow, _previewWindowRectCache.position, _previewWindowRectCache.size);
        }

        public static void HidePreviewWindow()
        {
            Debug.Log($"HidePreviewWindow()");
            if (IsPreviewWindowVisible && !IsFullscreen(PreviewWindow))
            {
                _previewWindowPlacement = GetWindowPlacement(PreviewWindow);
            }
            User32.SetParent(PreviewWindow, SRDWindow);
            User32.ShowWindow(PreviewWindow, (int)User32.ShowCommand.SW_SHOWMINIMIZED);
        }

        [AOT.MonoPInvokeCallback(typeof(User32.WinEventProc))]
        private static void MoveSizeEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, long idObject, long idChild, uint idEventThread, uint dwmsEventTIme)
        {
            if (hWnd == PreviewWindow)
            {
                if (eventType == User32.EVENT_SYSTEM_MOVESIZESTART)
                {
                    _isPreviewWindowMoving = true;
                }
                else if (eventType == User32.EVENT_SYSTEM_MOVESIZEEND)
                {
                    _isPreviewWindowMoving = false;
                    UpdateSecondDisplayParams();
                }
            }
        }

        static Vector2 _previewWindowClientSizeCache;
        [AOT.MonoPInvokeCallback(typeof(User32.WinEventProc))]
        private static void LocationChangeEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, long idObject, long idChild, uint idEventThread, uint dwmsEventTIme)
        {
            if (hWnd == PreviewWindow)
            {
                if (eventType == User32.EVENT_OBJECT_LOCATIONCHANGE)
                {
                    // Keep aspect ratio while resizing
                    if (_isPreviewWindowMoving)
                    {
                        var clientRect = GetClientRect(PreviewWindow);
                        if (_previewWindowClientSizeCache != clientRect.size )
                        {
                            var windowRect = GetWindowRect(PreviewWindow);
                            var targetPos = windowRect.position - _secondMonitor.screenRect.position;
                            _secondMonitor.display.SetParams((int)clientRect.width, (int)clientRect.height, (int)targetPos.x, (int)targetPos.y);
                            SetWindowPos(PreviewWindow, windowRect.position, windowRect.size);

                            _previewWindowClientSizeCache = clientRect.size;
                        }
                    }
                    else
                    {
                        UpdateSecondDisplayParams();
                    }
                }
            }
        }

        public static void InitPreviewWindow()
        {
            if (_isSecondDisplayActivated && !IsPreviewWindowInitialized)
            {
                _isPreviewWindowInitialized = true;

                // Make sure the SRD window stays on the currently active SRD
                SetWindowStyle(SRDWindow, WindowStyle(true));
                SetWindowPos(SRDWindow, _SRDMonitor.screenRect.position, _SRDMonitor.screenRect.size);

                // Move the second window to the target monitor with default size and position
                var pos = _secondMonitor.workArea.position + _secondMonitor.workArea.size / 4;
                var size = _secondMonitor.workArea.size / 2;

                SetWindowStyle(PreviewWindow, WindowStyle(false));
                SetWindowPos(PreviewWindow, pos, size);

                User32.ShowWindow(PreviewWindow, (int)User32.ShowCommand.SW_SHOWNORMAL);
                User32.ShowWindow(SRDWindow, (int)User32.ShowCommand.SW_RESTORE);
            }
        }

        public static bool ActivateSecondDisplay()
        {
            if (Display.displays.Length < 2)
            {
                return false;
            }
            
            if (_isSecondDisplayActivated)
            {
                RestorePreviewWindow();
                return true;
            }

            if (_SRDMonitor.displayIndex != 0)
            {
                _SRDMonitor.display.Activate();
                _secondWindowHandle = _SRDMonitor.hWnd;
            }
            else
            {
                _secondMonitor.display.Activate();
                _secondWindowHandle = _secondMonitor.hWnd;
            }

            User32.SetWinEventHook(User32.EVENT_SYSTEM_MOVESIZESTART, User32.EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero, MoveSizeEventProc, System.Diagnostics.Process.GetCurrentProcess().Id, 0, 0);
            User32.SetWinEventHook(User32.EVENT_OBJECT_LOCATIONCHANGE, User32.EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, LocationChangeEventProc, System.Diagnostics.Process.GetCurrentProcess().Id, 0, 0);

            Screen.SetResolution(_SRDMonitor.displayInfo.width, _SRDMonitor.displayInfo.height, false);

            _isSecondDisplayActivated = true;

            return true;
        }

        public static void DeactivateSecondDisplay()
        {
            if (!IsPreviewWindowVisible)
            {
                User32.SetParent(PreviewWindow, IntPtr.Zero);
            }

            User32.ShowWindow(_secondWindowHandle, (int)User32.ShowCommand.SW_SHOWMINIMIZED);
            User32.ShowWindow(_secondWindowHandle, (int)User32.ShowCommand.SW_HIDE);

            SetWindowStyle(_mainWindowHandle, WindowStyle(true));
            SetWindowPos(_mainWindowHandle, _SRDMonitor.screenRect.position, _SRDMonitor.screenRect.size);
            User32.ShowWindow(_mainWindowHandle, (int)User32.ShowCommand.SW_SHOW);

            _isPreviewWindowInitialized = false;
        }

        public static bool ActivateDisplay(int displayIndex)
        {
            if (displayIndex >= Display.displays.Length)
            {
                return false;
            }
            Display.displays[displayIndex].Activate();

            var monitor = _monitorList.Find(m => m.displayIndex == displayIndex);
            User32.ShowWindow(monitor.hWnd, (int)User32.ShowCommand.SW_SHOW);
            Debug.Log($"Activate display {displayIndex}: monitor is {monitor.friendlyName}, hWnd is {monitor.hWnd}");

            return true;
        }

        private struct User32
        {
            public static uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
            public static uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
            public static uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;

#region Functions
            [DllImport("user32.dll")]
            public static extern IntPtr GetTopWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindow(IntPtr hWnd, uint wCmd);

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            [DllImport("user32.dll")]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndParent);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll")]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern int SetWindowText(IntPtr hWnd, string lpString);

            [DllImport("user32.dll")]
            public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

            [DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hwnd, out LPRECT lpRect);

            [DllImport("user32.dll")]
            public static extern bool GetClientRect(IntPtr hwnd, out LPRECT lpRect);

            [DllImport("user32.dll")]
            public static extern bool ClientToScreen(IntPtr hwnd, ref LPPOINT lpPoint);

            [DllImport("user32.dll")]
            public static extern bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT lpwndpl);

            public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, long idObject, long idChild, uint idEventThread, uint dwmsEventTIme);
            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc pfnWinEventProc, int idProcess, uint idThread, uint dwFlags);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref LPRECT prect, IntPtr lParam);
            [DllImport("user32.dll")]
            public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX monitorInfo);

            [DllImport("user32.dll")]
            public static extern int GetDisplayConfigBufferSizes(QUERY_DEVICE_CONFIG_FLAGS flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

            [DllImport("user32.dll")]
            public static extern int QueryDisplayConfig(QUERY_DEVICE_CONFIG_FLAGS flags, ref uint numPathArrayElements, [Out] DISPLAYCONFIG_PATH_INFO[] PathInfoArray,
                ref uint numModeInfoArrayElements, [Out] DISPLAYCONFIG_MODE_INFO[] ModeInfoArray, IntPtr currentTopologyId);

            [DllImport("user32.dll")]
            public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

            [DllImport("user32.dll")]
            public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public delegate bool WndEnumProc(IntPtr hWnd, IntPtr lParam);
            [DllImport("user32.dll")]
            public static extern bool EnumWindows(WndEnumProc lpfn, IntPtr lParam);
#endregion

#region Enums
            [Flags()]
            public enum WindowStyle : long
            {
                WS_BORDER = 0x00800000L,
                WS_CAPTION = 0x00C00000L,
                WS_CHILD = 0x40000000L,
                WS_CHILDWINDOW = 0x40000000L,
                WS_CLIPCHILDREN = 0x02000000L,
                WS_CLIPSIBLINGS = 0x04000000L,
                WS_DISABLED = 0x08000000L,
                WS_DLGFRAME = 0x00400000L,
                WS_GROUP = 0x00020000L,
                WS_HSCROLL = 0x00100000L,
                WS_ICONIC = 0x20000000L,
                WS_MAXIMIZE = 0x01000000L,
                WS_MAXIMIZEBOX = 0x00010000L,
                WS_MINIMIZE = 0x20000000L,
                WS_MINIMIZEBOX = 0x00020000L,
                WS_OVERLAPPED = 0x00000000L,
                WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX),
                WS_POPUP = 0x80000000L,
                WS_POPUPWINDOW = (WS_POPUP | WS_BORDER | WS_SYSMENU),
                WS_SIZEBOX = 0x00040000L,
                WS_SYSMENU = 0x00080000L,
                WS_TABSTOP = 0x00010000L,
                WS_THICKFRAME = 0x00040000L,
                WS_TILED = 0x00000000L,
                WS_TILEDWINDOW = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX),
                WS_VISIBLE = 0x10000000L,
                WS_VSCROLL = 0x00200000L
            }

            public enum ShowCommand
            {
                SW_HIDE = 0,
                SW_SHOWNORMAL = 1,
                SW_NORMAL = SW_SHOWNORMAL,
                SW_SHOWMINIMIZED = 2,
                SW_SHOWMAXIMIZED = 3,
                SW_MAXIMIZE = SW_SHOWMAXIMIZED,
                SW_SHOWNOACTIVATE = 4,
                SW_SHOW = 5,
                SW_MINIMIZE = 6,
                SW_SHOWMINNOACTIVE = 7,
                SW_SHOWNA = 8,
                SW_RESTORE = 9,
                SW_SHOWDEFAULT = 10,
                SW_FORCEMINIMIZE = 11
            }

            [Flags()]
            public enum SetWindowPos_Flags
            {
                SWP_ASYNCWINDOWPOS = 0x4000,
                SWP_DEFERERASE = 0x2000,
                SWP_DRAWFRAME = 0x0020,
                SWP_FRAMECHANGED = 0x0020,
                SWP_HIDEWINDOW = 0x0080,
                SWP_NOACTIVATE = 0x0010,
                SWP_NOCOPYBITS = 0x0100,
                SWP_NOMOVE = 0x0002,
                SWP_NOOWNERZORDER = 0x0200,
                SWP_NOREDRAW = 0x0008,
                SWP_NOREPOSITION = 0x0200,
                SWP_NOSENDCHANGING = 0x0400,
                SWP_NOSIZE = 0x0001,
                SWP_NOZORDER = 0x0004,
                SWP_SHOWWINDOW = 0x0040
            }

            [Flags()]
            public enum QUERY_DEVICE_CONFIG_FLAGS : uint
            {
                QDC_ALL_PATHS = 0x00000001,
                QDC_ONLY_ACTIVE_PATHS = 0x00000002,
                QDC_DATABASE_CURRENT = 0x00000004
            }

            public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
            {
                DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
                DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
                DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
                DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
                DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
                DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
                DISPLAYCONFIG_DEVICE_INFO_GET_SUPPORT_VIRTUAL_RESOLUTION = 7,
                DISPLAYCONFIG_DEVICE_INFO_SET_SUPPORT_VIRTUAL_RESOLUTION = 8,
                DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9,
                DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10,
                DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11,
                DISPLAYCONFIG_DEVICE_INFO_GET_MONITOR_SPECIALIZATION,
                DISPLAYCONFIG_DEVICE_INFO_SET_MONITOR_SPECIALIZATION,
                DISPLAYCONFIG_DEVICE_INFO_FORCE_UINT32 = 0xFFFFFFFF
            }

            public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
            {
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO = 1,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO = 2,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO = 3,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI = 4,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS = 6,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN = 8,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI = 9,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL = 12,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED = 13,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE = 14,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST = 15,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_WIRED = 16,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INDIRECT_VIRTUAL = 17,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_USB_TUNNEL,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
                DISPLAYCONFIG_OUTPUT_TECHNOLOGY_FORCE_UINT32 = 0xFFFFFFFF
            }

            public enum DISPLAYCONFIG_SCANLINE_ORDERING : uint
            {
                DISPLAYCONFIG_SCANLINE_ORDERING_UNSPECIFIED = 0,
                DISPLAYCONFIG_SCANLINE_ORDERING_PROGRESSIVE = 1,
                DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED = 2,
                DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_UPPERFIELDFIRST = DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED,
                DISPLAYCONFIG_SCANLINE_ORDERING_INTERLACED_LOWERFIELDFIRST = 3,
                DISPLAYCONFIG_SCANLINE_ORDERING_FORCE_UINT32 = 0xFFFFFFFF
            }

            public enum DISPLAYCONFIG_ROTATION : uint
            {
                DISPLAYCONFIG_ROTATION_IDENTITY = 1,
                DISPLAYCONFIG_ROTATION_ROTATE90 = 2,
                DISPLAYCONFIG_ROTATION_ROTATE180 = 3,
                DISPLAYCONFIG_ROTATION_ROTATE270 = 4,
                DISPLAYCONFIG_ROTATION_FORCE_UINT32 = 0xFFFFFFFF
            }

            public enum DISPLAYCONFIG_SCALING : uint
            {
                DISPLAYCONFIG_SCALING_IDENTITY = 1,
                DISPLAYCONFIG_SCALING_CENTERED = 2,
                DISPLAYCONFIG_SCALING_STRETCHED = 3,
                DISPLAYCONFIG_SCALING_ASPECTRATIOCENTEREDMAX = 4,
                DISPLAYCONFIG_SCALING_CUSTOM = 5,
                DISPLAYCONFIG_SCALING_PREFERRED = 128,
                DISPLAYCONFIG_SCALING_FORCE_UINT32 = 0xFFFFFFFF
            }

            public enum DISPLAYCONFIG_PIXELFORMAT : uint
            {
                DISPLAYCONFIG_PIXELFORMAT_8BPP = 1,
                DISPLAYCONFIG_PIXELFORMAT_16BPP = 2,
                DISPLAYCONFIG_PIXELFORMAT_24BPP = 3,
                DISPLAYCONFIG_PIXELFORMAT_32BPP = 4,
                DISPLAYCONFIG_PIXELFORMAT_NONGDI = 5,
                DISPLAYCONFIG_PIXELFORMAT_FORCE_UINT32 = 0xffffffff
            }

            public enum DISPLAYCONFIG_MODE_INFO_TYPE : uint
            {
                DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1,
                DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 2,
                DISPLAYCONFIG_MODE_INFO_TYPE_FORCE_UINT32 = 0xFFFFFFFF
            }
#endregion

#region Types
            [StructLayout(LayoutKind.Sequential)]
            public struct LPRECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LPPOINT
            {
                public int x;
                public int y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct WINDOWPLACEMENT
            {
                public uint length;
                public uint flags;
                public uint showCmd;
                public LPPOINT ptMinPosition;
                public LPPOINT ptMaxPosition;
                public LPRECT rcNormalPosition;
                public LPRECT rcDevice;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct MONITORINFOEX
            {
                public uint cbSize;
                public LPRECT rcMonitor;
                public LPRECT rcWork;
                public uint dwFlags;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string szDevice;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public uint LowPart;
                public int HighPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_PATH_SOURCE_INFO
            {
                public LUID adapterId;
                public uint id;
                public uint modeInfoIdx;
                public uint statusFlags;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_PATH_TARGET_INFO
            {
                public LUID adapterId;
                public uint id;
                public uint modeInfoIdx;
                public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
                public DISPLAYCONFIG_ROTATION rotation;
                public DISPLAYCONFIG_SCALING scaling;
                public DISPLAYCONFIG_RATIONAL refreshRate;
                public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
                public bool targetAvailable;
                public uint statusFlags;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_RATIONAL
            {
                public uint Numerator;
                public uint Denominator;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_PATH_INFO
            {
                public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
                public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
                public uint flags;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_2DREGION
            {
                public uint cx;
                public uint cy;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
            {
                public ulong pixelRate;
                public DISPLAYCONFIG_RATIONAL hSyncFreq;
                public DISPLAYCONFIG_RATIONAL vSyncFreq;
                public DISPLAYCONFIG_2DREGION activeSize;
                public DISPLAYCONFIG_2DREGION totalSize;
                public uint videoStandard;
                public DISPLAYCONFIG_SCANLINE_ORDERING scanLineOrdering;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_TARGET_MODE
            {
                public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_SOURCE_MODE
            {
                public uint width;
                public uint height;
                public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
                public LPPOINT position;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct DISPLAYCONFIG_MODE_INFO_UNION
            {
                [FieldOffset(0)]
                public DISPLAYCONFIG_TARGET_MODE targetMode;

                [FieldOffset(0)]
                public DISPLAYCONFIG_SOURCE_MODE sourceMode;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_MODE_INFO
            {
                public DISPLAYCONFIG_MODE_INFO_TYPE infoType;
                public uint id;
                public LUID adapterId;
                public DISPLAYCONFIG_MODE_INFO_UNION modeInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
                public uint size;
                public LUID adapterId;
                public uint id;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
            {
                public uint value;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
                public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
                public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
                public ushort edidManufactureId;
                public ushort edidProductCodeId;
                public uint connectorInstance;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
                public string monitorFriendlyDeviceName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string monitorDevicePath;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
            {
                public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string viewGdiDeviceName;
            }
#endregion
        }

        public class Kernel32
        {
            [DllImport("kernel32.dll")]
            public static extern int GetCurrentThreadId();
        }
    }
}
