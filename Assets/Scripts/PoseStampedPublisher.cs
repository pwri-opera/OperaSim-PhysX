using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;

/// <summary>
/// シミュレータから得られる真の位置をPoseStampedメッセージとして送信する
/// </summary>
public class PoseStampedPublisher : MonoBehaviour
{
    ROSConnection ros;

    [Tooltip("ROSメッセージの接頭辞として用いられるロボット名")]
    public string robotName = "robot_name";

    [Tooltip("出力するROSトピック名")]
    public string topicName = "robot_name/unity/pose_stmp";

    private PoseStampedMsg message;

    // Publish the object's position and rotation every N seconds
    [Tooltip("メッセージの出力間隔(秒)")]
    public float publishMessageInterval = 0.05f;//20Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        message = new PoseStampedMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;
        // Get Rigidbody
        ArticulationBody ab = this.transform.GetComponent<ArticulationBody> ();
    

        if (timeElapsed >= publishMessageInterval)
        {
            message.header.frame_id = "world";
            message.header.stamp = new TimeStamp(Clock.time);

            // Unity -> ROS transformation
            // Position: Unity(x,y,z) -> ROS(z,-x,y)
            // Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)
            message.pose.position.x = this.transform.position.z;
            message.pose.position.y = - this.transform.position.x;
            message.pose.position.z = this.transform.position.y;

            message.pose.orientation.x = - this.transform.rotation.z;
            message.pose.orientation.y = this.transform.rotation.x;
            message.pose.orientation.z = - this.transform.rotation.y;
            message.pose.orientation.w = this.transform.rotation.w;

            ros.Publish(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
