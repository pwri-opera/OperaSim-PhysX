using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class PointHeadActionResult : ActionResult<PointHeadResult>
    {
        public const string k_RosMessageName = "control_msgs/PointHeadActionResult";
        public override string RosMessageName => k_RosMessageName;


        public PointHeadActionResult() : base()
        {
            this.result = new PointHeadResult();
        }

        public PointHeadActionResult(HeaderMsg header, GoalStatusMsg status, PointHeadResult result) : base(header, status)
        {
            this.result = result;
        }
        public static PointHeadActionResult Deserialize(MessageDeserializer deserializer) => new PointHeadActionResult(deserializer);

        PointHeadActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = PointHeadResult.Deserialize(deserializer);
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
