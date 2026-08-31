using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ShaderOFF : MonoBehaviour
{
    public Toggle toggle;

    void Start()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(delegate { TogglePostProcessing(toggle.isOn); });
            TogglePostProcessing(toggle.isOn);
        }
    }

    void TogglePostProcessing(bool isOn)
    {
        if (!isOn)
        {
            DisablePostProcessingInAllScenes();
        }
    }

    void DisablePostProcessingInAllScenes()
    {
        List<GameObject> rootObjects = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                rootObjects.AddRange(scene.GetRootGameObjects());
            }
        }

        foreach (var obj in rootObjects)
        {
            DisablePostProcessingInObject(obj);
        }
    }

    void DisablePostProcessingInObject(GameObject obj)
    {
        var volumes = obj.GetComponentsInChildren<PostProcessVolume>(true);
        foreach (var volume in volumes)
        {
            volume.enabled = false;
        }

        var layers = obj.GetComponentsInChildren<PostProcessLayer>(true);
        foreach (var layer in layers)
        {
            layer.enabled = false;
        }
    }
}
