using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class JointStatePublisher : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "joint_states";
    private JointStateMsg message;
    private List<ArticulationBody> joints;
    private List<string> jointNames;

    // Start is called before the first frame update
    void Start()
    {
        joints = new List<ArticulationBody>();
        jointNames = new List<string>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            var ujoint = joint.GetComponent<UrdfJoint>();
            if (ujoint && !(ujoint is UrdfJointFixed))
            {
                joints.Add(joint);
                jointNames.Add(ujoint.jointName);
            }
        }
        message = new JointStateMsg();
        message.header = new RosMessageTypes.Std.HeaderMsg();
        message.header.stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();
        message.name = jointNames.ToArray();
        ros = ROSConnection.instance;
        ros.RegisterPublisher<JointStateMsg>(topicName);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float sim_time = Time.time;
        uint secs = (uint)sim_time;
        uint nsecs = (uint)((sim_time % 1) * 1e6);
        message.header.frame_id = "world";
        message.header.stamp.sec = secs;
        message.header.stamp.nanosec = nsecs;
        message.position = new double[joints.Count];
        message.velocity = new double[joints.Count];
        message.effort = new double[joints.Count];
        for (int i = 0; i < joints.Count; i++)
        {
            message.position[i] = joints[i].jointPosition[0];
            message.velocity[i] = joints[i].jointVelocity[0];
            message.effort[i] = joints[i].jointForce[0];
        }
        ros.Send(topicName, message);
    }
}
