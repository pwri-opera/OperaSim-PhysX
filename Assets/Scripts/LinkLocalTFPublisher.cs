using System;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class LinkLocalTFPublisher : MonoBehaviour
{
    [SerializeField]
    float m_PublishRateHz = 20f;
    [SerializeField]
    string m_ParentFrameName = "";
    [SerializeField]
    string m_FrameName = "";

    float m_LastPublishTimeSeconds;

    ROSConnection ros;

    float PublishPeriodSeconds => 1.0f / m_PublishRateHz;

    bool ShouldPublishMessage => Time.time > m_LastPublishTimeSeconds + PublishPeriodSeconds;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TFMessageMsg>("/tf");
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

        var message = new TransformStampedMsg();
        message.header.stamp.sec = secs;
        message.header.stamp.nanosec = nsecs;
        message.header.frame_id = m_ParentFrameName;
        message.child_frame_id = m_FrameName;
        message.transform.translation = transform.localPosition.To<FLU>();
        message.transform.rotation = transform.localRotation.To<FLU>();
        tfMessageList.Add(message);

        var tfMessage = new TFMessageMsg(tfMessageList.ToArray());
        ros.Publish("/tf", tfMessage);
        m_LastPublishTimeSeconds = Time.time;
    }

    void FixedUpdate()
    {
        if (ShouldPublishMessage)
        {
            PublishMessage();
        }

    }
}