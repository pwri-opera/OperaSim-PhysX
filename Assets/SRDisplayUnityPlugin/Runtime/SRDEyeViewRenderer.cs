/*
 * Copyright 2019-2025 Sony Corporation
 */

#if UNITY_2019_1_OR_NEWER
    #define SRP_AVAILABLE

/// WORKAROUND
/// https://issuetracker.unity3d.com/issues/camera-doesnt-move-when-changing-its-position-in-the-begincamerarendering-and-the-endcamerarendering-methods
#if (UNITY_2020_1_OR_NEWER && !UNITY_2021_1_OR_NEWER) || (UNITY_2021_1_0 || UNITY_2021_1_1 || UNITY_2021_1_2 || UNITY_2021_1_3 || UNITY_2021_1_4 || UNITY_2021_1_5 || UNITY_2021_1_6)
    #define WORKAROUND_ISSUE1318629
#endif
#endif

using System;
using System.Collections.Generic;
using SRD.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if SRP_AVAILABLE
    using SRPCallbackFuncForCameraRendering = System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera>;
#if UNITY_2021_1_OR_NEWER
    using SRPCallbackFuncForFrameRendering = System.Action<UnityEngine.Rendering.ScriptableRenderContext, System.Collections.Generic.List<UnityEngine.Camera>>;
#else
    using SRPCallbackFuncForFrameRendering = System.Action<UnityEngine.Rendering.ScriptableRenderContext, UnityEngine.Camera[]>;
#endif
#endif

