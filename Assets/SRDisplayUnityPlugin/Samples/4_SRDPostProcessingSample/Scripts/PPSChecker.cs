/*
 * Copyright 2019,2020,2021 Sony Corporation
 */

using UnityEngine;
using UnityEditor;

public class PPSChecker
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#endif
    static void CheckPPS()
    {
#if UNITY_POST_PROCESSING_STACK_V2
        Debug.LogWarning("This SRDPostProcessingSample uses Post Processing effects included in URP package so you don't need to import Post Processing Stack v2 package");
#endif
    }
}
