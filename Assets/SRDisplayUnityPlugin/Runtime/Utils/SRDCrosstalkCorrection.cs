/*
 * Copyright 2019,2020,2021,2023,2024 Sony Corporation
 */

using System.Collections.Generic;

using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    internal class SRDCrosstalkCorrection
    {
        private SRDSession _session;
        private bool _previousFrameActiveState;
        private SrdXrCrosstalkCorrectionType _previousType;

        private static readonly Dictionary<SrdXrCrosstalkCorrectionType, string> _crosstalkCorrectionTypeNames =
            new Dictionary<SrdXrCrosstalkCorrectionType, string>
        {
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM,        "Low" },
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_ALL,           "Mid" },
            { SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_HIGH_PRECISE,  "High" }
        };

        public void Init(SRDSession session, ref bool isActive, ref SrdXrCrosstalkCorrectionType type)
        {
            _session = session;
            SetCrosstalkCorrection(isActive, type);
            UpdateState(ref isActive, ref type);
        }

        public void HookUnityInspector(ref bool isActive, ref SrdXrCrosstalkCorrectionType type)
        {
            ToggleActivateStateIfValueChanged(isActive, type);
            UpdateState(ref isActive, ref type);
        }

        private void ToggleActivateStateIfValueChanged(bool isActive, SrdXrCrosstalkCorrectionType type)
        {
            if(_previousFrameActiveState != isActive || _previousType != type)
            {
                SetCrosstalkCorrection(isActive, type);
            }
        }

        private void SetCrosstalkCorrection(bool isActive, SrdXrCrosstalkCorrectionType type)
        {
            SrdXrCrosstalkCorrectionMode mode = SRDCorePlugin.DefaultCrosstalkCorrectionMode;
            if (!isActive)
            {
                mode = SrdXrCrosstalkCorrectionMode.DISABLED;
            }
            else{
                switch(type)
                {
                    case SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM:
                        mode = SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_MEDIUM;
                        break;
                    case SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_ALL:
                        mode = SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_ALL;
                        break;
                    case SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_HIGH_PRECISE:
                        mode = SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_HIGH_PRECISE;
                        break;
                }
            }
            var result = _session.SetCrosstalkCorrectionMode(mode);
            if(result != SrdXrResult.SUCCESS)
            {
            //    Debug.LogWarning(string.Format("Failed to set CrosstalkCorrection mode: {0}", result));
            }
        }

        private bool UpdateState(ref bool appState, ref SrdXrCrosstalkCorrectionType appType)
        {
            if(SRDProjectSettings.IsRunWithoutSRDisplayMode())
            {
                return true;
            }

            var result = _session.GetCrosstalkCorrectionMode(out var mode);
            if(result != SrdXrResult.SUCCESS)
            {
            //    Debug.LogWarning(string.Format("Failed to get CrosstalkCorrection mode: {0}", result));
            }
            else
            {
                bool pluginState = appState;
                SrdXrCrosstalkCorrectionType pluginType = appType;
                switch (mode)
                {
                    case SrdXrCrosstalkCorrectionMode.DISABLED:
                    pluginState = false;
                    break;
                    case SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_MEDIUM:
                    pluginState = true;
                    pluginType = SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM;
                    break;
                    case SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_ALL:
                    pluginState = true;
                    pluginType = SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_ALL;
                    break;
                    case SrdXrCrosstalkCorrectionMode.GRADATION_CORRECTION_HIGH_PRECISE:
                    pluginState = true;
                    pluginType = SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_HIGH_PRECISE;
                    break;
                }

                if(appState != pluginState)
                {
                    if(appState)
                    {
                        Debug.LogWarning(
                            "Failed to activate Crosstalk Correction. " +
                            "Crosstalk Correction may not be supported in the installed SDK. " +
                            "Try to update SR Display SDK.");
                    }
                    else
                    {
                        Debug.LogWarning(
                            "Failed to deactivate Crosstalk Correction. " +
                            "Crosstalk Correction may not be supported in the installed SDK. " +
                            "Try to update SR Display SDK.");
                    }
                }
                else if(appType != pluginType)
                {
                    if(!_crosstalkCorrectionTypeNames.TryGetValue(pluginType, out var pluginTypeName))
                    {
                        pluginTypeName = pluginType.ToString();
                    }
                    if(!_crosstalkCorrectionTypeNames.TryGetValue(appType, out var appTypeName))
                    {
                        appTypeName = appType.ToString();
                    }
                    Debug.LogWarningFormat(
                        "Failed to set your CrosstalkCorrection mode to SR Display SDK. " +
                        "Forced to set {0}. {1} may not be supported in the installed SDK. " +
                        "Try to update SR Display SDK. ",
                        pluginTypeName, appTypeName);
                }

                appState = pluginState;
                appType = pluginType;
            }

            _previousFrameActiveState = appState;
            _previousType = appType;
            return (result == SrdXrResult.SUCCESS);
        }
    }
}
