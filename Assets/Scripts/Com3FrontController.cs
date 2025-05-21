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
                    if (drive.damping == 0) {
                        drive.damping = 1e+25f;
                        // drive.damping = 0;
                    }
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
    
        float kp = 1000000f;
        float kd = 0f;

        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;
        currentCmd = cmd;
        for (int i = 0; i < currentCmd.joint_name.Length; i++) {
            Debug.Log("target");
            try {
                var joint = joints[currentCmd.joint_name[i]];
                ArticulationDrive drive = joint.joint.xDrive;
                Debug.Log(joint.jointtype.GetControlType());
                
                switch (joint.jointtype.GetControlType()) {
                    case Com3.ControlType.Velocity:

                        float q  = (float)joint.joint.jointVelocity[0];             
                        float dq = (float)joint.joint.jointAcceleration[0];             
                        float qd = (float)currentCmd.velocity[i];   

                        float torque = kp * (qd - q) - kd * dq;        // PD
                        // torque = 300;        // PD
                        joint.joint.AddRelativeTorque(new Vector3(0, -torque, 0));

                        // drive.targetVelocity = (float)currentCmd.velocity[i] * Mathf.Rad2Deg;
                        // Debug.Log("targetVel : " + i + ":" + currentCmd.velocity[i]);
                        // Debug.Log("currentVel : " + i + ":" + (float)joint.joint.jointVelocity[0]);
                        // Debug.Log("currentAccVel : " + i + ":" + (float)joint.joint.jointAcceleration[0]);
                        Debug.Log("InputTorque : " + i + ":" + torque);

                        break;
                    default:
                        drive.target = (float)currentCmd.position[i] * Mathf.Rad2Deg;
                        Debug.Log("target");
                        joint.joint.xDrive = drive;  
                        break;
                }
            } catch (KeyNotFoundException) {
                Debug.LogWarning("Joint " + currentCmd.joint_name[i] + " not found.");
            }
        }
    }
}
