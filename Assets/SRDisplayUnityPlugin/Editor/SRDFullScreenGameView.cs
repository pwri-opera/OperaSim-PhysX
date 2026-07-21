/*
 * Copyright 2019-2025 Sony Corporation
 */

#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;

using System.Runtime.InteropServices;

using UnityEngine;
using UnityEditor;

using SRD.Core;
using SRD.Utils;
using SRD.Editor.AsssemblyWrapper;
using static SRD.Utils.SRDProjectSettings.BehaviorOptionWhenNoSRDisplay;

namespace SRD.Editor
{
    internal class SRDFullScreenGameView
    {
        private const string FullScreenMenuPath = "SpatialRealityDisplay/SRDisplay GameView (Full Screen)";
        private const string SRDGameViewName = "SRD Game View";
        private const string TemporalyGameViewName = "Temporary Game View";
        private const string ForceCloseGameViewMessage = "Multiple GameViews cannot be open at the same time in Spatial Reality Display. Force closes the GameView tabs.";

        private static SrdXrDeviceInfo targetDevice;
        private static EditorApplication.CallbackFunction OnPostClosingTempGameView;

        private static IEnumerable<EditorWindow> EnumGameViews()
        {
            return GameView.GetGameViews().AsEnumerable();
        }

        private static IEnumerable<EditorWindow> EnumSRDGameViews()
        {
            return EnumGameViews().Where(w => w.name == SRDGameViewName);
        }

        private static IEnumerable<EditorWindow> EnumUnityGameViews()
        {
            return EnumGameViews().Where(w => w.name != SRDGameViewName);
        }

        private static void CloseAllUnityGameView()
        {
            var unityGameViews = EnumUnityGameViews();

            foreach (var view in unityGameViews)
            {
                Debug.Log(ForceCloseGameViewMessage);
                view.Close();
            }
        }

        private static void CloseAllSRDGameView()
        {
            var srdGameViews = EnumSRDGameViews();

            foreach (var view in srdGameViews)
            {
                view.Close();
            }
        }

        private static void HideToolbarOfAllSRDGameView()
        {
            var srdGameViews = EnumSRDGameViews();

            foreach (var view in srdGameViews)
            {
                var gameView = new GameView(view);
                gameView.showToolbar = false;
            }
        }

        private static bool SRDGameViewExists()
        {
            return EnumSRDGameViews().Count() != 0;
        }

        private static string SrdXrResultToMessage(SrdXrResult result)
        {
            var errorToMessage = new Dictionary<SrdXrResult, string>()
            {
                { SrdXrResult.ERROR_RUNTIME_NOT_FOUND, SRDHelper.SRDMessages.DLLNotFoundError},
                { SrdXrResult.ERROR_DEVICE_NOT_FOUND, SRDHelper.SRDMessages.FullscreenGameViewError},
                { SrdXrResult.ERROR_RUNTIME_UNSUPPORTED, SRDHelper.SRDMessages.OldRuntimeUnsupportedError},
            };
            return errorToMessage.ContainsKey(result) ? errorToMessage[result] : SRDHelper.SRDMessages.UnknownError;
        }

#if UNITY_EDITOR_WIN
        [MenuItem(FullScreenMenuPath + " _F11", true)]
#endif
        private static bool ValidateMenuItem_ToggleSRDGameView()
        {
            bool srdGameViewExists = SRDGameViewExists();
            Menu.SetChecked(FullScreenMenuPath, srdGameViewExists);

            // SRD GameView cannot be opened/closed in Play Mode
            if(EditorApplication.isPlaying)
            {
                return false;
            }

            // If SRD GameView is open, it can be closed
            if(srdGameViewExists)
            {
                return true;
            }

            // SRD Gameview cannot be opened without one or more SRDisplay(s) connected
            SrdXrResult result = SRDCorePlugin.GetRealDeviceNum(out var deviceCount);
            if(result != SrdXrResult.SUCCESS)
            {
                Debug.LogWarning(SrdXrResultToMessage(result));
                return false;
            }
            if(deviceCount < 1)
            {
                Debug.LogWarning(SRDHelper.SRDMessages.FullscreenGameViewError);
                return false;
            }

            return true;
        }

#if UNITY_EDITOR_WIN
        [MenuItem(FullScreenMenuPath + " _F11", false, 2001)]
#endif
        private static void ToggleSRDGameView()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.LogWarning("SRDisplay GameView cannot be changed in Play Mode");
                return;
            }

