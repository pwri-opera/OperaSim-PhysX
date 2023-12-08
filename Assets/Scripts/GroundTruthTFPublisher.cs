using System;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class GroundTruthTFPublisher : MonoBehaviour
{
    [SerializeField]
    float m_PublishRateHz = 20f;
    [SerializeField]
    string m_FramePrefix = "";
    [SerializeField]
    string m_FrameSuffix = "_groundtruth";

    float m_LastPublishTimeSeconds;

    Dictionary<String, Component> base_links;

    ROSConnection ros;

    float PublishPeriodSeconds => 1.0f / m_PublishRateHz;

    bool ShouldPublishMessage => Time.time > m_LastPublishTimeSeconds + PublishPeriodSeconds;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TFMessageMsg>("/tf");
        base_links = new Dictionary<string, Component>();
        foreach (var robot in GameObject.FindGameObjectsWithTag("robot"))
        {
            if (robot.activeInHierarchy)
            {
                var childs = new List<Component>(robot.GetComponentsInChildren(typeof(Component)));
                try
                {
                    base_links.Add(robot.name, childs.Find(c => c.name == "base_link"));
                }
                catch (ArgumentNullException e)
                {
                    base_links.Add(robot.name, robot.GetComponent(typeof(Component)));
                }
            }
        }
        m_LastPublishTimeSeconds = Time.time + PublishPeriodSeconds;
    }

    void PublishMessage()
    {
        var tfMessageList = new List<TransformStampedMsg>();

        float sim_time = Time.time;
#if !ROS2
        uint secs = (uint)sim_time;
#else
        int secs = (int)sim_time;
#endif
        uint nsecs = (uint)((sim_time % 1) * 1e9);

        foreach(var base_link in base_links)
        {
            var message = new TransformStampedMsg();
            message.header.stamp.sec = secs;
            message.header.stamp.nanosec = nsecs;
            message.header.frame_id = "map";
            message.child_frame_id = m_FramePrefix + base_link.Key + m_FrameSuffix;
            message.transform.translation = base_link.Value.transform.position.To<FLU>();
            message.transform.rotation = base_link.Value.transform.rotation.To<FLU>();
            tfMessageList.Add(message);
        }

        var tfMessage = new TFMessageMsg(tfMessageList.ToArray());
        ros.Publish("/tf", tfMessage);
        m_LastPublishTimeSeconds = Time.time;
    }

    void Update()
    {
        if (ShouldPublishMessage)
        {
            PublishMessage();
        }

    }
}