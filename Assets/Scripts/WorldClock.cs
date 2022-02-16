 using System.Collections;
 using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Rosgraph;

// https://github.com/Field-Robotics-Japan/vtc_unity/blob/master/Assets/unit04_unity/Scripts/ROSClock/ROSClock.cs
public class WorldClock : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "clock";
    private ClockMsg message;

    // Start is called before the first frame update
    void Start()
    {
        message = new ClockMsg();
        ros = ROSConnection.instance;
        ros.RegisterPublisher<ClockMsg>(topicName);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float sim_time = Time.time;
        uint secs = (uint)sim_time;
        uint nsecs = (uint)((sim_time % 1) * 1e9);
        message.clock.sec = secs;
        message.clock.nanosec = nsecs;
        ros.Send(topicName, message);
    }
}
