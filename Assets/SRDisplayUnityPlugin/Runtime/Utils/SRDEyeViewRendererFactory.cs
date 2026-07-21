/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections.Generic;
using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    public enum EyeViewRendererSystem
    {
        UnityRenderCam, Texture,
    }

    internal class SRDEyeViewRendererFactory
    {
        public static ISRDEyeViewRenderer CreateEyeViewRenderer(EyeViewRendererSystem system, SRDManager srdManager)
        {
            var switcher = new Dictionary<EyeViewRendererSystem, Func<ISRDEyeViewRenderer>>()
            {
                {
                    EyeViewRendererSystem.UnityRenderCam, () =>
                    {
                        return new SRDEyeViewRenderer(srdManager);
                    }
                },
                {
                    EyeViewRendererSystem.Texture, () => {
                        return new SRDTexturesBasedEyeViewRenderer(srdManager, null, null);
                    }
                },
            };
            return switcher[system]();
        }
    }
}
