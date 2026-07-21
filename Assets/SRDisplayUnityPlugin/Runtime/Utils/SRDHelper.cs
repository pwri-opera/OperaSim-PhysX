/*
 * Copyright 2019-2025 Sony Corporation
 */


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using SRD.Core;
using System.Reflection;

namespace SRD.Utils
{
    public enum EyeType
    {
        Left = 0,
        Right = 1
    }

    public enum RenderPipelineType
    {
        BRP,
        URP,
        HDRP,
        UnknownSRP
    }

    /// <summary>
    /// A class to keep Spatial Reality Display's edge positions
    /// </summary>
    public class DisplayEdges
    {
        /// <summary>
        /// A constructor of DisplayEdges.
        /// </summary>
        /// <param name="presence"> Transform of display presence. </param>
        /// <param name="bodyBounds"> Display body bounds. </param>
        internal DisplayEdges(Transform presence, SRDSettings.BodyBounds bodyBounds)
        {
            Func<Vector3, GameObject> MakeEdge = (Vector3 edge) =>
            {
                var go = new GameObject();
                go.transform.SetParent(presence);
                go.transform.localPosition = edge;
                go.transform.localRotation = Quaternion.identity;
                go.hideFlags = HideFlags.HideAndDontSave;
                return go;
            };
            _leftUp = MakeEdge(bodyBounds.LeftUp).transform;
            _leftBottom = MakeEdge(bodyBounds.LeftBottom).transform;
            _rightUp = MakeEdge(bodyBounds.RightUp).transform;
            _rightBottom = MakeEdge(bodyBounds.RightBottom).transform;
        }

        internal DisplayEdges(Transform presence)
        {
            Func<GameObject> MakeEdge = () =>
            {
                var go = new GameObject();
                go.transform.SetParent(presence);
                go.transform.localRotation = Quaternion.identity;
                go.hideFlags = HideFlags.HideAndDontSave;
                return go;
            };
            _leftUp = MakeEdge().transform;
            _leftBottom = MakeEdge().transform;
            _rightUp = MakeEdge().transform;
            _rightBottom = MakeEdge().transform;
        }


        internal void FitToBodyBounds(SRDSettings.BodyBounds bodyBounds)
        {
            _leftUp.localPosition = bodyBounds.LeftUp;
            _leftBottom.localPosition = bodyBounds.LeftBottom;
            _rightUp.localPosition = bodyBounds.RightUp;
            _rightBottom.localPosition = bodyBounds.RightBottom;
        }

        ~DisplayEdges()
        {
            GameObject.DestroyImmediate(_leftUp.gameObject);
            GameObject.DestroyImmediate(_leftBottom.gameObject);
            GameObject.DestroyImmediate(_rightUp.gameObject);
            GameObject.DestroyImmediate(_rightBottom.gameObject);
        }

        private Transform _leftUp;
        /// <summary>
        /// A LeftUp edge.
        /// </summary>
        public Transform LeftUp { get { return _leftUp; } }

        private Transform _leftBottom;
        /// <summary>
        /// A LeftBottom edge.
        /// </summary>
        public Transform LeftBottom { get { return _leftBottom; } }

        private Transform _rightUp;
        /// <summary>
        /// A RightUp edge.
        /// </summary>
        public Transform RightUp { get { return _rightUp; } }

        private Transform _rightBottom;
        /// <summary>
        /// A RightBottom edge.
        /// </summary>
        public Transform RightBottom { get { return _rightBottom; } }

        /// <summary>
        /// Center position of Spatial Reality Display
        /// </summary>
        public Vector3 CenterPosition
        {
            get
            {
                return (_leftBottom.position + _rightUp.position) / 2f;
            }
        }

        /// <summary>
        /// Normal vector of Spatial Reality Display
        /// </summary>
        public Vector3 Normal
        {
            get
            {
                var lhs = _leftUp.position - _leftBottom.position;
                var rhs = _rightBottom.position - _leftBottom.position;
                return Vector3.Cross(lhs, rhs); // left hand rule
            }
        }

        /// <summary>
        /// An array of edge positions. The order is counterclockwise from LeftUp (i.e. LeftUp, LeftBottom, RightBottom, and RightUp).
        /// </summary>
        public Vector3[] Positions
        {
            get
            {
                return new Vector3[]
                {
                    _leftUp.position, _leftBottom.position,
                    _rightBottom.position, _rightUp.position
                };
            }
        }
    }

    internal class FacePose : IEquatable<FacePose>
    {
        public Pose HeadPose;
        public Pose EyePoseL;
        public Pose EyePoseR;

        private readonly float IPD = 0.065f;

