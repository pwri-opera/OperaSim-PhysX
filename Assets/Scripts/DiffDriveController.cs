using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Control;
using RosMessageTypes.Geometry;

public class DiffDriveController : MonoBehaviour
{
    private ROSConnection ros;

    public List<GameObject> leftWheels;
    public List<GameObject> rightWheels;
    public KeyCode leftUpKey = KeyCode.U;
    public KeyCode leftDownKey = KeyCode.J;
    public KeyCode rightUpKey = KeyCode.I;
    public KeyCode rightDownKey = KeyCode.K;

    public string TwistTopicName = "zx120/twist";
    private List<ArticulationBody> leftBodies;
    private List<ArticulationBody> rightBodies;
    private TwistMsg twist;
    private double tread_half = 2.0;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.instance;
        leftBodies = new List<ArticulationBody>();
        rightBodies = new List<ArticulationBody>();
        foreach (GameObject left in leftWheels)
        {
            var body = left.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            body.mass = 50.0f;
            drive.stiffness = 0;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            body.xDrive = drive;
            leftBodies.Add(body);
        }
        foreach (GameObject right in rightWheels)
        {
            var body = right.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            body.mass = 50.0f;
            drive.stiffness = 0;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            body.xDrive = drive;
            rightBodies.Add(body);
        }
        tread_half = Mathf.Abs(leftWheels[0].transform.localPosition.x - rightWheels[0].transform.localPosition.x)/2;

        Debug.Log("DiffDriveController starts!!");
        ros.Subscribe<TwistMsg>(TwistTopicName, ExecuteTwist);
    }

    // Update is called once per frame
    void Update()
    {
        const float speed = 100.0f;
        float leftVel = 0;
        float rightVel = 0;
        
        /*
        if (Input.GetKey(leftUpKey))
        {
            leftVel = speed;
        } else if (Input.GetKey(leftDownKey))
        {
            leftVel = -speed;
        }

        if (Input.GetKey(rightUpKey))
        {
            rightVel = speed;
        }
        else if (Input.GetKey(rightDownKey))
        {
            rightVel = -speed;
        }
        */
        
        rightVel = (float)(twist.linear.x + tread_half * twist.angular.z) * Mathf.Rad2Deg;
        leftVel = (float)(twist.linear.x - tread_half * twist.angular.z) * Mathf.Rad2Deg;
        
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

    void ExecuteTwist(TwistMsg msg)
    {
        //currentPose = trajectory;
        twist = msg;
        Debug.Log("Linear Velocity:"+twist.linear.x);
        Debug.Log("Angular Velocity:"+twist.angular.z);
    }
}
