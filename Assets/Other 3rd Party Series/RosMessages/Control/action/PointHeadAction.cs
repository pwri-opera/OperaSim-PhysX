using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Control
{
    public class PointHeadAction : Action<PointHeadActionGoal, PointHeadActionResult, PointHeadActionFeedback, PointHeadGoal, PointHeadResult, PointHeadFeedback>
    {
        public const string k_RosMessageName = "control_msgs/PointHeadAction";
        public override string RosMessageName => k_RosMessageName;


        public PointHeadAction() : base()
        {
            this.action_goal = new PointHeadActionGoal();
            this.action_result = new PointHeadActionResult();
            this.action_feedback = new PointHeadActionFeedback();
        }

        public static PointHeadAction Deserialize(MessageDeserializer deserializer) => new PointHeadAction(deserializer);

        PointHeadAction(MessageDeserializer deserializer)
        {
            this.action_goal = PointHeadActionGoal.Deserialize(deserializer);
            this.action_result = PointHeadActionResult.Deserialize(deserializer);
            this.action_feedback = PointHeadActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
