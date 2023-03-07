using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Control;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;

public class DiffDriveController : MonoBehaviour
{
    private ROSConnection ros;

    public List<GameObject> leftWheels;
    public List<GameObject> rightWheels;

    private OdometryMsg odomMessage;

    public string robotName = "robot_name";
    public string TwistTopicName = "robot_name/tracks/cmd_vel"; // Subscribe Messsage Topic Name
    public string OdomTopicName = "robot_name/odom"; // Publish Message Topic Name
    public string childFrameName = "robot_name/base_link";
    public double treadCollectionFactor = 2.0; // Factor Collecting Yaw angle of base_link. This Parameter is multiplied to tread to calculate angular velocity based on Vehicle's Kinematics.
    private List<ArticulationBody> leftBodies;
    private List<ArticulationBody> rightBodies;
    private ArticulationBody leftMiddleWheel;
    private ArticulationBody rightMiddleWheel;

    private TwistMsg twist;
    private double tread_half = 2.0;
    private double previousTime = 0.0;
    private double yaw = 0.0;

    // Publish the cube's position and rotation every N seconds
    public float publishMessageInterval = 0.02f;//50Hz

    // Used to determine how much time has elapsed since the last message was published
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.instance;
        leftBodies = new List<ArticulationBody>();
        rightBodies = new List<ArticulationBody>();
        twist = new TwistMsg();

        odomMessage = new OdometryMsg();
        odomMessage.header = new RosMessageTypes.Std.HeaderMsg();
        odomMessage.header.stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();

        /* Get ArticulationBody-type Components in Left Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject left in leftWheels)
        {
            var body = left.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            /* These parametaers has no reason to be set as each value (eiya!) */
            body.mass = 50.0f;
            drive.stiffness = 0;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            /* These parametaers has no reason to be set as each value (eiya!) */
            body.xDrive = drive;
            leftBodies.Add(body);
            Debug.Log("Check left!");

            /* Get ArticulationBody-type Component named "left_middle_wheel_link" */
            if(left.name == "left_middle_wheel_link"){
                leftMiddleWheel = body;
            }
        }
        /* Get ArticulationBody-type Components in Right Wheels and Set Parameters for xDrive in each Component */
        foreach (GameObject right in rightWheels)
        {
            var body = right.GetComponent<ArticulationBody>();
            var drive = body.xDrive;
            /* These parametaers has no reason to be set as each value (eiya!) */
            body.mass = 50.0f;
            drive.stiffness = 0;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            /* These parametaers has no reason to be set as each value (eiya!) */
            body.xDrive = drive;
            rightBodies.Add(body);
            Debug.Log("Check right!");
            
            /* Get ArticulationBody-type Component named "right_middle_wheel_link" */
            if(right.name == "right_middle_wheel_link"){
                rightMiddleWheel = body;
            }
        }
        tread_half = Mathf.Abs(leftWheels[0].transform.localPosition.x - rightWheels[0].transform.localPosition.x)/2;

        Debug.Log("DiffDriveController starts!!");
        ros.Subscribe<TwistMsg>(TwistTopicName, ExecuteTwist); //Register Subscriber
        ros.RegisterPublisher<OdometryMsg>(OdomTopicName); //Register Publisher
    }

    // Update is called once per frame
    void Update()
    {
        //const float speed = 100.0f;
        float leftVelCmd = 0.0f; // Velocity Command for Left Track
        float rightVelCmd = 0.0f; // Velocity Command for Right Track
        double leftVelMes = 0.0; // Measured Velocity from Left Track
        double rightVelMes = 0.0; // Measured Velocity from Right Track

        timeElapsed += Time.deltaTime;

        double time = Time.fixedTimeAsDouble;
        double deltaTime = time - previousTime;
    
        double leftTrackRadius = 0.0;
        double rightTrackRadius = 0.0;
        ArticulationReducedSpace leftTrackVelArs;
        ArticulationReducedSpace rightTrackVelArs;

        leftTrackVelArs = leftMiddleWheel.jointVelocity; // Unit is [rad/s]
        rightTrackVelArs = rightMiddleWheel.jointVelocity; // Unit is [rad/s]

        /* To Get Track's Radius use wheel collider parameter*/
        var leftWheelCollider = leftMiddleWheel.GetComponent<WheelCollider>();
        var rightWheelCollider = rightMiddleWheel.GetComponent<WheelCollider>();
        leftTrackRadius = leftWheelCollider.radius;
        rightTrackRadius = rightWheelCollider.radius;
        //Debug.Log("LeftTrackRadius:"+leftTrackRadius);
        //Debug.Log("RightTrackRadius:"+rightTrackRadius);

        /* velocity =  angular velocity[rad/s] * radius[m] */
        leftVelMes = leftTrackVelArs[0] * leftTrackRadius; // Unit is [m/s]
        rightVelMes = rightTrackVelArs[0] * rightTrackRadius; // Unit is [rad/s]
        // Debug.Log("LeftJointVelocity:"+leftVelMes);
        // Debug.Log("RightJointVelocity:"+rightVelMes);

        double linearVel = 0.0;
        double angularVel = 0.0;

        /* Calculate linear and angular velocity based on kinematics */
        linearVel = (rightVelMes + leftVelMes)/2.0;
        angularVel = (rightVelMes - leftVelMes)/(2.0*tread_half*treadCollectionFactor);   
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
            float sim_time = Time.time;
            uint secs = (uint)sim_time;
            uint nsecs = (uint)((sim_time % 1) * 1e9);
            odomMessage.header.frame_id = robotName + "_tf/odom";
            odomMessage.header.stamp.sec = secs;
            odomMessage.header.stamp.nanosec = nsecs;
            odomMessage.child_frame_id = childFrameName;

            ros.Send(OdomTopicName, odomMessage);
            timeElapsed = 0.0f;
        }

        /* Calculate velocity command value based on inverse kinematics */
        rightVelCmd = (float)(twist.linear.x + tread_half * twist.angular.z) * Mathf.Rad2Deg / (float)rightTrackRadius; // Unit is [rad/s]
        leftVelCmd = (float)(twist.linear.x - tread_half * twist.angular.z) * Mathf.Rad2Deg / (float)leftTrackRadius; // Unit is [rad/s]


        /* Set targetVelocity in xDrive in wheels */
        foreach (ArticulationBody left in leftBodies)
        {
            var drive = left.xDrive;
            drive.targetVelocity = leftVelCmd;
            left.xDrive = drive;
        }
        foreach (ArticulationBody right in rightBodies)
        {
            var drive = right.xDrive;
            drive.targetVelocity = rightVelCmd;
            right.xDrive = drive;
        }

        previousTime = time;
    }

    void ExecuteTwist(TwistMsg msg)
    {
        twist = msg;
        //Debug.Log("Linear Velocity:"+twist.linear.x);
        //Debug.Log("Angular Velocity:"+twist.angular.z);
    }
}
