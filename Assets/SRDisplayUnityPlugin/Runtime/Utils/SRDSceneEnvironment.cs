/*
 * Copyright 2019-2025 Sony Corporation
 */


using UnityEngine;

using SRD.Core;
using System;

namespace SRD.Utils
{
    /// <summary>
    /// A class for utility functions to manage objects in the scene.
    /// </summary>
    public class SRDSceneEnvironment
    {
        /// <summary>
        /// Get All SRDManager in the scene. Returns 0 length array if there is no SRDManager.
        /// This can be very expensive to execute. Calling this frequently is not recommended.
        /// </summary>
        /// <returns>Array of SRDManager objects.
        public static SRDManager[] GetSRDManagers()
        {
#if UNITY_2023_1_OR_NEWER
            var candidates = UnityEngine.Object.FindObjectsByType<SRDManager>(FindObjectsSortMode.None);
#else
            var candidates = UnityEngine.Object.FindObjectsOfType<SRDManager>();
#endif
            if(candidates.Length == 0)
            {
                Debug.LogWarning(SRDHelper.SRDMessages.SRDManagerNotFoundError);
            }
            return candidates as SRDManager[];
        }

        /// <summary>
        /// Get primary SRDManager in the scene. Returns null if there is no SRDManager.
        /// This can be very expensive to execute. Calling this frequently is not recommended.
        /// </summary>
        /// <returns>SRDManager object. Or null if there is no SRDManager. </returns>
        public static SRDManager GetSRDManager()
        {
            return Array.Find(GetSRDManagers(), manager => manager.DeviceIndex == 0);
        }

        /// <summary>
        /// Get SRDManager in the scene. Returns null if there is no SRDManager.
        /// This can be very expensive to execute. Calling this frequently is not recommended.
        /// </summary>
        /// <param name="deviceIndex"> Index of SRDisplay device. </param>
        /// <returns>SRDManager object with specified device index. Or null if there is no such SRDManager. </returns>
        public static SRDManager GetSRDManager(int deviceIndex)
        {
            return Array.Find(GetSRDManagers(), manager => manager.DeviceIndex == deviceIndex);
        }

        /// <summary>
        /// Find GameObject based on the name and get it or create a new one if there is no GameObject with the name
        /// This can be very expensive to execute. Calling this frequently is not recommended.
        /// </summary>
        /// <param name="parent"> Root transform to search. </param>
        /// <param name="name"> Search target name of GameObject. </param>
        /// <returns>GameObject with the name. </returns>
        public static GameObject GetOrCreateChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return GetOrCreateObject(name);
            }

            var childTransform = parent.Find(name);
            if(childTransform == null)
            {
                var gameObject = new GameObject(name);
                gameObject.transform.SetParent(parent);
                InitializePose(gameObject.transform);
                return gameObject;
            }
            return childTransform.gameObject;
        }

        private static GameObject GetOrCreateObject(string name)
        {
            var gameObject = GameObject.Find(name);
            if (gameObject == null)
            {
                gameObject = new GameObject(name);
                gameObject.transform.SetParent(null);
                InitializePose(gameObject.transform);
            }
            return gameObject;
        }

        /// <summary>
        /// Find Component based on the type T and get it or create a new one if there is no Component with the type T
        /// </summary>
        /// <typeparam name="T">Search target class type</typeparam>
        /// <param name="gameObject">Search target GameObject</param>
        /// <returns>Type T Component </returns>
        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var instance = gameObject.GetComponent<T>();
            if(instance == null)
            {
                return gameObject.AddComponent<T>();
            }
            return instance;
        }

        /// <summary>
        /// Set localPosition to Vector3.zero and localRotation to Quaternion.identity.
        /// </summary>
        /// <param name="transform">Target transform </param>
        public static void InitializePose(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
