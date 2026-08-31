using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Playercustomization : NetworkBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;
    public Button[]   buttons;

    // -- НОВОЕ: кнопка и объекты сброса --------------------------------------
    [Header("Reset")]
    public Button      resetButton;
    public GameObject[] resetObjects; // объекты, которые включаются при сбросе

    [Header("Objects to Activate")]
    public GameObject[] objects;

    [Header("Settings")]
    public bool showOnlyForHost = false;

    // Синхронизированный индекс активного объекта (-1 = исходное состояние)
    [SyncVar(hook = nameof(OnActiveObjectChanged))]
    private int activeObjectIndex = -1;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isLocalPlayer)
        {
            SetupUI();
        }

        ApplyObjectStates();
    }

    private void SetupUI()
    {
        if (panel != null)
        {
            bool shouldShowPanel = !showOnlyForHost || isServer;
            panel.SetActive(shouldShowPanel);
        }

        if (panel != null && panel.activeSelf && buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    int buttonIndex = i;
                    buttons[i].onClick.AddListener(() => OnButtonPressed(buttonIndex));
                }
            }
        }

        // -- НОВОЕ: подключаем кнопку Reset -----------------------------------
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetPressed);
        }
    }

    private void OnButtonPressed(int buttonIndex)
    {
        CmdActivateObject(buttonIndex);
    }

    // -- НОВОЕ: обработчик нажатия Reset --------------------------------------
    private void OnResetPressed()
    {
        CmdResetObjects();
    }

    [Command]
    private void CmdActivateObject(int objectIndex)
    {
        if (objects == null || objectIndex < 0 || objectIndex >= objects.Length)
            return;

        activeObjectIndex = objectIndex;
        
    }

    // -- НОВОЕ: команда сброса на сервере -------------------------------------
    [Command]
    private void CmdResetObjects()
    {
        // -1 триггерит OnActiveObjectChanged > восстанавливает начальные состояния
        activeObjectIndex = -1;
    }

    [ClientRpc]
    private void RpcHidePanel()
    {
        if (panel != null && isLocalPlayer)
        {
            panel.SetActive(false);
        }
    }

    private void OnActiveObjectChanged(int oldValue, int newValue)
    {
        UpdateObjectStates();
    }

    // -- ИЗМЕНЕНО: UpdateObjectStates обрабатывает два режима -----------------
    private void UpdateObjectStates()
    {
        if (activeObjectIndex == -1)
        {
            // Режим Reset: выключить все objects[], включить все resetObjects[]
            foreach (var obj in objects)
                if (obj != null) obj.SetActive(false);

            foreach (var obj in resetObjects)
                if (obj != null) obj.SetActive(true);
        }
        else
        {
            // Обычный режим: выключить все resetObjects[], включить только выбранный
            foreach (var obj in resetObjects)
                if (obj != null) obj.SetActive(false);

            for (int i = 0; i < objects.Length; i++)
                if (objects[i] != null) objects[i].SetActive(i == activeObjectIndex);
        }
    }

    private void ApplyObjectStates()
    {
        UpdateObjectStates();
    }

    public void ShowPanel()
    {
        if (panel != null && isLocalPlayer)
        {
            bool shouldShowPanel = !showOnlyForHost || isServer;
            panel.SetActive(shouldShowPanel);
        }
    }

        private void OnDestroy()
    {
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].onClick.RemoveAllListeners();
            }
        }

        // -- НОВОЕ: чистим listener у кнопки Reset ----------------------------
        if (resetButton != null)
            resetButton.onClick.RemoveAllListeners();
    }
}