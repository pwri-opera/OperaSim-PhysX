using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.Control
{
    public class GripperCommandAction : Action<GripperCommandActionGoal, GripperCommandActionResult, GripperCommandActionFeedback, GripperCommandGoal, GripperCommandResult, GripperCommandFeedback>
    {
        public const string k_RosMessageName = "control_msgs/GripperCommandAction";
        public override string RosMessageName => k_RosMessageName;


        public GripperCommandAction() : base()
        {
            this.action_goal = new GripperCommandActionGoal();
            this.action_result = new GripperCommandActionResult();
            this.action_feedback = new GripperCommandActionFeedback();
        }

        public static GripperCommandAction Deserialize(MessageDeserializer deserializer) => new GripperCommandAction(deserializer);

        GripperCommandAction(MessageDeserializer deserializer)
        {
            this.action_goal = GripperCommandActionGoal.Deserialize(deserializer);
            this.action_result = GripperCommandActionResult.Deserialize(deserializer);
            this.action_feedback = GripperCommandActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
