/*
 * Copyright 2019-2025 Sony Corporation
 */

using System;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
    using UnityEngine.UIElements;
#else
    using UnityEngine.Experimental.UIElements;
#endif
using UnityEditor;

using SRD.Utils;

namespace SRD.Editor
{
    internal class SRDProjectSettingsAsset
    {
        private const string AssetPath = SRDHelper.SRDConstants.SRDProjectSettingsAssetPath;
        private static SRDProjectSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SRDProjectSettings>(AssetPath);
            if(settings == null)
            {
                return Create();
            }
            else
            {
                return settings;
            }
        }

        private static SRDProjectSettings Create()
        {
            var directoryPath = System.IO.Path.GetDirectoryName(AssetPath);
            System.IO.Directory.CreateDirectory(directoryPath);

            var instance = SRDProjectSettings.GetDefault();
            AssetDatabase.CreateAsset(instance, AssetPath);
            AssetDatabase.SaveAssets();
            return instance;
        }

        internal static SerializedObject GetMutable()
        {
            return new SerializedObject(GetOrCreate());
        }

        public static SRDProjectSettings Get()
        {
            return GetOrCreate();
        }

        public static bool Exists()
        {
            return System.IO.File.Exists(AssetPath);
        }
    }

    internal class SRDProjectSettingsProvider : SettingsProvider
    {
        private SerializedObject mutableSettings;

        public SRDProjectSettingsProvider(string path, SettingsScope scope)
            : base(path, scope)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            mutableSettings = SRDProjectSettingsAsset.GetMutable();
        }

        [CustomPropertyDrawer(typeof(SRDProjectSettings.BehaviorOptionWhenNoSRDisplay))]
        class BehaviorOptionWhenNoSRDisplayDrawer : PropertyDrawer
        {
            private static readonly string _labelText = "Behavior with no Spatial Reality Display";

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                label.text = _labelText;
                property.intValue = Convert.ToInt32(EditorGUI.EnumPopup(position, label, (SRDProjectSettings.BehaviorOptionWhenNoSRDisplay)property.intValue));
            }
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 0, 10, 0) });
            EditorGUIUtility.labelWidth = 250;

            var RunWithoutSpatialRealityDisplay = mutableSettings.FindProperty("RunWithoutSpatialRealityDisplay");

            EditorGUILayout.PropertyField(RunWithoutSpatialRealityDisplay);
            EditorGUILayout.Separator();

            if (!RunWithoutSpatialRealityDisplay.boolValue)
            {
                EditorGUILayout.PropertyField(mutableSettings.FindProperty("BehaviorWhenNoSRDisplay"));
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Multi Display Settings", EditorStyles.boldLabel);

                var LinkingMode = mutableSettings.FindProperty("MultiDisplayMode");
                EditorGUILayout.PropertyField(LinkingMode);

                SRDProjectSettings settings = (SRDProjectSettings)mutableSettings.targetObject;
                var parameters = SRDProjectSettings.MultipleSRDParameters[settings.MultiDisplayMode];
                var showExtendedParameters = parameters.Show ? 1.0f : 0.0f;

                EditorGUI.indentLevel = 1;
                if (EditorGUILayout.BeginFadeGroup(showExtendedParameters))
                {
                    EditorGUI.BeginDisabledGroup(!parameters.CanEditDeviceNum);

                    var DeviceCount = mutableSettings.FindProperty("NumberOfDisplays");
                    EditorGUILayout.PropertyField(DeviceCount);
                    DeviceCount.intValue = Math.Clamp(DeviceCount.intValue, parameters.MinDeviceNum, parameters.MaxDeviceNum);

                    EditorGUI.EndDisabledGroup();

                    var PositionSwitchInterval = mutableSettings.FindProperty("PositionChangeTime");
                    EditorGUILayout.PropertyField(PositionSwitchInterval);
                    PositionSwitchInterval.floatValue = Mathf.Round(Mathf.Clamp(PositionSwitchInterval.floatValue, 1f, 15f) * 100f) / 100f;
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel = 0;
            }
            EditorGUILayout.EndVertical();

            mutableSettings.ApplyModifiedProperties();
        }
    }

    static class SRDProjectSettingsRegister
    {
        [SettingsProvider]
        private static SettingsProvider CreateProviderToRegister()
        {
            var path = "Project/Spatial Reality Display";
            var provider = new SRDProjectSettingsProvider(path, SettingsScope.Project);
            return provider;
        }
    }
}
