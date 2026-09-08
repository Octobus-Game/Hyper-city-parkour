using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace VideoSync
{
    /// <summary>
    /// Пример резолвера, который обращается к вашему собственному backend API.
    /// Backend внутри себя может использовать yt-dlp (Python) или youtube-dl,
    /// чтобы вытащить прямую ссылку на медиапоток по YouTube URL.
    ///
    /// Пример простого backend-эндпоинта (Python/Flask + yt-dlp), для справки:
    ///
    ///   POST /resolve
    ///   body: { "url": "https://www.youtube.com/watch?v=..." }
    ///   response: { "success": true, "directUrl": "https://...googlevideo.com/..." }
    ///
    /// Реализация backend-части не входит в этот файл — здесь только клиентский вызов.
    /// </summary>
    public class BackendVideoUrlResolver : IVideoUrlResolver
    {
        private readonly string _resolveEndpoint;

        public BackendVideoUrlResolver(string resolveEndpoint)
        {
            // Например: "https://your-server.com/api/resolve"
            _resolveEndpoint = resolveEndpoint;
        }

        public IEnumerator Resolve(string youtubeUrl, Action<bool, string, string> onResolved)
        {
            var payload = JsonUtility.ToJson(new ResolveRequest { url = youtubeUrl });
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(payload);

            using (var request = new UnityWebRequest(_resolveEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onResolved?.Invoke(false, null, $"Ошибка запроса к серверу: {request.error}");
                    yield break;
                }

                try
                {
                    var response = JsonUtility.FromJson<ResolveResponse>(request.downloadHandler.text);
                    if (response != null && response.success && !string.IsNullOrEmpty(response.directUrl))
                    {
                        onResolved?.Invoke(true, response.directUrl, null);
                    }
                    else
                    {
                        onResolved?.Invoke(false, null, "Сервер не смог обработать ссылку");
                    }
                }
                catch (Exception e)
                {
                    onResolved?.Invoke(false, null, $"Ошибка парсинга ответа: {e.Message}");
                }
            }
        }

        [Serializable]
        private class ResolveRequest
        {
            public string url;
        }

        [Serializable]
        private class ResolveResponse
        {
            public bool success;
            public string directUrl;
        }
    }
}
