using UnityEngine;

/// <summary>
/// Центральный менеджер порталов.
/// Размещается на любом объекте сцены (например, на Main Camera или пустом GameObject).
///
/// ИНСТРУКЦИЯ ПО НАСТРОЙКЕ:
/// 1. Создайте два объекта-плоскости (Plane или Quad) — это порталы.
/// 2. Назначьте им шейдер Unlit/Texture (или свой Portal шейдер).
/// 3. Прикрепите NonEuclideanPortal к каждой плоскости.
/// 4. Свяжите порталы друг с другом через поле linkedPortal.
/// 5. Укажите Main Camera в поле mainCamera каждого портала.
/// 6. Прикрепите PortalTeleporter к игроку.
/// 7. Добавьте этот PortalManager на любой GameObject, укажите камеру и список порталов.
/// </summary>
public class PortalManager : MonoBehaviour
{
    [Header("Сцена")]
    public Camera               mainCamera;
    public NonEuclideanPortal[] portals;
    public PortalTeleporter[]   travellers; // Объекты, которые могут телепортироваться

    void Awake()
    {
        // Автоматически назначаем mainCamera порталам, если не задано
        if (mainCamera == null)
            mainCamera = Camera.main;

        foreach (var portal in portals)
        {
            if (portal != null && portal.mainCamera == null)
                portal.mainCamera = mainCamera;
        }
    }

    // Рендерим порталы ДО того, как Unity рендерит основную камеру
    void OnPreRender()
    {
        RenderPortals();
    }

    // Запасной вариант через LateUpdate (если OnPreRender не срабатывает)
    void LateUpdate()
    {
        // Проверяем телепортацию каждый кадр
        foreach (var traveller in travellers)
        {
            if (traveller != null)
                traveller.CheckPortals(portals);
        }
    }

    private void RenderPortals()
    {
        foreach (var portal in portals)
        {
            if (portal != null && portal.isActiveAndEnabled)
                portal.RenderPortalView();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (portals == null) return;

        foreach (var portal in portals)
        {
            if (portal == null) continue;

            // Рисуем нормаль портала
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(portal.transform.position, portal.transform.forward * 0.5f);

            // Рисуем связь между порталами
            if (portal.linkedPortal != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(portal.transform.position, portal.linkedPortal.transform.position);
            }
        }
    }
#endif
}
