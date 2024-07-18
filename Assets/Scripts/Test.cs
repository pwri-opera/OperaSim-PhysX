using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public int count = 0;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("test!");
    }

    // Update is called once per frame
    void Update()
    {
        count = count + 1;  // Updateが呼び出される毎にカウントアップする
        // if (Time.time > 1f) // 1秒を超えたら表示する
        // {
            // Debug.Log("FINISH!!!");
            Debug.Log("count = " + count);
            // Debug.Log("Time.time = " + Time.time);
        // }        
    }
}
