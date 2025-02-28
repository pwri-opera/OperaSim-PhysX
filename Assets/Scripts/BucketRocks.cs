using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// バケットにアタッチして、地面との接触時に粒子を生成する
/// </summary>
public class BucketRocks : MonoBehaviour
{
    private void OnCollisionStay(Collision other)
    {
        SoilParticleSettings.instance.OnBucketCollision(other);
    }
}
