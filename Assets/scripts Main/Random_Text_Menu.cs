using UnityEngine;
using TMPro; 

public class RandomTextSelector : MonoBehaviour
{
    [SerializeField] private string[] TEXT; 
    [SerializeField] private TextMeshProUGUI textComponent; 

    void Start()
    {
        ShowRandomText();
    }

    void ShowRandomText()
    {
        if (TEXT == null || TEXT.Length == 0)
        {
            Debug.LogWarning("Массив TEXT пуст!");
            return;
        }

        int randomIndex = Random.Range(0, TEXT.Length);
        string chosenText = TEXT[randomIndex];

        if (textComponent != null)
        {
            textComponent.text = chosenText;
        }
        else
        {
            Debug.Log(chosenText);
        }
    }
}