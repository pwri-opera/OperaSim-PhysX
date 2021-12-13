using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Control
{
    public class JointTrajectoryAction : Action<JointTrajectoryActionGoal, JointTrajectoryActionResult, JointTrajectoryActionFeedback, JointTrajectoryGoal, JointTrajectoryResult, JointTrajectoryFeedback>
    {
        public const string k_RosMessageName = "control_msgs/JointTrajectoryAction";
        public override string RosMessageName => k_RosMessageName;


        public JointTrajectoryAction() : base()
        {
            this.action_goal = new JointTrajectoryActionGoal();
            this.action_result = new JointTrajectoryActionResult();
            this.action_feedback = new JointTrajectoryActionFeedback();
        }

        public static JointTrajectoryAction Deserialize(MessageDeserializer deserializer) => new JointTrajectoryAction(deserializer);

        JointTrajectoryAction(MessageDeserializer deserializer)
        {
            this.action_goal = JointTrajectoryActionGoal.Deserialize(deserializer);
            this.action_result = JointTrajectoryActionResult.Deserialize(deserializer);
            this.action_feedback = JointTrajectoryActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
