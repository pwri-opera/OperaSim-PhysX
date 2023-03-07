using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;
using Unity.Robotics.Core;

public class GroundTruthPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "robot_name/groundtruth";
    public string childFrameName = "robot_name/base_link";
    private OdometryMsg message;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.5f;//2Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        message = new OdometryMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<OdometryMsg>(topicName);
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= publishMessageInterval){
            message.header.frame_id = "map";
            message.header.stamp = new TimeStamp(Clock.time);
            message.child_frame_id = childFrameName;

            // Unity -> ROSへの変換方法
            //Position: Unity(x,y,z) -> ROS(z,-x,y)
            //Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)

            message.pose.pose.position.x = this.transform.position.z;
            message.pose.pose.position.y = - this.transform.position.x;
            message.pose.pose.position.z = this.transform.position.y;

            message.pose.pose.orientation.x = - this.transform.rotation.z;
            message.pose.pose.orientation.y = this.transform.rotation.x;
            message.pose.pose.orientation.z = - this.transform.rotation.y;
            message.pose.pose.orientation.w = this.transform.rotation.w;

            message.pose.covariance = new double[] {0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.1, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.1, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.1};

            ros.Publish(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
