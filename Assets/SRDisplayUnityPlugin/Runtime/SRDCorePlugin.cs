/*
 * Copyright 2019-2025 Sony Corporation
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

using SRD.Utils;

namespace SRD.Core
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    internal static class SRDStartup
    {
        static SRDStartup()
        {
            var result = SRDCorePlugin.ShowNativeLog();
            if (result != SrdXrResult.SUCCESS)
            {
                var errorToMessage = new Dictionary<SrdXrResult, string>()
                {
                    { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                    { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
                };
                var msg = errorToMessage.ContainsKey(result) ? errorToMessage[result] : SRDHelper.SRDMessages.UnknownError;
                Debug.LogError(msg);
            }
        }
    }
#endif  // UNITY_EDITOR

    internal static class SRDCorePlugin
    {
        public static int ShowMessageBox(string title, string message, Action<string> debugLogFunc = null)
        {
            if (debugLogFunc != null)
            {
                debugLogFunc(message);
            }
            return XRRuntimeAPI.ShowMessageBox(SRDApplicationWindow.GetSelfWindowHandle(), title, message);
        }

        [AOT.MonoPInvokeCallback(typeof(XRRuntimeAPI.DebugLogDelegate))]
        private static void RuntimeDebugLogCallback(string message, SrdXrLogLevels logLevel)
        {
            switch (logLevel)
            {
                case SrdXrLogLevels.LOG_LEVELS_TRACE:
                case SrdXrLogLevels.LOG_LEVELS_DEBUG:
                case SrdXrLogLevels.LOG_LEVELS_INFO:
                    Debug.Log(message);
                    break;
                case SrdXrLogLevels.LOG_LEVELS_WARN:
                    Debug.LogWarning(message);
                    break;
                case SrdXrLogLevels.LOG_LEVELS_ERR:
                case SrdXrLogLevels.LOG_LEVELS_CRITICAL:
                    Debug.LogError(message);
                    break;
                case SrdXrLogLevels.LOG_LEVELS_OFF:
                default:
                    break;
            }
        }

        public static SrdXrResult ShowNativeLog()
        {
            return XRRuntimeAPI.SetDebugLogCallback(RuntimeDebugLogCallback);

        }
        public static SrdXrResult HideNativeLog()
        {
            return XRRuntimeAPI.ResetDebugLogCallback();
        }

        public static SrdXrResult CreateSession(UInt32 deviceIndex, out Int32 sessionId)
        {
            return XRRuntimeAPI.CreateSession(deviceIndex, out sessionId);
        }

        public static SrdXrResult CreateSessionWithSelectedDevice(out Int32 sessionId)
        {
            return XRRuntimeAPI.CreateSessionWithSelectedDevice(out sessionId);
        }

        public static SrdXrResult CreateMultiSession(int numberOfSessions, out Int32[] sessionIds)
        {
            var result = XRRuntimeAPI.GetRealDeviceNum(out var deviceCount);
            if (result != SrdXrResult.SUCCESS)
            {
                sessionIds = null;
                return result;
            }
            if (deviceCount == 0)
            {
                numberOfSessions = 0;
                sessionIds = null;
                return SrdXrResult.ERROR_DEVICE_NOT_FOUND;
            }

            UInt32 n = Math.Min(deviceCount, (UInt32)numberOfSessions);
            sessionIds = new Int32[n];
            return XRRuntimeAPI.CreateMultiSession(ref n, sessionIds);
        }

        public static SrdXrResult BeginSession(Int32 sessionId)
        {
            return XRRuntimeAPI.BeginSession(sessionId);
        }

        public static bool IsSessionRunning(Int32 sessionId)
        {
            return XRRuntimeAPI.IsSessionRunning(sessionId);
        }

        public static SrdXrResult WaitForRunningState(Int32 sessionId)
        {
            return XRRuntimeAPI.WaitForRunningState(sessionId);
        }

        public static SrdXrResult EndSession(Int32 sessionId)
        {
            return XRRuntimeAPI.EndSession(sessionId);
        }

        public static SrdXrResult DestroySession(Int32 sessionId)
        {
            return XRRuntimeAPI.DestroySession(sessionId);
        }

        public static SrdXrResult GetDeviceInfo(Int32 sessionId, out SrdXrDeviceInfo deviceInfo)
        {
            return XRRuntimeAPI.GetDeviceInfo(sessionId, out deviceInfo);
        }

        public static SrdXrResult UpdateTrackingResultCache(Int32 sessionId)
        {
            return XRRuntimeAPI.UpdateTrackingResultCache(sessionId);
        }

        public static void EndFrame(Int32 sessionId)
        {
            GL.IssuePluginEvent(XRRuntimeAPI.GetEndFramePtr(sessionId, out var eventID), eventID);
        }

        public static SrdXrResult GetXrSystemError(Int32 sessionId, out SrdXrSystemError error)
        {
            return XRRuntimeAPI.GetXrSystemError(sessionId, out error);
        }

        public static SrdXrResult GetXrSystemErrorNum(Int32 sessionId, out UInt16 num)
        {
            return XRRuntimeAPI.GetXrSystemErrorNum(sessionId, out num);
        }

        public static SrdXrResult GetXrSystemErrorList(Int32 sessionId, SrdXrSystemError[] errors)
        {
            return XRRuntimeAPI.GetXrSystemErrorList(sessionId, (ushort)errors.Length, errors);
        }

        public static void GenerateTextureAndShaders(Int32 sessionId, ref SrdXrTexture leftTextureData, ref SrdXrTexture rightTextureData, ref SrdXrTexture outTextureData)
        {
            XRRuntimeAPI.SetTextures(sessionId, ref leftTextureData, ref rightTextureData, ref outTextureData);
            GL.IssuePluginEvent(XRRuntimeAPI.GetGenerateTextureAndShadersPtr(sessionId, out var eventID), eventID);
        }

        public static SrdXrResult ShowCameraWindow(Int32 sessionId, bool show)
        {
            return XRRuntimeAPI.ShowCameraWindow(sessionId, show);
        }

        public static SrdXrResult GetPauseHeadPose(Int32 sessionId, out bool pause)
        {
            return XRRuntimeAPI.GetPauseHeadPose(sessionId, out pause);
        }

        public static SrdXrResult SetPauseHeadPose(Int32 sessionId, bool pause)
        {
            return XRRuntimeAPI.SetPauseHeadPose(sessionId, pause);
        }

        public static SrdXrResult EnableStereo(Int32 sessionId, bool enable)
        {
            return XRRuntimeAPI.EnableStereo(sessionId, enable);
        }

        public static SrdXrResult GetFacePose(Int32 sessionId,
                                              out Pose headPose, out Pose eyePoseL, out Pose eyePoseR)
        {
            var result = XRRuntimeAPI.GetCachedHeadPose(sessionId, out var xrHeadPose);
            headPose = ToUnityPose(xrHeadPose.pose);
            eyePoseL = ToUnityPose(xrHeadPose.left_eye_pose);
            eyePoseR = ToUnityPose(xrHeadPose.right_eye_pose);

            return result;
        }

        public static SrdXrResult GetProjectionMatrix(Int32 sessionId, float nearClip, float farClip,
                                                      out Matrix4x4 headProjectionMatrix, 
                                                      out Matrix4x4 eyeProjectionMatrixL, 
                                                      out Matrix4x4 eyeProjectionMatrixR)
        {
            var result = XRRuntimeAPI.GetProjectionMatrix(sessionId, nearClip, farClip, out var xrProjMat);
            headProjectionMatrix = ToUnityMatrix4x4(xrProjMat.projection);
            eyeProjectionMatrixL = ToUnityMatrix4x4(xrProjMat.left_projection);
            eyeProjectionMatrixR = ToUnityMatrix4x4(xrProjMat.right_projection);
            return result;
        }

        public static SrdXrResult GetTargetMonitorRectangle(Int32 sessionId, out SrdXrRect rect)
        {
            return XRRuntimeAPI.GetTargetMonitorRectangle(sessionId, out rect);
        }

        public static SrdXrResult GetDisplaySpec(Int32 sessionId, out SrdXrDisplaySpec displaySpec)
        {
            return XRRuntimeAPI.GetDisplaySpec(sessionId, out displaySpec);
        }

        public static SrdXrResult GetRealDeviceNum(out UInt32 deviceCount)
        {
            SrdXrResult result = XRRuntimeAPI.GetRealDeviceNum(out deviceCount);
            if (result == SrdXrResult.SUCCESS)
            {
                if (deviceCount == 0)
                {
                    result = SrdXrResult.ERROR_DEVICE_NOT_FOUND;
                }
            }
            return result;
        }

        public static SrdXrResult EnumerateDevices(out List<SrdXrDeviceInfo> deviceList)
        {
            deviceList = new List<SrdXrDeviceInfo>();
            SrdXrResult result = XRRuntimeAPI.GetRealDeviceNum(out var deviceCount);
            if (result != SrdXrResult.SUCCESS)
            {
                return result;
            }
            if (deviceCount == 0)
            {
                return SrdXrResult.ERROR_DEVICE_NOT_FOUND;
            }

            var devices = new SrdXrDeviceInfo[deviceCount];
            result = XRRuntimeAPI.EnumerateRealDevices(deviceCount, devices);
            if (result == SrdXrResult.SUCCESS)
            {
                deviceList.AddRange(devices);
            }
            return result;
        }

        public static SrdXrResult SelectDevice(out SrdXrDeviceInfo device)
        {
            device = new SrdXrDeviceInfo();
            SrdXrResult result = XRRuntimeAPI.GetRealDeviceNum(out var deviceCount);
            if (result != SrdXrResult.SUCCESS)
            {
                return result;
            }
            if (deviceCount == 0)
            {
                return SrdXrResult.ERROR_DEVICE_NOT_FOUND;
            }

            var devices = new SrdXrDeviceInfo[deviceCount];
            result = XRRuntimeAPI.EnumerateRealDevices(deviceCount, devices);
            if (result != SrdXrResult.SUCCESS)
            {
                return result;
            }

            if (deviceCount == 1)
            {
                result = XRRuntimeAPI.SelectDevice(0);
                if (result == SrdXrResult.SUCCESS)
                {
                    device = devices[0];
                }
            }
            else
            {
                var item_list = new string[deviceCount];
                for (var i = 0; i < deviceCount; ++i)
                {
                    item_list[i] = devices[i].product_id;
                    if (!String.IsNullOrWhiteSpace(devices[i].device_serial_number))
                    {
                        item_list[i] += ' ' + devices[i].device_serial_number;
                    }
                }

                var device_index = XRRuntimeAPI.ShowComboBoxDialog(
                    SRDApplicationWindow.GetSelfWindowHandle(), item_list, (int)deviceCount);

                if (device_index < 0)
                {
                    return SrdXrResult.ERROR_USER_CANCEL;
                }
                else if (deviceCount <= device_index)
                {
                    return SrdXrResult.ERROR_RUNTIME_FAILURE;
                }

                result = SelectDevice((UInt32)device_index);
                if (result == SrdXrResult.SUCCESS)
                {
                    device = devices[device_index];
                }
            }
            return result;
        }

        public static SrdXrResult SelectDevice(UInt32 device_index)
        {
            return XRRuntimeAPI.SelectDevice(device_index);
        }

        public static SrdXrResult GetRuntimeVersionString(out string version, bool includeBuildVersion = true)
        {
            version = string.Empty;
            var ret = XRRuntimeAPI.GetXrRuntimeVersion(out UInt16 major, out UInt16 minor, out UInt16 revision, out UInt16 build);
            if (ret == SrdXrResult.SUCCESS)
            {
                version = $"{major}.{minor}.{revision}";
                if (includeBuildVersion)
                {
                    version += $".{build}";
                }
            }
            return ret;
        }

        public const SrdXrCrosstalkCorrectionMode DefaultCrosstalkCorrectionMode = SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_MEDIUM;

        public static SrdXrResult SetCrosstalkCorrectionMode(Int32 sessionId, SrdXrCrosstalkCorrectionMode mode = DefaultCrosstalkCorrectionMode)
        {
            return XRRuntimeAPI.SetCrosstalkCorrectionMode(sessionId, mode);
        }

        public static SrdXrResult GetCrosstalkCorrectionMode(Int32 sessionId, out SrdXrCrosstalkCorrectionMode mode)
        {
            return XRRuntimeAPI.GetCrosstalkCorrectionMode(sessionId, out mode);
        }

        public static SrdXrResult SetColorSpaceSettings(Int32 sessionId, ColorSpace colorSpace, GraphicsDeviceType graphicsAPI, RenderPipelineType renderPipeline)
        {
            Debug.Assert((colorSpace == ColorSpace.Gamma) || (colorSpace == ColorSpace.Linear));

            var unityGamma = 2.2f;
            int input_gamma_count = (colorSpace != ColorSpace.Gamma) ? 0 : 1;
            int output_gamma_count = input_gamma_count;
            if ((!SRDCorePlugin.IsARGBHalfSupported()) && (colorSpace == ColorSpace.Linear) && (graphicsAPI == GraphicsDeviceType.Direct3D11))
            {
                output_gamma_count = 1;
            }

            return XRRuntimeAPI.SetColorSpace(sessionId, input_gamma_count, output_gamma_count, unityGamma);
        }

        public static bool GetCountOfSupportedDevices(out Int32 size)
        {
            return XRRuntimeAPI.GetCountOfSupportedDevices(out size);
        }

        public static bool GetPanelSpecOfSupportedDevices(supported_panel_spec[] panel_specs)
        {
            return XRRuntimeAPI.GetPanelSpecOfSupportedDevices(panel_specs, panel_specs.Length);
        }

        public static SrdXrResult GetDisplayFirmwareVersion(Int32 sessionId, out string version)
        {
            version = string.Empty;
            const int max_length = 16;
            var versionSB = new StringBuilder(max_length);
            var result = XRRuntimeAPI.GetDisplayFirmwareVersion(sessionId, versionSB, max_length);
            if (result == SrdXrResult.SUCCESS)
            {
                version = versionSB.ToString();
            }
            return result;
        }

        public static SrdXrResult GetPerformancePriorityEnabled(Int32 sessionId, out bool enable)
        {
            return XRRuntimeAPI.GetPerformancePriorityEnabled(sessionId, out enable);
        }

        public static SrdXrResult GetLensShiftEnabled(Int32 sessionId, out bool enable)
        {
            return XRRuntimeAPI.GetLensShiftEnabled(sessionId, out enable);
        }

        public static SrdXrResult SetLensShiftEnabled(Int32 sessionId, bool enable)
        {
            return XRRuntimeAPI.SetLensShiftEnabled(sessionId, enable);
        }

        public static SrdXrResult GetSystemTiltDegree(Int32 sessionId, out int degree)
        {
            return XRRuntimeAPI.GetSystemTiltDegree(sessionId, out degree);
        }

        public static SrdXrResult SetSystemTiltDegree(Int32 sessionId, int degree)
        {
            return XRRuntimeAPI.SetSystemTiltDegree(sessionId, degree);
        }

        public static SrdXrResult GetForce90Degree(Int32 sessionId, out bool enable)
        {
            return XRRuntimeAPI.GetForce90Degree(sessionId, out enable);
        }

        public static SrdXrResult SetForce90Degree(Int32 sessionId, bool enable)
        {
            return XRRuntimeAPI.SetForce90Degree(sessionId, enable);
        }

        public static SrdXrResult GetPoseSmootherEnabled(Int32 sessionId, out bool enable)
        {
            return XRRuntimeAPI.GetPoseSmootherEnabled(sessionId, out enable);
        }

        public static SrdXrResult SetPoseSmootherEnabled(Int32 sessionId, bool enable)
        {
            return XRRuntimeAPI.SetPoseSmootherEnabled(sessionId, enable);
        }

        public static bool IsARGBHalfSupported()
        {
            UInt16 major = 0, minor = 0, revision = 0, build = 0;
            var ret = XRRuntimeAPI.GetXrRuntimeVersion(out major, out minor, out revision, out build);
            if (ret != SrdXrResult.SUCCESS)
            {
                return false;
            }
            var version = major * 10000 + minor * 100 + revision;
            return (version < 20101) ? false : true;
        }

        public static bool IsDirect3D12Supported()
        {
            UInt16 major = 0, minor = 0, revision = 0, build = 0;
            var ret = XRRuntimeAPI.GetXrRuntimeVersion(out major, out minor, out revision, out build);
            if (ret != SrdXrResult.SUCCESS)
            {
                return false;
            }
            var version = major * 10000 + minor * 100 + revision;
            return (version < 20400) ? false : true;
        }

        public static SrdXrResult GetRealityCreationLevel(Int32 sessionId, out int level)
        {
            return XRRuntimeAPI.GetRealityCreationLevel(sessionId, out level);
        }

        public static SrdXrResult SetRealityCreationLevel(Int32 sessionId, int level)
        {
            return XRRuntimeAPI.SetRealityCreationLevel(sessionId, level);
        }

        public static SrdXrResult GetSensorRangeMode(Int32 sessionId, out int mode)
        {
            return XRRuntimeAPI.GetSensorRangeMode(sessionId, out mode);
        }

        public static SrdXrResult SetSensorRangeMode(Int32 sessionId,  int mode)
        {
            return XRRuntimeAPI.SetSensorRangeMode(sessionId, mode);
        }

        public static SrdXrResult GetXtalkAdjustParam(Int32 sessionId, out int param)
        {
            return XRRuntimeAPI.GetXtalkAdjustParam(sessionId, out param);
        }

        public static SrdXrResult SetXtalkAdjustParam(Int32 sessionId, int param)
        {
            return XRRuntimeAPI.SetXtalkAdjustParam(sessionId, param);
        }

        public static SrdXrResult RestartHeadTracking(Int32 sessionId)
        {
            return XRRuntimeAPI.RestartHeadTracking(sessionId);
        }

        public static SrdXrResult EnumerateMultiDisplayDevices(out List<SrdXrDeviceInfo> deviceList, out SrdXrMultiDisplayMode mode)
        {
            mode = SrdXrMultiDisplayMode.Single;
            deviceList = new List<SrdXrDeviceInfo>();
            SrdXrResult result = XRRuntimeAPI.GetMultiDisplayDeviceNum(out var deviceCount);
            if (result != SrdXrResult.SUCCESS)
            {
                return result;
            }
            if (deviceCount == 0)
            {
                return SrdXrResult.ERROR_DEVICE_NOT_FOUND;
            }

            var devices = new SrdXrDeviceInfo[deviceCount];
            result = XRRuntimeAPI.EnumerateMultiDisplayDevices(deviceCount, devices, out mode);
            if (result == SrdXrResult.SUCCESS)
            {
                deviceList.AddRange(devices);
            }
            return result;
        }


        private struct XRRuntimeAPI
        {
            const string dll_path = SRD.Utils.SRDHelper.SRDConstants.XRRuntimeWrapperDLLName;

            [DllImport(dll_path, EntryPoint = "srd_xr_get_BeginFrame_func")]
            public static extern IntPtr GetBeginFramePtr(Int32 session_id, out int eventID);

            [DllImport(dll_path, EntryPoint = "srd_xr_get_EndFrame_func")]
            public static extern IntPtr GetEndFramePtr(Int32 session_id, out int eventID);

            [DllImport(dll_path, EntryPoint = "srd_xr_get_GenerateTextureAndShaders_func")]
            public static extern IntPtr GetGenerateTextureAndShadersPtr(Int32 session_id, out int eventID);

            [DllImport(dll_path, EntryPoint = "srd_xr_ShowMessageBox", CharSet = CharSet.Unicode)]
            public static extern int ShowMessageBox(IntPtr hWnd, string title, string msg);

            [DllImport(dll_path, EntryPoint = "srd_xr_EnumerateDevices")]
            public static extern SrdXrResult EnumerateDevices(UInt32 load_count, [Out] SrdXrDeviceInfo[] devices);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetDeviceNum")]
            public static extern SrdXrResult GetDeviceNum(out UInt32 num);

            [DllImport(dll_path, EntryPoint = "srd_xr_EnumerateRealDevices")]
            public static extern SrdXrResult EnumerateRealDevices(UInt32 load_count, [Out] SrdXrDeviceInfo[] devices);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetRealDeviceNum")]
            public static extern SrdXrResult GetRealDeviceNum(out UInt32 num);

            [DllImport(dll_path, EntryPoint = "srd_xr_SelectDevice")]
            public static extern SrdXrResult SelectDevice(UInt32 device_index);

            [DllImport(dll_path, EntryPoint = "srd_xr_CreateSession")]
            public static extern SrdXrResult CreateSession(UInt32 device_index, out Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_CreateSessionWithSelectedDevice")]
            public static extern SrdXrResult CreateSessionWithSelectedDevice(out Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_CreateMultiSession")]
            public static extern SrdXrResult CreateMultiSession([In, Out] ref UInt32 count, [Out] Int32[] session_ids);

            [DllImport(dll_path, EntryPoint = "srd_xr_BeginSession")]
            public static extern SrdXrResult BeginSession(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_IsSessionRunning")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool IsSessionRunning(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_WaitForRunningState")]
            public static extern SrdXrResult WaitForRunningState(Int32 session_id);
    
            [DllImport(dll_path, EntryPoint = "srd_xr_EndSession")]
            public static extern SrdXrResult EndSession(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_DestroySession")]
            public static extern SrdXrResult DestroySession(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetDeviceInfo")]
            public static extern SrdXrResult GetDeviceInfo(Int32 session_id, out SrdXrDeviceInfo device_info);

            [DllImport(dll_path, EntryPoint = "srd_xr_EnableStereo")]
            public static extern SrdXrResult EnableStereo(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool enable);

            [DllImport(dll_path, EntryPoint = "srd_xr_UpdateTrackingResultCache")]
            public static extern SrdXrResult UpdateTrackingResultCache(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetCachedPose")]
            public static extern SrdXrResult GetCachedPose(Int32 session_id, SrdXrPoseId pose_id, out SrdXrPosef pose);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetCachedHeadPose")]
            public static extern SrdXrResult GetCachedHeadPose(Int32 session_id, out SrdXrHeadPosef pose);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetProjectionMatrix")]
            public static extern SrdXrResult GetProjectionMatrix(Int32 session_id, float near_clip, float far_clip, out SrdXrProjectionMatrix data);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetTargetMonitorRectangle")]
            public static extern SrdXrResult GetTargetMonitorRectangle(Int32 session_id, out SrdXrRect rect);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetDisplaySpec")]
            public static extern SrdXrResult GetDisplaySpec(Int32 session_id, out SrdXrDisplaySpec data);

            [DllImport(dll_path, EntryPoint = "srd_xr_SetColorSpace")]
            public static extern SrdXrResult SetColorSpace(Int32 session_id, int input_gamma_count, int output_gamma_count, float gamma);

            [DllImport(dll_path, EntryPoint = "srd_xr_SetTextures")]
            public static extern SrdXrResult SetTextures(Int32 session_id, [In] ref SrdXrTexture left_texture, [In] ref SrdXrTexture right_texture, [In] ref SrdXrTexture render_target);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void DebugLogDelegate([MarshalAs(UnmanagedType.LPStr)] string str, SrdXrLogLevels log_levels);
            [DllImport(dll_path, EntryPoint = "srd_xr_SetDebugLogCallback")]
            public static extern SrdXrResult SetDebugLogCallback(DebugLogDelegate debug_log_delegate);

            [DllImport(dll_path, EntryPoint = "srd_xr_ResetDebugLogCallback")]
            public static extern SrdXrResult ResetDebugLogCallback();

            [DllImport(dll_path, EntryPoint = "srd_xr_GetXrSystemError")]
            public static extern SrdXrResult GetXrSystemError(Int32 session_id, out SrdXrSystemError error);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetXrSystemErrorNum")]
            public static extern SrdXrResult GetXrSystemErrorNum(Int32 session_id, out UInt16 num);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetXrSystemErrorList")]
            public static extern SrdXrResult GetXrSystemErrorList(Int32 session_id, UInt16 num, [Out] SrdXrSystemError[] errors);

            [DllImport(dll_path, EntryPoint = "srd_xr_GetXrRuntimeVersion")]
            public static extern SrdXrResult GetXrRuntimeVersion(out UInt16 major, out UInt16 minor, out UInt16 revision, out UInt16 build);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetCameraWindowEnabled")]
            public static extern SrdXrResult ShowCameraWindow(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool show);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetPauseHeadPose")]
            public static extern SrdXrResult GetPauseHeadPose(Int32 session_id, [MarshalAs(UnmanagedType.U1)] out bool pause);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetPauseHeadPose")]
            public static extern SrdXrResult SetPauseHeadPose(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool pause);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetCrosstalkCorrectionMode")]
            public static extern SrdXrResult GetCrosstalkCorrectionMode(Int32 session_id, out SrdXrCrosstalkCorrectionMode mode);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetCrosstalkCorrectionMode")]
            public static extern SrdXrResult SetCrosstalkCorrectionMode(Int32 session_id, SrdXrCrosstalkCorrectionMode mode);

            [DllImport(dll_path, EntryPoint = "srd_ax_ShowComboBoxDialog", CharSet = CharSet.Unicode)]
            public static extern Int64 ShowComboBoxDialog(IntPtr hWnd, [In, MarshalAs(UnmanagedType.LPArray,
                                              ArraySubType = UnmanagedType.LPWStr)] string[] item_list, Int32 size);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetCountOfSupportedDevices")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetCountOfSupportedDevices(out Int32 size);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetPanelSpecOfSupportedDevices")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static extern bool GetPanelSpecOfSupportedDevices([Out] supported_panel_spec[] panel_specs, Int32 size);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetDisplayFirmwareVersion", CharSet = CharSet.Ansi)]
            public static extern SrdXrResult GetDisplayFirmwareVersion(Int32 session_id, StringBuilder version, int length);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetPerformancePriorityEnabled")]
            public static extern SrdXrResult GetPerformancePriorityEnabled(Int32 session_id, [MarshalAs(UnmanagedType.U1)] out bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetLensShiftEnabled")]
            public static extern SrdXrResult GetLensShiftEnabled(Int32 session_id, [MarshalAs(UnmanagedType.U1)] out bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetLensShiftEnabled")]
            public static extern SrdXrResult SetLensShiftEnabled(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetRealityCreationLevel")]
            public static extern SrdXrResult GetRealityCreationLevel(Int32 session_id, out Int32 level);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetRealityCreationLevel")]
            public static extern SrdXrResult SetRealityCreationLevel(Int32 session_id, Int32 level);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetSensorRangeMode")]
            public static extern SrdXrResult GetSensorRangeMode(Int32 session_id, out Int32 mode);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetSensorRangeMode")]
            public static extern SrdXrResult SetSensorRangeMode(Int32 session_id, Int32 mode);

            [DllImport(dll_path, EntryPoint = "srd_ax_RestartHeadTracking")]
            public static extern SrdXrResult RestartHeadTracking(Int32 session_id);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetXtalkAdjustParam")]
            public static extern SrdXrResult GetXtalkAdjustParam(Int32 session_id, out Int32 param);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetXtalkAdjustParam")]
            public static extern SrdXrResult SetXtalkAdjustParam(Int32 session_id, Int32 param);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetSystemTiltDegree")]
            public static extern SrdXrResult GetSystemTiltDegree(Int32 session_id, out int degree);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetSystemTiltDegree")]
            public static extern SrdXrResult SetSystemTiltDegree(Int32 session_id, int degree);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetForce90Degree")]
            public static extern SrdXrResult GetForce90Degree(Int32 session_id, [MarshalAs(UnmanagedType.U1)] out bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetForce90Degree")]
            public static extern SrdXrResult SetForce90Degree(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetPoseSmootherEnabled")]
            public static extern SrdXrResult GetPoseSmootherEnabled(Int32 session_id, [MarshalAs(UnmanagedType.U1)] out bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_SetPoseSmootherEnabled")]
            public static extern SrdXrResult SetPoseSmootherEnabled(Int32 session_id, [MarshalAs(UnmanagedType.U1)] bool enable);

            [DllImport(dll_path, EntryPoint = "srd_ax_EnumerateMultiDisplayDevices")]
            public static extern SrdXrResult EnumerateMultiDisplayDevices(UInt32 load_count, [Out] SrdXrDeviceInfo[] devices, out SrdXrMultiDisplayMode mode);

            [DllImport(dll_path, EntryPoint = "srd_ax_GetMultiDisplayDeviceNum")]
            public static extern SrdXrResult GetMultiDisplayDeviceNum(out UInt32 num);

        }


        private static Pose ToUnityPose(SrdXrPosef p)
        {
            return new Pose(ToUnityVector(p.position), ToUnityQuaternion(p.orientation));
        }

        private static Vector3 ToUnityVector(SrdXrVector3f v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        private static Quaternion ToUnityQuaternion(SrdXrQuaternionf q)
        {
            return new Quaternion(q.x, q.y, q.z, q.w);
        }

        private static Matrix4x4 ToUnityMatrix4x4(SrdXrMatrix4x4f m)
        {
            var ret = new Matrix4x4();
            for (var i = 0; i < 16; i++)
            {
                ret[i] = m.matrix[i];
            }
            return ret;
        }

    }

    public enum SrdXrResult
    {
        SUCCESS = 0,
        ERROR_RUNTIME_NOT_FOUND = -1,
        ERROR_VALIDATION_FAILURE = -2,
        ERROR_RUNTIME_FAILURE = -3,
        ERROR_FUNCTION_UNSUPPORTED = -4,
        ERROR_HANDLE_INVALID = -5,
        ERROR_SESSION_CREATED = -6,
        ERROR_SESSION_READY = -7,
        ERROR_SESSION_STARTING = -8,
        ERROR_SESSION_RUNNING = -9,
        ERROR_SESSION_STOPPING = -10,
        ERROR_SESSION_NOT_CREATE = -11,
        ERROR_SESSION_NOT_READY = -12,
        ERROR_SESSION_NOT_RUNNING = -13,
        ERROR_SESSION_STILL_USED = -14,
        ERROR_POSE_INVALID = -15,
        ERROR_SET_DATA_FAILURE = -16,
        ERROR_GET_DATA_FAILURE = -17,
        ERROR_FILE_ACCESS_ERROR = -18,
        ERROR_DEVICE_NOT_FOUND = -19,
        ERROR_RUNTIME_UNSUPPORTED = -20,

        // Following error codes are plugin-defined error codes
        ERROR_USER_CANCEL = -2001,

        RESULT_MAX_ENUM = 0x7FFFFFFF
    };

    public enum SrdXrSystemErrorResult
    {
        SYSTEM_ERROR_RESULT_SUCCESS = 0,
        SYSTEM_ERROR_RESULT_WARNING = 1,
        SYSTEM_ERROR_RESULT_ERROR = 2,
    };

    public enum SrdXrSystemErrorCode
    {
        SYSTEM_ERROR_SUCCESS = 0,
        SYSTEM_ERROR_NO_AVAILABLE_DEVICE = 1,
        SYSTEM_ERROR_DEVICE_LOST = 2,
        SYSTEM_ERROR_DEVICE_BUSY = 3,
        SYSTEM_ERROR_INVALID_DATA = 4,
        SYSTEM_ERROR_NO_DATA = 5,
        SYSTEM_ERROR_OPERATION_FAILED = 6,
        SYSTEM_ERROR_USB_NOT_CONNECTED = 7,
        SYSTEM_ERROR_CAMERA_WITH_USB20 = 8,
        SYSTEM_ERROR_NO_USB_OR_NO_POWER = 9,
        SYSTEM_ERROR_ANOTHER_APPLICATION_RUNNING = -10002,
    };

    public enum SrdXrLogLevels
    {
        LOG_LEVELS_TRACE = 0,
        LOG_LEVELS_DEBUG = 1,
        LOG_LEVELS_INFO = 2,
        LOG_LEVELS_WARN = 3,
        LOG_LEVELS_ERR = 4,
        LOG_LEVELS_CRITICAL = 5,
        LOG_LEVELS_OFF = 6,
    };

    public enum SrdXrPoseId
    {
        POSE_ID_HEAD = 0,
        POSE_ID_LEFT_EYE = 1,
        POSE_ID_RIGHT_EYE = 2,
    };

    public enum SrdXrCrosstalkCorrectionMode
    {
        DISABLED = 0,
        DEPENDS_ON_APPLICATION = 1,
        GRADATION_CORRECTION_MEDIUM = 2,
        GRADATION_CORRECTION_ALL = 3,
        GRADATION_CORRECTION_HIGH_PRECISE = 4,
    }

    public enum SrdXrMultiDisplayMode
    {
        [InspectorName("Single Display")]
        Single = 0,
        [InspectorName("Vertical Multi Display")]
        Virtical = 1,
        [InspectorName("Horizontal Multi Display")]
        Horizontal = 2,
        [InspectorName("Grid Multi Display")]
        Grid = 3,
        [InspectorName("Duplicated Output")]
        Clone = 4,
        [InspectorName("Varied Output")]
        Multiple = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrPosef
    {
        public SrdXrQuaternionf orientation;
        public SrdXrVector3f position;

        public SrdXrPosef(SrdXrQuaternionf in_orientation, SrdXrVector3f in_position)
        {
            orientation = in_orientation;
            position = in_position;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrHeadPosef
    {
        public SrdXrPosef pose;
        public SrdXrPosef left_eye_pose;
        public SrdXrPosef right_eye_pose;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplaySize
    {
        public float width_m;
        public float height_m;

        public SrdXrDisplaySize(float in_width_m, float in_height_m)
        {
            width_m = in_width_m;
            height_m = in_height_m;
        }

        public static implicit operator Vector2(SrdXrDisplaySize display)
        {
            return new Vector2(display.width_m, display.height_m);
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public SrdXrRect(int in_left, int in_top, int in_right, int in_bottom)
        {
            left = in_left;
            top = in_top;
            right = in_right;
            bottom = in_bottom;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplayResolution
    {
        public UInt32 width;
        public UInt32 height;
        public UInt32 area;

        public SrdXrDisplayResolution(UInt32 in_width, UInt32 in_height)
        {
            width = in_width;
            height = in_height;
            area = width * height;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrProjectionMatrix
    {
        public SrdXrMatrix4x4f projection;
        public SrdXrMatrix4x4f left_projection;
        public SrdXrMatrix4x4f right_projection;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrTexture
    {
        public IntPtr texture;
        public UInt32 width;
        public UInt32 height;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrQuaternionf
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SrdXrQuaternionf(float in_x, float in_y, float in_z, float in_w)
        {
            x = in_x;
            y = in_y;
            z = in_z;
            w = in_w;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrVector3f
    {
        public float x;
        public float y;
        public float z;

        public SrdXrVector3f(float in_x, float in_y, float in_z)
        {
            x = in_x;
            y = in_y;
            z = in_z;
        }

        public static SrdXrVector3f operator +(SrdXrVector3f a, SrdXrVector3f b)
        => new SrdXrVector3f(a.x + b.x, a.y + b.y, a.z + b.z);

        public static SrdXrVector3f operator -(SrdXrVector3f a, SrdXrVector3f b)
        => new SrdXrVector3f(a.x - b.x, a.y - b.y, a.z - b.z);

        public static SrdXrVector3f operator *(SrdXrVector3f a, float b)
        => new SrdXrVector3f(a.x * b, a.y * b, a.z * b);

        public static SrdXrVector3f operator /(SrdXrVector3f a, float b)
        => new SrdXrVector3f(a.x / b, a.y / b, a.z / b);

        public float Dot(SrdXrVector3f a)
        {
            return x * a.x + y * a.y + z * a.z;
        }

        public void Normalize()
        {
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            x /= length;
            y /= length;
            z /= length;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrVector4f
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SrdXrVector4f(float in_x, float in_y, float in_z, float in_w)
        {
            x = in_x;
            y = in_y;
            z = in_z;
            w = in_w;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrMatrix4x4f
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 4)]
        public float[] matrix;

        public SrdXrMatrix4x4f(SrdXrVector4f in_x, SrdXrVector4f in_y, SrdXrVector4f in_z, SrdXrVector4f in_w)
        {
            matrix = new float[4 * 4];

            matrix[4 * 0 + 0] = in_x.x;
            matrix[4 * 0 + 1] = in_x.y;
            matrix[4 * 0 + 2] = in_x.z;
            matrix[4 * 0 + 3] = in_x.w;
            matrix[4 * 1 + 0] = in_y.x;
            matrix[4 * 1 + 1] = in_y.y;
            matrix[4 * 1 + 2] = in_y.z;
            matrix[4 * 1 + 3] = in_y.w;
            matrix[4 * 2 + 0] = in_z.x;
            matrix[4 * 2 + 1] = in_z.y;
            matrix[4 * 2 + 2] = in_z.z;
            matrix[4 * 2 + 3] = in_z.w;
            matrix[4 * 3 + 0] = in_w.x;
            matrix[4 * 3 + 1] = in_w.y;
            matrix[4 * 3 + 2] = in_w.z;
            matrix[4 * 3 + 3] = in_w.w;
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SrdXrDeviceInfo
    {
        public UInt32 device_index;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string device_serial_number;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string product_id;
        public SrdXrRect target_monitor_rectangle;
        public SrdXrRect primary_monitor_rectangle;

        public SrdXrDeviceInfo(UInt32 in_device_index, string in_device_serial_number
                               , string in_product_id, SrdXrRect in_target_monitor_rectangle, SrdXrRect in_primary_monitor_rectangle)
        {
            device_index = in_device_index;
            device_serial_number = in_device_serial_number;
            product_id = in_product_id;
            target_monitor_rectangle = in_target_monitor_rectangle;
            primary_monitor_rectangle = in_primary_monitor_rectangle;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct SrdXrDisplaySpec
    {
        public SrdXrDisplaySize display_size;
        public SrdXrDisplayResolution display_resolution;
        public float display_tilt_rad;

        public SrdXrDisplaySpec(SrdXrDisplaySize in_display_size, float in_display_tilt_rad
                            , SrdXrDisplayResolution in_display_resolution)
        {
            display_size = in_display_size;
            display_tilt_rad = in_display_tilt_rad;
            display_resolution = in_display_resolution;
        }
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct supported_panel_spec
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        public string device_name; // max 15 characters
        public float width;    // in meter
        public float height;   // in meter
        public float angle;    // in radian
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SrdXrSystemError
    {
        [MarshalAs(UnmanagedType.U1)]
        public SrdXrSystemErrorResult result;
        [MarshalAs(UnmanagedType.I4)]
        public SrdXrSystemErrorCode code;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string msg;
    };
}
