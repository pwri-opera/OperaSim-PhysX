using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class VesselController : MonoBehaviour
{
    private ROSConnection ros;
    public string DumpTopicName = "dump/cmd";
    public ArticulationBody dump_joint;
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
            drive.stiffness = 100000;
            drive.damping = 100000;
            drive.forceLimit = 100000;
            dump_joint.xDrive = drive;
        }
        else
        {
            Debug.Log("No ArticulationBody are found");
        }

        target_pos.data = 0.0;
        ros.Subscribe<Float64Msg>(DumpTopicName, ExecuteVesselControl);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Dump Target Position:" + target_pos.data);
        var drive = dump_joint.xDrive;
        drive.target = (float)(target_pos.data * Mathf.Rad2Deg);
        dump_joint.xDrive = drive;
    }

    void ExecuteVesselControl(Float64Msg msg)
    {
        target_pos = msg;
        //Debug.Log("Dump Target Position:" + target_pos.data);
    }
}
