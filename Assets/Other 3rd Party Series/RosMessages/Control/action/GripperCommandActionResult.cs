using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class GripperCommandActionResult : ActionResult<GripperCommandResult>
    {
        public const string k_RosMessageName = "control_msgs/GripperCommandActionResult";
        public override string RosMessageName => k_RosMessageName;


        public GripperCommandActionResult() : base()
        {
            this.result = new GripperCommandResult();
        }

        public GripperCommandActionResult(HeaderMsg header, GoalStatusMsg status, GripperCommandResult result) : base(header, status)
        {
            this.result = result;
        }
        public static GripperCommandActionResult Deserialize(MessageDeserializer deserializer) => new GripperCommandActionResult(deserializer);

        GripperCommandActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = GripperCommandResult.Deserialize(deserializer);
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
