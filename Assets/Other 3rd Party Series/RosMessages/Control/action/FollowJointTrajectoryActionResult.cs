using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class FollowJointTrajectoryActionResult : ActionResult<FollowJointTrajectoryResult>
    {
        public const string k_RosMessageName = "control_msgs/FollowJointTrajectoryActionResult";
        public override string RosMessageName => k_RosMessageName;


        public FollowJointTrajectoryActionResult() : base()
        {
            this.result = new FollowJointTrajectoryResult();
        }

        public FollowJointTrajectoryActionResult(HeaderMsg header, GoalStatusMsg status, FollowJointTrajectoryResult result) : base(header, status)
        {
            this.result = result;
        }
        public static FollowJointTrajectoryActionResult Deserialize(MessageDeserializer deserializer) => new FollowJointTrajectoryActionResult(deserializer);

        FollowJointTrajectoryActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = FollowJointTrajectoryResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
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
