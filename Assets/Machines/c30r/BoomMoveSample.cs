using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCharacter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // transformを取得
        Transform myTransform = this.transform;

        // 座標を取得
        Vector3 pos = myTransform.position;
        pos.x = 1f;    // x座標へ0.01加算
        pos.y = 0f;    // y座標へ0.01加算
        pos.z = 0f;    // z座標へ0.01加算

        myTransform.position = pos;  // 座標を設定

    }

    // Update is called once per frame
    void Update()
    {

        // transformを取得
        Transform myTransform = this.transform;

        // 座標を取得
        Vector3 pos = myTransform.position;

        if (pos.x == 1.1f)
        {
            pos.x = 1f;    // x座標へ0.01加算
            pos.y = 0f;    // y座標へ0.01加算
            pos.z = 0f;    // z座標へ0.01加算

        }
        else
        {
            pos.x += 0.01f;    // x座標へ0.01加算
            pos.y += 0.01f;    // y座標へ0.01加算
            pos.z += 0.01f;    // z座標へ0.01加算

        }
        myTransform.position = pos;  // 座標を設定


    }

}