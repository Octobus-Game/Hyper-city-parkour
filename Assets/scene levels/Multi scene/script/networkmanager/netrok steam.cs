using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;

namespace Mirror_steam
{
    public class SteamManager : MonoBehaviour
    {
        private static SteamManager s_instance;
        public static SteamManager Instance => s_instance;

        private void Awake()
        {
            if (s_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);

            if (!SteamAPI.Init())
            {
                Debug.LogError("SteamAPI_Init() failed");
            }
        }


        [System.Serializable]
        public class PlayerProfileData
        {
            public string steamName;
            public ulong steamId;
            public byte[] avatarData;
            public Texture2D avatarTexture;
        }












    }














}


