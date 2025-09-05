using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Com3;
using Unity.Robotics.Core;
using System;
using PID_Controller;

public enum ProjectionMode
{
    Radial,         // 原点方向へ等比縮小
    RadialRatio     // 
}


/// <summary>
/// 差動駆動車の制御を行う
/// </summary>
public class DiffDriveController : MonoBehaviour
{
    private ROSConnection ros;

    [Tooltip("VW挙動調整モードを使うか？\n（VW挙動調整モード：cmd_vel (vw入力) が与えられた際，vw組合せ時の出力を制限するモード")]
    public bool EnableVWBehaviorMode = true;

    [Tooltip("車体並進速度v と 車体旋回速度ω　が同時に与えられた際に，出力値を抑えるパラメータ: 値が小さい程，v，w 同時出力時の速度が制限される")]
    public double VWDecelFactor = 1.0;

    [Tooltip("車体並進速度v と 車体旋回速度ω の縮小配分を決める重み（0＝v優先でωを多く削る、1＝ω優先でvを多く削る）")]
    public double VWRatioFactor = 0.9;

    [Tooltip("左の車輪のgameObjectを登録してください（複数登録可能）")]
    public List<GameObject> leftWheels;
        
    [Tooltip("右の車輪のgameObjectを登録してください（複数登録可能）")]
    public List<GameObject> rightWheels;

    private OdometryMsg odomMessage;

    [Tooltip("ROSトピック名の頭に付与されるロボット名")]
    public string robotName = "[robot_name]";
    private string preprocessedRobotName;

    [Tooltip("cmd_velコマンドを受け取るROSトピック名")]
    public string TwistTopicName = "[robot_name]/tracks/cmd_vel"; // Subscribe Messsage Topic Name

    [Tooltip("オドメトリ情報を出力するROSトピック名")]
    public string OdomTopicName = "[robot_name]/odom"; // Publish Message Topic Name
    private string preprocessedOdomTopicName;

    public string Com3TopicName = "[robot_name]/track_cmd"; // Subscribe Messsage Topic Name

    [Tooltip("オドメトリ情報を出力する際に用いられる基準フレーム名")]
    public string childFrameName = "[robot_name]/base_link";
    private string preprocessedChildFrameName;

    [Tooltip("オドメトリを計算する際にはトレッド幅が用いられます。トレッド幅に係数をかけることで計算を補正できます。")]
    public double treadCollectionFactor = 2.0; // Factor Collecting Yaw angle of base_link. This Parameter is multiplied to tread to calculate angular velocity based on Vehicle's Kinematics.

    private List<WheelCollider> leftWheelColliders;
    private List<WheelCollider> rightWheelColliders;
    private WheelCollider leftMiddleWheel;
    private WheelCollider rightMiddleWheel;

    [Tooltip("PID制御を用いて車輪を制御する際の比例ゲイン")]
    public double pGain = 100.0;

    [Tooltip("PID制御を用いて車輪を制御する際の積分ゲイン")]
    public double iGain = 0.0;

    [Tooltip("PID制御を用いて車輪を制御する際の微分ゲイン")]
    public double dGain = 0.0;

    [Tooltip("PID制御を用いて車輪を制御する際の最大トルク")]
    public double torqueLimit = 1000.0;

    [Tooltip("車輪を静止する際に用いられるブレーキトルク")]
    public float brakeTorque = 10000.0F;

    [Tooltip("cmd_velコマンドで指定可能な最大速度(m/s)")]
    public double maxLinearVelocity = 3.00;  // unit is m/sec

    [Tooltip("cmd_velコマンドで指定可能な最大角速度(度/s)")]
    public double maxAngularVelocity = Math.PI * 2.0 * 5.0 / 360.0;  // unit is rad/sec

    private List<PID> leftWheelControllers;
    private List<PID> rightWheelControllers;

    private double leftVelCmd = 0.0f; // Velocity Command for Left Track
    private double rightVelCmd = 0.0f; // Velocity Command for Right Track