        public FacePose()
        {
            this.HeadPose = new Pose();
            this.EyePoseL = new Pose();
            this.EyePoseR = new Pose();
        }
        public FacePose(Pose headPose, Pose eyePoseL, Pose eyePoseR)
        {
            this.HeadPose = headPose;
            this.EyePoseL = eyePoseL;
            this.EyePoseR = eyePoseR;
        }

        public static FacePose operator *(FacePose fp, float f)
        {
            fp.HeadPose.position *= f;
            fp.EyePoseL.position *= f;
            fp.EyePoseR.position *= f;
            return fp;
        }

        public FacePose GetTransformedBy(Transform lhs)
        {
            return new FacePose(this.HeadPose.GetTransformedBy(lhs),
                                this.EyePoseL.GetTransformedBy(lhs),
                                this.EyePoseR.GetTransformedBy(lhs));
        }

        public void UpdateWithNewHeadPose(Pose newHeadPose, Vector3 lookAtTarget)
        {
            this.HeadPose = newHeadPose;
            this.EyePoseL = (new Pose(Vector3.left * IPD / 2f, Quaternion.identity)).GetTransformedBy(newHeadPose);
            this.EyePoseL.rotation = Quaternion.LookRotation(lookAtTarget - this.EyePoseL.position, Vector3.up);
            this.EyePoseR = (new Pose(Vector3.right * IPD / 2f, Quaternion.identity)).GetTransformedBy(newHeadPose);
            this.EyePoseR.rotation = Quaternion.LookRotation(lookAtTarget - this.EyePoseR.position, Vector3.up);
        }

        public bool Equals(FacePose other)
        {
            return this.HeadPose == other.HeadPose && this.EyePoseL == other.EyePoseL && this.EyePoseR == other.EyePoseR;
        }

        public Pose GetEyePose(EyeType type)
        {
            return (type == EyeType.Left) ? this.EyePoseL : this.EyePoseR;
        }
    }

    internal class FaceProjectionMatrix
    {
        public Matrix4x4 HeadMatrix;
        public Matrix4x4 LeftMatrix;
        public Matrix4x4 RightMatrix;

        public FaceProjectionMatrix()
        {
            this.HeadMatrix = Matrix4x4.identity;
            this.LeftMatrix = Matrix4x4.identity;
            this.RightMatrix = Matrix4x4.identity;
        }

        public FaceProjectionMatrix(Matrix4x4 headProjectionMatrix, Matrix4x4 leftProjectionMatrix, Matrix4x4 rightProjectionMatrix)
        {
            this.HeadMatrix = headProjectionMatrix;
            this.LeftMatrix = leftProjectionMatrix;
            this.RightMatrix = rightProjectionMatrix;
        }

        public Matrix4x4 GetProjectionMatrix(EyeType type)
        {
            return (type == EyeType.Left) ? this.LeftMatrix : this.RightMatrix;
        }
    }

    internal static partial class SRDHelper
    {
        public static class SRDConstants
        {
            public const string PresenceGameObjDefaultName = "Presence";
            public const string WatcherGameObjDefaultName = "WatcherAnchor";
            public const string WatcherCameraGameObjDefaultName = "WatcherCamera";
            public const string EyeCamGameObjDefaultName = "EyeCamera";
            public const string EyeAnchorGameObjDefaultName = "EyeAnchor";
            public const string EyeCamRenderTexDefaultName = "EyeCamRenderTex";
            public const string HomographyCommandBufferName = "SRDHomographyCommandBuffer";
            public const string RenderingGameObjDefaultName = "Rendering";
            public const string MainViewGameObjDefaultName = "MainView";
            public const string SideBySideGameObjDefaultName = "SideBySide";
            public const string SRD2DViewGameObjDefaultName = "2DView";
            public const string SRD2DViewImageDefaultName = "RenderImage";
            public const string MultiDisplayControllerDefaultName = "SRDMultiDisplayController";

            public const string SRDProjectSettingsAssetPath = "Assets/SRDisplayUnityPlugin/Resources/SRDProjectSettings.asset";

            public const string XRRuntimeWrapperDLLName = "xr_runtime_unity_wrapper";
        }

        public static readonly Dictionary<EyeType, string> EyeSideName = new Dictionary<EyeType, string>
        {
            { EyeType.Left, "Left" },
            { EyeType.Right, "Right" }
        };

        public static readonly RenderPipelineType renderPipelineType;

        static SRDHelper()
        {
            renderPipelineType = DetectRenderPipelineType();
        }

