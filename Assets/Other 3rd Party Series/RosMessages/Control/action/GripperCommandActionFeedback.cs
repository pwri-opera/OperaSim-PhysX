using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class GripperCommandActionFeedback : ActionFeedback<GripperCommandFeedback>
    {
        public const string k_RosMessageName = "control_msgs/GripperCommandActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public GripperCommandActionFeedback() : base()
        {
            this.feedback = new GripperCommandFeedback();
        }

        public GripperCommandActionFeedback(HeaderMsg header, GoalStatusMsg status, GripperCommandFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static GripperCommandActionFeedback Deserialize(MessageDeserializer deserializer) => new GripperCommandActionFeedback(deserializer);

        GripperCommandActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = GripperCommandFeedback.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.feedback);
        }


#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
