using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;        // Float64MultiArrayMsg
using System;

/// <summary>
/// ROS2 から std_msgs/msg/Float64MultiArray を購読し、
/// Inspector で指定した ArticulationBody 群の xDrive.stiffness / damping を更新する。
/// data[0] = stiffness, data[1] = damping を想定。
/// </summary>
public class FrontDriveGainParamSubscriber : MonoBehaviour
{
    [Header("ROS Settings")]
    [Tooltip("購読する ROS2 トピック名。例: /drive_gains")]
    public string topicName = "/drive_gains";

    [Header("Target Joints")]
    [Tooltip("stiffness / damping を適用する対象 ArticulationBody 群。")]
    public ArticulationBody[] targetJoints;

    [Header("Debug")]
    [Tooltip("受信した値を Debug.Log で表示するか。")]
    public bool verbose = false;

    private ROSConnection _ros;

    void Start()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        _ros.Subscribe<Float64MultiArrayMsg>(topicName, OnDriveGainMsg);

        if (targetJoints == null || targetJoints.Length == 0)
        {
            Debug.LogWarning($"[{nameof(FrontDriveGainParamSubscriber)}] Target joints not set. No joints will be updated.");
        }
    }

    private void OnDriveGainMsg(Float64MultiArrayMsg msg)
    {
        if (msg == null || msg.data == null || msg.data.Length < 2)
        {
            Debug.LogWarning(
                $"[{nameof(FrontDriveGainParamSubscriber)}] Received message but data length < 2. Ignored.");
            return;
        }

        double s = msg.data[0];
        double d = msg.data[1];

        ApplyDriveGains((float)s, (float)d);
    }

    private void ApplyDriveGains(float stiffnessIn, float dampingIn)
    {
        float s = stiffnessIn;
        float d = dampingIn;

        if (verbose)
        {
            Debug.Log($"[{nameof(FrontDriveGainParamSubscriber)}] Applying stiffness={s}, damping={d} to {targetJoints?.Length ?? 0} joints.");
        }

        if (targetJoints == null) return;

        foreach (var ab in targetJoints)
        {
            if (ab == null) continue;

            // ArticulationDrive は struct。コピーして編集→戻す必要あり。
            var drive = ab.xDrive;
            drive.stiffness = s;
            drive.damping   = d;
            ab.xDrive = drive;
        }
    }
}
