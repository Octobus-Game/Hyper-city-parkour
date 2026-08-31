using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI; // For Button

public class ModelLoader : MonoBehaviour
{
    public TMP_InputField inputField;   // Drag your TMP InputField here
    public Button loadButton;           // Drag your Button here
    public Transform spawnPoint;        // Where the model will appear
    private GameObject currentModel;    // Reference to the currently spawned model

    private void Start()
    {
        // Attach listener to button
        loadButton.onClick.AddListener(OnLoadButtonClicked);
    }

    private void OnLoadButtonClicked()
    {
        string modelName = inputField.text.Trim();

        if (string.IsNullOrEmpty(modelName))
        {
            Debug.LogWarning("InputField is empty!");
            return;
        }

        // Load model from Resources/model folder
        GameObject modelPrefab = Resources.Load<GameObject>("Assets" + modelName);

        if (modelPrefab == null)
        {
            Debug.LogError("Model not found: " + modelName);
            return;
        }

        // Destroy existing model if any
        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        // Instantiate the new model
        currentModel = Instantiate(modelPrefab, spawnPoint.position, Quaternion.identity);
    }
}

