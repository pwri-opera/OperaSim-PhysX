using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class SingleJointPositionActionResult : ActionResult<SingleJointPositionResult>
    {
        public const string k_RosMessageName = "control_msgs/SingleJointPositionActionResult";
        public override string RosMessageName => k_RosMessageName;


        public SingleJointPositionActionResult() : base()
        {
            this.result = new SingleJointPositionResult();
        }

        public SingleJointPositionActionResult(HeaderMsg header, GoalStatusMsg status, SingleJointPositionResult result) : base(header, status)
        {
            this.result = result;
        }
        public static SingleJointPositionActionResult Deserialize(MessageDeserializer deserializer) => new SingleJointPositionActionResult(deserializer);

        SingleJointPositionActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = SingleJointPositionResult.Deserialize(deserializer);
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
