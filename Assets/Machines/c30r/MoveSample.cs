using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBoom: MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // transformを取得
        Transform myTransform = this.transform;

        // 座標を取得
        Vector3 pos = myTransform.position;
        pos.x = 1f;    // x座標へ0.01加算
        pos.y = 1f;    // y座標へ0.01加算
        pos.z = 1f;    // z座標へ0.01加算

        myTransform.position = pos;  // 座標を設定

    }

    // Update is called once per frame
    void Update()
    {

        // transformを取得
        Transform myTransform = this.transform;

        // 座標を取得
        Vector3 pos1 = myTransform.position;

        if (pos1.x == 1.1f)
        { 
            pos1.x = 1f;    // x座標へ0.01加算
            pos1.y = 1f;    // y座標へ0.01加算
            pos1.z = 1f;    // z座標へ0.01加算
 
        }
        else
        {
            pos1.x += 0.01f;    // x座標へ0.01加算
            pos1.y += 0.01f;    // y座標へ0.01加算
            pos1.z += 0.01f;    // z座標へ0.01加算

        }
        myTransform.position = pos1;  // 座標を設定


    }
}
