using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// 各関節を独立して制御する
/// </summary>
public class JointPosController : MonoBehaviour
{
    private ROSConnection ros;

    [Tooltip("角度設定コマンドのROSトピック名")]
    public string setpointTopicName = "joint_name/setpoint";

    [Tooltip("初期の目標角度(degree)")]
    public double initTargetPos;

    private ArticulationBody joint;
    private Float64Msg targetPos;
    private EmergencyStop emergencyStop;
    private bool currentEmergencyStop = false;
    private float emergencyStopPosition = 0.0f;

    // Start is called before the first frame update
   IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); // 少し待機してから設定
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);
        joint = this.GetComponent<ArticulationBody>();
        targetPos = new Float64Msg();

       if(joint)
        {
            if (joint.GetComponent<Com3.ControlTypeAnnotation>() == null) {
                var drive = joint.xDrive;
                if (drive.stiffness == 0)
                    drive.stiffness = 200000;
                if (drive.damping == 0)
                    drive.damping = 100000;
                if (drive.forceLimit == 0)
                    drive.forceLimit = 100000;

                drive.target = (float)initTargetPos;
                joint.xDrive = drive;
            }
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        ros.Subscribe<Float64Msg>(Utils.PreprocessNamespace(this.gameObject, setpointTopicName), ExecuteJointPosControl);
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
