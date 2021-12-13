using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Control
{
    public class FollowJointTrajectoryAction : Action<FollowJointTrajectoryActionGoal, FollowJointTrajectoryActionResult, FollowJointTrajectoryActionFeedback, FollowJointTrajectoryGoal, FollowJointTrajectoryResult, FollowJointTrajectoryFeedback>
    {
        public const string k_RosMessageName = "control_msgs/FollowJointTrajectoryAction";
        public override string RosMessageName => k_RosMessageName;


        public FollowJointTrajectoryAction() : base()
        {
            this.action_goal = new FollowJointTrajectoryActionGoal();
            this.action_result = new FollowJointTrajectoryActionResult();
            this.action_feedback = new FollowJointTrajectoryActionFeedback();
        }

        public static FollowJointTrajectoryAction Deserialize(MessageDeserializer deserializer) => new FollowJointTrajectoryAction(deserializer);

        FollowJointTrajectoryAction(MessageDeserializer deserializer)
        {
            this.action_goal = FollowJointTrajectoryActionGoal.Deserialize(deserializer);
            this.action_result = FollowJointTrajectoryActionResult.Deserialize(deserializer);
            this.action_feedback = FollowJointTrajectoryActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
