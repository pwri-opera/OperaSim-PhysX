using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class JointTrajectoryActionFeedback : ActionFeedback<JointTrajectoryFeedback>
    {
        public const string k_RosMessageName = "control_msgs/JointTrajectoryActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public JointTrajectoryActionFeedback() : base()
        {
            this.feedback = new JointTrajectoryFeedback();
        }

        public JointTrajectoryActionFeedback(HeaderMsg header, GoalStatusMsg status, JointTrajectoryFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static JointTrajectoryActionFeedback Deserialize(MessageDeserializer deserializer) => new JointTrajectoryActionFeedback(deserializer);

        JointTrajectoryActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = JointTrajectoryFeedback.Deserialize(deserializer);
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
