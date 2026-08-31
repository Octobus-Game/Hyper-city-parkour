using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class scenetrigger : MonoBehaviour
{
    [Tooltip("Name of the scene to load when the player touches this object.")]
    public string sceneName;

    [Tooltip("Tag to identify the player object (default: Player).")]
    public string playerTag = "Player";

    // ── 3D ──────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }

    // ── 2D ──────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            LoadScene();
        }
    }

    // ── Общий метод загрузки ─────────────────────────────
    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name not set in SceneTrigger!");
        }
    }
}