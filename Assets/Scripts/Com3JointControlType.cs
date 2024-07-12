using UnityEngine;

namespace Com3 {
    public enum ControlType
    {
        None,
        Position,
        Velocity,
        Effort
    }

    public class ControlTypeAnnotation : MonoBehaviour
    {
        [SerializeField] private ControlType controlType = ControlType.Position;

        public ControlType GetControlType()
        {
            return controlType;
        }
    }
}