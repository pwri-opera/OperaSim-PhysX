using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoilParticleSettings : MonoBehaviour
{
    public static SoilParticleSettings instance = null;

    public float particleVisualRadius = 0.2f;
    public double partileStickDistance = 0.25;
    public float stickForce = 30.0f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
