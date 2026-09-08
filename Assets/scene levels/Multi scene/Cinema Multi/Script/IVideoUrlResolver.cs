using System;
using System.Collections;

namespace VideoSync
{
    /// <summary>
    /// Интерфейс для получения прямой ссылки на видеопоток из YouTube URL.
    /// Unity VideoPlayer не может напрямую открыть youtube.com/watch?v=...,
    /// поэтому нужен резолвер, который вернёт прямой .mp4/.m3u8 адрес.
    ///
    /// Варианты реализации:
    /// 1) Свой backend-сервис (например, на Node.js/Python с yt-dlp),
    ///    который принимает YouTube URL и отдаёт прямую ссылку на поток.
    /// 2) Готовый ассет из Unity Asset Store для проигрывания YouTube
    ///    (учитывайте условия использования YouTube API / ToS).
    ///
    /// В данном проекте резолвинг делает ТОЛЬКО СЕРВЕР (Host/Server),
    /// а полученный прямой URL уже рассылается клиентам через SyncVar,
    /// чтобы не заставлять каждого клиента дергать сторонний сервис.
    /// </summary>
    public interface IVideoUrlResolver
    {
        /// <summary>
        /// Асинхронно резолвит YouTube-ссылку в прямой видео-поток.
        /// Вызывать через StartCoroutine.
        /// </summary>
        /// <param name="youtubeUrl">Исходная ссылка, введённая пользователем</param>
        /// <param name="onResolved">Колбэк с результатом: (success, directStreamUrl, error)</param>
        IEnumerator Resolve(string youtubeUrl, Action<bool, string, string> onResolved);
    }
}
