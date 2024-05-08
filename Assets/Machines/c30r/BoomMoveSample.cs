using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCharacter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // transform���擾
        Transform myTransform = this.transform;

        // ���W���擾
        Vector3 pos = myTransform.position;
        pos.x = 1f;    // x���W��0.01���Z
        pos.y = 0f;    // y���W��0.01���Z
        pos.z = 0f;    // z���W��0.01���Z

        myTransform.position = pos;  // ���W��ݒ�

    }

    // Update is called once per frame
    void Update()
    {

        // transform���擾
        Transform myTransform = this.transform;

        // ���W���擾
        Vector3 pos = myTransform.position;

        if (pos.x == 1.1f)
        {
            pos.x = 1f;    // x���W��0.01���Z
            pos.y = 0f;    // y���W��0.01���Z
            pos.z = 0f;    // z���W��0.01���Z

        }
        else
        {
            pos.x += 0.01f;    // x���W��0.01���Z
            pos.y += 0.01f;    // y���W��0.01���Z
            pos.z += 0.01f;    // z���W��0.01���Z

        }
        myTransform.position = pos;  // ���W��ݒ�


    }

}