/*
 * Copyright 2019,2020 Sony Corporation
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Sample.Simple
{
    public class FloatingObject : MonoBehaviour
    {
        public float DeltaAngleDegPerSec = 90f;
        private Vector3 _rotationAxis;
        private float _scaleMultiplier = 1f;
        public float ScaleMultiplier
        {
            get
            {
                return _scaleMultiplier;
            }

            set
            {
                _scaleMultiplier = value;
                transform.localScale = _scaleMultiplier * _baseScale;
            }
        }
        private Vector3 _baseScale;

        void Start()
        {
            _rotationAxis = Vector3.up;
            _baseScale = transform.localScale;
        }

        void Update()
        {
            _rotationAxis += new Vector3(Random.Range(0f, 0.1f), Random.Range(0f, 0.1f), Random.Range(0f, 0.1f));
            _rotationAxis.Normalize();
            this.transform.rotation *= Quaternion.AngleAxis(DeltaAngleDegPerSec * Time.deltaTime, _rotationAxis);
        }
    }
}
