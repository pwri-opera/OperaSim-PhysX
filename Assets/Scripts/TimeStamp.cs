using System;
using UnityEngine;
using RosMessageTypes.BuiltinInterfaces;
// From: https://github.com/Unity-Technologies/Robotics-Nav2-SLAM-Example/tree/main/Nav2SLAMExampleProject/Assets/Scripts

namespace Unity.Robotics.Core
{
    public readonly struct TimeStamp
    {
        public const double k_NanosecondsInSecond = 1e9f;

        // TODO: specify base time this stamp is measured against (Sim 0, time since application start, etc.)
        public readonly uint Seconds;
        public readonly uint NanoSeconds;

        // (From Unity Time.time)
        public TimeStamp(double timeInSeconds)
        {
            var sec = Math.Floor(timeInSeconds);
            var nsec = (timeInSeconds - sec) * k_NanosecondsInSecond;
            // TODO: Check for negatives to ensure safe cast
            Seconds = (uint)sec;
            NanoSeconds = (uint)nsec;
        }

        //// (From a ROS2 Time message)
        //TimeStamp(int sec, uint nsec)
        //{
        //    Seconds = sec;
        //    NanoSeconds = nsec;
        //}

        // (From a ROS1 Time message)
        TimeStamp(uint sec, uint nsec)
        {
            Seconds = sec;
            NanoSeconds = nsec;
        }

        // NOTE: We could define these operators in a transport-specific extension package
        public static implicit operator TimeMsg(TimeStamp stamp)
        {
            return new TimeMsg(stamp.Seconds, stamp.NanoSeconds);
        }

        public static implicit operator TimeStamp(TimeMsg stamp)
        {
            return new TimeStamp(stamp.sec, stamp.nanosec);
        }
    }
}
