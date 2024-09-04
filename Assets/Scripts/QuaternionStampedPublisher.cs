using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;


public class QuaternionStampedPublisher : MonoBehaviour
{
    ROSConnection ros;
    public string frameID = "frame_id";
    public string topicName = "robot_name/unity/quat_stmp";
    private QuaternionStampedMsg message;

    // Publish the object's position and rotation every N seconds
    public float publishMessageInterval = 0.1f;//10Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        message = new QuaternionStampedMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();

        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<QuaternionStampedMsg>(topicName);
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;
        // Get Rigidbody
        ArticulationBody ab = this.transform.GetComponent<ArticulationBody> ();
    
        if (timeElapsed >= publishMessageInterval)
        {
            message.header.frame_id = frameID;
            message.header.stamp = new TimeStamp(Clock.time);

            // Unity -> ROS transformation
            // Position: Unity(x,y,z) -> ROS(z,-x,y)
            // Quaternion: Unity(x,y,z,w) -> ROS(-z,x,-y,w)
            message.quaternion.x = - this.transform.rotation.z;
            message.quaternion.y = this.transform.rotation.x;
            message.quaternion.z = - this.transform.rotation.y;
            message.quaternion.w = this.transform.rotation.w;

            ros.Publish(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
