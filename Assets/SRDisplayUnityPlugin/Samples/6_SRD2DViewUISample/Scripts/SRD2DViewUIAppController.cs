using SRD.Core;
using SRD.Utils;
using UnityEngine;
using SRD.Sample.Simple;

namespace SRD.Sample.UI2DView
{
    public class SRD2DViewUIAppController : MonoBehaviour
    {
        [SerializeField]
        private UIController _uiController;

        [SerializeField]
        private Camera _spectatorCamera;

        [SerializeField]
        private Camera _srdFixedCamera;
        private RenderTexture _fixedCameraTexture;

        [SerializeField]
        private FloatingObject _floatingObject;
        private Vector3 _targetObjectDefaultScale;

        private SRDManager _srdManager;
        private SRDCameras _srdCameras;

        private AppContext _appContext;

        void Start()
        {
            _srdManager = SRDSceneEnvironment.GetSRDManager();
            if (_srdManager != null)
            {
                _srdCameras = new SRDCameras(_srdManager);
                if (_srdManager.Init2DView())
                {
                    _srdManager.SRD2DView.Show(true);

                    _fixedCameraTexture = new RenderTexture(_srdCameras.LeftEyeCamera.targetTexture);
                    if (!_fixedCameraTexture.IsCreated())
                    {
                        _fixedCameraTexture.Create();
                    }

                    RenderTexture spectatorCameraTexture = new RenderTexture(_fixedCameraTexture);
                    if (!spectatorCameraTexture.IsCreated())
                    {
                        spectatorCameraTexture.Create();
                    }
                    _spectatorCamera.targetTexture = spectatorCameraTexture;
                }
            }
            _targetObjectDefaultScale = _floatingObject.transform.localScale;

            _appContext = AppContext.Instance;
            _appContext.meshChangedEvent.AddListener(SetFloatingObjectMesh);
            _appContext.scaleChangedEvent.AddListener(SetFloatingObjectScale);
            _appContext.rotationSpeedChangedEvent.AddListener(SetFloatingObjectRotationSpeed);
            _appContext.activeCameraModeChangedEvent.AddListener(SetCameraMode);

            _appContext.RotationSpeed = 90;
            _appContext.Scale = 1.0f;

            _uiController.Init();

            _appContext.SrdTexture = _srdCameras.LeftEyeCamera.activeTexture;
        }

        public void SetCameraMode(SRD2DViewCameraMode cameraMode)
        {
            if (cameraMode == SRD2DViewCameraMode.FixedAngle)
            {
                _srdFixedCamera.CopyFrom(_srdCameras.LeftEyeCamera);
                _srdFixedCamera.targetTexture = _fixedCameraTexture;
                _srdManager.SRD2DView.CustomTexture = _fixedCameraTexture;
            }
            else if (cameraMode == SRD2DViewCameraMode.ThirdPerson)
            {
                _srdManager.SRD2DView.CustomTexture = _spectatorCamera.targetTexture;
            }

            _appContext.SrdTexture = cameraMode == SRD2DViewCameraMode.FixedAngle ? _fixedCameraTexture : _srdCameras.LeftEyeCamera.activeTexture;
            _srdManager.SRD2DView.SetSourceTexture(cameraMode == SRD2DViewCameraMode.FollowSRD ? SRD2DView.SRDTextureType.LeftEye : SRD2DView.SRDTextureType.Custom);
        }

        void SetFloatingObjectMesh(Mesh mesh)
        {
            MeshFilter meshFilter = _floatingObject.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
        }

        void SetFloatingObjectScale(float scale)
        {
            _floatingObject.transform.localScale = _targetObjectDefaultScale * scale;
        }

        void SetFloatingObjectRotationSpeed(int rotationSpeed)
        {
            _floatingObject.DeltaAngleDegPerSec = rotationSpeed;
        }
    }
}