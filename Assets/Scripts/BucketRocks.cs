using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// バケットにアタッチして、地面との接触時に粒子を生成する
/// </summary>
public class BucketRocks : MonoBehaviour
{
    private void Start()
    {
        SoilParticleSettings.instance.RegisterBucket(gameObject);
    }

    private void OnDestroy()
    {
        if (SoilParticleSettings.instance != null)
        {
            SoilParticleSettings.instance.UnregisterBucket(gameObject);
        }
    }

    private void OnCollisionStay(Collision other)
    {
        SoilParticleSettings.instance.OnBucketCollision(other, transform.position);
    }

}