        public static void PopupMessageAndForceToTerminate(string message, bool forceToTerminate = true)
        {
#if UNITY_EDITOR
            bool isPlaying = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            bool isPlaying = Application.isPlaying;
#endif
            if(forceToTerminate && isPlaying)
            {
                message += ("\n" + SRDHelper.SRDMessages.AppCloseMessage);
                SRDCorePlugin.ShowMessageBox("Error", message, Debug.LogError);

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            else
            {
                SRDCorePlugin.ShowMessageBox("Error", message, Debug.LogError);
            }
        }

        public static void PopupWarningMessage(string message)
        {
            SRDCorePlugin.ShowMessageBox("Warning", message, Debug.LogWarning);
        }


        public static bool HasNanOrInf(Matrix4x4 m)
        {
            for(var i = 0; i < 16; i++)
            {
                if(float.IsNaN(m[i]) || float.IsInfinity(m[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static Pose InvPose(Pose p)
        {
            var invq = Quaternion.Inverse(p.rotation);
            return new Pose((invq * -p.position), invq);
        }

        public static Matrix4x4 PoseToMatrix(Pose p)
        {
            return Matrix4x4.TRS(p.position, p.rotation, Vector3.one);
        }

        public static Pose MatrixToPose(Matrix4x4 m)
        {
            return new Pose(m.GetColumn(3), m.rotation);
        }


        public static float[] CalcHomographyMatrix(Vector3 leftUp, Vector3 leftBottom, Vector3 rightBottom, Vector3 rightUp, UnityEngine.Camera camera)
        {
            Vector2 p00 = camera.WorldToViewportPoint(leftBottom);
            Vector2 p01 = camera.WorldToViewportPoint(leftUp);
            Vector2 p10 = camera.WorldToViewportPoint(rightBottom);
            Vector2 p11 = camera.WorldToViewportPoint(rightUp);

            var x00 = p00.x;
            var y00 = p00.y;
            var x01 = p01.x;
            var y01 = p01.y;
            var x10 = p10.x;
            var y10 = p10.y;
            var x11 = p11.x;
            var y11 = p11.y;

            var a = x10 - x11;
            var b = x01 - x11;
            var c = x00 - x01 - x10 + x11;
            var d = y10 - y11;
            var e = y01 - y11;
            var f = y00 - y01 - y10 + y11;

            var h13 = x00;
            var h23 = y00;
            var h32 = (c * d - a * f) / (b * d - a * e);
            var h31 = (c * e - b * f) / (a * e - b * d);
            var h11 = x10 - x00 + h31 * x10;
            var h12 = x01 - x00 + h32 * x01;
            var h21 = y10 - y00 + h31 * y10;
            var h22 = y01 - y00 + h32 * y01;

            return new float[] { h11, h12, h13, h21, h22, h23, h31, h32, 1f };
        }

        public static float[] CalcInverseMatrix3x3(float[] mat)
        {
            var i11 = mat[0];
            var i12 = mat[1];
            var i13 = mat[2];
            var i21 = mat[3];
            var i22 = mat[4];
            var i23 = mat[5];
            var i31 = mat[6];
            var i32 = mat[7];
            var i33 = mat[8];
            var a = 1f / (
                        +(i11 * i22 * i33)
                        + (i12 * i23 * i31)
                        + (i13 * i21 * i32)
                        - (i13 * i22 * i31)
                        - (i12 * i21 * i33)
                        - (i11 * i23 * i32)
                    );

            var o11 = (i22 * i33 - i23 * i32) / a;
            var o12 = (-i12 * i33 + i13 * i32) / a;
            var o13 = (i12 * i23 - i13 * i22) / a;
            var o21 = (-i21 * i33 + i23 * i31) / a;
            var o22 = (i11 * i33 - i13 * i31) / a;
            var o23 = (-i11 * i23 + i13 * i21) / a;
            var o31 = (i21 * i32 - i22 * i31) / a;
            var o32 = (-i11 * i32 + i12 * i31) / a;
            var o33 = (i11 * i22 - i12 * i21) / a;

            return new float[] { o11, o12, o13, o21, o22, o23, o31, o32, o33 };
        }

        public static RenderPipelineType DetectRenderPipelineType()
        {
            var renderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset == null)
            {
                return RenderPipelineType.BRP;
            }

            var renderPipelineAssetName = renderPipelineAsset.GetType().Name;
            if (renderPipelineAssetName.Contains("UniversalRenderPipelineAsset"))
            {
                return RenderPipelineType.URP;
            }
            if (renderPipelineAssetName.Contains("HDRenderPipelineAsset"))
            {
                return RenderPipelineType.HDRP;
            }

            return RenderPipelineType.UnknownSRP;
        }

        public static string InspectorName(this Enum value)
        {
            var valueName = value.ToString();

            var inspectorNameAttribute = value.GetType().GetField(valueName).GetCustomAttribute<InspectorNameAttribute>();
            if (inspectorNameAttribute != null)
            {
                valueName = inspectorNameAttribute.displayName;
            }

            return valueName;
        }
    }

}

