using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class SingleJointPositionActionFeedback : ActionFeedback<SingleJointPositionFeedback>
    {
        public const string k_RosMessageName = "control_msgs/SingleJointPositionActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public SingleJointPositionActionFeedback() : base()
        {
            this.feedback = new SingleJointPositionFeedback();
        }

        public SingleJointPositionActionFeedback(HeaderMsg header, GoalStatusMsg status, SingleJointPositionFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static SingleJointPositionActionFeedback Deserialize(MessageDeserializer deserializer) => new SingleJointPositionActionFeedback(deserializer);

        SingleJointPositionActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = SingleJointPositionFeedback.Deserialize(deserializer);
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
