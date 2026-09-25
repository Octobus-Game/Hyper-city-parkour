using Steamworks;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PlayerAvatarImage : MonoBehaviour
{
    private Image image;
    private CSteamID steamId;
    private Callback<AvatarImageLoaded_t> avatarLoadedCallback;

    private Texture2D texture;
    private Sprite sprite;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnDestroy()
    {
        avatarLoadedCallback?.Dispose();

        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);
    }

    /// <summary>
    /// Загружает и показывает аватар игрока Steam.
    /// </summary>
    public void Setup(CSteamID id)
    {
        steamId = id;

        if (!SteamManager.Initialized) return;

        // Подписываемся на догрузку аватара, если он ещё не готов
        if (avatarLoadedCallback == null)
            avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);

        // Просим Steam подгрузить данные пользователя (нужно для незнакомых игроков)
        SteamFriends.RequestUserInformation(steamId, false);

        TryLoadAvatar();
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t data)
    {
        if (data.m_steamID != steamId) return;
        ApplyAvatar(data.m_iImage);
    }

    private void TryLoadAvatar()
    {
        int handle = SteamFriends.GetLargeFriendAvatar(steamId);
        ApplyAvatar(handle);
    }

    private void ApplyAvatar(int handle)
    {
        // -1 = данные ещё запрашиваются, 0 = у игрока нет аватара
        if (handle <= 0) return;

        if (!SteamUtils.GetImageSize(handle, out uint width, out uint height)) return;
        if (width == 0 || height == 0) return;

        byte[] rgba = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(handle, rgba, rgba.Length)) return;

        // Steam отдаёт картинку перевёрнутой по вертикали — исправляем
        byte[] flipped = new byte[rgba.Length];
        int rowSize = (int)width * 4;
        for (int y = 0; y < height; y++)
        {
            System.Buffer.BlockCopy(
                rgba, y * rowSize,
                flipped, ((int)height - 1 - y) * rowSize,
                rowSize);
        }

        // Освобождаем прошлые ресурсы при повторной загрузке
        if (sprite != null) Destroy(sprite);
        if (texture != null) Destroy(texture);

        texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(flipped);
        texture.Apply();

        sprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f));

        image.sprite = sprite;
    }
}