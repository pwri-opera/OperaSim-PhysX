using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiffDriveController : MonoBehaviour
{
    public List<GameObject> leftWheels;
    public List<GameObject> rightWheels;
    public KeyCode leftUpKey = KeyCode.U;
    public KeyCode leftDownKey = KeyCode.J;
    public KeyCode rightUpKey = KeyCode.I;
    public KeyCode rightDownKey = KeyCode.K;

    private List<ArticulationBody> leftBodies;
    private List<ArticulationBody> rightBodies;

    // Start is called before the first frame update
    void Start()
    {
        leftBodies = new List<ArticulationBody>();
        rightBodies = new List<ArticulationBody>();
        foreach (GameObject left in leftWheels)
        {
            var body = left.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            drive.stiffness = 100000;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            body.xDrive = drive;
            leftBodies.Add(body);
        }
        foreach (GameObject right in rightWheels)
        {
            var body = right.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            drive.stiffness = 100000;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            body.xDrive = drive;
            rightBodies.Add(body);
        }
    }

    // Update is called once per frame
    void Update()
    {
        const float speed = 100.0f;
        float leftVel = 0;
        if (Input.GetKey(leftUpKey))
        {
            leftVel = speed;
        } else if (Input.GetKey(leftDownKey))
        {
            leftVel = -speed;
        }
        float rightVel = 0;
        if (Input.GetKey(rightUpKey))
        {
            rightVel = speed;
        }
        else if (Input.GetKey(rightDownKey))
        {
            rightVel = -speed;
        }
        foreach (ArticulationBody left in leftBodies)
        {
            var drive = left.xDrive;
            drive.targetVelocity = leftVel;
            left.xDrive = drive;
        }
        foreach (ArticulationBody right in rightBodies)
        {
            var drive = right.xDrive;
            drive.targetVelocity = rightVel;
            right.xDrive = drive;
        }
    }
}
