using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using Unity.Robotics.UrdfImporter;

public class JointPosController : MonoBehaviour
{
    private ROSConnection ros;
    public string setpointTopicName = "joint_name/setpoint";
    public double initTargetPos;
    private ArticulationBody joint;
    private Float64Msg targetPos;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.instance;
        joint = this.GetComponent<ArticulationBody>();
        targetPos = new Float64Msg();

        if (joint)
        {
            var drive = joint.xDrive;
            drive.stiffness = 1000000;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            joint.xDrive = drive;
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        targetPos.data = initTargetPos;
        ros.Subscribe<Float64Msg>(setpointTopicName, ExecuteJointPosControl);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Joint Target Position:" + targetPos.data);
        var drive = joint.xDrive;
        drive.target = (float)(targetPos.data * Mathf.Rad2Deg);
        joint.xDrive = drive;
    }

    void ExecuteJointPosControl(Float64Msg msg)
    {
        targetPos = msg;
        Debug.Log("Joint Target Position:" + targetPos.data);
    }
}
