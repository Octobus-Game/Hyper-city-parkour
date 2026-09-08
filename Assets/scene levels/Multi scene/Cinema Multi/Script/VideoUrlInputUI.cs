using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VideoSync
{
    /// <summary>
    /// UI-скрипт: связывает TMP_InputField и Button с NetworkVideoPlayer.
    /// Повесить на UI-объект в сцене (Canvas). Не обязательно NetworkBehaviour.
    /// </summary>
    public class VideoUrlInputUI : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private TMP_InputField urlInputField;
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;

        [Header("Network target")]
        [Tooltip("Ссылка на сетевой видеоплеер в сцене")]
        [SerializeField] private NetworkVideoPlayer networkVideoPlayer;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseButtonClicked);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        private void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(OnPlayButtonClicked);

            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeButtonClicked);
        }

        private void OnPlayButtonClicked()
        {
            if (urlInputField == null || networkVideoPlayer == null)
            {
                Debug.LogWarning("[VideoUrlInputUI] Не заданы ссылки на UI или NetworkVideoPlayer");
                return;
            }

            string url = urlInputField.text;
            networkVideoPlayer.RequestPlayVideo(url);
        }

        private void OnPauseButtonClicked()
        {
            networkVideoPlayer?.RequestPause();
        }

        private void OnResumeButtonClicked()
        {
            networkVideoPlayer?.RequestResume();
        }
    }
}
