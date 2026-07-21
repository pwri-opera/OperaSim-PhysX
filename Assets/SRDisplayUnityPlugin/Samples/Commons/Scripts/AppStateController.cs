/*
 * Copyright 2019,2020,2023,2024 Sony Corporation
 */


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;
using SRD.Utils;

namespace SRD.Sample.Common
{
    [RequireComponent(typeof(TextMesh))]
    internal class AppStateController : MonoBehaviour
    {
        public KeyCode AppExitKey = KeyCode.Escape;
        public KeyCode Toggle2DViewVisibleKey = KeyCode.F5;
        public KeyCode Toggle2DViewFullscreenKey = KeyCode.F6;
        public KeyCode Switch2DViewSourceKey = KeyCode.F7;
        public KeyCode CameraWindowToggleKey = KeyCode.F10;

        private SRDManager _srdManager;
        private SRD2DView _srd2DView;

        private bool _isDebugWindowEnabled = false;
        private static readonly SRD2DView.SRDTextureType[] _srd2DViewSourceValues
            = (SRD2DView.SRDTextureType[])System.Enum.GetValues(typeof(SRD2DView.SRDTextureType));

        void Start()
        {
            this.GetComponent<TextMesh>().text = GetTextToShowKey();
            _srdManager = SRDSceneEnvironment.GetSRDManager();
            if (_srdManager != null)
            {
                _srd2DView = _srdManager?.SRD2DView;
            }
        }

        void OnValidate()
        {
            this.GetComponent<TextMesh>().text = GetTextToShowKey();
        }

        string GetTextToShowKey()
        {
            return string.Format("Press {0} to exit the app", AppExitKey.ToString());
        }

        void Update()
        {
            if(Input.GetKeyDown(AppExitKey))
            {
                if(Application.isPlaying)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }

            if (_srdManager != null)
            {
                if (Input.GetKeyDown(Toggle2DViewVisibleKey))
                {
                    if (_srd2DView == null)
                    {
                        if (_srdManager.Init2DView())
                        {
                            _srd2DView = _srdManager.SRD2DView;
                        }
                    }
                    else
                    {
                        _srd2DView.Show(!_srd2DView.IsVisible);
                    }
                }

                if (_srd2DView != null)
                {
                    if (Input.GetKeyDown(Toggle2DViewFullscreenKey))
                    {
                        _srd2DView.SetFullScreen(!_srd2DView.IsFullScreen);
                    }

                    if (Input.GetKeyDown(Switch2DViewSourceKey))
                    {
                        var srd2DViewSourceIdx = ((int)_srd2DView.SourceTexture + 1);
                        if (_srd2DViewSourceValues[srd2DViewSourceIdx] == SRD2DView.SRDTextureType.Custom)
                        {
                            srd2DViewSourceIdx = 0;
                        }
                        _srd2DView.SetSourceTexture(_srd2DViewSourceValues[srd2DViewSourceIdx]);
                    }
                }
            }

            if(Input.GetKeyDown(CameraWindowToggleKey))
            {
                _isDebugWindowEnabled = !_isDebugWindowEnabled;
                _srdManager.ShowCameraWindow(_isDebugWindowEnabled);
                Debug.Log(string.Format("Debug window: {0}", _isDebugWindowEnabled ? "ON" : "OFF"));
            }
        }
    }
}
