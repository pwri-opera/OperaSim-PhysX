using UnityEngine;
/// <summary>
/// 二つのオブジェクト間の距離（x方向, y方向, z方向及びそれらのノルム）を取得する
/// </summary>
public class DistanceCalculator : MonoBehaviour
{
    public GameObject object1;
    public GameObject object2;

    void Update()
    {
        if (object1 != null && object2 != null)
        {
            float distance = Vector3.Distance(object1.transform.position, object2.transform.position);
            float dist_x = object1.transform.position.x - object2.transform.position.x;
            float dist_y = object1.transform.position.y - object2.transform.position.y;
            float dist_z = object1.transform.position.z - object2.transform.position.z;

            Debug.Log("Distance between object1 and object2: " + distance);
            Debug.Log("Distance x: " + dist_x + ",\t y: " + dist_y + ",\t z: " + dist_z);
        }
    }
}