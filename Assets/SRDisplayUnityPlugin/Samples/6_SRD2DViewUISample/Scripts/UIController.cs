using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRD.Sample.UI2DView
{
    public class UIController : MonoBehaviour
    {
        [SerializeField]
        private OverlayGUIController overlayGUIController;

        [SerializeField]
        private SettingsUIController settingsUIController;

        private float expandAnimationDuration = 0.25f;

        private bool isSettingsUIVisible = false;

        void Start()
        {
            var rect = settingsUIController.GetComponent<RectTransform>();
            rect.localScale = new Vector3(0, 0, 0);
        }

        public void Init()
        {
            overlayGUIController.Init();
            settingsUIController.Init();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleSettingsUI();
            }
        }

        public void ToggleSettingsUI()
        {
            StartCoroutine(ShowSettingsUI(!isSettingsUIVisible));
        }

        private IEnumerator ShowSettingsUI(bool show)
        {
            float time = 0f;
            while (time < expandAnimationDuration)
            {
                time += Time.deltaTime;
                float progress = time / expandAnimationDuration;
                if (!show)
                {
                    progress = 1 - progress;
                }
                var rect = settingsUIController.GetComponent<RectTransform>();
                rect.localScale = Vector3.one * Mathf.Lerp(0, 1, progress);
                yield return null;
            }
            isSettingsUIVisible = show;
        }
    }
}