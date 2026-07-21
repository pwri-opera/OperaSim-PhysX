using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SRD.Sample.UI2DView
{
    public enum SRD2DViewCameraMode
    {
        FollowSRD = 0,
        FixedAngle = 1,
        ThirdPerson = 2
    }

    public class AppContext
    {
        private static AppContext _instance;
        public static AppContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppContext();
                }
                return _instance;
            }
        }

        private Mesh _mesh;
        [SerializeField]
        private float _minScale = 0.5f;
        [SerializeField]
        private float _maxScale = 1.5f;
        private float _scale = 1.0f;
        [SerializeField]
        private int _minRotationSpeed = 0;
        [SerializeField]
        private int _maxRotationSpeed = 180;
        private int _rotationSpeed = 90;
        private SRD2DViewCameraMode _activeCameraMode = SRD2DViewCameraMode.FollowSRD;
        private Texture _srdTexture;

        #region UnityEvents
        public UnityEvent<Mesh> meshChangedEvent;
        public UnityEvent<float> scaleChangedEvent;
        public UnityEvent<int> rotationSpeedChangedEvent;
        public UnityEvent<bool> postProcessingChangedEvent;
        public UnityEvent<SRD2DViewCameraMode> activeCameraModeChangedEvent;
        public UnityEvent<Texture> srdTextureChangedEvent;
        #endregion

        private AppContext()
        {
            meshChangedEvent = new UnityEvent<Mesh>();
            scaleChangedEvent = new UnityEvent<float>();
            rotationSpeedChangedEvent = new UnityEvent<int>();
            postProcessingChangedEvent = new UnityEvent<bool>();
            activeCameraModeChangedEvent = new UnityEvent<SRD2DViewCameraMode>();
            srdTextureChangedEvent = new UnityEvent<Texture>();
        }

        public Mesh Mesh
        {
            get
            {
                return _mesh;
            }

            set
            {
                _mesh = value;
                meshChangedEvent.Invoke(_mesh);
            }
        }

        public float ScaleMin
        {
            get
            {
                return _minScale;
            }
        }
        public float ScaleMax
        {
            get
            {
                return _maxScale;
            }
        }
        public float Scale
        {
            get
            {
                return _scale;
            }

            set
            {
                _scale = Mathf.Clamp(_minScale, value, _maxScale);
                scaleChangedEvent.Invoke(_scale);
            }
        }

        public int RotationMin
        {
            get
            {
                return _minRotationSpeed;
            }
        }
        public int RotationMax
        {
            get
            {
                return _maxRotationSpeed;
            }
        }
        public int RotationSpeed
        {
            get
            {
                return _rotationSpeed;
            }

            set
            {
                _rotationSpeed = Mathf.Clamp(_minRotationSpeed, value, _maxRotationSpeed);
                rotationSpeedChangedEvent.Invoke(_rotationSpeed);
            }
        }

        public SRD2DViewCameraMode ActiveCameraMode
        {
            get
            {
                return _activeCameraMode;
            }
            set
            {
                _activeCameraMode = value;
                activeCameraModeChangedEvent.Invoke(_activeCameraMode);
            }
        }

        public Texture SrdTexture
        {
            get
            {
                return _srdTexture;
            }

            set
            {
                _srdTexture = value;
                srdTextureChangedEvent.Invoke(_srdTexture);
            }
        }
    }
}