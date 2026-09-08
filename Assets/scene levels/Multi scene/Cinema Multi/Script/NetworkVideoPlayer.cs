using System;
using Mirror;
using UnityEngine;
using UnityEngine.Video;

namespace VideoSync
{
    /// <summary>
    /// Сетевой видеоплеер для Mirror.
    /// - Сервер резолвит YouTube URL в прямой видео-поток.
    /// - Прямой URL рассылается всем клиентам через SyncVar.
    /// - Play/Pause/Stop/Seek синхронизируются через Commands + ClientRpc,
    ///   чтобы видео у всех игроков шло синхронно.
    ///
    /// Повесить этот компонент на сетевой объект (с NetworkIdentity),
    /// который заспавнен в сцене (Scene Object), либо иметь его
    /// как singleton-объект, зарегистрированный в NetworkManager.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class NetworkVideoPlayer : NetworkBehaviour
    {
        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RenderTexture renderTexture;

        [Header("Resolver settings")]
        [Tooltip("URL вашего backend-эндпоинта для резолва YouTube ссылок")]
        [SerializeField] private string resolveEndpoint = "https://your-server.com/api/resolve";

        // Текущий прямой URL стрима, синхронизируется всем клиентам автоматически
        [SyncVar(hook = nameof(OnVideoUrlChanged))]
        private string currentDirectUrl = "";

        // Текущее состояние воспроизведения, тоже синхронизируется
        [SyncVar(hook = nameof(OnPlayingStateChanged))]
        private bool isPlaying;

        private IVideoUrlResolver _resolver;

        private void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();

            if (renderTexture != null)
            {
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.source = VideoSource.Url;

            _resolver = new BackendVideoUrlResolver(resolveEndpoint);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Если клиент подключился, когда видео уже задано - применяем сразу
            if (!string.IsNullOrEmpty(currentDirectUrl))
            {
                ApplyUrlLocally(currentDirectUrl, isPlaying);
            }
        }

        // ---------- Публичный метод, вызываемый из UI (кнопка) ----------

        /// <summary>
        /// Вызывается локально с клиента (например, UI Button), когда пользователь
        /// ввёл URL и нажал "Воспроизвести". Сам запрос идёт на сервер через Command.
        /// </summary>
        public void RequestPlayVideo(string youtubeUrl)
        {
            if (string.IsNullOrWhiteSpace(youtubeUrl))
            {
                Debug.LogWarning("[NetworkVideoPlayer] Пустой URL");
                return;
            }

            CmdRequestPlayVideo(youtubeUrl.Trim());
        }

        public void RequestPause()
        {
            CmdSetPlaying(false);
        }

        public void RequestResume()
        {
            CmdSetPlaying(true);
        }

        // ---------- Commands (Client -> Server) ----------

        [Command(requiresAuthority = false)]
        private void CmdRequestPlayVideo(string youtubeUrl, NetworkConnectionToClient sender = null)
        {
            // Резолвинг делает только сервер
            StartCoroutine(_resolver.Resolve(youtubeUrl, (success, directUrl, error) =>
            {
                if (!success)
                {
                    Debug.LogError($"[NetworkVideoPlayer] Не удалось резолвить URL: {error}");
                    TargetNotifyError(sender, error);
                    return;
                }

                currentDirectUrl = directUrl; // SyncVar - разлетится всем клиентам
                isPlaying = true;

                // На сервере (если это Host) тоже нужно применить локально,
                // хук SyncVar на сервере для себя самого не вызывается,
                // поэтому применяем явно.
                ApplyUrlLocally(directUrl, true);
            }));
        }

        [Command(requiresAuthority = false)]
        private void CmdSetPlaying(bool play)
        {
            isPlaying = play;
            ApplyPlayingStateLocally(play);
        }

        // ---------- TargetRpc для уведомления об ошибке конкретного клиента ----------

        [TargetRpc]
        private void TargetNotifyError(NetworkConnectionToClient target, string error)
        {
            Debug.LogError($"[NetworkVideoPlayer] Ошибка на сервере: {error}");
            // Здесь можно показать UI-сообщение об ошибке пользователю
        }

        // ---------- SyncVar hooks (Server -> Clients автоматически) ----------

        private void OnVideoUrlChanged(string oldUrl, string newUrl)
        {
            ApplyUrlLocally(newUrl, isPlaying);
        }

        private void OnPlayingStateChanged(bool oldValue, bool newValue)
        {
            ApplyPlayingStateLocally(newValue);
        }

        // ---------- Локальное применение состояния к VideoPlayer ----------

        private void ApplyUrlLocally(string directUrl, bool play)
        {
            if (string.IsNullOrEmpty(directUrl))
                return;

            videoPlayer.url = directUrl;
            videoPlayer.Prepare();

            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.prepareCompleted += OnPrepareCompleted;

            void OnPrepareCompleted(VideoPlayer vp)
            {
                vp.prepareCompleted -= OnPrepareCompleted;
                if (play)
                    vp.Play();
            }
        }

        private void ApplyPlayingStateLocally(bool play)
        {
            if (videoPlayer == null) return;

            if (play)
            {
                if (!videoPlayer.isPlaying)
                    videoPlayer.Play();
            }
            else
            {
                if (videoPlayer.isPlaying)
                    videoPlayer.Pause();
            }
        }
    }
}
