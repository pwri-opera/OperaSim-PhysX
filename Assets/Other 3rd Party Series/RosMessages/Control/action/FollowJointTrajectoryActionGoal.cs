using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.Control
{
    public class FollowJointTrajectoryActionGoal : ActionGoal<FollowJointTrajectoryGoal>
    {
        public const string k_RosMessageName = "control_msgs/FollowJointTrajectoryActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public FollowJointTrajectoryActionGoal() : base()
        {
            this.goal = new FollowJointTrajectoryGoal();
        }

        public FollowJointTrajectoryActionGoal(HeaderMsg header, GoalIDMsg goal_id, FollowJointTrajectoryGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static FollowJointTrajectoryActionGoal Deserialize(MessageDeserializer deserializer) => new FollowJointTrajectoryActionGoal(deserializer);

        FollowJointTrajectoryActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = FollowJointTrajectoryGoal.Deserialize(deserializer);
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
