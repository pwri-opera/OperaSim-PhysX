using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class FollowJointTrajectoryActionFeedback : ActionFeedback<FollowJointTrajectoryFeedback>
    {
        public const string k_RosMessageName = "control_msgs/FollowJointTrajectoryActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public FollowJointTrajectoryActionFeedback() : base()
        {
            this.feedback = new FollowJointTrajectoryFeedback();
        }

        public FollowJointTrajectoryActionFeedback(HeaderMsg header, GoalStatusMsg status, FollowJointTrajectoryFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static FollowJointTrajectoryActionFeedback Deserialize(MessageDeserializer deserializer) => new FollowJointTrajectoryActionFeedback(deserializer);

        FollowJointTrajectoryActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = FollowJointTrajectoryFeedback.Deserialize(deserializer);
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
