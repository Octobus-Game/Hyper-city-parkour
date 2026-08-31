namespace Ready_manager
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Mirror;

    

    namespace ReadyManager
    {
        public class PlayerReadyTrigger : MonoBehaviour
        {
            [Header("Trigger Settings")]
            [SerializeField] private bool oneTimeUse = true;
            [SerializeField] private bool cancelOnExit = true;
            [SerializeField] private LayerMask playerLayerMask = -1;

            [Header("UI Settings")]
            [SerializeField] private TextMesh readyCountText;
            [SerializeField] private TextMesh readyCountTextTMP; // Added TextMeshPro support
            [SerializeField] private string textFormat = "{0} из {1}";
            [SerializeField] private bool autoFindText = true;

            private bool hasTriggered = false;
            private NetworkRoomManager roomManager;

            void Start()
            {
                // Get reference to Network Room Manager
                roomManager = NetworkManager.singleton as NetworkRoomManager;

                if (roomManager == null)
                {
                    Debug.LogError("NetworkRoomManager not found! Make sure you're using NetworkRoomManager.");
                }

                // Automatically find text component
                if (autoFindText)
                {
                    if (readyCountText == null)
                    {
                        readyCountText = GetComponentInChildren<TextMesh>();
                        if (readyCountText == null)
                        {
                            readyCountText = GetComponentInParent<TextMesh>();
                        }
                    }

                    // Also try to find TextMeshPro component
                    if (readyCountTextTMP == null)
                    {
                        readyCountTextTMP = GetComponentInChildren<TextMesh>();
                        if (readyCountTextTMP == null)
                        {
                            readyCountTextTMP = GetComponentInParent<TextMesh>();
                        }
                    }
                }

                // Ensure object has Collider with IsTrigger enabled
                Collider col = GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogWarning("Collider not found on object " + gameObject.name + ". Adding BoxCollider.");
                    col = gameObject.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;

                // Update text on start
                UpdateReadyCountText();
            }

            void OnTriggerEnter(Collider other)
            {
                // Check if this is a player and trigger hasn't been activated yet
                if (oneTimeUse && hasTriggered) return;

                // Check object layer
                if (!IsInLayerMask(other.gameObject.layer, playerLayerMask)) return;

                // Check if object has NetworkIdentity (network player)
                NetworkIdentity networkIdentity = other.GetComponent<NetworkIdentity>();
                if (networkIdentity == null) return;

                // Check if this is local player
                if (!networkIdentity.isLocalPlayer) return;

                Debug.Log($"Player {other.name} activated ready trigger!");

                // Set player ready
                SetPlayerReady(networkIdentity, true);

                if (oneTimeUse)
                    hasTriggered = true;

                // Update text after changing ready state
                Invoke(nameof(UpdateReadyCountText), 0.1f); // Small delay for network state update
            }

            void OnTriggerExit(Collider other)
            {
                // If cancel on exit is disabled, do nothing
                if (!cancelOnExit) return;

                // If this is one-time use and already triggered, don't cancel
                if (oneTimeUse && hasTriggered) return;

                // Check object layer
                if (!IsInLayerMask(other.gameObject.layer, playerLayerMask)) return;

                // Check if object has NetworkIdentity (network player)
                NetworkIdentity networkIdentity = other.GetComponent<NetworkIdentity>();
                if (networkIdentity == null) return;

                // Check if this is local player
                if (!networkIdentity.isLocalPlayer) return;

                Debug.Log($"Player {other.name} exited trigger - canceling ready state!");

                // Cancel player ready state
                SetPlayerReady(networkIdentity, false);

                // Update text after state change
                Invoke(nameof(UpdateReadyCountText), 0.1f);
            }

            private void SetPlayerReady(NetworkIdentity playerIdentity, bool readyState)
            {
                if (roomManager == null) return;

                // Get NetworkRoomPlayer component
                NetworkRoomPlayer roomPlayer = playerIdentity.GetComponent<NetworkRoomPlayer>();

                if (roomPlayer != null)
                {
                    // Call ready command on server
                    roomPlayer.CmdChangeReadyState(readyState);
                    Debug.Log($"Player {(readyState ? "ready" : "cancelled ready")}!");
                }
                else
                {
                    Debug.LogWarning("NetworkRoomPlayer component not found on player!");
                }
            }

            // Method to update ready player counter text
            private void UpdateReadyCountText()
            {
                if (roomManager == null) return;

                int readyPlayers = GetReadyPlayersCount();
                int totalPlayers = GetTotalPlayersCount();

                string formattedText = string.Format(textFormat, readyPlayers, totalPlayers);

                // Update TextMesh if available
                if (readyCountText != null)
                {
                    readyCountText.text = formattedText;
                }

                // Update TextMeshPro if available
                if (readyCountTextTMP != null)
                {
                    readyCountTextTMP.text = formattedText;
                }
            }

            // Count ready players
            private int GetReadyPlayersCount()
            {
                if (roomManager == null) return 0;

                int count = 0;
                foreach (NetworkRoomPlayer roomPlayer in roomManager.roomSlots)
                {
                    if (roomPlayer != null && roomPlayer.readyToBegin)
                    {
                        count++;
                    }
                }
                return count;
            }

            // Count total players
            private int GetTotalPlayersCount()
            {
                if (roomManager == null) return 0;

                int count = 0;
                foreach (NetworkRoomPlayer roomPlayer in roomManager.roomSlots)
                {
                    if (roomPlayer != null)
                    {
                        count++;
                    }
                }
                return count;
            }

            // Alternative method for direct Ready call through Room Manager
            private void SetPlayerReadyAlternative(bool readyState)
            {
                if (roomManager == null) return;

                // Find local player in room
                foreach (NetworkRoomPlayer roomPlayer in roomManager.roomSlots)
                {
                    if (roomPlayer != null && roomPlayer.isLocalPlayer)
                    {
                        roomPlayer.CmdChangeReadyState(readyState);
                        break;
                    }
                }

                // Update text after state change
                Invoke(nameof(UpdateReadyCountText), 0.1f);
            }

            // Method to update text in real time (called periodically)
            void Update()
            {
                // Update text every 0.5 seconds to track ready state changes
                if (Time.time % 0.5f < Time.deltaTime)
                {
                    UpdateReadyCountText();
                }
            }

            // Helper method to check layer
            private bool IsInLayerMask(int layer, LayerMask mask)
            {
                return (mask.value & (1 << layer)) != 0;
            }

            // Method to reset trigger (if needed)
            public void ResetTrigger()
            {
                hasTriggered = false;
                UpdateReadyCountText();
            }

            // Public methods for external ready state control
            public void SetLocalPlayerReady()
            {
                SetPlayerReadyAlternative(true);
            }

            public void SetLocalPlayerNotReady()
            {
                SetPlayerReadyAlternative(false);
            }

            // Public method to force text update
            public void ForceUpdateText()
            {
                UpdateReadyCountText();
            }

            // Trigger visualization in editor
            void OnDrawGizmosSelected()
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    Gizmos.color = hasTriggered ? Color.green : Color.yellow;
                    Gizmos.matrix = transform.localToWorldMatrix;

                    if (col is BoxCollider box)
                    {
                        Gizmos.DrawWireCube(box.center, box.size);
                    }
                    else if (col is SphereCollider sphere)
                    {
                        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                    }
                }
            }
        }
    }
}

