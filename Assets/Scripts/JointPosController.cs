using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// 各関節を独立して制御する
/// </summary>
public class JointPosController : MonoBehaviour
{
    private ROSConnection ros;

    [Tooltip("角度設定コマンドのROSトピック名")]
    public string setpointTopicName = "joint_name/setpoint";

    [Tooltip("初期の目標角度(degree)")]
    public double initTargetPos;

    [Tooltip("関節目標角速度の上限 (deg/s)。0 = 無制限（従来動作）")]
    [Min(0)] public float maxAngularSpeed = 0f;

    [Tooltip("ランプ目標の微分を drive.targetVelocity に供給する（速度フィードフォワード）")]
    public bool enableVelocityFeedforward = true;

    private ArticulationBody joint;
    private Float64Msg targetPos;
    private EmergencyStop emergencyStop;
    private bool currentEmergencyStop = false;
    private float emergencyStopPosition = 0.0f;

    // 最新の指令角 (deg) と、速度制限で整形された現在目標角 (deg)
    private float commandedTargetDeg;
    private float currentTargetDeg;
    private bool hasCommand = false;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f); // 少し待機してから設定
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);
        joint = this.GetComponent<ArticulationBody>();
        targetPos = new Float64Msg();

        if (joint)
        {
            if (joint.GetComponent<Com3.ControlTypeAnnotation>() == null)
            {
                var drive = joint.xDrive;
                if (drive.stiffness == 0)
                    drive.stiffness = 200000;
                if (drive.damping == 0)
                    drive.damping = 100000;
                if (drive.forceLimit == 0)
                    drive.forceLimit = 100000;

                drive.target = (float)initTargetPos;
                drive.targetVelocity = 0f;
                joint.xDrive = drive;
                currentTargetDeg = commandedTargetDeg = (float)initTargetPos;
                hasCommand = true;
            }
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        ros.Subscribe<Float64Msg>(Utils.PreprocessNamespace(this.gameObject, setpointTopicName), ExecuteJointPosControl);
    }

    void FixedUpdate()
    {
        if (joint == null)
            return;

        var drive = joint.xDrive;
        float dt = Time.fixedDeltaTime;

        if (emergencyStop && emergencyStop.isEmergencyStop)
        {
            if (currentEmergencyStop == false)
            {
                emergencyStopPosition = joint.jointPosition[0] * Mathf.Rad2Deg;
                currentEmergencyStop = true;
            }
            drive.target = emergencyStopPosition;
            drive.targetVelocity = 0f;
            joint.xDrive = drive;
            // e-stop解除時にランプが指令値へ跳ばないよう、保持角から再開する
            currentTargetDeg = emergencyStopPosition;
            commandedTargetDeg = emergencyStopPosition;
            return;
        }
        currentEmergencyStop = false;

        if (!hasCommand)
            return;

        float prevTargetDeg = currentTargetDeg;
        float nextTargetDeg = (maxAngularSpeed > 0f && dt > 0f)
            ? Mathf.MoveTowards(prevTargetDeg, commandedTargetDeg, maxAngularSpeed * dt)
            : commandedTargetDeg;

        drive.target = nextTargetDeg;
        if (enableVelocityFeedforward)
            drive.targetVelocity = (dt > 0f) ? (nextTargetDeg - prevTargetDeg) / dt : 0f;
        joint.xDrive = drive;
        currentTargetDeg = nextTargetDeg;
    }

    void ExecuteJointPosControl(Float64Msg msg)
    {
        if (emergencyStop && emergencyStop.isEmergencyStop)
            return;
        targetPos = msg;
        // 指令角を保持するのみ。drive.target への反映は FixedUpdate で
        // 速度制限とフィードフォワードと共に行う。
        commandedTargetDeg = (float)(targetPos.data * Mathf.Rad2Deg);
        // 初回指令時は実際の関節角からランプを開始する（0基点の誤移動を防ぐ）
        if (!hasCommand && joint != null)
            currentTargetDeg = joint.jointPosition[0] * Mathf.Rad2Deg;
        hasCommand = true;
        //Debug.Log("Joint Target Position:" + targetPos.data);
    }
}