            if(SRDGameViewExists())
            {
                CloseAllSRDGameView();
            }
            else
            {
                ShowSRDGameView();
            }
        }

        private static bool ShowSRDGameView()
        {
            SrdXrResult result = SRDCorePlugin.SelectDevice(out var device);
            if(result == SrdXrResult.ERROR_USER_CANCEL)
            {
                return false;
            }
            else if(result != SrdXrResult.SUCCESS)
            {
                var msg = SrdXrResultToMessage(result);
                switch(SRDProjectSettings.GetBehaviorOptionWhenNoSRDisplay())
                {
                case RunWithDisabled:
                    Debug.LogWarning(msg);
                    break;
                case ExitWithError:
                default:
                    SRDHelper.PopupMessageAndForceToTerminate(msg);
                    break;
                }
                return false;
            }

            targetDevice = device;

            if(Prepare())
            {
                CloseAllUnityGameView();
                SetupGameView();
            }
            else
            {
                OnPostClosingTempGameView += SetupGameViewAfterCloseTempGameView;
            }
            return true;
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            EditorApplication.playModeStateChanged += PlayModeState;

            // GameView's showToolbar flag is restored to true when the script is recompiled, so set false again. 
            EditorApplication.delayCall += () =>
            {
                HideToolbarOfAllSRDGameView();
            };
        }

        private static void PlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if((!SRDGameViewExists())&&(!SRDProjectSettings.IsRunWithoutSRDisplayMode()))
                {
                    var playAfter = ShowSRDGameView();
                    if (playAfter)
                    {
                        UnityEditor.EditorApplication.isPlaying = false;

                        var framesBeforePlay = 30;
                        void PlayAfterShowingSRDGameView()
                        {
                            if(--framesBeforePlay > 0) return;
            
                            EditorApplication.update -= PlayAfterShowingSRDGameView;
                            UnityEditor.EditorApplication.isPlaying = true;
                        }

                        EditorApplication.update += PlayAfterShowingSRDGameView;
                    }
                }
            }
        }


        private static Vector2Int TargetScreenSize
        {
            get 
            {
                var targetScreen = targetDevice.target_monitor_rectangle;
                return new Vector2Int(targetScreen.right - targetScreen.left,
                                      targetScreen.bottom - targetScreen.top);
            }
        }

        private static Rect TargetScreenRect
        {
            get
            {
                var targetScreen = targetDevice.target_monitor_rectangle;
                return new Rect(targetScreen.left, 
                                targetScreen.top,
                                targetScreen.right - targetScreen.left, 
                                targetScreen.bottom - targetScreen.top);
            }
        }

        private static bool Prepare()
        {
            var screenSize = TargetScreenSize;

            // Set the SRD screen size to project settings
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = screenSize.x;
            PlayerSettings.defaultScreenHeight = screenSize.y;

            bool ready = GameViewSizeList.IsReadyDestinationSize(screenSize);
            if (!ready)
            {
                EditorApplication.update += CreateTemporaryGameView;
            }
            return ready;
        }

        private static void CreateTemporaryGameView()
        {
            // SRD Screen Size is added to GameViewSizes List by creating temporary GameView with Toolbar. 
            var gameView = new GameView(TemporalyGameViewName);
            gameView.showToolbar = true;
            gameView.position = new Rect(TargetScreenRect.position, Vector2.zero);
            gameView.ShowWithPopupMenu();

            EditorApplication.update -= CreateTemporaryGameView;
            EditorApplication.update += CloseTemporaryGameView;
        }

        private static void CloseTemporaryGameView()
        {
            // Close Temporary GameView that finished the task of updating GameViewSizes.
            var tmpGameViews = EnumGameViews().Where(w => w.name == TemporalyGameViewName);
            foreach (var view in tmpGameViews)
            {
                view.Close();
            }

            if(GameViewSizeList.IsReadyDestinationSize(TargetScreenSize))
            {
            }
            else
            {
                Debug.LogWarning("Fail to create destination size GameView. If you have a wrong size of SRDisplayGameView, please re-open SRDisplayGameView.");
            }

            EditorApplication.update -= CloseTemporaryGameView;
            if(OnPostClosingTempGameView != null)
            {
                OnPostClosingTempGameView.Invoke();
            }
        }

        private static void SetupGameView()
        {
            var gameView = new GameView(SRDGameViewName);
            gameView.scale = 1.0f;
            gameView.targetDisplay = 0;
            gameView.noCameraWarning = false;
            gameView.showToolbar = false;
            gameView.rectangle = TargetScreenRect;
            gameView.ShowWithPopupMenu();
        }

        private static void SetupGameViewAfterCloseTempGameView()
        {
            CloseAllUnityGameView();
            SetupGameView();
            OnPostClosingTempGameView -= SetupGameViewAfterCloseTempGameView;
        }
    }
}

#endif
