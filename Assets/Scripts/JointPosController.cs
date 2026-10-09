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

    [Tooltip("むだ時間機能の 有効/無効 切替え")]
    public bool enableDeadTime;

    [Tooltip("角度設定コマンドのROSトピック名")]
    public string setpointTopicName = "joint_name/setpoint";

    [Tooltip("初期の目標角度(degree)")]
    public double initTargetPos;

    [Tooltip("入力に対するむだ時間 (msec) \n40 msec 以上に設定") ]
    [Min(40)] public double deadTime;

    private ArticulationBody joint;
    private Float64Msg targetPos;
    private EmergencyStop emergencyStop;
    private bool currentEmergencyStop = false;
    private float emergencyStopPosition = 0.0f;

    private Queue<(double timestamp, Float64Msg data)> InputQueue = new Queue<(double, Float64Msg)>();

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

        if (joint)
        {
            if (joint.GetComponent<Com3.ControlTypeAnnotation>() == null)
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
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        internalDeadTime = deadTime - unityDeadTime;

        if (enableDeadTime == false)
        {
            // Debug.Log("Normal Mode");
            // ros.Subscribe<Float64Msg>(setpointTopicName, ExecuteJointPosControl);
            ros.Subscribe<Float64Msg>(Utils.PreprocessNamespace(this.gameObject, setpointTopicName), AddInputData);
        }
        else
        {
            // Debug.Log("Dead Time Mode");
            // Debug.Log("deadTime" + deadTime);
            ros.Subscribe<Float64Msg>(Utils.PreprocessNamespace(this.gameObject, setpointTopicName), AddInputData);
            // ros.Subscribe<Float64Msg>(setpointTopicName, AddInputData);
        }        
    }


    /// <summary>
    /// 入力値を関節に与える（むだ時間込み）
    /// </summary>
    void FixedUpdate()
    {
        // Dead Time 
        if (internalDeadTime != 0.0)
        {
            GetDelayedData();
        }
        
        if (emergencyStop && emergencyStop.isEmergencyStop)
        {
            if (currentEmergencyStop == false)
            {
                emergencyStopPosition = joint.jointPosition[0] * Mathf.Rad2Deg;
                currentEmergencyStop = true;
            }
            var drive = joint.xDrive;
            drive.target = emergencyStopPosition;
            joint.xDrive = drive;
        }
        else
        {
            currentEmergencyStop = false;
        }
    }

    /// <summary>
    /// 入力値を関節に与える（むだ時間無し）
    /// </summary>
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

    /// <summary>
    /// 入力値をむだ時間実装用のキューに保存
    /// </summary>
    void AddInputData(Float64Msg msg)
    { 
        double currentTime = Time.timeAsDouble * 1000.0; // sec -> msec
        InputQueue.Enqueue((currentTime, msg));
    }

    /// <summary>
    /// むだ時間経過後の入力値をキューから取り出す
    /// </summary>
    void GetDelayedData()
    {
        // 疑似的なスレッドを使えると while を入れずに済む（要検討）
        while (InputQueue.Count > 0)
        { 
            var (timestamp, data) = InputQueue.Peek();
            if ((Time.timeAsDouble*1000 - timestamp) >= internalDeadTime)
            {
                ExecuteJointPosControl(data);
                InputQueue.Dequeue();
            }
            else if (InputQueue.Count <= 0 || (Time.timeAsDouble - timestamp) < internalDeadTime)
            {
                break;
            }
            else
            {
                break;
            }
        }
    }
}
