using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBoom: MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // transform���擾
        Transform myTransform = this.transform;

        // ���W���擾
        Vector3 pos = myTransform.position;
        pos.x = 1f;    // x���W��0.01���Z
        pos.y = 1f;    // y���W��0.01���Z
        pos.z = 1f;    // z���W��0.01���Z

        myTransform.position = pos;  // ���W��ݒ�

    }

    // Update is called once per frame
    void Update()
    {

        // transform���擾
        Transform myTransform = this.transform;

        // ���W���擾
        Vector3 pos1 = myTransform.position;

        if (pos1.x == 1.1f)
        { 
            pos1.x = 1f;    // x���W��0.01���Z
            pos1.y = 1f;    // y���W��0.01���Z
            pos1.z = 1f;    // z���W��0.01���Z
 
        }
        else
        {
            pos1.x += 0.01f;    // x���W��0.01���Z
            pos1.y += 0.01f;    // y���W��0.01���Z
            pos1.z += 0.01f;    // z���W��0.01���Z

        }
        myTransform.position = pos1;  // ���W��ݒ�


    }
}
