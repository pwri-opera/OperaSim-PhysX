using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;
using Unity.Robotics.Core;

public class JointStatePublisher : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "joint_states";
    private JointStateMsg message;
    private List<ArticulationBody> joints;
    private List<string> jointNames;

    public bool enableJointEffortSensor = false;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.5f;

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        joints = new List<ArticulationBody>();
        jointNames = new List<string>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            if (joint.isActiveAndEnabled)
            {
                var ujoint = joint.GetComponent<UrdfJoint>();
                if (ujoint && !(ujoint is UrdfJointFixed))
                {
                    joints.Add(joint);
                    jointNames.Add(ujoint.jointName);
                }
            }
        }
        message = new JointStateMsg();
        message.header = new HeaderMsg();
        message.header.stamp = new TimeMsg();
        message.name = jointNames.ToArray();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<JointStateMsg>(topicName);
    }

    // Update is called once per constant rate
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= publishMessageInterval)
        {
            message.header.frame_id = "world";
            message.header.stamp = new TimeStamp(Clock.time);
            message.position = new double[joints.Count];
            message.velocity = new double[joints.Count];
            message.effort = new double[joints.Count];
            for (int i = 0; i < joints.Count; i++)
            {
                message.position[i] = joints[i].jointPosition[0];
                message.velocity[i] = joints[i].jointVelocity[0];
                message.effort[i] = enableJointEffortSensor ? joints[i].driveForce[0] : 0.0;
            }
            ros.Publish(topicName, message);
            timeElapsed = 0.0f;
        }
    }
}
