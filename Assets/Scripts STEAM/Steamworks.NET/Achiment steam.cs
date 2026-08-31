using UnityEngine;
using Steamworks;

public class AchievementTrigger : MonoBehaviour
{
    [Header("Настройки достижения")]
    [Tooltip("Введите название достижения из Steam (например: ACH_WIN_ONE_GAME)")]
    [SerializeField] private string achievementName = "ACH_WIN_ONE_GAME";

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем, что это CharacterController игрока
        CharacterController characterController = other.GetComponent<CharacterController>();

        if (characterController != null && other.CompareTag("Player"))
        {
            UnlockAchievement();
        }
    }

    private void UnlockAchievement()
    {
        // Проверяем, что название достижения не пустое
        if (string.IsNullOrEmpty(achievementName))
        {
            Debug.LogError("Название достижения не указано!");
            return;
        }

        // Проверяем, инициализирован ли Steam API
        if (SteamManager.Initialized)
        {
            // Получаем достижение
            SteamUserStats.GetAchievement(achievementName, out bool achieved);

            // Если достижение еще не получено
            if (!achieved)
            {
                // Устанавливаем достижение
                SteamUserStats.SetAchievement(achievementName);

                // Сохраняем изменения на серверах Steam
                bool success = SteamUserStats.StoreStats();

                if (success)
                {
                    Debug.Log($"Достижение {achievementName} успешно получено!");
                }
                else
                {
                    Debug.LogWarning($"Не удалось сохранить достижение {achievementName}");
                }
            }
            else
            {
                Debug.Log($"Достижение {achievementName} уже получено ранее");
            }
        }
        else
        {
            Debug.LogError("Steam API не инициализирован!");
        }
    }
}