using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class JointTrajectoryActionResult : ActionResult<JointTrajectoryResult>
    {
        public const string k_RosMessageName = "control_msgs/JointTrajectoryActionResult";
        public override string RosMessageName => k_RosMessageName;


        public JointTrajectoryActionResult() : base()
        {
            this.result = new JointTrajectoryResult();
        }

        public JointTrajectoryActionResult(HeaderMsg header, GoalStatusMsg status, JointTrajectoryResult result) : base(header, status)
        {
            this.result = result;
        }
        public static JointTrajectoryActionResult Deserialize(MessageDeserializer deserializer) => new JointTrajectoryActionResult(deserializer);

        JointTrajectoryActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = JointTrajectoryResult.Deserialize(deserializer);
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
