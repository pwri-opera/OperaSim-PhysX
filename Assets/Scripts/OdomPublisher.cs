using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class OdomPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string robotName = "robot_name";
    public string topicName = "robot_name/diff_drive_controller/odom";
    public string childFrameName = "robot_name/base_link";
    private OdometryMsg message;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.05f;//20Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        message = new OdometryMsg();
        message.header = new RosMessageTypes.Std.HeaderMsg();
        message.header.stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();

        ros = ROSConnection.instance;
        ros.RegisterPublisher<OdometryMsg>(topicName);
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
            message.header.frame_id = robotName + "_tf/odom";
            message.header.stamp.sec = secs;
            message.header.stamp.nanosec = nsecs;
            message.child_frame_id = childFrameName;

            // Unity -> ROS transformation
            //Position: Unity(x,y,z) -> ROS(z,-x,y)
            //Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)
            message.pose.pose.position.x = ab.transform.localPosition.z;
            message.pose.pose.position.y = - ab.transform.localPosition.x;
            message.pose.pose.position.z = ab.transform.localPosition.y;

            message.pose.pose.orientation.x = - ab.transform.localRotation.z;
            message.pose.pose.orientation.y = ab.transform.localRotation.x;
            message.pose.pose.orientation.z = - ab.transform.localRotation.y;
            message.pose.pose.orientation.w = ab.transform.localRotation.w;

            message.pose.covariance = new double[] {0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0};

            message.twist.twist.linear.x = ab.velocity.z;
            message.twist.twist.linear.y = - ab.velocity.x;
            message.twist.twist.linear.z = ab.velocity.y;
            //message.twist.twist.linear.y = 0.0;
            //message.twist.twist.linear.z = 0.0;
            /*If 2D odomtetry's stablity is important linear y&z + angular x&y should be 0.0*/

            message.twist.twist.angular.x = ab.angularVelocity.z;
            message.twist.twist.angular.y = - ab.angularVelocity.x;
            //message.twist.twist.angular.x = 0.0;
            //message.twist.twist.angular.y = 0.0;
            message.twist.twist.angular.z = - ab.angularVelocity.y;

            message.twist.covariance = new double[] {0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0};

            ros.Send(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
