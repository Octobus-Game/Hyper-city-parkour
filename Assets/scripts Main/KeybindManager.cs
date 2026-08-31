using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeybindManager : MonoBehaviour
{
    [System.Serializable]
    public class KeybindButton
    {
        public string actionName;        // Название действия (Forward, Back, Left, Right...)
        public Button button;            // UI Button
        public TextMeshProUGUI keyText;  // TMP текст на кнопке
        public KeyCode defaultKey;       // Клавиша по умолчанию
        [HideInInspector] public KeyCode currentKey;
    }

    [Header("Keybind Buttons")]
    [SerializeField] private KeybindButton[] keybinds = new KeybindButton[]
    {
        new KeybindButton { actionName = "Forward",    defaultKey = KeyCode.W },
        new KeybindButton { actionName = "Back",       defaultKey = KeyCode.S },
        new KeybindButton { actionName = "Left",       defaultKey = KeyCode.A },
        new KeybindButton { actionName = "Right",      defaultKey = KeyCode.D },
        new KeybindButton { actionName = "Jump",       defaultKey = KeyCode.Space },
        new KeybindButton { actionName = "Dash",       defaultKey = KeyCode.LeftControl },
        new KeybindButton { actionName = "Crouch",     defaultKey = KeyCode.CapsLock },
    };

    [Header("UI")]
    [SerializeField] private GameObject rebindOverlay;      // Затемнение/оверлей "Нажмите клавишу..."
    [SerializeField] private TextMeshProUGUI overlayText;   // Текст в оверлее

    [Header("Colors")]
    [SerializeField] private Color normalColor    = Color.white;
    [SerializeField] private Color waitingColor   = Color.yellow;
    [SerializeField] private Color duplicateColor = Color.red;

    private KeybindButton _currentRebinding = null;
    private bool _isWaiting = false;

    // Публичный доступ к клавишам для CharacterMovement0
    public static KeybindManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadAllKeys();

        foreach (var kb in keybinds)
        {
            var capture = kb; // захват для лямбды
            capture.button.onClick.AddListener(() => StartRebind(capture));
            UpdateButtonText(capture);
        }

        if (rebindOverlay != null)
            rebindOverlay.SetActive(false);
    }

    // ── Получение клавиши по имени действия (для CharacterMovement0) ─
    public KeyCode GetKey(string actionName)
    {
        foreach (var kb in keybinds)
            if (kb.actionName == actionName)
                return kb.currentKey;
        return KeyCode.None;
    }

    // ── Начало ожидания нажатия ──────────────────────────────────────
    private void StartRebind(KeybindButton kb)
    {
        if (_isWaiting) return;

        _currentRebinding = kb;
        _isWaiting = true;

        kb.keyText.color = waitingColor;
        kb.keyText.text = "...";

        if (rebindOverlay != null)
        {
            rebindOverlay.SetActive(true);
            if (overlayText != null)
                overlayText.text = "Назначение: " + kb.actionName + "\nНажмите любую клавишу";
        }

        StartCoroutine(WaitForKey());
    }

    // ── Ожидание нажатия клавиши ─────────────────────────────────────
    private IEnumerator WaitForKey()
    {
        // Пропускаем один кадр чтобы не поймать клик по кнопке
        yield return null;

        while (_isWaiting)
        {
            // Отмена по Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelRebind();
                yield break;
            }

            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kc))
                {
                    ApplyKey(kc);
                    yield break;
                }
            }

            yield return null;
        }
    }

    // ── Применение новой клавиши ─────────────────────────────────────
    private void ApplyKey(KeyCode newKey)
    {
        _isWaiting = false;

        // Проверка дублирования
        bool isDuplicate = false;
        foreach (var kb in keybinds)
        {
            if (kb != _currentRebinding && kb.currentKey == newKey)
            {
                isDuplicate = true;

                // Сбрасываем старый биндинг на None
                kb.currentKey = KeyCode.None;
                PlayerPrefs.SetString("Key_" + kb.actionName, KeyCode.None.ToString());
                kb.keyText.color = duplicateColor;
                kb.keyText.text = "---";
                break;
            }
        }

        _currentRebinding.currentKey = newKey;
        PlayerPrefs.SetString("Key_" + _currentRebinding.actionName, newKey.ToString());
        PlayerPrefs.Save();

        _currentRebinding.keyText.color = normalColor;
        UpdateButtonText(_currentRebinding);

        if (rebindOverlay != null)
            rebindOverlay.SetActive(false);

        _currentRebinding = null;
    }

    // ── Отмена ───────────────────────────────────────────────────────
    private void CancelRebind()
    {
        _isWaiting = false;

        if (_currentRebinding != null)
        {
            _currentRebinding.keyText.color = normalColor;
            UpdateButtonText(_currentRebinding);
            _currentRebinding = null;
        }

        if (rebindOverlay != null)
            rebindOverlay.SetActive(false);
    }

    // ── Обновление текста кнопки ─────────────────────────────────────
    private void UpdateButtonText(KeybindButton kb)
    {
        if (kb.keyText == null) return;

        kb.keyText.text = kb.currentKey == KeyCode.None
            ? "---"
            : kb.currentKey.ToString();
    }

    // ── Сохранение / загрузка ────────────────────────────────────────
    private void LoadAllKeys()
    {
        foreach (var kb in keybinds)
        {
            string saved = PlayerPrefs.GetString("Key_" + kb.actionName, kb.defaultKey.ToString());
            if (System.Enum.TryParse(saved, out KeyCode parsed))
                kb.currentKey = parsed;
            else
                kb.currentKey = kb.defaultKey;
        }
    }

    // ── Сброс всех клавиш к дефолту ─────────────────────────────────
    public void ResetToDefaults()
    {
        foreach (var kb in keybinds)
        {
            kb.currentKey = kb.defaultKey;
            PlayerPrefs.SetString("Key_" + kb.actionName, kb.defaultKey.ToString());
            kb.keyText.color = normalColor;
            UpdateButtonText(kb);
        }
        PlayerPrefs.Save();
    }
}