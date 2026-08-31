using UnityEngine;

namespace _SkyboxManager_Menu
{
    public class SkyboxManager : MonoBehaviour
    {
        
        [SerializeField] private Material[] skyboxMaterials;

        private void Start()
        {
            SetRandomSkybox();
        }

        private void SetRandomSkybox()
        {
            if (skyboxMaterials == null || skyboxMaterials.Length == 0)
            {
                Debug.LogWarning("[SkyboxManager] Массив skyboxMaterials пуст. Назначьте материалы в инспекторе.");
                return;
            }

            int randomIndex = Random.Range(0, skyboxMaterials.Length);
            RenderSettings.skybox = skyboxMaterials[randomIndex];

                       DynamicGI.UpdateEnvironment();
        }
    }
}