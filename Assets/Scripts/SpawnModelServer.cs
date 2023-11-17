using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Gazebo;
using UnityEngine;
using System.Xml.Linq;

public class SpawnModelServer : MonoBehaviour
{
    private Dictionary<string, GameObject> models;

    ROSConnection ros;
    private double pub_tick;
    private const double pub_interval = 0.1;

    private SpawnModelResponse LoadUrdf(SpawnModelRequest req)
    {
        var ret = new SpawnModelResponse();
        
        var model = XDocument.Parse(req.model_xml);
        var model_type = model.Root.Attribute("name").Value;

        var currentRobot = models.GetValueOrDefault(req.model_name, null);

        // clear the existing robot to avoid collision
        if (currentRobot != null)
        {
            currentRobot.SetActive(false);
            Destroy(currentRobot);
        }

        GameObject prefabObj = (GameObject)Resources.Load("Prefab/" + model_type);
        var position = req.initial_pose.position.From<FLU>();
        var rotation = req.initial_pose.orientation.From<FLU>();
        GameObject robotObject = Instantiate(prefabObj, position, rotation);
        robotObject.name = req.model_name;

        if (robotObject != null)
        {
            Debug.Log("Successfully Loaded URDF" + robotObject.name);
            models.Add(req.model_name, robotObject);
        }

        ret.success = true;
        return ret;
    }

    // Start is called before the first frame update
    void Start()
    {
        models = new Dictionary<string, GameObject>();
        pub_tick = Time.time;
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ModelStatesMsg>("/gazebo/model_states");
        ros.ImplementService<SpawnModelRequest, SpawnModelResponse>("/gazebo/spawn_urdf_model", SpawnURDFModel);
        ros.ImplementService<SpawnModelRequest, SpawnModelResponse>("/gazebo/spawn_sdf_model", SpawnSDFModel);
        ros.ImplementService<SetModelConfigurationRequest, SetModelConfigurationResponse>("/gazebo/set_model_configuration", SetModelConfiguration);
    }

    private SpawnModelResponse SpawnURDFModel(SpawnModelRequest req)
    {
        Debug.Log(req);
        return LoadUrdf(req);
    }

    private SpawnModelResponse SpawnSDFModel(SpawnModelRequest req)
    {
        Debug.Log(req);
        return LoadUrdf(req);
    }

    private SetModelConfigurationResponse SetModelConfiguration(SetModelConfigurationRequest req)
    {
        Debug.Log(req);
        var ret = new SetModelConfigurationResponse();
        ret.success = true;
        return ret;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - pub_tick > pub_interval)
        {
            pub_tick = Time.time;
            var msg = new ModelStatesMsg();
            msg.name = new string[models.Keys.Count];
            models.Keys.CopyTo(msg.name, 0);
            ros.Publish("/gazebo/model_states", msg);
        }
    }
}