    private double tread_half = 2.0;
    private double previousTime = 0.0;
    private double yaw = 0.0;

    // Publish the cube's position and rotation every N seconds
    [Tooltip("ROSメッセージの出力間隔(秒)")]
    public float publishMessageInterval = 0.02f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private double timeElapsed;

    private EmergencyStop emergencyStop;

    private static double Abs(double x) => Math.Abs(x);
    private static double Pow(double x, double e) => Math.Pow(x, e);
    private static double Clamp(double x, double lo, double hi) => x < lo ? lo : (x > hi ? hi : x);
    private static double Clamp01(double x) => Clamp(x, 0.0, 1.0);
    private static double SignNonzero(double x) => x < 0.0 ? -1.0 : (x > 0.0 ? 1.0 : 0.0);

    // Start is called before the first frame update
    protected virtual void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        emergencyStop = EmergencyStop.GetEmergencyStop(this.gameObject);
        leftWheelColliders = new List<WheelCollider>();
        rightWheelColliders = new List<WheelCollider>();

        leftVelCmd = 0.0; // Velocity Command for Left Track
        rightVelCmd = 0.0; // Velocity Command for Right Track

        leftWheelControllers = new List<PID>();
        rightWheelControllers = new List<PID>();

        odomMessage = new OdometryMsg();
        odomMessage.header = new HeaderMsg();
        odomMessage.header.stamp = new TimeMsg();

