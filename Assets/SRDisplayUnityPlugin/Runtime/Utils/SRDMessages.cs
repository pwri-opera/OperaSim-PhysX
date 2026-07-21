/*
 * Copyright 2019,2020,2023 Sony Corporation
 */

using SRD.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Utils
{
    internal static partial class SRDHelper
    {
        public static class SRDMessages
        {
            private enum SRDMessageType
            {
                AppCloseMessage,
                UnknownError,
                DisplayConnectionError, DeviceConnectionError, USB3ConnectionError,
                DeviceNotFoundError, DLLNotFoundError,
                DisplayInterruptionError, DeviceInterruptionError, AppConflictionError,
                FullscreenGameViewError, SRDManagerNotFoundError, OldRuntimeUnsupportedError,
                NoDeviceSelectedError,
                FunctionUnsupportedError,
            }

            private static Dictionary<SRDMessageType, string> SRDMessagesDictEn = new Dictionary<SRDMessageType, string>()
            {
                {SRDMessageType.AppCloseMessage, "The application will be terminated."},
                {SRDMessageType.UnknownError, "Unknown error has occurred."},
                {SRDMessageType.DisplayConnectionError, "Failed to detect Spatial Reality Display. Make sure Display cable is connected correctly between PC and Spatial Reality Display."},
                {SRDMessageType.DeviceConnectionError, "Failed to detect Spatial Reality Display. Make sure USB cable is connected correctly between PC and Spatial Reality Display, and Spatial Reality Display device is powered on."},
                {
                    SRDMessageType.USB3ConnectionError, string.Join("\n", new string[]{
                        "Spatial Reality Display is not recognized correctly. Please make sure Spatial Reality Display and PC's USB 3.0 port are connected with USB3.0 cable. Also, please try following steps.",
                        "    1. Unplug USB cable from PC's USB 3.0 port.",
                        "    2. Turn Spatial Reality Display device's power off.",
                        "    3. Plug USB cable into PC's USB 3.0 port.",
                        "    4. Wait for 30 seconds.",
                        "    5. Turn Spatial Reality Display device's power on.",
                        "    6. Launch this application again.\n",
                    })
                },
                {SRDMessageType.DeviceNotFoundError, "Failed to find Spatial Reality Display device. Make sure Spatial Reality Display device is powered on."},
                {SRDMessageType.DLLNotFoundError, "Spatial Reality Display SDK is not found. Spatial Reality Display SDK may be not installed correctly. Try to re-install with  Spatial Reality Display Settings Installer."},
                {SRDMessageType.DisplayInterruptionError, "Display connection has been interrupted. The Display cable may be disconnected."},
                {SRDMessageType.DeviceInterruptionError, "USB connection has been interrupted. The USB cable may be disconnected or Spatial Reality Display device's power may be turned off."},
                {SRDMessageType.AppConflictionError, "Another Spatial Reality Display application is already running. Please close it and start this application again."},
                {SRDMessageType.FullscreenGameViewError, "No Spatial Reality Display is connected. Please make sure that Spatial Reality Display is connected with your PC correctly."},
                {SRDMessageType.SRDManagerNotFoundError, "No SRDManager. You must add active SRDManager for Spatial Reality Display Apps."},
                {
                    SRDMessageType.OldRuntimeUnsupportedError, string.Join("\n", new string[]{
                        "The old version of the Spatial Reality Display SDK has been installed. ",
                        "The Spatial Reality Display Settings Installer version 2.0 or later is required to run this application."
                    }) 
                },
                {SRDMessageType.NoDeviceSelectedError, "Spatial Reality Display device is not selected."},
                {
                    SRDMessageType.FunctionUnsupportedError, string.Join("\n", new string[]{
                        "The Spatial Reality Display SDK version {0} or later is required to run this application. The currently installed version is {1}.",
                        "Please re-install the Spatial Reality Display SDK with the latest version of the Spatial Reality Display Settings Installer.\n"
                    })
                },
            };

            private static Dictionary<SRDMessageType, string> _messageDict;
            private static Dictionary<SRDMessageType, string> MessageDict
            {
                get
                {
                    if(_messageDict == null)
                    {
                        _messageDict = SRDMessagesDictEn;
                    }
                    return _messageDict;
                }
            }


            public static string AppCloseMessage
            {
                get { return MessageDict[SRDMessageType.AppCloseMessage]; }
            }
            public static string UnknownError
            {
                get { return MessageDict[SRDMessageType.UnknownError]; }
            }
            public static string DisplayConnectionError
            {
                get { return MessageDict[SRDMessageType.DisplayConnectionError]; }
            }
            public static string DeviceConnectionError
            {
                get { return MessageDict[SRDMessageType.DeviceConnectionError]; }
            }
            public static string USB3ConnectionError
            {
                get { return MessageDict[SRDMessageType.USB3ConnectionError]; }
            }
            public static string DeviceNotFoundError
            {
                get { return MessageDict[SRDMessageType.DeviceNotFoundError]; }
            }
            public static string DisplayInterruptionError
            {
                get { return MessageDict[SRDMessageType.DisplayInterruptionError]; }
            }
            public static string DeviceInterruptionError
            {
                get { return MessageDict[SRDMessageType.DeviceInterruptionError]; }
            }
            public static string DLLNotFoundError
            {
                get { return MessageDict[SRDMessageType.DLLNotFoundError]; }
            }
            public static string AppConflictionError
            {
                get { return MessageDict[SRDMessageType.AppConflictionError]; }
            }
            public static string FullscreenGameViewError
            {
                get { return MessageDict[SRDMessageType.FullscreenGameViewError]; }
            }
            public static string SRDManagerNotFoundError
            {
                get { return MessageDict[SRDMessageType.SRDManagerNotFoundError]; }
            }
            public static string OldRuntimeUnsupportedError
            {
                get { return MessageDict[SRDMessageType.OldRuntimeUnsupportedError]; }
            }
            public static string NoDeviceSelectedError
            {
                get { return MessageDict[SRDMessageType.NoDeviceSelectedError]; }
            }

            public static string FunctionUnsupportedError (string requiredVersion)
            {
                SRDCorePlugin.GetRuntimeVersionString(out string currentVersion, false);
                return string.Format(MessageDict[SRDMessageType.FunctionUnsupportedError], requiredVersion, currentVersion);
            }
        }
    }
}
