/*
 * Copyright 2024 Sony Corporation
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SRD.Utils;

namespace SRD.Core
{
    internal class SRDSession : IDisposable
    {
        private const Int32 INVALID_SESSION_ID = -1;
        private Int32 _sessionId = INVALID_SESSION_ID;

        private bool _isRunning = false;
        private bool _disposeRequired = false;

        private SRDSettings _settings;
        public SRDSettings Settings
        {
            get { return _settings; }
        }

#region Session lifecycle
        public bool IsRunning()
        {
            if((!_isRunning)&&(_sessionId != INVALID_SESSION_ID))
            {
                _isRunning = SRDCorePlugin.IsSessionRunning(_sessionId);
                if(_isRunning)
                {
                    _settings.Load(_sessionId);
                }
            }
            return _isRunning;
        }

        private SRDSession()
        {
            _sessionId = INVALID_SESSION_ID;
            _isRunning = false;
            _disposeRequired = false;
            _settings = new SRDSettings();
        }

        private SRDSession(Int32 sessionId, bool disposeRequired = false)
        {
            _sessionId = sessionId;
            _disposeRequired = disposeRequired;
            _isRunning = SRDCorePlugin.IsSessionRunning(_sessionId);
            _settings = new SRDSettings();
            if(_isRunning)
            {
                _settings.Load(_sessionId);
            }
        }

        ~SRDSession()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(_disposeRequired)
            {
                DestroySession();
            }
        }

        internal static SRDSession CreateSession()
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                return new SRDSession();
            }

            var result = SRDCorePlugin.CreateSessionWithSelectedDevice(out var sessionId);
            if((result != SrdXrResult.SUCCESS)&&(result != SrdXrResult.ERROR_SESSION_CREATED))
            {
                if (SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay() == SRDProjectSettings.BehaviorOptionWhenNoSRDisplay.ExitWithError)
                {
                    PopupErrorMessageAndForceToTerminate(result);
                }
                return null;
            }

            var session = FindSession(sessionId);
            if(session != null) return session;

            session = new SRDSession(sessionId, result != SrdXrResult.ERROR_SESSION_CREATED);
            RegisterSession(session);
            return session;
        }

        internal static List<SRDSession> CreateMultiSession(int max)
        {
            var sessions = new List<SRDSession>(max);
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                for(int i = 0; i < max; ++i)
                {
                    sessions.Add(new SRDSession());
                }
                return sessions;
            }

            var result = SRDCorePlugin.CreateMultiSession(max, out var sessionIds);
            if(result != SrdXrResult.SUCCESS)
            {
                if (SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay() == SRDProjectSettings.BehaviorOptionWhenNoSRDisplay.ExitWithError)
                {
                    PopupErrorMessageAndForceToTerminate(result);
                }
                return null;
            }

            foreach(var sessionId in sessionIds)
            {
                var session = FindSession(sessionId);
                if(session != null)
                {
                    sessions.Add(session);
                }
                else
                {
                    session = new SRDSession(sessionId);
                    RegisterSession(session);
                    sessions.Add(session);
                }
            }
            return sessions;
        }

        internal bool StartAsync()
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                _isRunning = true;
                return true;
            }

            if(_sessionId == INVALID_SESSION_ID)
            {
                return false;
            }

            if(IsRunning())
            {
                return true;
            }

            return BeginSession();
        }

        internal bool Start()
        {
            if(!StartAsync())
            {
                return false;
            }

            if(IsRunning())
            {
                return true;
            }

            var result = SRDCorePlugin.WaitForRunningState(_sessionId);
            if(result != SrdXrResult.SUCCESS)
            {
                PopupErrorMessageAndForceToTerminate(result);
                return false;
            }

            _isRunning = true;
            _settings.Load(_sessionId);

            return true;
        }

        private bool BeginSession()
        {
            var result = SRDCorePlugin.BeginSession(_sessionId);
            if((result != SrdXrResult.SUCCESS)&&(result != SrdXrResult.ERROR_SESSION_RUNNING))
            {
                PopupErrorMessageAndForceToTerminate(result);
                return false;
            }

            return true;
        }

        private static bool PopupErrorMessageAndForceToTerminate(SrdXrResult result)
        {
            Debug.LogError($"Result Code: {result}");

            var errorToMessage = new Dictionary<SrdXrResult, string>()
            {
                { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                { SrdXrResult.ERROR_VALIDATION_FAILURE, SRDHelper.SRDMessages.NoDeviceSelectedError},
                { SrdXrResult.ERROR_DEVICE_NOT_FOUND, SRDHelper.SRDMessages.DisplayConnectionError},
                { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
                { SrdXrResult.ERROR_SESSION_NOT_CREATE, SRDHelper.SRDMessages.UnknownError},
                { SrdXrResult.ERROR_SESSION_STILL_USED, SRDHelper.SRDMessages.AppConflictionError},
            };

            if (!errorToMessage.ContainsKey(result))
            {
                return false;
            }
            SRDHelper.PopupMessageAndForceToTerminate(errorToMessage[result]);
            return true;
        }

        public bool CheckSystemError()
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                return true;
            }

            var result = SRDCorePlugin.GetXrSystemError(_sessionId, out var systemError);

            if(result != SrdXrResult.SUCCESS)
            {
                Debug.LogError($"GetXrSystemError fails. Result Code: {result}");
                SRDHelper.PopupMessageAndForceToTerminate(SRDHelper.SRDMessages.UnknownError);
                return false;
            }

            if (systemError.code == SrdXrSystemErrorCode.SYSTEM_ERROR_SUCCESS)
            {
                return true;
            }

            Debug.LogError($"System Error Code: {systemError.code}");

            var errorToMessage = new Dictionary<SrdXrSystemErrorCode, string>()
            {
                { SrdXrSystemErrorCode.SYSTEM_ERROR_NO_AVAILABLE_DEVICE, SRDHelper.SRDMessages.DeviceNotFoundError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_DEVICE_LOST, SRDHelper.SRDMessages.DeviceInterruptionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_DEVICE_BUSY, SRDHelper.SRDMessages.AppConflictionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_OPERATION_FAILED, SRDHelper.SRDMessages.DeviceConnectionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_USB_NOT_CONNECTED, SRDHelper.SRDMessages.DeviceConnectionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_CAMERA_WITH_USB20, SRDHelper.SRDMessages.USB3ConnectionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_NO_USB_OR_NO_POWER, SRDHelper.SRDMessages.DeviceConnectionError},
                { SrdXrSystemErrorCode.SYSTEM_ERROR_ANOTHER_APPLICATION_RUNNING, SRDHelper.SRDMessages.AppConflictionError },
            };
            var msg = errorToMessage.ContainsKey(systemError.code) ? errorToMessage[systemError.code] : SRDHelper.SRDMessages.UnknownError;
            SRDHelper.PopupMessageAndForceToTerminate(msg);
            return false;
        }

        internal bool WaitForRunningState()
        {
            if(IsRunning())
            {
                return true;
            }

            var result = SRDCorePlugin.WaitForRunningState(_sessionId);
            if(result != SrdXrResult.SUCCESS)
            {
                if(!PopupErrorMessageAndForceToTerminate(result))
                {
                    CheckSystemError();
                }
                return false;
            }

            _isRunning = true;
            _settings.Load(_sessionId);

            return true;
        }

        internal bool Stop()
        {
            if(!_isRunning)
            {
                return true;
            }
            _isRunning = false;

            if(_sessionId == INVALID_SESSION_ID)
            {
                return true;
            }

            return EndSession();
        }

        private bool EndSession()
        {
            var result = SRDCorePlugin.EndSession(_sessionId);
            if((result != SrdXrResult.SUCCESS)&&(result != SrdXrResult.ERROR_SESSION_NOT_RUNNING))
            {
                return false;
            }
            return true;
        }

        internal bool DestroySession()
        {
            if(_sessionId == INVALID_SESSION_ID)
            {
                return true;
            }

            Stop();

            var result = SRDCorePlugin.DestroySession(_sessionId);
            if((result != SrdXrResult.SUCCESS)&&(result != SrdXrResult.ERROR_SESSION_NOT_CREATE))
            {
                return false;
            }

            _sessionId = INVALID_SESSION_ID;
            return true;
        }
#endregion // Session lifecycle

#region Session Instances Management
        private static List<SRDSession> _srdSessions = new List<SRDSession>();

        private static SRDSession FindSession(Int32 sessionId)
        {
            foreach(var session in _srdSessions)
            {
                if(session._sessionId == sessionId)
                {
                    return session;
                }
            }
            return null;
        }

        private static void RegisterSession(SRDSession session)
        {
            _srdSessions.Add(session);
        }

        internal static void DisposeAll()
        {
            foreach(var session in _srdSessions)
            {
                session.Dispose();
            }
            _srdSessions.Clear();
        }
#endregion // Session Instances Management

#region Session API
        public SrdXrResult UpdateTrackingResultCache()
        {
            return SRDCorePlugin.UpdateTrackingResultCache(_sessionId);
        }

        public void EndFrame()
        {
            SRDCorePlugin.EndFrame(_sessionId);
        }

        public void GenerateTextureAndShaders(ref SrdXrTexture leftTextureData, ref SrdXrTexture rightTextureData, ref SrdXrTexture outTextureData)
        {
            SRDCorePlugin.GenerateTextureAndShaders(_sessionId, ref leftTextureData, ref rightTextureData, ref outTextureData);
        }

        public SrdXrResult ShowCameraWindow(bool show)
        {
            return SRDCorePlugin.ShowCameraWindow(_sessionId, show);
        }

        public SrdXrResult GetPauseHeadPose(out bool pause)
        {
            return SRDCorePlugin.GetPauseHeadPose(_sessionId, out pause);
        }

        public SrdXrResult SetPauseHeadPose(bool pause)
        {
            return SRDCorePlugin.SetPauseHeadPose(_sessionId, pause);
        }

        public SrdXrResult GetDeviceInfo(out SrdXrDeviceInfo deviceInfo)
        {
            return SRDCorePlugin.GetDeviceInfo(_sessionId, out deviceInfo);
        }

        public SrdXrResult EnableStereo(bool enable)
        {
            return SRDCorePlugin.EnableStereo(_sessionId, enable);
        }

        public SrdXrResult GetFacePose(out Pose headPose, out Pose eyePoseL, out Pose eyePoseR)
        {
            return SRDCorePlugin.GetFacePose(_sessionId, out headPose, out eyePoseL, out eyePoseR);
        }

        public SrdXrResult GetProjectionMatrix(float nearClip, float farClip,
                                                      out Matrix4x4 headProjectionMatrix, 
                                                      out Matrix4x4 eyeProjectionMatrixL, 
                                                      out Matrix4x4 eyeProjectionMatrixR)
        {
            return SRDCorePlugin.GetProjectionMatrix(_sessionId, nearClip, farClip,
                                                      out headProjectionMatrix, 
                                                      out eyeProjectionMatrixL, 
                                                      out eyeProjectionMatrixR);
        }

        public SrdXrResult SetCrosstalkCorrectionMode(SrdXrCrosstalkCorrectionMode mode)
        {
            return SRDCorePlugin.SetCrosstalkCorrectionMode(_sessionId, mode);
        }

        public SrdXrResult GetCrosstalkCorrectionMode(out SrdXrCrosstalkCorrectionMode mode)
        {
            return SRDCorePlugin.GetCrosstalkCorrectionMode(_sessionId, out mode);
        }

        public SrdXrResult SetColorSpaceSettings(ColorSpace colorSpace, GraphicsDeviceType graphicsAPI, RenderPipelineType renderPipeline)
        {
            return SRDCorePlugin.SetColorSpaceSettings(_sessionId, colorSpace, graphicsAPI, renderPipeline);
        }

        public SrdXrResult GetDisplayFirmwareVersion(out string version)
        {
            return SRDCorePlugin.GetDisplayFirmwareVersion(_sessionId, out version);
        }

        public SrdXrResult GetPerformancePriorityEnabled(out bool enable)
        {
            return SRDCorePlugin.GetPerformancePriorityEnabled(_sessionId, out enable);
        }

        public SrdXrResult GetLensShiftEnabled(out bool enable)
        {
            return SRDCorePlugin.GetLensShiftEnabled(_sessionId, out enable);
        }

        public SrdXrResult SetLensShiftEnabled(bool enable)
        {
            return SRDCorePlugin.SetLensShiftEnabled(_sessionId, enable);
        }

        public SrdXrResult GetSystemTiltDegree(out int degree)
        {
            return SRDCorePlugin.GetSystemTiltDegree(_sessionId, out degree);
        }

        public SrdXrResult SetSystemTilteDegree(int degree)
        {
            return SRDCorePlugin.SetSystemTiltDegree(_sessionId, degree);
        }

        public SrdXrResult GetForce90Degree(out bool enable)
        {
            return SRDCorePlugin.GetForce90Degree(_sessionId, out enable);
        }

        public SrdXrResult SetForce90Degree(bool enable)
        {
            return SRDCorePlugin.SetForce90Degree(_sessionId, enable);
        }

        public SrdXrResult GetPoseSmootherEnabled(out bool enable)
        {
            return SRDCorePlugin.GetPoseSmootherEnabled(_sessionId, out enable);
        }

        public SrdXrResult SetPoseSmootherEnabled(bool enable)
        {
            return SRDCorePlugin.SetPoseSmootherEnabled(_sessionId, enable);
        }

        public SrdXrResult GetRealityCreationLevel(out int level)
        {
            return SRDCorePlugin.GetRealityCreationLevel(_sessionId, out level);
        }

        public SrdXrResult SetRealityCreationLevel(int level)
        {
            return SRDCorePlugin.SetRealityCreationLevel(_sessionId, level);
        }

        public SrdXrResult GetSensorRangeMode(out int mode)
        {
            return SRDCorePlugin.GetSensorRangeMode(_sessionId, out mode);
        }

        public SrdXrResult SetSensorRangeMode(int mode)
        {
            return SRDCorePlugin.SetSensorRangeMode(_sessionId, mode);
        }

        public SrdXrResult GetXtalkAdjustParam(out int param)
        {
            return SRDCorePlugin.GetXtalkAdjustParam(_sessionId, out param);
        }

        public SrdXrResult SetXtalkAdjustParam(int param)
        {
            return SRDCorePlugin.SetXtalkAdjustParam(_sessionId, param);
        }

        public SrdXrResult RestartHeadTracking()
        {
            return SRDCorePlugin.RestartHeadTracking(_sessionId);
        }

#endregion // Session API
    }
}