namespace SRD.Core
{
    internal interface ISRDEyeViewRenderer : IDisposable
    {
        void Start();
        void Stop();
        SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive);
        // For control of scene render timing
        void Render();
        Texture GetLeftEyeViewTexture();
        Texture GetRightEyeViewTexture();
    }


    internal class SRDEyeViewRenderer : ISRDEyeViewRenderer
    {
        private SRDManager _srdManager;
        private ISRDFaceTracker _faceTracker;

        private Dictionary<EyeType, Transform> _eyeTransform;
        private Dictionary<EyeType, Camera> _eyeCamera;
        private Dictionary<EyeType, RenderTexture> _eyeCamRenderTextureCache;
        private Dictionary<EyeType, Material> _eyeCamMaterial;
        private Dictionary<EyeType, Material> _eyeCamLowpassMaterial;
        private SRDCameras _srdCameras;

        private List<EyeType> _eyeTypes;
        private bool _isSRPUsed = false;
#if SRP_AVAILABLE
        private Dictionary<EyeType, SRPCallbackFuncForCameraRendering> _eyeCamSRPPreCallback = new Dictionary<EyeType, SRPCallbackFuncForCameraRendering>();
        private Dictionary<EyeType, SRPCallbackFuncForCameraRendering> _eyeCamSRPPostCallback = new Dictionary<EyeType, SRPCallbackFuncForCameraRendering>();
        private Dictionary<EyeType, SRPCallbackFuncForFrameRendering> _frameSRPPreCallback = new Dictionary<EyeType, SRPCallbackFuncForFrameRendering>();
        private Dictionary<EyeType, SRPCallbackFuncForFrameRendering> _frameSRPPostCallback = new Dictionary<EyeType, SRPCallbackFuncForFrameRendering>();
#endif
        private Dictionary<EyeType, Camera.CameraCallback> _eyeCamStateUpdateCallback = new Dictionary<EyeType, Camera.CameraCallback>();

        private FacePose _currentFacePose;
        private FaceProjectionMatrix _currentProjMat;
        private bool _isBoxFrontClippingCache = true;

        private readonly float ObliquedNearClipOffset = -0.025f;
        private readonly int RenderTextureDepth = 24;

        public SRDEyeViewRenderer(SRDManager srdManager)
        {
            _eyeTransform = new Dictionary<EyeType, Transform>();
            _eyeCamera = new Dictionary<EyeType, Camera>();
            _eyeCamRenderTextureCache = new Dictionary<EyeType, RenderTexture>();
            _eyeCamMaterial = new Dictionary<EyeType, Material>();
            _eyeCamLowpassMaterial = new Dictionary<EyeType, Material>();

            _eyeTypes = new List<EyeType>() { EyeType.Left, EyeType.Right };
            _isSRPUsed = (SRDHelper.renderPipelineType != RenderPipelineType.BRP);

            _currentFacePose = SRDFaceTracker.CreateDefaultFacePose();
            _currentProjMat = SRDFaceTracker.CreateDefaultProjMatrix();

            _srdManager = srdManager;
            _srdCameras = new SRDCameras(_srdManager);

            var width = _srdManager.Settings.DeviceInfo.ScreenRect.Width;
            var height = _srdManager.Settings.DeviceInfo.ScreenRect.Height;
            if (_srdManager.IsPerformancePriorityEnabled)
            {
                width /= 2; height /= 2;
            }
            foreach(var type in _eyeTypes)
            {
                var bufferFormat = SRDCorePlugin.IsARGBHalfSupported() ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                var camrt = new RenderTexture(width, height, RenderTextureDepth, bufferFormat,
                                              (QualitySettings.desiredColorSpace == ColorSpace.Linear) ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
                camrt.name = SRDHelper.SRDConstants.EyeCamRenderTexDefaultName + SRDHelper.EyeSideName[type];
                camrt.Create();
                _eyeCamRenderTextureCache.Add(type, camrt);

                var homographyMaterial = new Material(Shader.Find("uHomography/Homography"));
                homographyMaterial.hideFlags = HideFlags.HideAndDontSave;
                _eyeCamMaterial[type] = homographyMaterial;

                var lowpassMaterial = new Material(Shader.Find("uLowPassFilter/LowPassFilter"));
                lowpassMaterial.hideFlags = HideFlags.HideAndDontSave;
                lowpassMaterial.SetFloat("_TexWidth", width);
                lowpassMaterial.SetInt("_is9tap", width == 3840 ? 1 : 0);
                _eyeCamLowpassMaterial[type] = lowpassMaterial;
            }
        }

        ~SRDEyeViewRenderer()
        {
        }

        private void Initialize()
        {
            _srdCameras.ToggleWatcherCamera(true);
            foreach(var type in _eyeTypes)
            {
                _eyeCamera[type] = _srdCameras.GetEyeCamera(type);
                _eyeCamera[type].targetTexture = _eyeCamRenderTextureCache[type];

                var eyeAnchorName = SRDHelper.EyeSideName[type] + SRDHelper.SRDConstants.EyeAnchorGameObjDefaultName;
                var eyeAnchor = SRDSceneEnvironment.GetOrCreateChild(_srdCameras.AnchorTransform, eyeAnchorName);
                _eyeTransform[type] = eyeAnchor.transform;
            }
            _srdCameras.ToggleWatcherCamera(false);
        }

        private void SetupCameraUpdateCallback(EyeType type)
        {
            var eyeCamera = _eyeCamera[type];
            var eyeTransform = _eyeTransform[type];
            var homographyMaterial = _eyeCamMaterial[type];

            Action<Camera> updateState = (camera) =>
            {
                _faceTracker.GetCurrentFacePose(out _currentFacePose);
                var eyePose = _currentFacePose.GetEyePose(type);
                eyeTransform.SetPositionAndRotation(eyePose.position, eyePose.rotation);

                _faceTracker.GetCurrentProjMatrix(eyeCamera.nearClipPlane, eyeCamera.farClipPlane,
                                                  out _currentProjMat);
                var projMat = _currentProjMat.GetProjectionMatrix(type);

                if (!SRDHelper.HasNanOrInf(projMat))
                {
                    eyeCamera.ResetProjectionMatrix();
                    eyeCamera.fieldOfView = CalcVerticalFoVFromProjectionMatrix(projMat);
                    eyeCamera.aspect = CalcAspectWperHFromProjectionMatrix(projMat);
                    eyeCamera.projectionMatrix = projMat;

                    if (_isBoxFrontClippingCache)
                    {
                        var bodyBounds = _srdManager.Settings.DeviceInfo.BodyBounds;

                        Quaternion displayRotation = Quaternion.Euler((45.0f - ((_srdManager.IsWallmountMode ? 90.0f : 45.0f) + _srdManager.TiltDegree)) * Vector3.left);

                        var LeftBottomPositon = Quaternion.Inverse(displayRotation) * (bodyBounds.LeftBottom / bodyBounds.ScaleFactor);
                        var LeftTopPositon = Quaternion.Inverse(displayRotation) * (bodyBounds.LeftUp / bodyBounds.ScaleFactor);

                        float clipPlaneOffset = Mathf.Max(Mathf.Abs(LeftTopPositon.z - ObliquedNearClipOffset), 
                            SRDProjectSettings.GetMutlipleDisplayMode() == SRDProjectSettings.MultiSRDMode.SingleDisplay ? .10545f : .168f);

                        var clipPlanePos = new Vector3(LeftBottomPositon.x, LeftBottomPositon.y, 
                            Mathf.Max(LeftBottomPositon.z, LeftTopPositon.z) - clipPlaneOffset) * _srdManager.SRDViewSpaceScale;

                        clipPlanePos = _srdManager.transform.rotation * clipPlanePos + _srdManager.transform.position;

                        var tiltedRotation = _srdManager.transform.rotation * displayRotation;
                        Vector3 nearClipForward = tiltedRotation * Vector3.forward;

                        eyeCamera.projectionMatrix = CalcObliquedNearClipProjectionMatrix(eyeCamera, nearClipForward, clipPlanePos);
                    }
                }
                if (!_srdManager.IsLensShiftEnabled) {
                    var homographyMat = SRDHelper.CalcHomographyMatrix(_srdManager.DisplayEdges.LeftUp.position, _srdManager.DisplayEdges.LeftBottom.position,
                                                                       _srdManager.DisplayEdges.RightBottom.position, _srdManager.DisplayEdges.RightUp.position,
                                                                       eyeCamera);
                    var invHomographyMat = SRDHelper.CalcInverseMatrix3x3(homographyMat);
                    homographyMaterial.SetFloatArray("_Homography", invHomographyMat);
                    homographyMaterial.SetFloatArray("_InvHomography", homographyMat);
                }
            };

            if (_isSRPUsed)
            {
#if SRP_AVAILABLE
                var hookFrameRendering = (SRDHelper.renderPipelineType == RenderPipelineType.HDRP);
#if WORKAROUND_ISSUE1318629
                hookFrameRendering |= (SRDHelper.renderPipelineType == RenderPipelineType.URP);
#endif
                if (hookFrameRendering)
                {
                    SRPCallbackFuncForFrameRendering srpCallback = (context, cameras) =>
                    {
                        foreach (Camera camera in cameras)
                        {
                            if (camera.name != eyeCamera.name)
                            {
                                continue;
                            }
                            updateState(camera);
                        }
                    };
                    _frameSRPPreCallback[type] = srpCallback;
#if UNITY_2021_1_OR_NEWER
                    RenderPipelineManager.beginContextRendering += _frameSRPPreCallback[type];
#else
                    RenderPipelineManager.beginFrameRendering += _frameSRPPreCallback[type];
#endif
                }
                else
                {
                    SRPCallbackFuncForCameraRendering srpCallback = (context, camera) =>
                    {
                        if (camera.name != eyeCamera.name)
                        {
                            return;
                        }
                        updateState(camera);
                    };
                    _eyeCamSRPPreCallback[type] = srpCallback;
                    RenderPipelineManager.beginCameraRendering += _eyeCamSRPPreCallback[type];
                }
#endif
            }
            else
            {
                Camera.CameraCallback cameraStateUpdate = (camera) =>
                {
                    if (camera.name != eyeCamera.name)
                    {
                        return;
                    }
                    updateState(camera);
                };
                _eyeCamStateUpdateCallback[type] = cameraStateUpdate;
                // This Should be onPreCull for correct frustum culling, however onPreCull is fired before vblank sometimes.
                // That's why onPreRender is used to make the latency shorter as possible
                Camera.onPreRender += _eyeCamStateUpdateCallback[type];
            }
        }

        private void SetupHomographyCallback(EyeType type)
        {
            var eyeCamera = _eyeCamera[type];
            var homographyMaterial = _eyeCamMaterial[type];
            var lowpassFilterMaterial = _eyeCamLowpassMaterial[type];

            if (_isSRPUsed)
            {
#if SRP_AVAILABLE
                var hookFrameRendering = (SRDHelper.renderPipelineType == RenderPipelineType.HDRP);
                if (hookFrameRendering)
                {
                    SRPCallbackFuncForFrameRendering srpCallback = (context, cameras) =>
                    {
                        foreach (Camera camera in cameras)
                        {
                            if (camera.name != eyeCamera.name)
                            {
                                continue;
                            }

                            if (_srdManager.IsLensShiftEnabled)
                            {
                                if (!_srdManager.IsPerformancePriorityEnabled)
                                {
                                    var rt = RenderTexture.GetTemporary(_eyeCamera[type].targetTexture.descriptor);
                                    Graphics.Blit(_eyeCamera[type].targetTexture, rt, lowpassFilterMaterial);
                                    Graphics.Blit(rt, _eyeCamera[type].targetTexture);
                                    RenderTexture.ReleaseTemporary(rt);
                                }
                            }
                            else
                            {
                                var rt = RenderTexture.GetTemporary(_eyeCamera[type].targetTexture.descriptor);
                                Graphics.Blit(_eyeCamera[type].targetTexture, rt, homographyMaterial);
                                if (!_srdManager.IsPerformancePriorityEnabled)
                                {
                                    Graphics.Blit(rt, _eyeCamera[type].targetTexture, lowpassFilterMaterial);
                                }
                                else
                                {
                                    Graphics.Blit(rt, _eyeCamera[type].targetTexture);
                                }
                                RenderTexture.ReleaseTemporary(rt);
                            }
                        }
                    };
                    _frameSRPPostCallback[type] = srpCallback;
#if UNITY_2021_1_OR_NEWER
                    RenderPipelineManager.endContextRendering += _frameSRPPostCallback[type];
#else
                    RenderPipelineManager.endFrameRendering += _frameSRPPostCallback[type];
#endif
                }
                else
                {
                    SRPCallbackFuncForCameraRendering srpCallback = (context, camera) =>
                    {
                        if (camera.name != eyeCamera.name)
                        {
                            return;
                        }

                        if (_srdManager.IsLensShiftEnabled)
                        {
                            if (!_srdManager.IsPerformancePriorityEnabled)
                            {
                                var rt = RenderTexture.GetTemporary(_eyeCamera[type].targetTexture.descriptor);
                                Graphics.Blit(_eyeCamera[type].targetTexture, rt, lowpassFilterMaterial);
                                Graphics.Blit(rt, _eyeCamera[type].targetTexture);
                                RenderTexture.ReleaseTemporary(rt);
                            }
                        }
                        else
                        {
                            var rt = RenderTexture.GetTemporary(_eyeCamera[type].targetTexture.descriptor);
                            Graphics.Blit(_eyeCamera[type].targetTexture, rt, homographyMaterial);
                            if (!_srdManager.IsPerformancePriorityEnabled)
                            {
                                Graphics.Blit(rt, _eyeCamera[type].targetTexture, lowpassFilterMaterial);
                            }
                            else
                            {
                                Graphics.Blit(rt, _eyeCamera[type].targetTexture);
                            }
                            RenderTexture.ReleaseTemporary(rt);
                        }
                    };
                    _eyeCamSRPPostCallback[type] = srpCallback;
                    RenderPipelineManager.endCameraRendering += _eyeCamSRPPostCallback[type];
                }
#endif
            }
            else
            {
                // CommandBuffer
                var camEvent = CameraEvent.AfterImageEffects;
                var buf = new CommandBuffer();
                buf.name = SRDHelper.SRDConstants.HomographyCommandBufferName;
                foreach(var attachedBuf in _eyeCamera[type].GetCommandBuffers(camEvent))
                {
                    if(attachedBuf.name == buf.name)
                    {
                        _eyeCamera[type].RemoveCommandBuffer(camEvent, attachedBuf);
                        break;
                    }
                }

                if (_srdManager.IsLensShiftEnabled)
                {
                    if (!_srdManager.IsPerformancePriorityEnabled)
                    {
                        var rt = Shader.PropertyToID("_Temp");
                        buf.GetTemporaryRT(rt, -1, -1, 0, FilterMode.Bilinear);
                        buf.Blit(_eyeCamera[type].targetTexture, rt, lowpassFilterMaterial);
                        buf.Blit(rt, _eyeCamera[type].targetTexture);
                        buf.ReleaseTemporaryRT(rt);
                    }
                }
                else
                {
                    var rt = Shader.PropertyToID("_Temp_homography");
                    buf.GetTemporaryRT(rt, -1, -1, 0, FilterMode.Bilinear);
                    buf.Blit(_eyeCamera[type].targetTexture, rt, homographyMaterial);

                    if (!_srdManager.IsPerformancePriorityEnabled)
                    {
                        buf.Blit(rt, _eyeCamera[type].targetTexture, lowpassFilterMaterial);
                    }
                    else
                    {
                        buf.Blit(rt, _eyeCamera[type].targetTexture);
                    }
                    buf.ReleaseTemporaryRT(rt);
                }
                _eyeCamera[type].AddCommandBuffer(camEvent, buf);
            }
        }

        public SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive)
        {
            _faceTracker = faceTracker;
            var xrResult = _faceTracker.GetCurrentFacePose(out var facePose);
            _srdCameras.AnchorTransform.SetPositionAndRotation(facePose.HeadPose.position,
                                                               facePose.HeadPose.rotation);

            var headCamera = _srdCameras.WatcherCamera;
            _faceTracker.GetCurrentProjMatrix(headCamera.nearClipPlane, headCamera.farClipPlane,
                                              out var faceProjMat);
            var projMat = faceProjMat.HeadMatrix;
            if (!SRDHelper.HasNanOrInf(projMat))
            {
                headCamera.ResetProjectionMatrix();
                headCamera.fieldOfView = CalcVerticalFoVFromProjectionMatrix(projMat);
                headCamera.aspect = CalcAspectWperHFromProjectionMatrix(projMat);
                headCamera.projectionMatrix = projMat;
            }

            _isBoxFrontClippingCache = isBoxFrontNearClipActive;
            return xrResult;
        }

        public static Matrix4x4 CalcLensShiftProjectionMatrix(Camera cam, Utils.DisplayEdges edges)
        {
            Transform v0 = edges.RightUp;
            Transform v1 = edges.LeftUp;
            Transform v2 = edges.LeftBottom;
            Transform v3 = edges.RightBottom;

            Vector3 v0Camera = cam.worldToCameraMatrix.MultiplyPoint(v0.position);
            Vector3 v2Camera = cam.worldToCameraMatrix.MultiplyPoint(v2.position);

            float nearScale = -cam.nearClipPlane / v2Camera.z;
            float farScale = -cam.farClipPlane / v2Camera.z;


            float bottom = v2Camera.y * nearScale;
            float left = v2Camera.x * nearScale;
            float right = v0Camera.x * nearScale;
            float top = v0Camera.y * nearScale;
            float zFar = -v2Camera.z * farScale;
            float zNear = -v2Camera.z * nearScale;

            return Matrix4x4.Frustum(left, right, bottom, top, zNear, zFar);
        }

        public static Matrix4x4 CalcObliquedNearClipProjectionMatrix(Camera cam, Vector3 obliquedNearClipNormalVecInWorldCoord, Vector3 obliquedNearClipIncludedPointInWorldCoord)
        {
            var worldToCameraMatrix = cam.worldToCameraMatrix;
            var normalVecInCamCoord = worldToCameraMatrix.MultiplyVector(obliquedNearClipNormalVecInWorldCoord);
            var centerPosInCamCoord = worldToCameraMatrix.MultiplyPoint(obliquedNearClipIncludedPointInWorldCoord);
            var clipPlane = new Vector4(normalVecInCamCoord.x, normalVecInCamCoord.y, normalVecInCamCoord.z, -Vector3.Dot(normalVecInCamCoord, centerPosInCamCoord));
            return cam.CalculateObliqueMatrix(clipPlane);
        }

        public static float CalcVerticalFoVFromProjectionMatrix(Matrix4x4 projMat)
        {
            return Mathf.Rad2Deg * 2 * Mathf.Atan(1 / projMat.m11);
        }

        public static float CalcAspectWperHFromProjectionMatrix(Matrix4x4 projMat)
        {
            return projMat.m11 / projMat.m00;
        }

        public void Render()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamera[type].Render();
            }
        }

        public Texture GetLeftEyeViewTexture()
        {
            return _eyeCamera[Utils.EyeType.Left].targetTexture;
        }
        public Texture GetRightEyeViewTexture()
        {
            return _eyeCamera[Utils.EyeType.Right].targetTexture;
        }

        public void Start()
        {
            Initialize();

            foreach(var type in _eyeTypes)
            {
                SetupCameraUpdateCallback(type);
                SetupHomographyCallback(type);
            }
        }

        public void Stop()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamera[type].targetTexture.Release();

                if(_isSRPUsed)
                {
#if SRP_AVAILABLE
                    if (_eyeCamSRPPreCallback.ContainsKey(type))
                        RenderPipelineManager.beginCameraRendering -= _eyeCamSRPPreCallback[type];
                    if (_eyeCamSRPPostCallback.ContainsKey(type))
                        RenderPipelineManager.endCameraRendering -= _eyeCamSRPPostCallback[type];
#if UNITY_2021_1_OR_NEWER
                    if (_frameSRPPreCallback.ContainsKey(type))
                        RenderPipelineManager.beginContextRendering -= _frameSRPPreCallback[type];
                    if (_frameSRPPostCallback.ContainsKey(type))
                        RenderPipelineManager.endContextRendering -= _frameSRPPostCallback[type];
#else
                    if (_frameSRPPreCallback.ContainsKey(type))
                        RenderPipelineManager.beginFrameRendering -= _frameSRPPreCallback[type];
                    if (_frameSRPPostCallback.ContainsKey(type))
                        RenderPipelineManager.endFrameRendering -= _frameSRPPostCallback[type];
#endif
#endif
                }
                else
                {
                    Camera.onPreRender -= _eyeCamStateUpdateCallback[type];
                }
            }
        }

        public void Dispose()
        {
            foreach(var type in _eyeTypes)
            {
                _eyeCamRenderTextureCache[type].Release();
                UnityEngine.Object.Destroy(_eyeCamRenderTextureCache[type]);
            }
        }
    }

    internal class SRDTexturesBasedEyeViewRenderer : ISRDEyeViewRenderer
    {
        private Texture2D _leftTexture;
        private Texture2D _rightTexture;
        private SRDStereoTexture _stereoTextureIO;

        private readonly float DefaultNearClip = 0.3f;
        private readonly float DefaultFarClip = 100.0f;

        public SRDTexturesBasedEyeViewRenderer(SRDManager srdManager, Texture2D leftTexture, Texture2D rightTexture)
        {
            var texWidth = srdManager.Settings.DeviceInfo.ScreenRect.Width;
            var texHeight = srdManager.Settings.DeviceInfo.ScreenRect.Height;
            _leftTexture = new Texture2D(texWidth, texHeight);
            _rightTexture = new Texture2D(texWidth, texHeight);
#if UNITY_2023_1_OR_NEWER
            _stereoTextureIO = UnityEngine.Object.FindFirstObjectByType<SRDStereoTexture>();
#else
            _stereoTextureIO = UnityEngine.Object.FindObjectOfType<SRDStereoTexture>();
#endif
            if(_stereoTextureIO)
            {
                UpdateTextures();
                return;
            }
            if(leftTexture != null && rightTexture != null)
            {
                Graphics.ConvertTexture(leftTexture, _leftTexture);
                Graphics.ConvertTexture(rightTexture, _rightTexture);
            }
        }

        ~SRDTexturesBasedEyeViewRenderer()
        {
        }

        public SrdXrResult UpdateFacePose(ISRDFaceTracker faceTracker, bool isBoxFrontNearClipActive)
        {
            if(_stereoTextureIO == null)
            {
#if UNITY_2023_1_OR_NEWER
                _stereoTextureIO = UnityEngine.Object.FindFirstObjectByType<SRDStereoTexture>();
#else
                _stereoTextureIO = UnityEngine.Object.FindObjectOfType<SRDStereoTexture>();
#endif
            }

            if(_stereoTextureIO && _stereoTextureIO.Changed)
            {
                UpdateTextures();
            }
            return SrdXrResult.SUCCESS;
        }

        private void UpdateTextures()
        {
            if(_stereoTextureIO.leftTexture && _stereoTextureIO.rightTexture)
            {
                Graphics.ConvertTexture(_stereoTextureIO.leftTexture, _leftTexture);
                Graphics.ConvertTexture(_stereoTextureIO.rightTexture, _rightTexture);
            }
            _stereoTextureIO.ResolveChanges();
        }

        public void Render()
        {
            //do nothing
        }
        public Texture GetLeftEyeViewTexture()
        {
            return _leftTexture;
        }
        public Texture GetRightEyeViewTexture()
        {
            return _rightTexture;
        }

        public float GetNearClip()
        {
            return DefaultNearClip;
        }

        public float GetFarClip()
        {
            return DefaultFarClip;
        }

        public void Start()
        {
            // do nothing
        }

        public void Stop()
        {
            // do nothing
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}


