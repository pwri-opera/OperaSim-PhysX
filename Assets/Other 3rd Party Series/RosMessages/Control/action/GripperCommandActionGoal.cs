using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class GripperCommandActionGoal : ActionGoal<GripperCommandGoal>
    {
        public const string k_RosMessageName = "control_msgs/GripperCommandActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public GripperCommandActionGoal() : base()
        {
            this.goal = new GripperCommandGoal();
        }

        public GripperCommandActionGoal(HeaderMsg header, GoalIDMsg goal_id, GripperCommandGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static GripperCommandActionGoal Deserialize(MessageDeserializer deserializer) => new GripperCommandActionGoal(deserializer);

        GripperCommandActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = GripperCommandGoal.Deserialize(deserializer);
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
