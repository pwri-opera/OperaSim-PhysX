using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.UrdfImporter;

public class FollowJointTrajectoryAction : MonoBehaviour
{
    private ROSConnection ros;
    private const float JOINT_ASSIGNMENT_WAIT = 0.038f;
    private Dictionary<string, ArticulationBody> jointArticulationBodies;
    private JointStateMsg currentPose;

    public string fakeControllerTopicName = "move_group/fake_controller_joint_states";
    public List<GameObject> initialPoseObjects;
    public List<float> initialPoseValues;

    // Start is called before the first frame update
    void Start()
    {
        currentPose = new JointStateMsg();
        ros = ROSConnection.GetOrCreateInstance();
        jointArticulationBodies = new Dictionary<string, ArticulationBody>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            var ujoint = joint.GetComponent<UrdfJoint>();
            if (ujoint)
            {
                jointArticulationBodies.Add(ujoint.jointName, joint);
                ArticulationDrive drive = joint.xDrive;
                if (drive.stiffness == 0)
                    drive.stiffness = 200000;
                if (drive.damping == 0)
                    drive.damping = 100000;
                if (drive.forceLimit == 0)
                    drive.forceLimit = 100000;
                joint.xDrive = drive;
            }
        }
        if (initialPoseObjects.Count == initialPoseValues.Count)
        {
            for (int i = 0; i < initialPoseObjects.Count; i++)
            {
                var body = initialPoseObjects[i].GetComponent<ArticulationBody>();
                ArticulationDrive drive = body.xDrive;
                drive.target = initialPoseValues[i];
                body.xDrive = drive;
            }
        }
        else
        {
            Debug.Log("size of InitialPoseObject and InitialPoseValues does not match");
        }
        ros.Subscribe<JointStateMsg>(fakeControllerTopicName, ExecuteTrajectory);
    }

    void ExecuteTrajectory(JointStateMsg trajectory)
    {
        currentPose = trajectory;
        for (int i = 0; i < currentPose.name.Length; i++)
        {
            var joint = jointArticulationBodies[currentPose.name[i]];
            ArticulationDrive drive = joint.xDrive;
            drive.target = (float)currentPose.position[i] * Mathf.Rad2Deg;
            joint.xDrive = drive;
        }
    }
}
