/*
 * Copyright 2019,2020,2024 Sony Corporation
 */

using UnityEditor;

using SRD.Core;
using SRD.Utils;

namespace SRD.Editor
{
    [CustomEditor(typeof(SRDManager))]
    internal class SRDManagerInspector : UnityEditor.Editor
    {
        private const string _errorMessage = "Too many SRDManagers in a scene is not supported. Remove unnecessary SRDManagers.";

        private void OnEnable()
        {
            var managersNum = SRDSceneEnvironment.GetSRDManagers().Length;
            if(managersNum > SRDProjectSettings.GetNumberOfDevices())
            {
                UnityEngine.Debug.LogError(_errorMessage);
                EditorUtility.DisplayDialog("ERROR", _errorMessage, "OK");
                var instance = (Core.SRDManager)target;
                EditorApplication.delayCall += () => UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}

