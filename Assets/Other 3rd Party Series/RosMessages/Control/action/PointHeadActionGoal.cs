using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class PointHeadActionGoal : ActionGoal<PointHeadGoal>
    {
        public const string k_RosMessageName = "control_msgs/PointHeadActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public PointHeadActionGoal() : base()
        {
            this.goal = new PointHeadGoal();
        }

        public PointHeadActionGoal(HeaderMsg header, GoalIDMsg goal_id, PointHeadGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static PointHeadActionGoal Deserialize(MessageDeserializer deserializer) => new PointHeadActionGoal(deserializer);

        PointHeadActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = PointHeadGoal.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.goal_id);
            serializer.Write(this.goal);
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
