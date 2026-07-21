/*
 * Copyright 2019,2020,2023,2024 Sony Corporation
 */

using System;
using UnityEngine;

using SRD.Core;

namespace SRD.Utils
{
    internal class SRDSettings
    {
        private SRDDeviceInfo _deviceInfo;
        public SRDDeviceInfo DeviceInfo { get { return _deviceInfo; } }

        private SRDSettings(SRDDeviceInfo deviceInfo)
        {
            _deviceInfo = deviceInfo;
        }

        internal SRDSettings() : this(new SRDDeviceInfo()) {}

        internal SRDSettings(Int32 sessionId) : this(new SRDDeviceInfo(sessionId)) {}

        internal bool Load(Int32 sessionId)
        {
            return _deviceInfo.Load(sessionId);
        }

        public class SRDDeviceInfo
        {
            private ScreenRect _screenRect;
            public ScreenRect ScreenRect { get { return _screenRect; } }

            private BodyBounds _bodyBounds;
            public BodyBounds BodyBounds { get { return _bodyBounds; } }

            public static ScreenRect DefaultScreenRect { get { return getDefaultScreenRect(); } }
            public static BodyBounds DefaultBodyBounds { get { return getDefaultBodyBounds(); } }

            private static ScreenRect getDefaultScreenRect()
            {
                return new ScreenRect();
            }

            private static BodyBounds getDefaultBodyBounds()
            {
                return new BodyBounds();
            }

            private static ScreenRect loadScreenRect(Int32 sessionId)
            {
                if (SRDCorePlugin.GetTargetMonitorRectangle(sessionId, out var rect) != SrdXrResult.SUCCESS)
                {
                    return null;
                }
                return new ScreenRect(rect.left, rect.top,
                                      rect.right - rect.left, rect.bottom - rect.top);
            }

            private static BodyBounds loadBodyBounds(Int32 sessionId)
            {
                if (SRDCorePlugin.GetDisplaySpec(sessionId, out var spec) != SrdXrResult.SUCCESS)
                {
                    return null;
                }
                return new BodyBounds(spec.display_size, spec.display_tilt_rad);
            }

            public bool Load(Int32 sessionId)
            {
                var resSR = LoadScreenRect(sessionId);
                var resBB = LoadBodyBounds(sessionId);
                return (resSR && resBB);
            }

            public bool LoadScreenRect(Int32 sessionId)
            {
                var screenRect = loadScreenRect(sessionId);
                if(screenRect == null)
                {
                    _screenRect = getDefaultScreenRect();
                    return false;
                }
                _screenRect = screenRect;
                return true;
            }

            public bool LoadBodyBounds(Int32 sessionId)
            {
                var bodyBounds = loadBodyBounds(sessionId);
                if(bodyBounds == null)
                {
                    _bodyBounds = getDefaultBodyBounds();
                    return false;
                }
                _bodyBounds = bodyBounds;
                return true;
            }

            private SRDDeviceInfo(ScreenRect resolution, BodyBounds displayBounds)
            {
                _screenRect = resolution;
                _bodyBounds = displayBounds;
            }

            internal SRDDeviceInfo() : this(new ScreenRect(), new BodyBounds())
            {
            }

            internal SRDDeviceInfo(Int32 sessionId)
            {
                Load(sessionId);
            }
        }

        [System.Serializable]
        public class ScreenRect
        {
            public const int DefaultWidth = 3840;
            public const int DefaultHeight = 2160;
            public const int DefaultLeft = 0;
            public const int DefaultTop = 0;

            public ScreenRect()
                : this(ScreenRect.DefaultLeft,  ScreenRect.DefaultTop,
                       ScreenRect.DefaultWidth, ScreenRect.DefaultHeight)
            {
                // do nothing
            }

            public ScreenRect(int left, int top, int width, int height)
            {
                _left = left;
                _top = top;
                _width = width;
                _height = height;
            }

            [SerializeField]
            private int _width;
            public int Width { get { return _width; } }

            [SerializeField]
            private int _height;
            public int Height { get { return _height; } }

            [SerializeField]
            private int _left;
            public int Left { get { return _left; } }

            [SerializeField]
            private int _top;
            public int Top { get { return _top; } }

            public Vector2Int Resolution { get { return new Vector2Int(_width, _height); } }
            public Vector2Int Position { get { return new Vector2Int(_left, _top); } }
        }

        [System.Serializable]
        public class BodyBounds
        {
            public const float DefaultWidth = 0.3442176f;
            public const float DefaultHeight = 0.1369117f;
            public const float DefaultDepth = 0.1369117f;

            public BodyBounds() : this(BodyBounds.DefaultWidth, 
                                       BodyBounds.DefaultHeight,
                                       BodyBounds.DefaultDepth)
            {
            }

            public BodyBounds(float width, float height, float depth)
            {
                _width = width;
                _height = height;
                _depth = depth;

                _leftUp = new Vector3(-this.Width / 2f, this.Height, this.Depth);
                _leftBottom = new Vector3(-this.Width / 2f, 0.0f, 0.0f);
                _rightUp = new Vector3(this.Width / 2f, this.Height, this.Depth);
                _rightBottom = new Vector3(this.Width / 2f, 0.0f, 0.0f);
                _center = new Vector3(0f, this.Height / 2f, this.Depth / 2f);
                _boxSize = new Vector3(this.Width, this.Height, this.Depth);
            }

            public BodyBounds(Vector2 panelSize, float tiltRad)
                : this(panelSize.x,
                       panelSize.y * Mathf.Cos(tiltRad),
                       panelSize.y * Mathf.Sin(tiltRad))
            {
            }

            public BodyBounds(Rect panelRect, float tiltRad)
                : this(panelRect.size, tiltRad)
            {
            }

            [SerializeField]
            private float _width;
            public float Width { get { return _width; } }
            [SerializeField]
            private float _height;
            public float Height { get { return _height; } }
            [SerializeField]
            private float _depth;
            public float Depth { get { return _depth; } }

            // in PositionTrackingCoord
            private Vector3 _leftUp;
            public Vector3 LeftUp { get { return _leftUp; } }
            private Vector3 _leftBottom;
            public Vector3 LeftBottom { get { return _leftBottom; } }
            private Vector3 _rightUp;
            public Vector3 RightUp { get { return _rightUp; } }
            private Vector3 _rightBottom;
            public Vector3 RightBottom { get { return _rightBottom; } }

            public Vector3[] EdgePositions { get { return new Vector3[] { this.LeftUp, this.LeftBottom, this.RightBottom, this.RightUp }; } }

            private Vector3 _center;
            public Vector3 Center { get { return _center; } }
            private Vector3 _boxSize;
            public Vector3 BoxSize { get { return _boxSize; } }

            public float ScaleFactor { get { return _width / DefaultWidth; } }
        }
    }
}
