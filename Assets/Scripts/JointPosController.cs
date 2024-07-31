using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class JointPosController : MonoBehaviour
{
    private ROSConnection ros;
    public string setpointTopicName = "joint_name/setpoint";
    public double initTargetPos;
    private ArticulationBody joint;
    private Float64Msg targetPos;
    private EmergencyStop emergencyStop;
    private bool currentEmergencyStop = false;
    private float emergencyStopPosition = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);
        joint = this.GetComponent<ArticulationBody>();
        targetPos = new Float64Msg();

        if (joint)
        {
            var drive = joint.xDrive;
            if (drive.stiffness == 0)
                drive.stiffness = 200000;
            if (drive.damping == 0)
                drive.damping = 100000;
            if (drive.forceLimit == 0)
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

    void FixedUpdate()
    {
        if (emergencyStop && emergencyStop.isEmergencyStop) {
            if (currentEmergencyStop == false) {
                emergencyStopPosition = joint.jointPosition[0] * Mathf.Rad2Deg;
                currentEmergencyStop = true;
            }
            var drive = joint.xDrive;
            drive.target = emergencyStopPosition;
            joint.xDrive = drive;
        } else {
            currentEmergencyStop = false;
        }
    }

    void ExecuteJointPosControl(Float64Msg msg)
    {
        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;
        targetPos = msg;
        var drive = joint.xDrive;
        drive.target = (float)(targetPos.data * Mathf.Rad2Deg);
        joint.xDrive = drive;
        //Debug.Log("Joint Target Position:" + targetPos.data);
    }
}
