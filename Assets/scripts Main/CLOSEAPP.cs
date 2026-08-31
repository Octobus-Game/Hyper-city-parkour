using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CLOSEAPP : MonoBehaviour
{
    void Start()
    {
        // Запуск корутины, которая закроет игру через 10 секунд
        StartCoroutine(CloseGameAfterDelay(10f));
    }

    private IEnumerator CloseGameAfterDelay(float delay)
    {
        // Ожидание указанного количества секунд
        yield return new WaitForSeconds(delay);

        // Закрытие игры
        Application.Quit();
#if UNITY_EDITOR
        // Остановка режима игры в редакторе Unity
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
