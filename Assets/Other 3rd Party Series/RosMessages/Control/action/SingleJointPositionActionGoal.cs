using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class SingleJointPositionActionGoal : ActionGoal<SingleJointPositionGoal>
    {
        public const string k_RosMessageName = "control_msgs/SingleJointPositionActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public SingleJointPositionActionGoal() : base()
        {
            this.goal = new SingleJointPositionGoal();
        }

        public SingleJointPositionActionGoal(HeaderMsg header, GoalIDMsg goal_id, SingleJointPositionGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static SingleJointPositionActionGoal Deserialize(MessageDeserializer deserializer) => new SingleJointPositionActionGoal(deserializer);

        SingleJointPositionActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = SingleJointPositionGoal.Deserialize(deserializer);
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
