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

    [Tooltip("入力に対するむだ時間 (msec)")]
    public double deadTime;

    private ArticulationBody joint;
    private Float64Msg targetPos;

    private Queue<(float timestamp, Float64Msg data)> InputQueue = new Queue<(float, Float64Msg)>();
    // private Queue<(float timestamp, string data)> dataQueue = new Queue<(float, string)>();

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); // 少し待機してから設定
        ros = ROSConnection.GetOrCreateInstance();
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

        if (deadTime == 0.0)
        {
            Debug.Log("Normal Mode");
            ros.Subscribe<Float64Msg>(setpointTopicName, ExecuteJointPosControl);
        }
        else
        {
            Debug.Log("Dead Time Mode");
            Debug.Log("deadTime" + deadTime);
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
    // fiXupdate の方 が良い　（必須）
    　// Unity でスレッドを実装できるライブラリ有，検討推奨（内部に update があるのか？）
    {
        if (deadTime != 0.0)
        {
            // Debug.Log("Get Delay Data");
            GetDelayedData();
            // Debug.Log("InputQueue.Count: " + InputQueue.Count);
        }
    }

    // ----- Functions of Dead Time ----- //
    void AddInputData(Float64Msg msg) // DateTime.Now は使うべきでない (Unity 内の時間を使用する（※必須）)
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
            if ((Time.time - timestamp) >= deadTime)
            {
                ExecuteJointPosControl(data);
                Debug.Log("Data: " + data);
                InputQueue.Dequeue();
            }
            else if (InputQueue.Count <= 0 || (Time.time - timestamp) < deadTime)
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

