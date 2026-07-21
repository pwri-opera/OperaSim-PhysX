using UnityEngine;
using SRD.Core;

namespace SRD.Sample.UI2DView
{
    [RequireComponent(typeof(Canvas))]
    public class SRD2DViewCanvas : MonoBehaviour
    {
        void Start()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.targetDisplay = SRD2DView.DisplayIndex;
        }
    }
}