using SRD.Core;
using SRD.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SRD.Sample.UI2DView
{
    public class SettingsUIController : MonoBehaviour
    {
        #region UI
        /* OverviewCameraView */
        [SerializeField]
        private RawImage _cameraViewImage;

        /* SRDView */
        [SerializeField]
        private RawImage _srdViewImage;

        /* Overview Camera Settings */
        [Space]
        [SerializeField]
        private Slider _cameraFovSlider;
        [SerializeField]
        private InputField _cameraFovInput;

        /* Scene Settings */
        [Space]
        [SerializeField]
        private Dropdown _shapeDropdown;
        [SerializeField]
        private List<Mesh> _shapeMeshes;
        [SerializeField]
        private Slider _scaleSlider;
        [SerializeField]
        private InputField _scaleInput;
        [SerializeField]
        private Slider _rotationSlider;
        [SerializeField]
        private InputField _rotationInput;
        [SerializeField]
        private Toggle _showFpsToggle;

        /* SRD Settings */
        [Space]
        [SerializeField]
        private Toggle _spatialClippingToggle;
        [SerializeField]
        private Toggle _wallmountModeToggle;
        [SerializeField]
        private Toggle _postProcessingToggle;
        #endregion

        #region GameObjects
        [Space]
        [SerializeField]
        private Camera _spectatorCamera;
        private SRDManager _srdManager;
        private GameObject _fpsShower;
        private AppContext _appContext;
        #endregion

        private void Awake()
        {
            _appContext = AppContext.Instance;

            _shapeDropdown.onValueChanged.AddListener((int idx) =>
            {
                _appContext.Mesh = _shapeMeshes[idx];
            });

            _scaleSlider.onValueChanged.AddListener((float value) =>
            {
                _appContext.Scale = value;
                _scaleInput.text = value.ToString("F2");
            });
            _scaleInput.contentType = InputField.ContentType.DecimalNumber;

            _rotationSlider.onValueChanged.AddListener((float value) =>
            {
                _appContext.RotationSpeed = (int)value;
                _rotationInput.text = value.ToString();
            });
            _rotationInput.contentType = InputField.ContentType.IntegerNumber;

            _cameraFovSlider.onValueChanged.AddListener((float value) =>
            {
                this._spectatorCamera.fieldOfView = value;
                _cameraFovInput.text = value.ToString("F2");
            });
            _cameraFovInput.contentType = InputField.ContentType.DecimalNumber;

            _spatialClippingToggle.onValueChanged.AddListener((bool value) =>
            {
                _srdManager.IsSpatialClippingActive = value;
            });

            _showFpsToggle.onValueChanged.AddListener((bool value) =>
            {
                _fpsShower.GetComponent<Renderer>().enabled = value;
            });

            _wallmountModeToggle.onValueChanged.AddListener((bool value) =>
            {
                _srdManager.IsWallmountMode = value;
            });

            _appContext.srdTextureChangedEvent.AddListener(OnSRDViewTextureChanged);
        }

        public void Init()
        {
            _srdManager = SRDSceneEnvironment.GetSRDManager();

            _srdViewImage.texture = _appContext.SrdTexture;

            _cameraViewImage.texture = _spectatorCamera.activeTexture;

            _fpsShower = GameObject.Find("FPSShower");
            _shapeDropdown.options.Clear();
            foreach (Mesh mesh in _shapeMeshes)
            {
                _shapeDropdown.options.Add(new Dropdown.OptionData(mesh.name));
            }

            _scaleSlider.minValue = _appContext.ScaleMin;
            _scaleSlider.maxValue = _appContext.ScaleMax;
            _scaleSlider.value = _appContext.Scale;
            _scaleInput.text = _appContext.Scale.ToString("F2");

            _rotationSlider.minValue = _appContext.RotationMin;
            _rotationSlider.maxValue = _appContext.RotationMax;
            _rotationSlider.value = _appContext.RotationSpeed;
            _rotationInput.text = _appContext.RotationSpeed.ToString();

            _cameraFovSlider.minValue = 45;
            _cameraFovSlider.maxValue = 105;
            _cameraFovSlider.value = _spectatorCamera.fieldOfView;
            _cameraFovInput.text = _spectatorCamera.fieldOfView.ToString("F2");

            _spatialClippingToggle.isOn = _srdManager.IsSpatialClippingActive;

            _showFpsToggle.isOn = _fpsShower.GetComponent<Renderer>().enabled;

            _wallmountModeToggle.isOn = _srdManager.IsWallmountMode;
        }

        void OnSRDViewTextureChanged(Texture texture)
        {
            _srdViewImage.texture = texture;
        }
    }
}