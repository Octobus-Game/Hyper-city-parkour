using UnityEngine;
using Mirror;

/// <summary>
/// Прикрепить к GameObject игрока.
/// Сообщает серверу, откуда и куда смотрит камера этого игрока.
///
/// Работает так:
///   - На владельце (hasAuthority) каждый кадр шлём актуальные данные на сервер.
///   - Сервер читает EyePosition / LookDirection когда ему нужно.
/// </summary>
public class ScpPlayerEyes : NetworkBehaviour
{
    [Header("Привязка камеры")]
    [Tooltip("Если null — будет использован дочерний объект с тегом MainCamera")]
    public Transform cameraTransform;

    // Синхронизированные поля, читаемые ТОЛЬКО сервером
    // (SyncVar здесь не нужен, т.к. сервер сам запрашивает через CmdUpdateEyes)
    [HideInInspector] public Vector3 EyePosition;
    [HideInInspector] public Vector3 LookDirection;

    // ----------------------------------------------------------------
    // Инициализация
    // ----------------------------------------------------------------

    public override void OnStartLocalPlayer()
    {
        // Ищем камеру автоматически, если не задана
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                // Попытка найти по тегу среди дочерних объектов
                foreach (Transform t in GetComponentsInChildren<Transform>())
                {
                    if (t.CompareTag("MainCamera"))
                    {
                        cameraTransform = t;
                        break;
                    }
                }
            }
            else
            {
                cameraTransform = cam.transform;
            }
        }

        if (cameraTransform == null)
            Debug.LogWarning($"[ScpPlayerEyes] {name}: Camera not found. Assign cameraTransform in Inspector.");
    }

    // ----------------------------------------------------------------
    // Обновление
    // ----------------------------------------------------------------

    private void Update()
    {
        // Только локальный игрок отсылает свой взгляд
        if (!isLocalPlayer || cameraTransform == null) return;

        // Отправляем каждый кадр — SCP логика работает на FixedUpdate сервера,
        // так что небольшая задержка (1 кадр) некритична.
        CmdUpdateEyes(cameraTransform.position, cameraTransform.forward);
    }

    // ----------------------------------------------------------------
    // Mirror Command: клиент → сервер
    // ----------------------------------------------------------------

    /// <summary>
    /// Передаёт позицию и направление взгляда на сервер.
    /// </summary>
    [Command(requiresAuthority = true)]
    private void CmdUpdateEyes(Vector3 eyePos, Vector3 lookDir)
    {
        EyePosition  = eyePos;
        LookDirection = lookDir;
    }
}
