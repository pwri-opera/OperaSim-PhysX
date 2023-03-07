using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class PoseStampedPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string robotName = "robot_name";
    public string topicName = "robot_name/unity/pose_stmp";
    private PoseStampedMsg message;

    // Publish the object's position and rotation every N seconds
    public float publishMessageInterval = 0.05f;//20Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        message = new PoseStampedMsg();
        message.header = new RosMessageTypes.Std.HeaderMsg();
        message.header.stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();

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
            float sim_time = Time.time;
            uint secs = (uint)sim_time;
            uint nsecs = (uint)((sim_time % 1) * 1e9);
            message.header.frame_id = "world";
            message.header.stamp.sec = secs;
            message.header.stamp.nanosec = nsecs;

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
