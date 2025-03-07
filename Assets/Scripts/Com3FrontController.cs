using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Com3;
using Unity.Robotics.UrdfImporter;

public class Com3JointInfo
{
    public ArticulationBody joint;
    public Com3.ControlTypeAnnotation jointtype;

    public Com3JointInfo(ArticulationBody joint, Com3.ControlTypeAnnotation jointtype)
    {
        this.joint = joint;
        this.jointtype = jointtype;
    }
}

public class Com3FrontController : MonoBehaviour
{
    private ROSConnection ros;
    private Dictionary<string, Com3JointInfo> joints;
    private JointCmdMsg currentCmd;

    public string com3FrontControllerTopicName = "[robot_name]/front_cmd";

    private EmergencyStop emergencyStop;

    // Start is called before the first frame update
    void Start()
    {
        currentCmd = new JointCmdMsg();
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);

        joints = new Dictionary<string, Com3JointInfo>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>()) {
            var ujoint = joint.GetComponent<UrdfJoint>();
            var jointtype = joint.GetComponent<Com3.ControlTypeAnnotation>();
            if (ujoint && jointtype) {
                joints.Add(ujoint.jointName, new Com3JointInfo(joint, jointtype));
                ArticulationDrive drive = joint.xDrive;
                if (jointtype.GetControlType() == Com3.ControlType.Effort) {
                    Debug.LogWarning("Effort control not supported yet!");
                }
                if (jointtype.GetControlType() == Com3.ControlType.Velocity) {
                    Debug.Log("Set joint " + ujoint.jointName + " to velocity control.");
                    drive.stiffness = 0;
                    if (drive.damping == 0)
                        drive.damping = 1e+25f;
                } else {
                    Debug.Log("Set joint " + ujoint.jointName + " to position control.");
                    if (drive.stiffness == 0)
                        drive.stiffness = 200000;
                }
                if (drive.damping == 0)
                    drive.damping = 100000;
                if (drive.forceLimit == 0)
                    drive.forceLimit = 100000;
                joint.xDrive = drive;
            }
        }

        ros.Subscribe<JointCmdMsg>(Utils.PreprocessNamespace(this.gameObject, com3FrontControllerTopicName), OnCommand);
    }

    void OnCommand(JointCmdMsg cmd)
    {
        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;
        currentCmd = cmd;
        for (int i = 0; i < currentCmd.joint_name.Length; i++) {
            try {
                var joint = joints[currentCmd.joint_name[i]];
                ArticulationDrive drive = joint.joint.xDrive;
                switch (joint.jointtype.GetControlType()) {
                    case Com3.ControlType.Velocity:
                        drive.targetVelocity = (float)currentCmd.velocity[i] * Mathf.Rad2Deg;
                        break;
                    default:
                        drive.target = (float)currentCmd.position[i] * Mathf.Rad2Deg;
                        break;
                }
                joint.joint.xDrive = drive;                
            } catch (KeyNotFoundException) {
                //Debug.LogWarning("Joint " + currentCmd.joint_name[i] + " not found.");
            }
        }
    }
}
