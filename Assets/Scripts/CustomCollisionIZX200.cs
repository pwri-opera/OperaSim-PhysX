using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ZX200のシミュレーションモデルに対して細かな干渉検出設定を行う
/// </summary>
public class CustomCollisionZX200 : MonoBehaviour
{
    Transform arm_link;
    Transform bucket_inner_link;

    // Start is called before the first frame update
    void Start()
    {
        // The collider on the ZX200 root is the simplified body envelope used
        // for collisions with other machines. Keep it from colliding with the
        // ZX200's own links while allowing contacts with external colliders.
        Collider bodyEnvelope = GetComponent<Collider>();
        if (bodyEnvelope != null)
        {
            foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
            {
                if (ownCollider != bodyEnvelope)
                {
                    Physics.IgnoreCollision(bodyEnvelope, ownCollider, true);
                }
            }
        }

        arm_link = gameObject.transform.Find("base_link/body_link/boom_link/arm_link/Collisions");
        bucket_inner_link = gameObject.transform.Find("base_link/body_link/boom_link/arm_link/bucket_link/bucket_inner");
        
        foreach (Collider c1 in arm_link.GetComponentsInChildren(typeof(Collider)))
        {
            foreach (Collider c2 in bucket_inner_link.GetComponentsInChildren(typeof(Collider)))
            {
                Physics.IgnoreCollision(c1, c2);
            }
        }
    }
}
