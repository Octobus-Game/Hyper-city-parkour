using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SAVEEEE : MonoBehaviour
{
    // Static variable to track if the button should be active
    public static bool isButtonEnabled = false;

    // Reference to the button
    public Button targetButton;

    void Start()
    {
        // Ensure the button's interactable state reflects the static variable
        if (targetButton != null)
        {
            targetButton.interactable = isButtonEnabled;
        }
    }

    void Update()
    {
        // Continuously check and update button state (in case scenes change dynamically)
        if (targetButton != null)
        {
            targetButton.interactable = isButtonEnabled;
        }
    }
}
