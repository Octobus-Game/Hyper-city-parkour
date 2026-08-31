using Mirror;
using UnityEngine;

/// <summary>
/// Повесьте на префаб куба (рядом с NetworkIdentity).
///
/// Отвечает за синхронизацию цвета куба по сети.
/// Сервер устанавливает цвет через ServerSetColor() до NetworkServer.Spawn,
/// SyncVar автоматически доставляет его всем клиентам при создании объекта.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class CubeColorSync : NetworkBehaviour
{
    // SyncVar с hook: при изменении значения на сервере hook вызывается на всех клиентах
    [SyncVar(hook = nameof(OnColorChanged))]
    private Color _color = Color.white;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    // Вызывается сервером из CubePlacer.CmdSpawnCube до NetworkServer.Spawn
    [Server]
    public void ServerSetColor(Color color)
    {
        _color = color;          // SyncVar — клиенты получат значение при spawn
        ApplyColor(color);       // применяем сразу на сервере
    }

    // Hook срабатывает на каждом клиенте, когда SyncVar меняется
    private void OnColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor(newColor);
    }

    private void ApplyColor(Color color)
    {
        if (_renderer != null)
            _renderer.material.color = color;
    }
}
