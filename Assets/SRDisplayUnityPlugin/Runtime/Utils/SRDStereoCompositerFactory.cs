/*
 * Copyright 2019,2020 Sony Corporation
 */


using System;
using System.Collections.Generic;

using SRD.Core;

namespace SRD.Utils
{
    public enum StereoCompositerSystem
    {
        SRD,
        PassThrough,
    }

    internal class SRDStereoCompositerFactory
    {
        public static ISRDStereoCompositer CreateStereoCompositer(StereoCompositerSystem system, SRDManager srdManager)
        {
            var switcher = new Dictionary<StereoCompositerSystem, Func<ISRDStereoCompositer>>()
            {
                {StereoCompositerSystem.SRD, () => { return new SRDStereoCompositer(srdManager); } },
                {StereoCompositerSystem.PassThrough, () => { return new SRDPassThroughStereoCompositer(); } }
            };
            return switcher[system]();
        }
    }
}
