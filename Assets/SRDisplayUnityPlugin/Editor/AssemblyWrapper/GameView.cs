/*
 * Copyright 2019,2020,2021,2023 Sony Corporation
 */

#if UNITY_EDITOR

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

namespace SRD.Editor.AsssemblyWrapper
{
    internal class GameViewSize
    {
        public GameViewSize(int width, int height, string name)
        {
            this.width = width;
            this.height = height;
            this.name = name;
        }
        public int width { get; }
        public int height { get; }
        public string name { get; }
    }

    internal class GameViewSizeList
    {
        public static bool IsReadyDestinationSize(Vector2Int destinationSize)
        {
            return FindDestinationIndex(destinationSize) != -1;
        }

        private static int FindDestinationIndex(Vector2Int destinationSize)
        {
            var sizes = GetSizes();
            return sizes.FindIndex(size => size.width == destinationSize.x && size.height == destinationSize.y);
        }

        public static int FindDestinationIndex(Vector2 destinationSize)
        {
            return FindDestinationIndex(new Vector2Int((int)destinationSize.x, (int)destinationSize.y));
        }

        private static List<GameViewSize> GetSizes()
        {
            var source = typeof(UnityEditor.Editor).Assembly;
            var gameViewSizesType = source.GetType("UnityEditor.GameViewSizes");
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            var gameViewSizes = singletonType.GetProperty("instance").GetValue(null);

            var currentGroupType = (GameViewSizeGroupType)gameViewSizes.GetType()
                                   .GetProperty("currentGroupType")
                                   .GetValue(gameViewSizes);
            var sizeGroup = gameViewSizes.GetType()
                            .GetMethod("GetGroup")
                            .Invoke(gameViewSizes, new object[] { (int)currentGroupType });
            var totalCount = (int)sizeGroup.GetType()
                             .GetMethod("GetTotalCount")
                             .Invoke(sizeGroup, new object[] { });

            var displayTexts = sizeGroup.GetType()
                               .GetMethod("GetDisplayTexts")
                               .Invoke(sizeGroup, null) as string[];
            Debug.Assert(totalCount == displayTexts.Length);
            var sizes = Enumerable.Range(0, totalCount)
                        .Select(i => ToGameViewSize(sizeGroup, i, displayTexts[i]))
                        .ToList();

            return sizes;
        }

        private static GameViewSize ToGameViewSize(object sizeGroup, int index, string name)
        {
            var gameViewSize = sizeGroup.GetType()
                               .GetMethod("GetGameViewSize")
                               .Invoke(sizeGroup, new object[] { index });
            var width = (int)gameViewSize.GetType()
                        .GetProperty("width")
                        .GetValue(gameViewSize);
            var height = (int)gameViewSize.GetType()
                         .GetProperty("height")
                         .GetValue(gameViewSize);
            return new GameViewSize(width, height, name);
        }
    }

    internal class GameView
    {
        const BindingFlags nonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        //const BindingFlags publicStatic = BindingFlags.Static | BindingFlags.Public;
#if UNITY_2019_3_OR_NEWER
        const BindingFlags publicInstance = BindingFlags.Instance | BindingFlags.Public;
#endif
        const BindingFlags nonPublicStatic = BindingFlags.Static | BindingFlags.NonPublic;

        static readonly Type gameViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
        static readonly PropertyInfo showToolbarMemberInfo = gameViewType.GetProperty("showToolbar", nonPublicInstance);
        static readonly PropertyInfo viewInWindowMemberInfo = gameViewType.GetProperty("viewInWindow", nonPublicInstance);
#if UNITY_2022_1_OR_NEWER
        static readonly PropertyInfo selectedSizeIndexMemberInfo = gameViewType.GetProperty("selectedSizeIndex", publicInstance);
        static readonly PropertyInfo targetDisplayMemberInfo = gameViewType.GetProperty("targetDisplay", publicInstance);
#elif UNITY_2019_3_OR_NEWER
        static readonly PropertyInfo selectedSizeIndexMemberInfo = gameViewType.GetProperty("selectedSizeIndex", nonPublicInstance);
        static readonly PropertyInfo targetDisplayMemberInfo = gameViewType.GetProperty("targetDisplay", nonPublicInstance);
#else
        static readonly PropertyInfo selectedSizeIndexMemberInfo = gameViewType.GetProperty("selectedSizeIndex", nonPublicInstance);
        static readonly FieldInfo targetDisplayMemberInfo = gameViewType.GetField("m_TargetDisplay", nonPublicInstance);
#endif

        private EditorWindow gameView;

        // keep values because assembly refererence values are different before Apply
        private Rect applyRectangle;

        public static EditorWindow[] GetGameViews()
        {
            return Resources.FindObjectsOfTypeAll(gameViewType) as EditorWindow[];
        }