        /* Get ArticulationBody-type Components in Left Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject left in leftWheels)
        {
            var body = left.GetComponent<WheelCollider>();
            body.ConfigureVehicleSubsteps(5f, 100, 100);
            leftWheelColliders.Add(body);
            Debug.Log("Check left!");

            /* Get ArticulationBody-type Component named "left_middle_wheel_link" */
            if(left.name == "left_middle_wheel_link"){
                leftMiddleWheel = body;
            }
            leftWheelControllers.Add(new PID(pGain, iGain, dGain, 1, torqueLimit, -torqueLimit));
        }
        /* Get ArticulationBody-type Components in Right Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject right in rightWheels)
        {
            var body = right.GetComponent<WheelCollider>();
            body.ConfigureVehicleSubsteps(5f, 100, 100);
            rightWheelColliders.Add(body);
            Debug.Log("Check right!");
            
            /* Get ArticulationBody-type Component named "right_middle_wheel_link" */
            if(right.name == "right_middle_wheel_link"){
                rightMiddleWheel = body;
            }
            rightWheelControllers.Add(new PID(pGain, iGain, dGain, 1, torqueLimit, -torqueLimit));
        }
        tread_half = Mathf.Abs(leftWheels[0].transform.localPosition.x - rightWheels[0].transform.localPosition.x)/2;

        preprocessedRobotName = Utils.PreprocessNamespace(this.gameObject, robotName);
        preprocessedChildFrameName = Utils.PreprocessNamespace(this.gameObject, childFrameName);
        preprocessedOdomTopicName = Utils.PreprocessNamespace(this.gameObject, OdomTopicName);

        Debug.Log("DiffDriveController starts!!");
        ros.Subscribe<TwistMsg>(Utils.PreprocessNamespace(this.gameObject, TwistTopicName), ExecuteTwist); //Register Subscriber
        ros.Subscribe<JointCmdMsg>(Utils.PreprocessNamespace(this.gameObject, Com3TopicName), ExecuteJointCmd); //Register Subscriber
        ros.RegisterPublisher<OdometryMsg>(preprocessedOdomTopicName); //Register Publisher
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timeElapsed += Time.fixedDeltaTime;

        double time = Time.fixedTimeAsDouble;
        double deltaTime = time - previousTime;

        double leftTrackVel = 2.0 * Math.PI * leftMiddleWheel.rpm / 60.0; // Unit is [rad/s]
        double rightTrackVel = 2.0 * Math.PI * rightMiddleWheel.rpm / 60.0; // Unit is [rad/s]
        // Debug.Log("LeftTrackRPM:" + leftMiddleWheel.rpm);
        // Debug.Log("RightTrackRPM:" + rightMiddleWheel.rpm);
        // Debug.Log("LeftTrackVelocity:" + leftTrackVel);
        // Debug.Log("RightTrackVelocity:" + rightTrackVel);

        /* To Get Track's Radius use wheel collider parameter*/
        double leftTrackRadius = leftMiddleWheel.radius;
        double rightTrackRadius = rightMiddleWheel.radius;
        // Debug.Log("LeftTrackRadius:"+leftTrackRadius);
        // Debug.Log("RightTrackRadius:"+rightTrackRadius);

        /* velocity =  angular velocity[rad/s] * radius[m] */
        double leftVelMes = leftTrackVel * leftTrackRadius; // Measured Velocity from Left Track. Unit is [m/s]
        double rightVelMes = rightTrackVel * rightTrackRadius; // Measured Velocity from Right Track. Unit is [m/s]
        // Debug.Log("LeftJointVelocity:"+leftVelMes);
        // Debug.Log("RightJointVelocity:"+rightVelMes);

        /* Calculate linear and angular velocity based on kinematics */
        double linearVel = (rightVelMes + leftVelMes)/2.0;
        double angularVel = (rightVelMes - leftVelMes)/(2.0*tread_half*treadCollectionFactor);   
        // Debug.Log("LinearVelocity:"+linearVel);
        // Debug.Log("AngularVelocity:"+angularVel);
        // Debug.Log("tread_half:"+tread_half);
        // Debug.Log("deltaTime:"+deltaTime);

        yaw += angularVel * deltaTime;
        /* Normalize Yaw [batween -PI and +PI] */
        if(Mathf.Abs((float)yaw) > Mathf.PI){
            yaw -= (double)(2*Mathf.PI*Mathf.Sign((float)yaw));
        }

        odomMessage.pose.pose.position.x += linearVel * (double)Mathf.Cos((float)yaw) * deltaTime;
        odomMessage.pose.pose.position.y += linearVel * (double)Mathf.Sin((float)yaw) * deltaTime;

        // Debug.Log("x:"+odomMessage.pose.pose.position.x);
        // Debug.Log("y:"+odomMessage.pose.pose.position.y);
        // Debug.Log("yaw:"+yaw);

        Quaternion rotation = Quaternion.Euler(0, 0, (float)(yaw*180.0/(double)Mathf.PI));

        odomMessage.pose.pose.orientation.w = rotation.w;
        odomMessage.pose.pose.orientation.x = rotation.x;
        odomMessage.pose.pose.orientation.y = rotation.y;
        odomMessage.pose.pose.orientation.z = rotation.z;

        odomMessage.pose.covariance = new double[] {0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0};

        odomMessage.twist.twist.linear.x = linearVel;
        odomMessage.twist.twist.linear.y = 0.0;
        odomMessage.twist.twist.linear.z = 0.0;

        odomMessage.twist.twist.angular.x = 0.0;
        odomMessage.twist.twist.angular.y = 0.0;
        odomMessage.twist.twist.angular.z = angularVel;

        odomMessage.twist.covariance = new double[] {0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.001, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000000.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 1000.0};
        
        if (timeElapsed >= publishMessageInterval)
        {
            odomMessage.header.frame_id = preprocessedRobotName + "_tf/odom";
            odomMessage.header.stamp = new TimeStamp(Clock.time);
            odomMessage.child_frame_id = preprocessedChildFrameName;

            ros.Publish(preprocessedOdomTopicName, odomMessage);
            timeElapsed = 0.0f;
        }

        if (emergencyStop && emergencyStop.isEmergencyStop) {
            leftVelCmd = 0.0;
            rightVelCmd = 0.0;
        }

        /* Set targetVelocity in xDrive in wheels */
        var ts = TimeSpan.FromSeconds(deltaTime);
        for (var i = 0; i < leftWheelColliders.Count; i++) {
            var left = leftWheelColliders[i];
            var pid = leftWheelControllers[i];
            var v = (float)pid.PID_iterate(leftVelCmd, leftVelMes, ts);
            if (Math.Abs(leftVelCmd) < 0.001)
            {
                left.brakeTorque = brakeTorque;
                left.motorTorque = 0.0F;
            }
            else
            {
                left.brakeTorque = 0.0F;
                left.motorTorque = v;
            }
            //Debug.Log("LeftJointVelocityPID:" + v);
        }
        for (var i = 0; i < rightWheelColliders.Count; i++)
        {
            var right = rightWheelColliders[i];
            var pid = rightWheelControllers[i];
            var v = (float)pid.PID_iterate(rightVelCmd, rightVelMes, ts);
            if (Math.Abs(rightVelCmd) < 0.001)
            {
                right.brakeTorque = brakeTorque;
                right.motorTorque = 0.0F;
            }
            else
            {
                right.brakeTorque = 0.0F;
                right.motorTorque = v;
            }
            //Debug.Log("RightJointVelocityPID:" + v);
        }

        //Debug.Log("LeftJointVelocityDiff:" + (leftVelCmd - leftTrackVel));
        //Debug.Log("RightJointVelocityDiff:" + (rightVelCmd - rightTrackVel));

        previousTime = time;
    }

    void CommandLinearAngularVelocity(double cmdLinearVel, double cmdAngularVel)
    {
        /* Calculate velocity command value based on inverse kinematics */
        cmdLinearVel = Math.Min(cmdLinearVel, maxLinearVelocity);
        cmdLinearVel = Math.Max(cmdLinearVel, -maxLinearVelocity);
        cmdAngularVel = Math.Min(cmdAngularVel, maxAngularVelocity);
        cmdAngularVel = Math.Max(cmdAngularVel, -maxAngularVelocity);
        leftVelCmd = (cmdLinearVel - tread_half * cmdAngularVel); // Unit is [m/s]
        rightVelCmd = (cmdLinearVel + tread_half * cmdAngularVel); // Unit is [m/s]
        // Debug.Log("LeftJointVelocityCommand:" + leftVelCmd);
        // Debug.Log("RightJointVelocityCommand:" + rightVelCmd);
    }


    void CommandLinearAngularVelocityVWBehaviorMode(double cmdLinearVel, double cmdAngularVel)
    {

        /* Calculate velocity command value based on inverse kinematics */
        cmdLinearVel = Math.Min(cmdLinearVel, maxLinearVelocity);
        cmdLinearVel = Math.Max(cmdLinearVel, -maxLinearVelocity);
        cmdAngularVel = Math.Min(cmdAngularVel, maxAngularVelocity);
        cmdAngularVel = Math.Max(cmdAngularVel, -maxAngularVelocity); 

        double p = VWDecelFactor;
        double ratio = VWRatioFactor;

        // 1. 可行域判定
        double g = Math.Pow(
                    Math.Pow(Math.Abs(cmdLinearVel) / maxLinearVelocity, p) +
                    Math.Pow(Math.Abs(cmdAngularVel) / maxAngularVelocity, p),
                    1.0 / p);

        double v_out = cmdLinearVel;
        double w_out = cmdAngularVel;

        ProjectionMode projMode = ProjectionMode.RadialRatio;

        if (g > 1.0)        // ===== 投影が必要 =====
        {
            switch (projMode)
            {
                // --- 原点に向け等比縮小 (Radial) -----------------
                case ProjectionMode.Radial:
                    double s = 1.0 / g;
                    v_out *= s;
                    w_out *= s;
                    break;

                case ProjectionMode.RadialRatio:
                    (v_out, w_out) = ProjectByRatioScale(
                        cmdLinearVel, cmdAngularVel,
                        maxLinearVelocity, maxAngularVelocity,
                        p, ratio);
                    break;
            }
        }

        // 2. 左右クローラ速度へ変換 (逆運動学)
        leftVelCmd = v_out - tread_half * w_out;   // [m/s]
        rightVelCmd = v_out + tread_half * w_out;   // [m/s]

        // --- デバッグ出力（任意） -------------------------------
        // Debug.Log($"projMode={projMode}  v_in={cmdLinearVel:F2} ω_in={cmdAngularVel:F2} "
        //         +$"-> v_out={v_out:F2} ω_out={w_out:F2}");
    }

    /// <summary>
    /// ratio に基づく異方性スケーリング投影
    /// v_out = s^alpha * v_in, w_out = s^(1-alpha) * w_in を満たす s を二分法で解く
    /// </summary>
    private static (double v_out, double w_out) ProjectByRatioScale(
        double v_in, double w_in, double v_max, double w_max, double p, double ratio)
    {
        double V = Abs(v_in) / v_max;
        double W = Abs(w_in) / w_max;

        // 端点は数値的に不安定なので少しだけ離す
        double alpha = Clamp01(ratio);
        const double EPS = 1e-6;
        alpha = Clamp(alpha, EPS, 1.0 - EPS);

        double Va = Pow(V, p);
        double Wb = Pow(W, p);
        double ap = alpha * p;
        double bp = (1.0 - alpha) * p;

        // f(s) = Va*s^ap + Wb*s^bp - 1 = 0 を s∈(0,1] で解く
        Func<double, double> f = s => Va * Pow(s, ap) + Wb * Pow(s, bp) - 1.0;

        double sL = 0.0;     // f(sL) < 0
        double sR = 1.0;     // f(sR) >= 0
        for (int i = 0; i < 50; i++)
        {
            double sM = 0.5 * (sL + sR);
            double fM = f(sM);
            if (fM < 0.0) sL = sM; else sR = sM;
        }
        double s = 0.5 * (sL + sR);

        double scaleV = Pow(s, alpha);
        double scaleW = Pow(s, 1.0 - alpha);

        double v_out = SignNonzero(v_in) * scaleV * Abs(v_in);
        double w_out = SignNonzero(w_in) * scaleW * Abs(w_in);

        // 浮動誤差の安全クリップ
        v_out = Clamp(v_out, -v_max, v_max);
        w_out = Clamp(w_out, -w_max, w_max);
        return (v_out, w_out);
    }

    void ExecuteTwist(TwistMsg twist)
    {
        //Debug.Log("Linear Velocity:"+twist.linear.x);
        //Debug.Log("Angular Velocity:"+twist.angular.z);

        // ここで条件分岐を行い，vw 挙動調整モードを 使うかどうか選択
        if (EnableVWBehaviorMode == true)
        {
            CommandLinearAngularVelocityVWBehaviorMode(twist.linear.x, twist.angular.z);
        }
        else
        {
            CommandLinearAngularVelocity(twist.linear.x, twist.angular.z);
        }
    }

    void ExecuteJointCmd(JointCmdMsg cmd)
    {
        double linearVelCmd = Double.NaN, angularVelCmd = Double.NaN;
        for (int i = 0; i < cmd.joint_name.Length; i++) {
            if (cmd.joint_name[i] == "left_track") {
                leftVelCmd = cmd.velocity[i];
            } else if (cmd.joint_name[i] == "right_track") {
                rightVelCmd = cmd.velocity[i];
            } else if (cmd.joint_name[i] == "forward_volume") {
                linearVelCmd = cmd.velocity[i];
            } else if (cmd.joint_name[i] == "turn_volume") {
                angularVelCmd = cmd.velocity[i];
            }
        }
        if (!double.IsNaN(linearVelCmd) && !double.IsNaN(angularVelCmd)) {
            CommandLinearAngularVelocity(linearVelCmd, angularVelCmd);
        }
    }
}
