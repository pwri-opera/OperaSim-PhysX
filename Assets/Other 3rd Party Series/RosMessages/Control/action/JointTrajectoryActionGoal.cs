using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class JointTrajectoryActionGoal : ActionGoal<JointTrajectoryGoal>
    {
        public const string k_RosMessageName = "control_msgs/JointTrajectoryActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public JointTrajectoryActionGoal() : base()
        {
            this.goal = new JointTrajectoryGoal();
        }

        public JointTrajectoryActionGoal(HeaderMsg header, GoalIDMsg goal_id, JointTrajectoryGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static JointTrajectoryActionGoal Deserialize(MessageDeserializer deserializer) => new JointTrajectoryActionGoal(deserializer);

        JointTrajectoryActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = JointTrajectoryGoal.Deserialize(deserializer);
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
