/// <summary>
/// むだ時間を設定するコンポーネントを追加する．
/// </summary>
using UnityEngine;

public class DeadTimeDeadTime : MonoBehaviour
{
    [Tooltip("入力に対するむだ時間 (msec) \n40 msec 以上に設定") ]
    [Min(40)] public double deadTime;
}
