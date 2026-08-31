using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PinCodeInput : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI codeDisplay; // TextMeshPro для отображения кода
    public Button[] numberButtons; // Массив из 9 кнопок (1-9)
    
    [Header("Settings")]
    public string correctCode = "1234"; // Правильный код
    public int maxCodeLength = 4; // Максимальная длина кода
    
    [Header("Events")]
    public UnityEvent onActivated; // Событие при правильном коде
    public UnityEvent onCodeWrong; // Событие при неправильном коде (опционально)
    
    private string currentInput = ""; // Текущий введённый код

    void Start()
    {
        // Подписываемся на события кнопок
        for (int i = 0; i < numberButtons.Length; i++)
        {
            int buttonNumber = i + 1; // Номер кнопки (1-9)
            numberButtons[i].onClick.AddListener(() => OnNumberButtonClick(buttonNumber));
        }
        
        UpdateDisplay();
    }

    // Обработчик нажатия на кнопку с цифрой
    void OnNumberButtonClick(int number)
    {
        // Проверяем, не превышена ли максимальная длина
        if (currentInput.Length < maxCodeLength)
        {
            currentInput += number.ToString();
            UpdateDisplay();
            
            // Проверяем код, если достигнута нужная длина
            if (currentInput.Length == maxCodeLength)
            {
                CheckCode();
            }
        }
    }

    // Обновление отображения кода
    void UpdateDisplay()
    {
        // Можно отобразить звёздочки вместо цифр для безопасности
        codeDisplay.text = new string('*', currentInput.Length);
        
        // Или показать сам код
        codeDisplay.text = currentInput;
    }

    // Проверка введённого кода
    void CheckCode()
    {
        if (currentInput == correctCode)
        {
            Debug.Log("Код правильный!");
            codeDisplay.text = "Успех!";
            codeDisplay.color = Color.green;
            
            // Вызываем событие активации
            onActivated?.Invoke();
            
            // Здесь можно добавить действие при правильном коде
            // Например, открыть дверь, перейти на другую сцену и т.д.
        }
        else
        {
            Debug.Log("Код неправильный!");
            codeDisplay.text = "Ошибка!";
            codeDisplay.color = Color.red;
            
            // Вызываем событие неправильного кода
            onCodeWrong?.Invoke();
            
            // Сбрасываем после небольшой задержки
            Invoke("ResetInput", 1f);
        }
    }

    // Сброс введённых данных
    public void ResetInput()
    {
        currentInput = "";
        codeDisplay.color = Color.white;
        UpdateDisplay();
    }

    // Удаление последней цифры (если нужна кнопка "Backspace")
    public void DeleteLastDigit()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }
}