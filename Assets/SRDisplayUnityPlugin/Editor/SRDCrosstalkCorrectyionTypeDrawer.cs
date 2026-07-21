/*
 * Copyright 2021,2023 Sony Corporation
 */


using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using SRD.Core;

namespace SRD.Editor
{
    [CustomPropertyDrawer(typeof(SrdXrCrosstalkCorrectionType))]
    class SRDCrosstalkCorrectionTypeDrawer : PropertyDrawer
    {
        private static readonly int[] _enumValues = Enum.GetValues(typeof(SrdXrCrosstalkCorrectionType)) as int[];

        private static readonly Dictionary<SrdXrCrosstalkCorrectionType, GUIContent> _dicCrosstalkCorrectionType =
            new Dictionary<SrdXrCrosstalkCorrectionType, GUIContent>
        {
            {
                SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_MEDIUM,
                new GUIContent("Low",
                               "Corrects crosstalk and make it less noticeable at medium gradation. GPU load will be a little higher than when crosstalk correction is not used.")
            },
            {
                SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_ALL,
                new GUIContent("Mid",
                               "Corrects crosstalk and make it less noticeable at all gradation. GPU load will be higher than that of \"Low\".")
            },
            {
                SrdXrCrosstalkCorrectionType.GRADATION_CORRECTION_HIGH_PRECISE,
                new GUIContent("High",
                               "Corrects crosstalk at all gradation. Crosstalk will be less noticeable than \"Mid\". GPU load will be higher than when \"Mid\".")
            }
        };

        private static readonly GUIContent[] _enumAppearances = new GUIContent[_enumValues.Length];

        public SRDCrosstalkCorrectionTypeDrawer()
        {
            int i = 0;
            foreach(SrdXrCrosstalkCorrectionType type in _enumValues)
            {
                if(_dicCrosstalkCorrectionType.TryGetValue(type, out var guiContent))
                {
                    _enumAppearances[i] = guiContent;
                }
                else
                {
                    _enumAppearances[i] = new GUIContent(type.ToString());
                }
                ++i;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var isActiveProperty = property.serializedObject.FindProperty("IsCrosstalkCorrectionActive");
            var isActive = (isActiveProperty != null) && isActiveProperty.boolValue;

            EditorGUI.BeginDisabledGroup(!isActive);
            using(new EditorGUI.PropertyScope(position, label, property))
            {
                var tooltipAttributeArray = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true);
                if(tooltipAttributeArray.Length > 0)
                {
                    label.tooltip = ((TooltipAttribute)tooltipAttributeArray[0]).tooltip;
                }

                property.enumValueIndex = EditorGUI.IntPopup(position, label, property.enumValueIndex, _enumAppearances, _enumValues);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}

