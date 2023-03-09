using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class PointHeadActionFeedback : ActionFeedback<PointHeadFeedback>
    {
        public const string k_RosMessageName = "control_msgs/PointHeadActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public PointHeadActionFeedback() : base()
        {
            this.feedback = new PointHeadFeedback();
        }

        public PointHeadActionFeedback(HeaderMsg header, GoalStatusMsg status, PointHeadFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static PointHeadActionFeedback Deserialize(MessageDeserializer deserializer) => new PointHeadActionFeedback(deserializer);

        PointHeadActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = PointHeadFeedback.Deserialize(deserializer);
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
