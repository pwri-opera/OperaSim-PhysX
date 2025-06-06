/// <summary>
/// このスクリプトを持つオブジェクトの位置に target で指定したオブジェクトを強制的に追従させる追随用スクリプト
/// zx120 油圧ショベルに於いて，バケット部の閉リンク機構構成に使用
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFix : MonoBehaviour
{
    public Transform target;
    
    void Update()
    {
        target.position = this.transform.position;
    }
}