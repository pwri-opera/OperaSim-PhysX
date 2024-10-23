using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// 各関節を独立して制御する
/// </summary>
public class JointVelController : MonoBehaviour
{
    private ROSConnection ros;

    [Tooltip("角度設定コマンドのROSトピック名")]
    public string setpointTopicName = "joint_name/setpoint";

    private ArticulationBody joint;
    private Float64Msg targetVel;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); // 少し待機してから設定
        ros = ROSConnection.GetOrCreateInstance();
        joint = this.GetComponent<ArticulationBody>();

        if(joint)
        {
            var drive = joint.xDrive;
            // if (drive.stiffness == 0)
            //     drive.stiffness = 200000;
            // if (drive.damping == 0)
            //     drive.damping = 100000;
            // if (drive.forceLimit == 0)
            //     drive.forceLimit = 100000;

            joint.xDrive = drive;
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        ros.Subscribe<Float64Msg>(setpointTopicName, ExecuteJointVelControl);
    }

    void ExecuteJointVelControl(Float64Msg msg)
    {
        targetVel = msg;
        var drive = joint.xDrive;
        drive.targetVelocity = (float)(targetVel.data * Mathf.Rad2Deg);
        joint.xDrive = drive;
        //Debug.Log("Joint Target Position:" + targetPos.data);
    }
}
