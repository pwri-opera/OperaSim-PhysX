using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// ダンプトラックのダンプ角度を制御する
/// </summary>
public class VesselController : MonoBehaviour
{
    private ROSConnection ros;

    [Tooltip("ダンプ角度設定コマンドのROSトピック名")]
    public string DumpTopicName = "dump/cmd";

    private ArticulationBody dump_joint;

    private Float64Msg target_pos;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        dump_joint = this.GetComponent<ArticulationBody>();
        target_pos = new Float64Msg();

        if (dump_joint)
        {
            var drive = dump_joint.xDrive;
            if (drive.stiffness == 0)
                drive.stiffness = 100000;
            if (drive.damping == 0)
                drive.damping = 100000;
            if (drive.forceLimit == 0)
                drive.forceLimit = 100000;
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        target_pos.data = 0.0;
        ros.Subscribe<Float64Msg>(Utils.PreprocessNamespace(this.gameObject, DumpTopicName), ExecuteVesselControl);
    }

    // Update is called once per frame
    // void Update()
    // {
    //     // Debug.Log("Dump Target Position:" + target_pos.data);
        
    // }
// Velocity
    void ExecuteVesselControl(Float64Msg msg)
    {
        target_pos = msg;
        var drive = dump_joint.xDrive;
        drive.target = (float)(target_pos.data * Mathf.Rad2Deg);
        Debug.Log("Dump Target Position:" + drive.target);
        dump_joint.xDrive = drive;
        //Debug.Log("Dump Target Position:" + target_pos.data);
    }
}
