using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

namespace mirror_hand_smena
{
    public class WEapon : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        public GameObject[] objects;
        public Button[] buttons;
        public KeyCode[] switchKeys;
	
	[Header("Weapon List Auto-Hide")]
	public float weaponListHideDelay = 10f;
        
	[Header("Weapon Switch Keys")]
        public bool enableNextPrevKeys = true;
        public KeyCode nextWeaponKey = KeyCode.E;
        public KeyCode prevWeaponKey = KeyCode.Q;

        [Header("Mouse Wheel Switch")]
        public bool enableMouseWheelSwitch = true;

        [Header("Weapon List Display")]
        public TextMeshProUGUI weaponListText; // TMP текст дл€ отображени€ списка оружий

        [SyncVar(hook = nameof(OnActiveWeaponChanged))]
        private int currentActiveIndex = 0;

        void Start()
        {
            if (switchKeys == null || switchKeys.Length == 0)
            {
                switchKeys = new KeyCode[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    switchKeys[i] = (KeyCode)((int)KeyCode.Alpha1 + i);
                }
            }

            SetupButtons();
            InitializeWeaponState();
            UpdateWeaponListText();
        }

        void SetupButtons()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    int index = i;
                    buttons[i].onClick.RemoveAllListeners();
                    buttons[i].onClick.AddListener(() => OnButtonClick(index));
                }
            }
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            for (int i = 0; i < switchKeys.Length && i < objects.Length; i++)
            {
                if (Input.GetKeyDown(switchKeys[i]))
                {
                    Debug.Log($"Key pressed: {switchKeys[i]} for weapon {i}");
                    CmdSwitchToWeapon(i);
                    break;
                }
            }

            if (enableNextPrevKeys)
            {
                if (Input.GetKeyDown(nextWeaponKey))
                {
                    Debug.Log("Next weapon key pressed");
                    CmdSwitchToNextWeapon();
                }

                if (Input.GetKeyDown(prevWeaponKey))
                {
                    Debug.Log("Previous weapon key pressed");
                    CmdSwitchToPrevWeapon();
                }
            }

            if (enableMouseWheelSwitch)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll > 0f)
                {
                    Debug.Log("Mouse wheel up Ч next weapon");
                    CmdSwitchToNextWeapon();
                }
                else if (scroll < 0f)
                {
                    Debug.Log("Mouse wheel down Ч previous weapon");
                    CmdSwitchToPrevWeapon();
                }
            }
        }

        void OnButtonClick(int index)
        {
            if (!isLocalPlayer) return;

            Debug.Log($"Button clicked: {index} by local player");
            CmdSwitchToWeapon(index);
        }

        [Command(requiresAuthority = false)]
        void CmdSwitchToWeapon(int index)
        {
            if (index < 0 || index >= objects.Length) return;
            if (currentActiveIndex == index) return;

            Debug.Log($"Command received: Switching weapon to index: {index}");
            currentActiveIndex = index;
        }

        [Command(requiresAuthority = false)]
        void CmdSwitchToNextWeapon()
        {
            if (objects.Length == 0) return;

            int nextIndex = (currentActiveIndex + 1) % objects.Length;
            Debug.Log($"Command received: Switching to next weapon: {nextIndex}");
            CmdSwitchToWeapon(nextIndex);
        }

        [Command(requiresAuthority = false)]
        void CmdSwitchToPrevWeapon()
        {
            if (objects.Length == 0) return;

            int prevIndex = (currentActiveIndex - 1 + objects.Length) % objects.Length;
            Debug.Log($"Command received: Switching to previous weapon: {prevIndex}");
            CmdSwitchToWeapon(prevIndex);
        }

        void OnActiveWeaponChanged(int oldIndex, int newIndex)
        {
            Debug.Log($"Weapon changed from {oldIndex} to {newIndex}");

            if (oldIndex >= 0 && oldIndex < objects.Length)
                objects[oldIndex].SetActive(false);

            if (newIndex >= 0 && newIndex < objects.Length)
                objects[newIndex].SetActive(true);

            UpdateButtonsVisualState();
            UpdateWeaponListText();
        }

        void InitializeWeaponState()
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(false);
            }

            if (objects.Length > 0 && objects[0] != null)
                objects[0].SetActive(true);

            UpdateButtonsVisualState();
        }

        void UpdateButtonsVisualState()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    Image buttonImage = buttons[i].GetComponent<Image>();
                    if (buttonImage != null)
                        buttonImage.color = (i == currentActiveIndex) ? Color.yellow : Color.white;
                }
            }
        }

        void UpdateWeaponListText()
        {
            if (weaponListText == null) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null) continue;

                string weaponName = objects[i].name;

                if (i == currentActiveIndex)
                {
                    // јктивное оружие Ч жЄлтым цветом и стрелка
                    sb.AppendLine($"<color=#FFD700>> {i + 1}: {weaponName}</color>");
                }
                else
                {
                    // ќстальные Ч обычный белый
                    sb.AppendLine($"  {i + 1}: {weaponName}");
                }
            }

            weaponListText.text = sb.ToString();
        }

        [ClientRpc]
        void RpcPlayWeaponSwitchEffect(int weaponIndex)
        {
            Debug.Log($"Weapon switched to index: {weaponIndex}");
        }

        public int GetCurrentWeaponIndex() => currentActiveIndex;

        public GameObject GetCurrentWeapon()
        {
            if (currentActiveIndex >= 0 && currentActiveIndex < objects.Length)
                return objects[currentActiveIndex];
            return null;
        }

        public void SetWeapon(int index)
        {
            if (isLocalPlayer)
                CmdSwitchToWeapon(index);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log("Local player started - weapon system ready");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("Client started - weapon system initialized");
        }
    }
}