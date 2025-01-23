using System;
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

    [Tooltip("初期の目標角度 (degree)")]
    public double initTargetPos;

    [Tooltip("むだ時間機能の 有効/無効 切替え")]
    public bool enableDeadTime;

    [Tooltip("入力に対するむだ時間 (msec) \n40 msec 以上に設定") ]
    [Min(40)] public double deadTime;

    private ArticulationBody joint;
    private Float64Msg targetPos;
    private EmergencyStop emergencyStop;
    private bool currentEmergencyStop = false;
    private float emergencyStopPosition = 0.0f;

    private Queue<(float timestamp, Float64Msg data)> InputQueue = new Queue<(float, Float64Msg)>();

    private double unityDeadTime = 40.0f; // msec
    private double internalDeadTime; // msec
    
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
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        internalDeadTime = deadTime - unityDeadTime;
        

        if (enableDeadTime == false)
        {
            Debug.Log("Normal Mode");
            ros.Subscribe<Float64Msg>(setpointTopicName, ExecuteJointPosControl);
        }
        else
        {
            Debug.Log("Dead Time Mode");
            // Debug.Log("deadTime" + deadTime);
            ros.Subscribe<Float64Msg>(setpointTopicName, AddInputData);
        }
    }

    void ExecuteJointPosControl(Float64Msg msg)
    {
        targetPos = msg;
        var drive = joint.xDrive;
        drive.target = (float)(targetPos.data * Mathf.Rad2Deg);
        joint.xDrive = drive;
    }

    void FixedUpdate() 
    {
        // Dead Time 
        if (internalDeadTime != 0.0)
        {
            GetDelayedData();
        }
 
        // Emergency Stop
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

    // ----- Functions of Dead Time ----- //
    void AddInputData(Float64Msg msg)
    { 
        float currentTime = Time.time * 1000; // sec -> msec
        InputQueue.Enqueue((currentTime, msg));
    }

    void GetDelayedData()
    {
        // 疑似的なスレッドを使えると while を入れずに済む？
        while (InputQueue.Count > 0)
        { 
            var (timestamp, data) = InputQueue.Peek();
            if ((Time.time*1000 - timestamp) >= internalDeadTime)
            {
                ExecuteJointPosControl(data);
                // Debug.Log("Data: " + data);
                InputQueue.Dequeue();
            }
            else if (InputQueue.Count <= 0 || (Time.time - timestamp) < internalDeadTime)
            {
                break;
            }
            else
            {
                break;
            }
        }
    }
    // ---------------------------------- //
}