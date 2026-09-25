using Mirror;
using UnityEngine;
using mirrorplayer;

public class ToggleUIOnKeyPress : NetworkBehaviour
{
    [Header("UI объект, который нужно показать/скрыть")]
    [SerializeField] private GameObject uiPanel;

    [Header("Клавиша активации")]
    [SerializeField] private KeyCode activationKey = KeyCode.Tab;

    [Header("Блокировка камеры и курсор")]
    [Tooltip("Скрипт игрока. Если не назначен — будет найден автоматически на этом же объекте.")]
    [SerializeField] private playermirror player;

    [Tooltip("Закрывать панель по Escape")]
    [SerializeField] private bool closeOnEscape = true;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<playermirror>();
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKeyDown(activationKey))
        {
            ToggleUI();
        }

        // Escape уже закрывает UI внутри playermirror (HandleUIToggle),
        // но панель здесь управляется отдельно, поэтому синхронизируем её.
        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape) && uiPanel != null && uiPanel.activeSelf)
        {
            SetUIState(false);
        }
    }

    private void ToggleUI()
    {
        if (uiPanel == null)
        {
            Debug.LogWarning("UI Panel не назначен в инспекторе!");
            return;
        }

        SetUIState(!uiPanel.activeSelf);
    }

    private void SetUIState(bool open)
    {
        uiPanel.SetActive(open);

        // playermirror.SetUIMode(true)  -> блокирует вращение камеры и включает курсор
        // playermirror.SetUIMode(false) -> возвращает вращение камеры и скрывает/блокирует курсор
        if (player != null)
            player.SetUIMode(open);
        else
            Debug.LogWarning("playermirror не найден — камера и курсор не будут переключены!");
    }
}