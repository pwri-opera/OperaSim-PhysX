/*
 * Copyright 2019,2020,2023,2024 Sony Corporation
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using SRD.Utils;

namespace SRD.Core
{
    internal interface ISRDStereoCompositer : IDisposable
    {
        void Start();
        void Stop();
        bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR);
        void RenderStereoComposition(RenderTexture backBuffer);
    }

    internal class SRDStereoCompositer: ISRDStereoCompositer
    {
        private SrdXrTexture _srdSideBySide;
        private SrdXrTexture _srdOut;
        private RenderTexture _outTexture;

        private Texture _sceneLeft;
        private RenderTexture _sideBySide;
        private Material _leftAndRightToSideBySide;
        private SRDManager _srdManager;

        public SRDStereoCompositer(SRDManager srdManager)
        {
            _leftAndRightToSideBySide = new Material(Shader.Find("Custom/LeftAndRightToSideBySide"));
            _leftAndRightToSideBySide.hideFlags = HideFlags.HideAndDontSave;

            _srdManager = srdManager;
        }

        public bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR)
        {
            if ((renderTextureL == null) || (renderTextureR == null))
            {
                Debug.LogError("RenderTextures are not set. Set renderTextures with RegisterSourceStereoTextures function.");
                return false;
            }

            var width = _srdManager.Settings.DeviceInfo.ScreenRect.Width;
            var height = _srdManager.Settings.DeviceInfo.ScreenRect.Height;

            var div = 1;
            if (SRDSceneEnvironment.GetSRDManager().IsPerformancePriorityEnabled)
            {
                div = 2;
            }
            var bufferFormat = SRDCorePlugin.IsARGBHalfSupported() ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            if (_sideBySide == null)
            {
                var width2 = _srdManager.Settings.DeviceInfo.ScreenRect.Width * 2;

                var RenderTextureDepth = 24;
                var readWrite = (QualitySettings.desiredColorSpace == ColorSpace.Linear) ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default;
                _sideBySide = new RenderTexture(width2 / div, height / div, RenderTextureDepth, bufferFormat,
                                              readWrite);
                _sideBySide.Create();
                _srdSideBySide.texture = _sideBySide.GetNativeTexturePtr();
            }

            if (_outTexture == null)
            {
                _outTexture = new RenderTexture(width, height, depth: 24, bufferFormat);
                _outTexture.filterMode = FilterMode.Point;
                _outTexture.Create();
                _srdOut.texture = _outTexture.GetNativeTexturePtr();
            }

            _srdSideBySide.width = _srdOut.width = (uint)width;
            _srdSideBySide.height = _srdOut.height = (uint)height;

            _srdManager.Session.GenerateTextureAndShaders(ref _srdSideBySide, ref _srdSideBySide, ref _srdOut);

            _leftAndRightToSideBySide.mainTexture = renderTextureL;
            _leftAndRightToSideBySide.SetTexture("_RightTex", renderTextureR);

            _sceneLeft = renderTextureL;
            return true;
        }

        public void RenderStereoComposition(RenderTexture backBuffer)
        {
            Graphics.Blit(_sceneLeft, _sideBySide, _leftAndRightToSideBySide);
            _srdManager.Session.EndFrame();
            Graphics.Blit(_outTexture, backBuffer);
        }

        public void Start()
        {
            // do nothing
        }

        public void Stop()
        {
            if(_sideBySide != null)
            {
                _sideBySide.Release();
                MonoBehaviour.Destroy(_sideBySide);
            }
            if(_outTexture != null)
            {
                _outTexture.Release();
                MonoBehaviour.Destroy(_outTexture);
            }
        }

        public void Dispose()
        {
            // do nothing
        }

    }

    internal class SRDPassThroughStereoCompositer : ISRDStereoCompositer
    {
        private Texture _leftTexture;
        private Texture _rightTexture;

        public SRDPassThroughStereoCompositer()
        {
        }
        public bool RegisterSourceStereoTextures(Texture renderTextureL, Texture renderTextureR)
        {
            _leftTexture = renderTextureL;
            _rightTexture = renderTextureR;
            return true;
        }

        public void RenderStereoComposition(RenderTexture backBuffer)
        {
            Graphics.Blit(_leftTexture, backBuffer);
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
