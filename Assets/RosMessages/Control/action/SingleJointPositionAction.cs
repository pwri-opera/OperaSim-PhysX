using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Control
{
    public class SingleJointPositionAction : Action<SingleJointPositionActionGoal, SingleJointPositionActionResult, SingleJointPositionActionFeedback, SingleJointPositionGoal, SingleJointPositionResult, SingleJointPositionFeedback>
    {
        public const string k_RosMessageName = "control_msgs/SingleJointPositionAction";
        public override string RosMessageName => k_RosMessageName;


        public SingleJointPositionAction() : base()
        {
            this.action_goal = new SingleJointPositionActionGoal();
            this.action_result = new SingleJointPositionActionResult();
            this.action_feedback = new SingleJointPositionActionFeedback();
        }

        public static SingleJointPositionAction Deserialize(MessageDeserializer deserializer) => new SingleJointPositionAction(deserializer);

        SingleJointPositionAction(MessageDeserializer deserializer)
        {
            this.action_goal = SingleJointPositionActionGoal.Deserialize(deserializer);
            this.action_result = SingleJointPositionActionResult.Deserialize(deserializer);
            this.action_feedback = SingleJointPositionActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
