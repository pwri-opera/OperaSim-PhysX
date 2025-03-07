using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class EmergencyStop : MonoBehaviour
{
    private ROSConnection ros;
    public string emergencyStopTopicName = "[robot_name]/emg_stop";

    public bool isEmergencyStop { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        isEmergencyStop = false;
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<BoolMsg>(Utils.PreprocessNamespace(this.gameObject,emergencyStopTopicName), OnCommand);
    }
    void OnCommand(BoolMsg cmd)
    {
        isEmergencyStop = cmd.data;
    }

    public static EmergencyStop GetEmergencyStop(GameObject self)
    {
        return self.GetComponentInParent<EmergencyStop>();
    }
}
