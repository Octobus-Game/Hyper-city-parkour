using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor-утилита: Tools → Remove Missing Scripts
/// Удаляет все пропавшие (null) компоненты со всех GameObject'ов на активной сцене.
/// </summary>
public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts")]
    private static void RemoveAll()
    {
        // Находим все GameObject'ы на сцене, включая неактивные
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        int totalRemoved   = 0;
        int affectedObjects = 0;

        // Начинаем группу отмены (Ctrl+Z), чтобы операция была обратимой
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Remove Missing Scripts");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject go in allObjects)
        {
            // Пропускаем объекты из пакетов/ассетов — берём только объекты сцены
            if (!go.scene.IsValid()) continue;

            // Регистрируем объект для отмены перед изменением
            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");

            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            if (removed > 0)
            {
                totalRemoved   += removed;
                affectedObjects++;

                // Помечаем объект как изменённый, чтобы сцена запросила сохранение
                EditorUtility.SetDirty(go);
            }
        }

        // Схлопываем все undo-записи в одну группу
        Undo.CollapseUndoOperations(undoGroup);

        if (totalRemoved == 0)
        {
            Debug.Log("[RemoveMissingScripts] Пропавших скриптов не найдено.");
        }
        else
        {
            Debug.Log(
                $"[RemoveMissingScripts] Удалено {totalRemoved} пропавших скрипт(ов) " +
                $"с {affectedObjects} объект(ов)."
            );
        }
    }

    // Пункт меню активен только в режиме редактора (не во время Play Mode)
    [MenuItem("Tools/Remove Missing Scripts", validate = true)]
    private static bool ValidateRemoveAll() => !EditorApplication.isPlaying;
}
