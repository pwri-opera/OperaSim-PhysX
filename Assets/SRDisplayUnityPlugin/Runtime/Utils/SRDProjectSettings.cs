/*
 * Copyright 2019-2026 Sony Corporation
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Utils
{
    /// <summary>
    /// A class for Project Settings of Spatial Reality Display
    /// </summary>
    public class SRDProjectSettings : ScriptableObject
    {
        public enum BehaviorOptionWhenNoSRDisplay
        {
            [InspectorName("Display error message and exit")]
            ExitWithError = 0,
            [InspectorName("Run with SRDisplayManager disabled")]
            RunWithDisabled,
        }

        public enum MultiSRDMode
        {
            // Note: Unity interprets a forward slash "/" (U+002F) in a dropdown item name as a menu separator.
            // For this reason, we instead use the similar looking division slash character "∕" (U+2215) as a replacement.
            [InspectorName("Single Display\u200A\u2215\u200ADuplicated Output")]
            SingleDisplay = 0,
            [InspectorName("Vertical Multi Display")]
            MultiVertical,
            [InspectorName("Horizontal Multi Display")]
            MultiHorizontal,
            [InspectorName("Grid Multi Display")]
            MultiGrid,
        }

        public struct ExtendedSRDParameters
        {
            internal bool Show;
            public int MinDeviceNum;
            public int MaxDeviceNum;
            internal bool CanEditDeviceNum;
        }

        public static Dictionary<MultiSRDMode, ExtendedSRDParameters> MultipleSRDParameters = new Dictionary<MultiSRDMode, ExtendedSRDParameters>
        {
            { MultiSRDMode.SingleDisplay,    new ExtendedSRDParameters{ Show = false, MinDeviceNum = 1, MaxDeviceNum = 1, CanEditDeviceNum = false } } ,
            { MultiSRDMode.MultiVertical,    new ExtendedSRDParameters{ Show = true,  MinDeviceNum = 2, MaxDeviceNum = 4, CanEditDeviceNum = true  } } ,
            { MultiSRDMode.MultiHorizontal,  new ExtendedSRDParameters{ Show = true,  MinDeviceNum = 2, MaxDeviceNum = 3, CanEditDeviceNum = true  } } ,
            { MultiSRDMode.MultiGrid,        new ExtendedSRDParameters{ Show = true,  MinDeviceNum = 4, MaxDeviceNum = 4, CanEditDeviceNum = false } } ,
        };

        private static SRDProjectSettings _instance;
        private SRDProjectSettings() { }

        /// <summary>
        /// A flag to run the app with no Spatial Reality Display.
        /// If this is true, the app is able to run with no Spatial Reality Display.
        /// </summary>
        [Tooltip("Check this if you want to run your app with no Spatial Reality Display.")]
        public bool RunWithoutSpatialRealityDisplay;

        [Tooltip("Select how the application should behave when there is no Spatial Reality Display connected to the PC.")]
        public BehaviorOptionWhenNoSRDisplay BehaviorWhenNoSRDisplay;

        [Tooltip("Select whether this application supports multiple Spatial Reality Displays.")]
        public MultiSRDMode MultiDisplayMode;

        [Tooltip("Define the number of Spatial Reality Displays this application is supposed to use.")]
        public int NumberOfDisplays;

        [Tooltip("Set the interval (in seconds) at which the SRDisplayManager switches between display positions in Editor Play mode.")]
        public float PositionChangeTime;

        internal static SRDProjectSettings GetDefault()
        {
            if(_instance != null)
            {
                _instance = null;
            }
            _instance = ScriptableObject.CreateInstance<SRDProjectSettings>();
            _instance.RunWithoutSpatialRealityDisplay = false;
            _instance.MultiDisplayMode = MultiSRDMode.SingleDisplay;
            _instance.NumberOfDisplays = 2;
            _instance.PositionChangeTime = 3f;
            return _instance;
        }

        /// <summary>
        /// Static function to get SRDProjectSettings
        /// </summary>
        /// <returns>SRDProjectSettings instance</returns>
        public static SRDProjectSettings LoadResourcesOrDefault()
        {
            if(_instance != null)
            {
                return _instance;
            }

            _instance = Resources.Load<SRDProjectSettings>("SRDProjectSettings");
            if(_instance != null)
            {
                return _instance;
            }

            return SRDProjectSettings.GetDefault();
        }

        /// <summary>
        /// Just returns current RunWithoutSpatialRealityDisplay
        /// </summary>
        /// <returns> A flag that shows RunWithoutSpatialRealityDisplay is ON or not </returns>
        public static bool IsRunWithoutSRDisplayMode()
        {
            return LoadResourcesOrDefault().RunWithoutSpatialRealityDisplay;
        }

        /// <summary>
        /// Returns application behavior when no Spatial Reality Displays are connected
        /// </summary>
        /// <returns> The current Behavior When No SRDisplay </returns>
        public static BehaviorOptionWhenNoSRDisplay GetBehaviorOptionWhenNoSRDisplay()
        {
            var behaviorWhenNoSRDisplay = LoadResourcesOrDefault().BehaviorWhenNoSRDisplay;
            if (IsRunWithoutSRDisplayMode())
            {
                behaviorWhenNoSRDisplay = BehaviorOptionWhenNoSRDisplay.ExitWithError;
            }
            return behaviorWhenNoSRDisplay;
        }

        /// <summary>
        /// Returns whether this application supports multiple Spatial Reality Displays
        /// </summary>
        /// <returns> The current Multiple Display Mode </returns>
        public static MultiSRDMode GetMutlipleDisplayMode()
        {
            var multiDisplayMode = LoadResourcesOrDefault().MultiDisplayMode;
            if (IsRunWithoutSRDisplayMode())
            {
                multiDisplayMode = MultiSRDMode.SingleDisplay;
            }
            return multiDisplayMode;
        }

        /// <summary>
        /// Returns the number of Spatial Reality Displays needed for the application to run properly 
        /// </summary>
        /// <returns> The number of devices needed </returns>
        public static int GetNumberOfDevices()
        {
            var numberOfDevice = LoadResourcesOrDefault().NumberOfDisplays;
            if (GetMutlipleDisplayMode() == MultiSRDMode.SingleDisplay)
            {
                numberOfDevice = 1;
            }
            return numberOfDevice;
        }

        /// <summary>
        /// Returns the frequency (in seconds) at which the SRDManager will switch between display positions when the application is in Extended mode 
        /// </summary>
        /// <returns> The interval in seconds between 2 position switches </returns>
        public static float GetPositionSwitchInterval()
        {
            return LoadResourcesOrDefault().PositionChangeTime;
        }
    }
}