        public GameView(EditorWindow gameView)
        {
            this.gameView = gameView;
            this.applyRectangle = Rect.zero;
        }

        public GameView()
            : this(ScriptableObject.CreateInstance(gameViewType) as EditorWindow)
        {
        }

        public GameView(string gameViewName)
            : this()
        {
            this.gameView.name = gameViewName;
            this.gameView.titleContent.text = gameViewName;
        }

        private int windowToolbarHeight
        {
            get
            {
                var source = typeof(UnityEditor.Editor).Assembly;
                var editorGUI = source.GetType("UnityEditor.EditorGUI");
#if UNITY_2019_3_OR_NEWER
                var windowToolbarHeightObj = editorGUI.GetField("kWindowToolbarHeight", nonPublicStatic).GetValue(source);
                var windowToolbarHeight = (float)(windowToolbarHeightObj.GetType().GetProperty("value", publicInstance).GetValue(windowToolbarHeightObj));
                return (int)windowToolbarHeight;
#else
                return (int)editorGUI.GetField("kWindowToolbarHeight", nonPublicStatic).GetRawConstantValue();
#endif
            }
        }

        private int selectedSizeIndex
        {
            get
            {
                return (int)selectedSizeIndexMemberInfo.GetValue(gameView);
            }
            set
            {
                selectedSizeIndexMemberInfo.SetValue(gameView, value);
            }
        }

        public int targetDisplay
        {
            set
            {
                targetDisplayMemberInfo.SetValue(gameView, value);
            }
        }

        public float scale
        {
            set
            {
                var zoomArea = gameViewType
                               .GetField("m_ZoomArea", nonPublicInstance)
                               .GetValue(gameView);
                zoomArea.GetType()
                .GetField("m_Scale", nonPublicInstance)
                .SetValue(zoomArea, new Vector2(value, value));
            }
        }

        public bool noCameraWarning
        {
            set
            {
                gameViewType
                .GetField("m_NoCameraWarning", nonPublicInstance)
                .SetValue(gameView, value);
            }
        }

        public bool showToolbar
        {
            // showToolbar property exists since 2019.3.0
            get
            {
                return (showToolbarMemberInfo != null) ? (bool)showToolbarMemberInfo.GetValue(gameView) : true;
            }
            set
            {
                showToolbarMemberInfo?.SetValue(gameView, value);
            }
        }

        public Rect viewInWindow
        {
            get
            {
                return (Rect)viewInWindowMemberInfo.GetValue(gameView);
            }
        }

        public Rect position
        {
            get
            {
                return gameView.position;
            }
            set
            {
                gameView.position = value;
            }
        }

        public Rect rectangle
        {
            set
            {
                applyRectangle = value;
            }
        }

        private Rect CalculateWindowPlacement(Rect view)
        {
            var currWindow = this.gameView.position;
            var currViewInWindow = this.viewInWindow;
            var window = view;
            window.position -= currViewInWindow.position;
            window.size += currWindow.size - currViewInWindow.size;
            return window;
        }

        public void ShowWithPopupMenu()
        {
            var showModeType  = typeof(UnityEditor.Editor).Assembly.
                                GetType("UnityEditor.ShowMode");
            var popupMenu = Enum.ToObject(showModeType, 1);
            var showWithMode = gameViewType.GetMethod("ShowWithMode", nonPublicInstance);
            showWithMode.Invoke(gameView, new[] { popupMenu });

            if (this.applyRectangle != Rect.zero)
            {
                var newWindow = CalculateWindowPlacement(this.applyRectangle);
                this.gameView.maxSize = newWindow.size;
                this.gameView.minSize = newWindow.size;
                this.gameView.position = newWindow;

                var index = GameViewSizeList.FindDestinationIndex(this.applyRectangle.size);
                if (index >= 0)
                {
                    this.selectedSizeIndex = index;
                }

                EditorApplication.update += CorrectViewPlacement;
            }
        }

        private void CorrectViewPlacement()
        {
            var place = CalculateWindowPlacement(this.applyRectangle);
            if (this.gameView.position == place)
            {
                EditorApplication.update -= CorrectViewPlacement;
            }
            else if (this.gameView.position.position == Vector2.zero)
            {
                // Perhaps the Unity is in the process of arranging the window, so wait.
            }
            else
            {
                IntPtr hWnd = User32.FindWindow("UnityPopupWndClass", this.gameView.titleContent.text);
                if (hWnd != null)
                {
                    User32.SetWindowPos(hWnd, 0,
                        (int)place.position.x, (int)place.position.y,
                        (int)place.size.x, (int)place.size.y, 0x0040);
                }
            }
        }

        private struct User32
        {
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpszClass, string lpszTitle);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);
        }

    }
}

#endif
