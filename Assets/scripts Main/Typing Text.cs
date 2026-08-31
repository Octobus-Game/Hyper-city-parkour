using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TypingText : MonoBehaviour
{
    [Header("Text Components")]
    public TextMeshPro textComponent; // Ссылка на UI Text компонент
    public TextMeshProUGUI tmpTextComponent; // Ссылка на TextMeshPro компонент

    [Header("Settings")]
    public string fullText; // Полный текст, который нужно напечатать
    public float delayBetweenChars = 0.1f; // Задержка между символами

    void Start()
    {
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        // Очищаем текст в зависимости от того, какой компонент используется
        if (textComponent != null)
        {
            textComponent.text = "";
        }
        else if (tmpTextComponent != null)
        {
            tmpTextComponent.text = "";
        }
        else
        {
            Debug.LogError("Не назначен ни Text, ни TextMeshPro компонент!");
            yield break;
        }

        // Печатаем текст по символам
        foreach (char letter in fullText.ToCharArray())
        {
            if (textComponent != null)
            {
                textComponent.text += letter;
            }
            else if (tmpTextComponent != null)
            {
                tmpTextComponent.text += letter;
            }

            yield return new WaitForSeconds(delayBetweenChars);
        }
    }

    // Метод для изменения текста во время выполнения
    public void SetText(string newText)
    {
        fullText = newText;
        StopAllCoroutines();
        StartCoroutine(TypeText());
    }

    // Метод для остановки печати
    public void StopTyping()
    {
        StopAllCoroutines();

        // Показываем весь текст сразу
        if (textComponent != null)
        {
            textComponent.text = fullText;
        }
        else if (tmpTextComponent != null)
        {
            tmpTextComponent.text = fullText;
        }
    }
}