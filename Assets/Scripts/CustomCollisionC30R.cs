using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCollisionC30R : MonoBehaviour
{
    Transform base_link;
    Transform vessel_link;
    Transform tailgate_link;
    List<Transform> wheels;

    string[] wheel_sides = { "right", "left" };
    string[] wheel_positions = { "front", "middle", "rear" };

    // Start is called before the first frame update
    void Start()
    {
        // get each transform items
        base_link = gameObject.transform.Find("base_link");
        vessel_link = gameObject.transform.Find("base_link/vessel_link");
        tailgate_link = gameObject.transform.Find("base_link/vessel_link/tailgate_link");
        /*
        foreach (var s in wheel_sides)
        {
            foreach (var p in wheel_positions)
            {
                wheels.Add(gameObject.transform.Find(s + "_" + p + "_wheel_link"));
            }
        }

        // wheels should not collide with base link
        foreach (var w in wheels)
        {
            Physics.IgnoreCollision(base_link.GetComponent<Collider>(), w.GetComponent<Collider>());
        }

        // wheels should not collide with each other
        foreach (var w1 in wheels)
        {
            foreach (var w2 in wheels)
            {
                if (w1 == w2) return;
                Physics.IgnoreCollision(w1.GetComponent<Collider>(), w2.GetComponent<Collider>());
            }
        }
        */

        // vessel should not collide with base link
        Physics.IgnoreCollision(base_link.GetComponent<Collider>(), vessel_link.GetComponent<Collider>());

        // tailgate should not collide with vessel link
        Physics.IgnoreCollision(vessel_link.GetComponent<Collider>(), tailgate_link.GetComponent<Collider>());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
