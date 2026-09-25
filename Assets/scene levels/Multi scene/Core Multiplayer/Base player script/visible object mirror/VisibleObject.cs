using Mirror;
using UnityEngine;

public class HideLocalPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject visualObject;

    [Header("Layer settings")]
    [SerializeField] private string localObjectLayerName = "Local Object";

    [Tooltip("Если включено — объект переводится на слой Local Object. Если выключено — объект просто отключается (как раньше).")]
    [SerializeField] private bool useLayer = true;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (visualObject == null)
            return;

        if (useLayer)
        {
            int layer = LayerMask.NameToLayer(localObjectLayerName);

            if (layer == -1)
            {
                Debug.LogError($"Слой \"{localObjectLayerName}\" не найден! Создай его в Project Settings → Tags and Layers.", this);
                return;
            }

            SetLayerRecursively(visualObject, layer);
        }
        else
        {
            visualObject.SetActive(false);
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}