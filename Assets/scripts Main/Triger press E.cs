using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables; // Для работы с Timeline
using UnityEngine.UI;

public class TrigerpressE : MonoBehaviour
{
    public GameObject panel; // UI панель, которая появляется
    public GameObject objectToActivate; // Объект, который активируется
    public GameObject objectToDeactivate; // Объект, который исчезает
    public PlayableDirector timeline; // Timeline, который активируется

    private bool isPlayerInTrigger = false;

    void Start()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            if (panel != null)
                panel.SetActive(false); // Скрыть панель

            if (objectToActivate != null)
                objectToActivate.SetActive(true);

            if (objectToDeactivate != null)
                objectToDeactivate.SetActive(false);

            if (timeline != null)
                timeline.Play();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
            if (panel != null)
                panel.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
