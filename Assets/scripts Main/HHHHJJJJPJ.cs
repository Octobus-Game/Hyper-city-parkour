using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeImageOnCollision : MonoBehaviour
{
    // Укажем изображение, которое нужно изменить
    public Image targetImage; // Для UI Image
    public Sprite newSprite;  // Новый спрайт при столкновении

    private string saveKey = "CurrentSprite";

    void Start()
    {
        // При запуске игры проверим, сохранено ли ранее изображение
        if (PlayerPrefs.HasKey(saveKey))
        {
            string savedSpritePath = PlayerPrefs.GetString(saveKey);
            Sprite savedSprite = Resources.Load<Sprite>(savedSpritePath);

            if (savedSprite != null)
            {
                targetImage.sprite = savedSprite;
            }
        }
    }

    // Метод для обнаружения столкновения
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Изменяем изображение
            targetImage.sprite = newSprite;

            // Сохраняем путь к новому изображению
            PlayerPrefs.SetString(saveKey, newSprite.name);
            PlayerPrefs.Save();
        }
    }

    void OnApplicationQuit()
    {
        // Сохраняем текущее изображение перед выходом из игры
        PlayerPrefs.SetString(saveKey, targetImage.sprite.name);
        PlayerPrefs.Save();
    }
}