using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SRD.Sample.UI2DView
{
    public class OverlayGUIController : MonoBehaviour
    {
        private static readonly Dictionary<SRD2DViewCameraMode, string> CameraModeNames = new Dictionary<SRD2DViewCameraMode, string>
        {
            {SRD2DViewCameraMode.FollowSRD,     "Follow SRD" },
            {SRD2DViewCameraMode.FixedAngle,    "Fixed Angle SRD" },
            {SRD2DViewCameraMode.ThirdPerson,   "Third Person" },
        };

        [SerializeField]
        private SRD2DViewUIAppController _appController;

        [SerializeField]
        private Dropdown _cameraModeDropdown;

        private AppContext _appContext;

        public void Init()
        {
            _appContext = AppContext.Instance;

            foreach (var cameraMode in (SRD2DViewCameraMode[])Enum.GetValues(typeof(SRD2DViewCameraMode)))
            {
                _cameraModeDropdown.options.Add(new Dropdown.OptionData(CameraModeNames[cameraMode]));
            }
            _cameraModeDropdown.onValueChanged.AddListener((int index) =>
            {
                _appContext.ActiveCameraMode = (SRD2DViewCameraMode) index;
            });

            _cameraModeDropdown.value = (int)SRD2DViewCameraMode.FollowSRD;
            _cameraModeDropdown.RefreshShownValue();
        }
    }
}