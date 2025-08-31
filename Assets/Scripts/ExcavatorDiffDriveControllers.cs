using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Com3;
using Unity.Robotics.Core;
using System;
using PID_Controller;

/// <summary>
/// 油圧ショベルの下部走行体制御を行う　（DiffDriveControllerを継承）
/// </summary>
public class ExcavatorDiffDriveController : DiffDriveController
{
    /// <summary>
    /// 速度プリセットの種類を選択（Inspector でドロップダウン表示）
    /// </summary>
    public enum SpeedPreset
    {
        turtle,   // 低速
        rabbit    // 高速
    }

    public class CmdVelMax
    {
        double vel;   // 低速
        double angvel;    // 高速
    }


    [Header("-- Excavator track param --")]

    public bool EnableSpeedPreset = true;
    [Tooltip("turtle ＝低速モード／rabbit ＝高速モード")]
    public SpeedPreset speedPreset = SpeedPreset.turtle;

    public double turtleMaxLinearVelocity;
    public double turtleMaxAngularVelocity;

    public double rabbitMaxLinearVelocity;
    public double rabbitMaxAngularVelocity;

    protected override void Start()
    {
        if (EnableSpeedPreset == true)
        {

            switch (speedPreset)
            {
                case SpeedPreset.turtle:
                    maxLinearVelocity = turtleMaxLinearVelocity;
                    maxAngularVelocity = turtleMaxAngularVelocity;
                    break;

                case SpeedPreset.rabbit:
                    maxLinearVelocity = turtleMaxLinearVelocity;
                    maxAngularVelocity = turtleMaxAngularVelocity;
                    break;
            }
        }
        base.Start();
    }

    // protected override void FixedUpdate()
    // {
    //     base.FixedUpdate();
    // }        
}
