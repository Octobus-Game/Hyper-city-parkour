using Mirror;
using UnityEngine;

/// <summary>
/// Вешается на сам префаб "spray plane" (вместе с NetworkIdentity).
/// Хранит картинку спрея как PNG-байты в SyncVar — это значит, что:
///  - все уже подключённые клиенты увидят применение текстуры мгновенно
///  - игроки, которые зайдут в игру позже, всё равно получат правильную
///    текстуру, т.к. SyncVar отдаётся в начальном снапшоте объекта
/// </summary>
[RequireComponent(typeof(Renderer))]
public class SprayDecal : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTextureChanged))]
    public byte[] textureBytes;

    [Tooltip("Через сколько секунд декаль исчезнет. 0 = никогда не исчезает")]
    public float lifeTime = 0f;

    private Renderer _renderer;
    private Texture2D _runtimeTexture;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    // Вызывается на каждом клиенте (включая того, кто заспавнил),
    // когда объект появляется в сцене у этого клиента.
    public override void OnStartClient()
    {
        ApplyTexture(textureBytes);
    }

    private void Start()
    {
        if (isServer && lifeTime > 0f)
        {
            Invoke(nameof(DespawnSelf), lifeTime);
        }
    }

    [Server]
    private void DespawnSelf()
    {
        if (gameObject != null)
            NetworkServer.Destroy(gameObject);
    }

    // Hook вызывается автоматически на клиентах при изменении SyncVar
    private void OnTextureChanged(byte[] oldBytes, byte[] newBytes)
    {
        ApplyTexture(newBytes);
    }

    private void ApplyTexture(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0 || _renderer == null)
            return;

        if (_runtimeTexture == null)
            _runtimeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        _runtimeTexture.LoadImage(bytes); // сам подгоняет размер текстуры под картинку

        // material — создаёт инстанс материала для этого конкретного декаля,
        // чтобы не перекрасить разом все остальные спреи, использующие тот же материал
        _renderer.material.mainTexture = _runtimeTexture;
    }
}
