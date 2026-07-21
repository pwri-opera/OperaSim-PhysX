/*
 * Copyright 2019-2025 Sony Corporation
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

using SRD.Utils;


namespace SRD.Core
{
    /// <summary>
    /// A core component for Spatial Reality Display that manages the session with SRD Runtime.
    /// </summary>
    //[ExecuteInEditMode]
    //[DisallowMultipleComponent]
    public class SRDManager : MonoBehaviour
    {
        /// <summary>
        /// A flag for SR Rendering
        /// </summary>
        /// <remarks>
        /// If this is disable, SR Rendering is turned off.
        /// </remarks>
        [Tooltip("If this is disable, SR Rendering is turned off.")]
        public bool IsSRRenderingActive = true;

        private bool prevIsSRRenderingActive = false;

        /// <summary>
        /// A flag for Spatial Clipping
        /// </summary>
        /// <remarks>
        /// If this is disable, the spatial clipping is turned off.
        /// </remarks>
        [Tooltip("If this is disable, the spatial clipping is turned off.")]
        public bool IsSpatialClippingActive = true;

        /// <summary>
        /// A flag for Crosstalk Correction
        /// </summary>
        /// <remarks>
        /// If this is disable, the crosstalk correction is turned off.
        /// </remarks>
        [Tooltip("ELF-SR1 exclusive function. If this is disable, the crosstalk correction is turned off.")]
        public bool IsCrosstalkCorrectionActive = true;

        /// <summary>
        /// Crosstalk Correction Type
        /// </summary>
        /// <remarks>
        /// This is valid only if the crosstalk correction is active.
        /// </remarks>
        [Tooltip("ELF-SR1 exclusive function. This is valid only if the crosstalk correction is active.")]
        public SrdXrCrosstalkCorrectionType CrosstalkCorrectionType;

        private SRDCrosstalkCorrection _srdCrosstalkCorrection;

        /// <summary>
        /// A flag for specify display method
        /// </summary>
        /// <remarks>
        /// If this is able, display method is LensShift. If this is disable, display method is homography.
        /// </remarks>
        [Tooltip("If the display such as shadows becomes strange, please try disabling it.")]
        public bool IsHighImageQualityMode = true;

        private SRDSystemDescription _description;


        public enum ScalingMode
        {
            [InspectorName("Scaled size")]
            ScaledSize,
            [InspectorName("Original size")]
            OriginalSize
        };
        public ScalingMode _scalingMode = ScalingMode.ScaledSize;

        [AttributeUsage(AttributeTargets.Field)]
        private sealed class GizmoSizeSelectionParameterAttribute : PropertyAttribute { }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(GizmoSizeSelectionParameterAttribute))]
        private sealed class GizmoSizeSelectionParameterAttributeDrawer : PropertyDrawer
        {
            private readonly int[] _enumValues;
            private readonly GUIContent[] _enumAppearances;

            public GizmoSizeSelectionParameterAttributeDrawer()
            {
                Int32 size;
                if (!SRDCorePlugin.GetCountOfSupportedDevices(out size))
                {
                    return;
                }

                var panel_specs = new supported_panel_spec[size];
                if(!SRDCorePlugin.GetPanelSpecOfSupportedDevices(panel_specs))
                {
                    return;
                }

                _enumValues = new int[size];
                _enumAppearances = new GUIContent[size];
                for(int i = 0; i < size; ++i)
                {
                    _enumValues[i] = i;
                    _enumAppearances[i] = new GUIContent(panel_specs[i].device_name);
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var isActiveProperty = property.serializedObject.FindProperty("_scalingMode");
                var isActive = (isActiveProperty != null) && (ScalingMode.OriginalSize == (ScalingMode)isActiveProperty.enumValueIndex);
                EditorGUI.BeginDisabledGroup(!isActive);
                EditorGUI.indentLevel++;
                using(new EditorGUI.PropertyScope(position, label, property))
                {
                    property.intValue = EditorGUI.IntPopup(position, label, property.intValue, _enumAppearances, _enumValues);
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();

            }
        }
#endif

        [GizmoSizeSelectionParameter, Tooltip("This is valid only if the Scaling Mode is Original size.")]
        [SerializeField] private int _GIZMOSize = 0;

        /// <summary>
        /// A flag for wallmount mode
        /// </summary>
        /// <remarks>
        /// If this is enable, GIZMO must be tilt 45 degree.
        /// </remarks>
        [Tooltip("If this is enable, wallmount mode is on.")]
        public bool IsWallmountMode = false;

        private bool prevIsWallmountMode = false;

        private const float _minScaleInInspector = 0.1f;
        private const float _maxScaleInInspector = 1000.0f; // float.MaxValue;
        [SerializeField]
        [Range(_minScaleInInspector, _maxScaleInInspector), Tooltip("The scale of SRDisplay View Space")]
        private float _SRDViewSpaceScale = 1.0f;

        /// <summary>
        /// The scale of SRDisplay View Space
        /// </summary>
        public float SRDViewSpaceScale
        {
            get
            {
                if(_SRDViewSpaceScale <= 0)
                {
                    Debug.LogWarning(String.Format("Wrong SRDViewSpaceScale: {0} \n SRDViewSpaceScale must be 0+. Now SRDViewSpaceScale is forced to 1.0.", _SRDViewSpaceScale));
                    _SRDViewSpaceScale = 1.0f;
                }
                return _SRDViewSpaceScale;
            }
            set
            {
                _SRDViewSpaceScale = value;
            }
        }

        #region Events
        /// <summary>
        /// A UnityEvent callback containing SRDisplayViewSpaceScale.
        /// </summary>
        [System.Serializable]
        public class SRDViewSpaceScaleChangedEvent : UnityEvent<float> { };
        /// <summary>
        /// An API of <see cref="SRDManager.SRDViewSpaceScaleChangedEvent"/>. Callbacks that are registered to this are called when SRDViewSpaceScale is changed.
        /// </summary>
        public SRDViewSpaceScaleChangedEvent OnSRDViewSpaceScaleChangedEvent;

        /// <summary>
        /// A UnityEvent callback containing a flag that describe FaceTrack is success or not in this frame.
        /// </summary>
        [System.Serializable]
        public class SRDFaceTrackStateEvent : UnityEvent<bool> { };
        /// <summary>
        /// An API of <see cref="SRDManager.SRDFaceTrackStateEvent"/>. Callbacks that are registered to this are called in every frame.
        /// </summary>
        public SRDFaceTrackStateEvent OnFaceTrackStateEvent;
        #endregion

        private SRDSession _session = null;

        private Transform _presence;
        /// <summary>
        /// Transform of presence in real world. </param>
        /// </summary>
        public Transform Presence { get { return _presence; } }

        private Utils.DisplayEdges _displayEdges;
        /// <summary>
        /// Contains the positions of Spatial Reality Display edges and center.
        /// </summary>
        public Utils.DisplayEdges DisplayEdges { get { return _displayEdges; } }

        private Coroutine _srRenderingCoroutine = null;

        private SRDCoreRenderer _srdCoreRenderer;
        internal SRDCoreRenderer SRDCoreRenderer { get { return _srdCoreRenderer; } }

        private RenderTexture _mainRenderTexture;
        internal RenderTexture MainRenderTexture 
        {
            get
            {
                return _mainRenderTexture;
            }
        }

        private SRD2DView _srd2dView;
        /// <summary>
        /// Contains an instance of SRD2DView, used to access the 2D view window API.
        /// </summary>
        public SRD2DView SRD2DView 
        {
            get
            {
                return _srd2dView;
            }
        }

        internal SRDSession Session { get { return _session; } }
        internal SRDSettings Settings { get { return _session.Settings; } }

        private bool _isPerformancePriorityEnabled = false;
        internal bool IsPerformancePriorityEnabled { get { return _isPerformancePriorityEnabled; } }
        private bool _isLensShiftEnabled = false;
        internal bool IsLensShiftEnabled { get { return _isLensShiftEnabled; } }
        private float _tilt_degree = 0;
        internal float TiltDegree { get { return _tilt_degree; } }

        private int _deviceIndex = 0;
        internal int DeviceIndex { get { return _deviceIndex; } }

        #region APIs
        /// <summary>
        /// An api to show/remove the CameraWindow
        /// </summary>
        /// <param name="isOn">The flag to show the CameraWindow. If this is true, the CameraWindow will open. If this is false, the CameraWindow will close.</param>
        /// <returns>Success or not.</returns>
        public bool ShowCameraWindow(bool isOn)
        {
            var res = _session.ShowCameraWindow(isOn);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool GetRuntimeVersion(out string version)
        {
            var res = SRDCorePlugin.GetRuntimeVersionString(out version);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool GetDisplayVersion(out string version)
        {
            var res = _session.GetDisplayFirmwareVersion(out version);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool GetRealityCreationLevel(out int level)
        {
            var res = _session.GetRealityCreationLevel(out level);
            return (SrdXrResult.SUCCESS == res);
        }
        public bool SetRealityCreationLevel(int level)
        {
            var res = _session.SetRealityCreationLevel(level);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool GetSensorRangeMode(out int mode)
        {
            var res = _session.GetSensorRangeMode(out mode);
            return (SrdXrResult.SUCCESS == res);
        }
        public bool SetSensorRangeMode(int mode)
        {
            var res = _session.SetSensorRangeMode(mode);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool GetXtalkAdjustParam(out int param)
        {
            var res = _session.GetXtalkAdjustParam(out param);
            return (SrdXrResult.SUCCESS == res);
        }
        public bool SetXtalkAdjustParam(int param)
        {
            var res = _session.SetXtalkAdjustParam(param);
            return (SrdXrResult.SUCCESS == res);
        }

        public bool RestartHeadTracking()
        {
            var res = _session.RestartHeadTracking();
            return (SrdXrResult.SUCCESS == res);
        }

        /// <summary>
        /// Creates a resizable window on the second monitor that is used to render a 2D view of the SRD screen.
        /// </summary>
        /// <returns>Whether the 2D view window was succesfully created or not. This method will fail if there is no secondary monitor connected to the computer.</returns>
        /// <remarks>Use the SRD2DView object to access the 2D view specific API.</remarks>
        public bool Init2DView()
        {
            if (_srd2dView)
            {
                return true;
            }

            var srd2dView = SRDSceneEnvironment.GetOrAddComponent<SRD2DView>(SRDSceneEnvironment.GetOrCreateChild(transform, SRDHelper.SRDConstants.SRD2DViewGameObjDefaultName));

            var success = srd2dView.Init(this);
            if (success)
            {
                _srd2dView = srd2dView;
                
                RegisterTargetDisplay(SRDApplicationWindow.SRDTargetDisplay);
            }
            else
            {
                Destroy(srd2dView);
            }

            return success;
        }
        #endregion

        #region MainFlow

        void Awake()
        {
            if (!SRDSessionHandler.Instance.Active)
            {
                this.gameObject.SetActive(false);
                return;
            }

            if (SRDProjectSettings.GetMutlipleDisplayMode() != SRDProjectSettings.MultiSRDMode.SingleDisplay || SRDApplicationWindow.NumberOfConnectedDevices > 1)
            {
                var multiDisplayControllerGameObject = SRDSceneEnvironment.GetOrCreateChild(null, SRDHelper.SRDConstants.MultiDisplayControllerDefaultName);
                var multiDisplayController = SRDSceneEnvironment.GetOrAddComponent<SRDMultiDisplayController>(multiDisplayControllerGameObject);

                _deviceIndex = multiDisplayController.CurrentIndex;
#if UNITY_EDITOR
                SRDCorePlugin.SelectDevice((uint)_deviceIndex);
#else
                SRDCorePlugin.SelectDevice(SRDApplicationWindow.DeviceIndexToSrdID[_deviceIndex]);
#endif
            }

            _session = SRDSessionHandler.Instance.AllocateSession();
            if (_session == null)
            {
                SRDSessionHandler.Instance.Active = false;
                this.gameObject.SetActive(false);
                return;
            }

            UpdateSettings();
            foreach (var cond in GetForceQuitConditions())
            {
                if (cond.IsForceQuit)
                {
                    ForceQuitWithAssertion(cond.ForceQuitMessage);
                }
            }

            if (!SRDProjectSettings.IsRunWithoutSRDisplayMode() &&
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 &&
                !SRDCorePlugin.IsDirect3D12Supported())
            {
                Debug.LogAssertion($"The graphics API is set to Direct3D12, but the currently installed runtime does not support it.");
                SRDHelper.PopupMessageAndForceToTerminate(SRDHelper.SRDMessages.FunctionUnsupportedError(requiredVersion: "2.4.0"));
                return;
            }

            if (SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                _description = new SRDSystemDescription(FaceTrackerSystem.Mouse,
                                                        EyeViewRendererSystem.UnityRenderCam,
                                                        StereoCompositerSystem.PassThrough);
            }
            else
            {
                _description = new SRDSystemDescription(FaceTrackerSystem.SRD,
                                                        EyeViewRendererSystem.UnityRenderCam,
                                                        StereoCompositerSystem.SRD);
            }

            _srdCrosstalkCorrection = new SRDCrosstalkCorrection();
            _srdCoreRenderer = new SRDCoreRenderer(_description, this);

            {
                var presenceName = SRDHelper.SRDConstants.PresenceGameObjDefaultName;
                var presenceObj = SRDSceneEnvironment.GetOrCreateChild(this.transform, presenceName);
                _presence = presenceObj.transform;
                _presence.localPosition = Vector3.zero;
                _presence.localRotation = Quaternion.identity;
                _presence.localScale = Vector3.one;
            }

            _displayEdges = new Utils.DisplayEdges(_presence);

            _srdCoreRenderer.OnSRDFaceTrackStateEvent += (bool result) =>
            {
#if DEVELOPMENT_BUILD
                //Debug.LogWarning("No data from FaceRecognition: See the DebugWindow with F10");
#endif
                if(OnFaceTrackStateEvent != null)
                {
                    OnFaceTrackStateEvent.Invoke(result);
                }
            };

        }

        void OnEnable()
        {
            if (!_session.WaitForRunningState())
            {
                return;
            }

            if (_scalingMode == ScalingMode.ScaledSize)
            {
                _presence.localScale /= _session.Settings.DeviceInfo.BodyBounds.ScaleFactor;
            }
            _displayEdges.FitToBodyBounds(_session.Settings.DeviceInfo.BodyBounds);

            _session.SetColorSpaceSettings(QualitySettings.activeColorSpace, SystemInfo.graphicsDeviceType, SRDHelper.renderPipelineType);
            _session.GetPerformancePriorityEnabled(out _isPerformancePriorityEnabled);
            _session.SetLensShiftEnabled(IsHighImageQualityMode);
            _session.GetLensShiftEnabled(out _isLensShiftEnabled);
            _srdCrosstalkCorrection.Init(_session, ref IsCrosstalkCorrectionActive, ref CrosstalkCorrectionType);

            _srdCoreRenderer.Start();
            _session.EnableStereo(IsSRRenderingActive);
            StartSRRenderingCoroutine();
        }

        void OnDisable()
        {
            StopSRRenderingCoroutine();
            _session?.EnableStereo(false);
            _srdCoreRenderer?.Stop();
        }

        void Update()
        {
            if(!_session.CheckSystemError())
            {
                return;
            }

            if (prevIsSRRenderingActive != IsSRRenderingActive)
            {
                _session.EnableStereo(IsSRRenderingActive);
                prevIsSRRenderingActive = IsSRRenderingActive;
            }
            UpdateTiltIfNeeded();
            UpdateScaleIfNeeded();
            _srdCrosstalkCorrection.HookUnityInspector(ref IsCrosstalkCorrectionActive, ref CrosstalkCorrectionType);
        }

        void OnValidate()
        {
            UpdateScaleIfNeeded();
        }

        void LateUpdate()
        {
            _srdCoreRenderer.Update(_presence.transform, IsSpatialClippingActive);
        }

        void OnDestroy()
        {
            _srdCoreRenderer?.Dispose();
        }

#endregion

#region RenderingCoroutine

        internal void RegisterTargetDisplay(int targetDisplay)
        {
            var mainView = SRDSceneEnvironment.GetOrCreateChild(transform, SRDHelper.SRDConstants.MainViewGameObjDefaultName);

            var canvas = SRDSceneEnvironment.GetOrAddComponent<Canvas>(mainView);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = targetDisplay;

            var canvasScaler = SRDSceneEnvironment.GetOrAddComponent<CanvasScaler>(mainView);
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var sideBySideObj = SRDSceneEnvironment.GetOrCreateChild(mainView.transform, SRDHelper.SRDConstants.SideBySideGameObjDefaultName);
            var mainViewImage = SRDSceneEnvironment.GetOrAddComponent<RawImage>(sideBySideObj);

            var screenRect = _session.Settings.DeviceInfo.ScreenRect;
            if (_mainRenderTexture == null)
            {
                var bufferFormat = SRDCorePlugin.IsARGBHalfSupported() ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                _mainRenderTexture = new RenderTexture(screenRect.Width, screenRect.Height, 24, bufferFormat,
                                              (QualitySettings.desiredColorSpace == ColorSpace.Linear) ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
                _mainRenderTexture.Create();
            }

            mainViewImage.texture = _mainRenderTexture;
            _srdCoreRenderer.RegisterOutputTexture(_mainRenderTexture);

            var aspectRatioFitter = SRDSceneEnvironment.GetOrAddComponent<AspectRatioFitter>(mainViewImage.gameObject);
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectRatioFitter.aspectRatio = (float)screenRect.Width / screenRect.Height;
        }
        
        private void StartSRRenderingCoroutine()
        {
            if(_srRenderingCoroutine == null)
            {
                _srRenderingCoroutine = StartCoroutine(SRRenderingCoroutine());
            }
        }

        private void StopSRRenderingCoroutine()
        {
            if(_srRenderingCoroutine != null)
            {
                StopCoroutine(_srRenderingCoroutine);
                _srRenderingCoroutine = null;
            }
        }

        private IEnumerator SRRenderingCoroutine()
        {
            var yieldEndOfFrame = new WaitForEndOfFrame();
            while(true)
            {
                yield return yieldEndOfFrame;
                _srdCoreRenderer.Composite();
            }
        }
#endregion


#region Utils

        private void UpdateSettings()
        {
            QualitySettings.maxQueuedFrames = 0;
        }

        private void UpdateScaleIfNeeded()
        {
            var viewSpaceScale = Vector3.one * SRDViewSpaceScale;
            if (this.transform.localScale != viewSpaceScale)
            {
                this.transform.localScale = viewSpaceScale;

                if(OnSRDViewSpaceScaleChangedEvent != null)
                {
                    OnSRDViewSpaceScaleChangedEvent.Invoke(SRDViewSpaceScale);
                }
            }
        }

        private void UpdateTiltIfNeeded()
        {
            bool needsUpdate = false;

            if (_session.GetSystemTiltDegree(out int degree) == SrdXrResult.SUCCESS)
            {
                if (TiltDegree != degree)
                {
                    _tilt_degree = degree;
                    needsUpdate = true;
                }
            }

            if (prevIsWallmountMode != IsWallmountMode)
            {
                var result = _session.SetForce90Degree(IsWallmountMode);
                if (result == SrdXrResult.SUCCESS)
                {
                    prevIsWallmountMode = IsWallmountMode;
                    needsUpdate = true;
                }
                else
                {
                    IsWallmountMode = prevIsWallmountMode;
#if UNITY_EDITOR
                    if (result == SrdXrResult.ERROR_FUNCTION_UNSUPPORTED)
                    {
                        Debug.LogWarning("Wall Mount mode is not supported for this device");
                    }
#endif
                }
            }

            if (needsUpdate)
            {
                Vector3 defaultAngle = this.transform.eulerAngles;
                defaultAngle.x = -_tilt_degree + (IsWallmountMode ? -45 : 0);
                this.transform.eulerAngles = defaultAngle;
            }
        }

        struct ForceQuitCondition
        {
            public bool IsForceQuit;
            public string ForceQuitMessage;
            public ForceQuitCondition(bool isForceQuit, string forceQuitMessage)
            {
                IsForceQuit = isForceQuit;
                ForceQuitMessage = forceQuitMessage;
            }
        }

        private List<ForceQuitCondition> GetForceQuitConditions()
        {
            var ret = new List<ForceQuitCondition>();

            var isGraphicsAPINotSupported = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore &&
                                            SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 &&
                                            SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;
            ret.Add(new ForceQuitCondition(isGraphicsAPINotSupported,
                                           "Select unsupported GraphicsAPI: GraphicsAPI must be DirectX11, DirectX12 or OpenGLCore."));

            var isSRPNotSupportedVersion = false;
#if !UNITY_2019_1_OR_NEWER
            isSRPNotSupportedVersion = (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null);
#endif
            ret.Add(new ForceQuitCondition(isSRPNotSupportedVersion,
                                           "SRP in Spatial Reality Display is supported in over 2019.1 only"));
            return ret;
        }

        private void ForceQuitWithAssertion(string assertionMessage)
        {
            Debug.LogAssertion(assertionMessage);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region GIZMO
        private void OnDrawGizmos()
        {
            if(!this.enabled)
            {
                return;
            }

            // Draw SRDisplay View Space
            Utils.SRDSettings.BodyBounds bodyBounds;
            if (_scalingMode == ScalingMode.ScaledSize)
            {
                bodyBounds = Utils.SRDSettings.SRDDeviceInfo.DefaultBodyBounds;

            }
            else if (_scalingMode == ScalingMode.OriginalSize)
            {
                Int32 size;
                if (!SRDCorePlugin.GetCountOfSupportedDevices(out size))
                {
                    return;
                }

                var panel_specs = new supported_panel_spec[size];
                if (!SRDCorePlugin.GetPanelSpecOfSupportedDevices(panel_specs))
                {
                    return;
                }

                if ((_GIZMOSize < 0) || (size <= _GIZMOSize))
                {
                    return;
                }

                var width = panel_specs[_GIZMOSize].width;
                var height = panel_specs[_GIZMOSize].height;

                var panelRect = new Vector2(width, height);
                bodyBounds = new Utils.SRDSettings.BodyBounds(panelRect, panel_specs[_GIZMOSize].angle);
            }
            else
            {
                return;
            }

            var multiDisplayMode = SRDProjectSettings.GetMutlipleDisplayMode();
            Gizmos.matrix = transform.localToWorldMatrix;

            var isForceWallMode = IsWallmountMode || multiDisplayMode == SRDProjectSettings.MultiSRDMode.MultiVertical || multiDisplayMode == SRDProjectSettings.MultiSRDMode.MultiGrid;
            var isBoxGizmo = multiDisplayMode != SRDProjectSettings.MultiSRDMode.SingleDisplay && isForceWallMode;
            var rotationAngle = isBoxGizmo ? -45.0f : 0f;

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(transform.rotation.eulerAngles - rotationAngle * Vector3.right), transform.localScale);
            }
            else
#endif
            if (IsWallmountMode && !isBoxGizmo)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(transform.rotation.eulerAngles - 45 * Vector3.right), transform.localScale);
            }

            float scaleToSR2 = (bodyBounds.Width / SRDMultiDisplayController.SR2PanelWidth);

            float boxDepth = isBoxGizmo ? (0.168f + 0.287f) / 2 * bodyBounds.ScaleFactor : 0;
            Vector3 boxCenterPos = isBoxGizmo ? (0.287f - 0.168f) / 2 * scaleToSR2 * Vector3.forward : Vector3.zero;
            Quaternion displayRotation = Quaternion.Euler(rotationAngle * Vector3.left);

            var positions = SRDMultiDisplayController.SRDManagerPositions[multiDisplayMode];
            for (int num = 0; num < Math.Min(SRDProjectSettings.GetNumberOfDevices(), positions.Length); num++)
            {
                DrawManagerBoxGizmo(bodyBounds, positions[num] * scaleToSR2, boxCenterPos, displayRotation, boxDepth);
            }
        }

        private void DrawManagerBoxGizmo(Utils.SRDSettings.BodyBounds bodyBounds, Vector3 positionShift, Vector3 boxCenterPos, Quaternion displayRotation, float boxDepth)
        {
            bool isCurrent = true;
            if (Application.isPlaying)
            {
                isCurrent = _presence?.localPosition == positionShift;
            }

            Gizmos.color = Color.Lerp(Color.clear, Color.blue, isCurrent ? 1.0f : 0.35f);
            Gizmos.DrawWireCube(Quaternion.Inverse(displayRotation) * (bodyBounds.Center + positionShift) + boxCenterPos, Quaternion.Inverse(displayRotation) * bodyBounds.BoxSize + boxDepth * Vector3.forward);
            Gizmos.color = Color.Lerp(Color.clear, Color.cyan, isCurrent ? 1.0f : 0.35f);
            for (var i = 0; i < 4; i++)
            {
                var from = i % 4;
                var to = (i + 1) % 4;
                Gizmos.DrawLine(Quaternion.Inverse(displayRotation) * (bodyBounds.EdgePositions[from] + positionShift),
                                Quaternion.Inverse(displayRotation) * (bodyBounds.EdgePositions[to] + positionShift));
            }
        }
#endregion
    }

    public enum SrdXrCrosstalkCorrectionType
    {
        GRADATION_CORRECTION_MEDIUM = 0,
        GRADATION_CORRECTION_ALL = 1,
        GRADATION_CORRECTION_HIGH_PRECISE = 2,
    }
}










