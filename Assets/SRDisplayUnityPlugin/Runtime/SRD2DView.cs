/*
 * Copyright 2024 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SRD.Utils;
using UnityEngine.UI;

namespace SRD.Core
{
    /// <summary>
    /// This class contains the API related to the 2D View window, which is used to display a 2D image of the SRD screen on a secondary monitor.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(Image))]
    public class SRD2DView : MonoBehaviour
    {
        /// <summary>
        /// Type of texture that can be displayed in the 2D View window.
        /// </summary>
        public enum SRDTextureType
        {
            /// <summary>Left eye image</summary>
            LeftEye = 0,
            /// <summary>Right eye image</summary>
            RightEye = 1,
            /// <summary>[ELF-SR2 only] Side by side (left+right) image</summary>
            SideBySide = 2,
            /// <summary>User-defined custom texture</summary>
            Custom = 3,
        }

        private RawImage _rawImage;
        private AspectRatioFitter _aspectRatioFitter;

        private SRDManager _srdManager;
        private SRDCameras _srdCameras;

        private bool IsInitialized
        {
            get
            {
                return SRDApplicationWindow.IsPreviewWindowInitialized;
            }
        }

        #region Public Properties
        [SerializeField]
        private SRDTextureType _sourceTexture;
        /// <summary>
        /// The texture type that is currently being displayed on screen.
        /// </summary>
        public SRDTextureType SourceTexture
        {
            get
            {
                return _sourceTexture;
            }
        }

        private RenderTexture _customTextureCache;
        /// <summary>
        /// The actual texture that will be displayed when SourceTexture is set to Custom.
        /// </summary>
        public RenderTexture CustomTexture
        {
            get
            {
                return _customTextureCache;
            }
            set
            {
                _customTextureCache = value;
                if (_sourceTexture == SRDTextureType.Custom)
                {
                    _rawImage.texture = value;
                    _aspectRatioFitter.aspectRatio = (float)_rawImage.texture.width / _rawImage.texture.height;
                }
            }
        }

        /// <summary>
        /// Whether the 2D View window is in full screen or windowed mode.
        /// </summary>
        public bool IsFullScreen
        {
            get
            {
                return SRDApplicationWindow.IsFullscreen(SRDApplicationWindow.PreviewWindow);
            }
        }

        /// <summary>
        /// Whether the 2D View window is currently visible or not.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return SRDApplicationWindow.IsPreviewWindowVisible;
            }
        }

        /// <summary>
        /// The index of the display associated with the 2D View window.
        /// </summary>
        /// <remarks>To render a Camera or a Canvas to the 2D View screen, you must set its target display to this value.</remarks>
        public static int DisplayIndex
        {
            get
            {
                return SRDApplicationWindow.PreviewWindowTargetDisplay;
            }
        }
        #endregion

        #region UnityEvents
        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.targetDisplay = SRDApplicationWindow.PreviewWindowTargetDisplay;

            var dummyImage = GetComponent<Image>();
            dummyImage.color = Color.black;
            dummyImage.raycastTarget = false;
            
            var rawImageObj = SRDSceneEnvironment.GetOrCreateChild(transform, SRDHelper.SRDConstants.SRD2DViewImageDefaultName);
            _rawImage = SRDSceneEnvironment.GetOrAddComponent<RawImage>(rawImageObj);
            _aspectRatioFitter = SRDSceneEnvironment.GetOrAddComponent<AspectRatioFitter>(rawImageObj);
            _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }

        private void OnDestroy()
        {
            if (IsInitialized)
            {
                SRDApplicationWindow.DeactivateSecondDisplay();
            }
        }
        #endregion

        private IEnumerator InitWindow()
        {
            yield return new WaitForEndOfFrame();
            SRDApplicationWindow.SetPreviewWindowFullScreen(false);

            yield return new WaitForEndOfFrame();
            SRDApplicationWindow.InitPreviewWindow();
        }

        internal bool Init(SRDManager srdManager)
        {
            if (Display.displays.Length < 2)
            {
#if UNITY_EDITOR
                Debug.LogError($"Could not activate 2D View window (Mutli-display rendering is not supported inside the Unity Editor).");
#else
		        Debug.LogError($"Could not activate 2D View window (No secondary display detected).");
#endif
                return false;
            }
            else if (SRDApplicationWindow.NumberOfConnectedDevices > 1)
            {
                Debug.LogError($"The 2D View function is unavailable when in Multi Display or Duplicated Output mode.");
                return false;
            }

            _srdManager = srdManager;

            Debug.Log($"SRD2DView.Init()");
            var success = SRDApplicationWindow.ActivateSecondDisplay();

            if (success)
            {
                _srdCameras = new SRDCameras(_srdManager);
                SetSourceTexture(_sourceTexture);

                StartCoroutine(InitWindow());
            }

            return success;
        }

#region Public API
        /// <summary>
        /// Set which texture to display to the screen.
        /// </summary>
        /// <param name="sourceTexture">Texture type.</param>
        /// <remarks>When setting SourceTexture to Custom, you need to also set the value of the CustomTexture property.</remarks>
        public void SetSourceTexture(SRDTextureType sourceTexture)
        {
            switch (sourceTexture)
            {
                case SRDTextureType.LeftEye:
                    {
                        _rawImage.texture = _srdCameras.LeftEyeCamera.activeTexture;
                        break;
                    }
                case SRDTextureType.RightEye:
                    {
                        _rawImage.texture = _srdCameras.RightEyeCamera.activeTexture;
                        break;
                    }
                case SRDTextureType.SideBySide:
                    {
                        _rawImage.texture = _srdManager.MainRenderTexture;
                        break;
                    }
                case SRDTextureType.Custom:
                    {
                        _rawImage.texture = _customTextureCache;
                        break;
                    }
                default:
                    {
                        _rawImage.texture = null;
                        break;
                    }
            }
            _sourceTexture = sourceTexture;
            _aspectRatioFitter.aspectRatio = (float)_rawImage.texture.width / _rawImage.texture.height;
        }

        /// <summary>
        /// Set whether the 2D View window should be visible or hidden.
        /// </summary>
        /// <param name="show">True: Visible. False: Hidden.</param>
        public void Show(bool show)
        {
            if (!IsInitialized)
            {
                return;
            }
            
            if (show)
            {
                SRDApplicationWindow.RestorePreviewWindow();
            }
            else
            {
                SRDApplicationWindow.HidePreviewWindow();
            }
        }

        /// <summary>
        /// Move the 2D View window to the specified display.
        /// </summary>
        /// <param name="targetDisplay">The display to which the 2D View window will be moved.</param>
        /// <returns>Whether the 2D View window was successfully moved or not.</returns>
        public bool SetTargetDisplay(DisplayInfo targetDisplay)
        {
            if (!IsInitialized)
            {
                return false;
            }

            Debug.Log($"SRD2DView.SetTargetDisplay({targetDisplay})");
            return SRDApplicationWindow.MovePreviewWindowToDisplay(targetDisplay);
        }

        /// <summary>
        /// Set whether to show the 2D View window in full screen or windowed mode.
        /// </summary>
        /// <param name="fullScreen">True: Full screen, False: Windowed.</param>
        /// <returns>Whether the method succeeded or not. This method will return false if the 2D View window is currently hidden.</returns>
        public bool SetFullScreen(bool fullScreen)
        {
            if (!IsInitialized || !IsVisible)
            {
                return false;
            }

            Debug.Log($"SRD2DView.SetFullScreen({fullScreen})");
            SRDApplicationWindow.SetPreviewWindowFullScreen(fullScreen);

            return true;
        }
#endregion
    }
}