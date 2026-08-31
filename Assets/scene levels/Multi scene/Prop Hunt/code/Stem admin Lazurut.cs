using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public class AdminPanelManager : NetworkBehaviour
{
    [Header("Admin Settings")]
    [SerializeField] private string adminSteamID = "76561199542823389"; 

    [Header("UI References")]
    [SerializeField] private GameObject adminPanel;
    [SerializeField] private Button flyButton;
    [SerializeField] private Button noclipButton;
    [SerializeField] private Button closeButton;

    [Header("Admin States")]
    [SyncVar] private bool isFlying = false;
    [SyncVar] private bool isNoclipEnabled = false;

    private bool isPanelActive = false;
    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private Collider playerCollider;
    private string currentUserSteamID;

    // Переменные для отслеживания комбинации клавиш
    private bool ctrlPressed = false;
    private bool lPressed = false;
    private bool f2Pressed = false;
    private float keyComboTimeout = 1f;
    private float lastKeyPressTime;

    void Start()
    {
        if (!isLocalPlayer) return;

        // Получаем Steam ID текущего пользователя
        if (SteamManager.Initialized)
        {
            CSteamID steamID = SteamUser.GetSteamID();
            currentUserSteamID = steamID.ToString();
        }

        // Получаем компоненты игрока
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();

        // Настраиваем UI
        if (adminPanel != null)
        {
            adminPanel.SetActive(false);
        }

        SetupButtons();
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (currentUserSteamID != adminSteamID) return;

        HandleKeyInput();
        HandleAdminFeatures();
    }

    private void HandleKeyInput()
    {
        // Отслеживаем нажатие комбинации Ctrl + L + F2
        bool currentCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool currentL = Input.GetKey(KeyCode.L);
        bool currentF2 = Input.GetKey(KeyCode.F2);

        // Проверяем последовательность нажатий
        if (currentCtrl && !ctrlPressed)
        {
            ctrlPressed = true;
            lastKeyPressTime = Time.time;
        }

        if (ctrlPressed && currentL && !lPressed)
        {
            if (Time.time - lastKeyPressTime < keyComboTimeout)
            {
                lPressed = true;
                lastKeyPressTime = Time.time;
            }
            else
            {
                ResetKeyCombo();
            }
        }

        if (ctrlPressed && lPressed && currentF2 && !f2Pressed)
        {
            if (Time.time - lastKeyPressTime < keyComboTimeout)
            {
                f2Pressed = true;
                ToggleAdminPanel();
                ResetKeyCombo();
            }
            else
            {
                ResetKeyCombo();
            }
        }

        // Сброс комбинации если прошло слишком много времени
        if (Time.time - lastKeyPressTime > keyComboTimeout)
        {
            ResetKeyCombo();
        }

        // Сброс при отпускании клавиш
        if (!currentCtrl) ctrlPressed = false;
        if (!currentL) lPressed = false;
        if (!currentF2) f2Pressed = false;
    }

    private void ResetKeyCombo()
    {
        ctrlPressed = false;
        lPressed = false;
        f2Pressed = false;
    }

    private void ToggleAdminPanel()
    {
        if (adminPanel != null)
        {
            isPanelActive = !isPanelActive;
            adminPanel.SetActive(isPanelActive);

            // Управление курсором
            Cursor.lockState = isPanelActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isPanelActive;

            Debug.Log($"Admin panel {(isPanelActive ? "opened" : "closed")} for Steam ID: {currentUserSteamID}");
        }
    }

    private void SetupButtons()
    {
        if (flyButton != null)
        {
            flyButton.onClick.AddListener(() => {
                CmdToggleFly();
            });
        }

        if (noclipButton != null)
        {
            noclipButton.onClick.AddListener(() => {
                CmdToggleNoclip();
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => {
                ToggleAdminPanel();
            });
        }
    }

    [Command]
    private void CmdToggleFly()
    {
        if (currentUserSteamID != adminSteamID) return;

        isFlying = !isFlying;
        RpcToggleFly(isFlying);
    }

    [Command]
    private void CmdToggleNoclip()
    {
        if (currentUserSteamID != adminSteamID) return;

        isNoclipEnabled = !isNoclipEnabled;
        RpcToggleNoclip(isNoclipEnabled);
    }

    [ClientRpc]
    private void RpcToggleFly(bool flyState)
    {
        if (!isLocalPlayer) return;

        isFlying = flyState;

        if (playerRigidbody != null)
        {
            playerRigidbody.useGravity = !isFlying;
        }

        Debug.Log($"Flying {(isFlying ? "enabled" : "disabled")}");
        UpdateButtonTexts();
    }

    [ClientRpc]
    private void RpcToggleNoclip(bool noclipState)
    {
        if (!isLocalPlayer) return;

        isNoclipEnabled = noclipState;

        if (playerCollider != null)
        {
            playerCollider.isTrigger = isNoclipEnabled;
        }

        Debug.Log($"Noclip {(isNoclipEnabled ? "enabled" : "disabled")}");
        UpdateButtonTexts();
    }

    private void HandleAdminFeatures()
    {
        // Обработка полёта
        if (isFlying)
        {
            HandleFlying();
        }

        // Обновление текста кнопок
        UpdateButtonTexts();
    }

    private void HandleFlying()
    {
        if (playerRigidbody == null) return;

        Vector3 flyDirection = Vector3.zero;
        float flySpeed = 10f;

        if (Input.GetKey(KeyCode.Space))
            flyDirection += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift))
            flyDirection += Vector3.down;
        if (Input.GetKey(KeyCode.W))
            flyDirection += transform.forward;
        if (Input.GetKey(KeyCode.S))
            flyDirection += -transform.forward;
        if (Input.GetKey(KeyCode.A))
            flyDirection += -transform.right;
        if (Input.GetKey(KeyCode.D))
            flyDirection += transform.right;

        playerRigidbody.velocity = flyDirection.normalized * flySpeed;
    }

    private void UpdateButtonTexts()
    {
        if (flyButton != null)
        {
            Text flyText = flyButton.GetComponentInChildren<Text>();
            if (flyText != null)
            {
                flyText.text = isFlying ? "Отключить полёт" : "Включить полёт";
            }
        }

        if (noclipButton != null)
        {
            Text noclipText = noclipButton.GetComponentInChildren<Text>();
            if (noclipText != null)
            {
                noclipText.text = isNoclipEnabled ? "Отключить прохождение" : "Включить прохождение";
            }
        }
    }

    // Метод для проверки прав администратора (можно использовать в других скриптах)
    public bool IsAdmin()
    {
        return currentUserSteamID == adminSteamID;
    }

    // Метод для добавления дополнительных Steam ID администраторов
    [System.Serializable]
    public class AdminList
    {
        public List<string> adminSteamIDs = new List<string>();
    }

    // Альтернативный метод проверки для множественных админов
    public bool IsAdminFromList(AdminList adminList)
    {
        return adminList.adminSteamIDs.Contains(currentUserSteamID);
    }
}

