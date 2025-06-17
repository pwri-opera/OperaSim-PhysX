using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Com3;
using Unity.Robotics.UrdfImporter;
using Unity.Profiling;  // Profile 用

using DataSample = System.ValueTuple<float, double>; // 型エイリアス

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
    public bool enableDeadTime = true;

    private EmergencyStop emergencyStop;


    // 型エイリアス

    // 各ジョイントごとのバッファ
    private Dictionary<string, Queue<DataSample>> inputJointsQueue = new Dictionary<string, Queue<DataSample>>();

    private double unityDeadTime = 40.0f; // msec
    private double internalDeadTime; // msec

    private Dictionary<string, double> joints_dt;

    static readonly ProfilerCounterValue<double> k_JointAngleBoom = new(ProfilerCategory.Scripts, "Joint Angle Boom",
        ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);

    // Start is called before the first frame update
    void Start()
    {
        currentCmd = new JointCmdMsg();
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);

        joints = new Dictionary<string, Com3JointInfo>();
        joints_dt = new Dictionary<string, double>();
        foreach (var joint in this.GetComponentsInChildren<ArticulationBody>())
        {
            var ujoint = joint.GetComponent<UrdfJoint>();
            var jointtype = joint.GetComponent<Com3.ControlTypeAnnotation>();
            var jointdt = joint.GetComponent<DeadTime>();
            
            if (ujoint && jointtype && jointdt)
            {
                joints.Add(ujoint.jointName, new Com3JointInfo(joint, jointtype));
                joints_dt.Add(ujoint.jointName, jointdt.GetDeadTime());

                // むだ時間制御用データバッファインスタンス作成（各関節）
                inputJointsQueue.Add(ujoint.jointName, new Queue<DataSample>());

                // Debug.Log("Set joint dead time " + ujoint.jointName +  jointdt.GetDeadTime() + ".");

                ArticulationDrive drive = joint.xDrive;
                if (jointtype.GetControlType() == Com3.ControlType.Effort)
                {
                    Debug.LogWarning("Effort control not supported yet!");
                }
                if (jointtype.GetControlType() == Com3.ControlType.Velocity)
                {
                    Debug.Log("Set joint " + ujoint.jointName + " to velocity control.");
                    drive.stiffness = 0;
                    if (drive.damping == 0)
                        drive.damping = 1e+25f;
                }
                else
                {
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

        if (enableDeadTime == false)
        {
            Debug.Log("Normal Mode");
            ros.Subscribe<JointCmdMsg>(Utils.PreprocessNamespace(this.gameObject, com3FrontControllerTopicName), OnCommand);
        }
        else
        {
            Debug.Log("Dead Time Mode");
            ros.Subscribe<JointCmdMsg>(Utils.PreprocessNamespace(this.gameObject, com3FrontControllerTopicName), AddInputData);
        }        
    }

    void OnCommand(JointCmdMsg cmd)
    {
        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;
        currentCmd = cmd;
        for (int i = 0; i < currentCmd.joint_name.Length; i++)
        {
            try
            {
                var joint = joints[currentCmd.joint_name[i]];
                ArticulationDrive drive = joint.joint.xDrive;
                switch (joint.jointtype.GetControlType())
                {
                    case Com3.ControlType.Velocity:
                        drive.targetVelocity = (float)currentCmd.velocity[i] * Mathf.Rad2Deg;
                        break;
                    default:
                        if (currentCmd.joint_name[i] == "boom_joint")
                        {
                            k_JointAngleBoom.Value = drive.target;
                            Debug.Log("Angle: " + currentCmd.position[i] * Mathf.Rad2Deg);
                        }
                        drive.target = (float)currentCmd.position[i] * Mathf.Rad2Deg;
                        break;
                }
                joint.joint.xDrive = drive;
            }
            catch (KeyNotFoundException)
            {
                //Debug.LogWarning("Joint " + currentCmd.joint_name[i] + " not found.");
            }
        }
    }


    // ----- Functions for controlling with dead time ----- //

    void AddInputData(JointCmdMsg cmd)
    {
        currentCmd = cmd;
        float currentTime = Time.time * 1000; // sec -> msec

        for (int i = 0; i < currentCmd.joint_name.Length; i++)
        {
            try
            {
                var joint = joints[currentCmd.joint_name[i]];
                ArticulationDrive drive = joint.joint.xDrive;
                switch (joint.jointtype.GetControlType())
                {
                    case Com3.ControlType.Velocity:
                        // Queue にデータを push (velocity)
                        inputJointsQueue[currentCmd.joint_name[i]].Enqueue((currentTime, currentCmd.velocity[i]));
                        break;
                    default:
                        // Queue にデータを push (position)
                        inputJointsQueue[currentCmd.joint_name[i]].Enqueue((currentTime, currentCmd.position[i]));
                        break;
                }
                joint.joint.xDrive = drive;
            }
            catch (KeyNotFoundException)
            {
                //Debug.LogWarning("Joint " + currentCmd.joint_name[i] + " not found.");
            }
        }

    }

    void FixedUpdate()
    {
        double data_sample = 0.0f;

        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;

    
        for (int i = 0; i < currentCmd.joint_name.Length; i++)
        {
            try
            {
                if (joints_dt[currentCmd.joint_name[i]] == 0)
                {
                    return;
                }
                var joint = joints[currentCmd.joint_name[i]];
                var queue = inputJointsQueue[currentCmd.joint_name[i]];
                // データ取り出し
                // 疑似的なスレッドを使えると while を入れずに済む？            
                while (queue.Count > 0)
                {
                    var (timestamp, data) = queue.Peek();
                    if ((Time.time * 1000 - timestamp) >= internalDeadTime)
                    {
                        data_sample = data;
                        inputJointsQueue[currentCmd.joint_name[i]].Dequeue();
                    }
                    else if (queue.Count <= 0 || (Time.time - timestamp) < internalDeadTime)
                    {
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                // 関節を駆動
                    ArticulationDrive drive = joint.joint.xDrive;
                switch (joint.jointtype.GetControlType())
                {
                    case Com3.ControlType.Velocity:
                        drive.targetVelocity = (float)data_sample * Mathf.Rad2Deg;
                        break;
                    default:
                        drive.target = (float)data_sample * Mathf.Rad2Deg;
                        break;
                }
                joint.joint.xDrive = drive;
            }
            catch (KeyNotFoundException)
            {
                //Debug.LogWarning("Joint " + currentCmd.joint_name[i] + " not found.");
            }
        }
    }
}
