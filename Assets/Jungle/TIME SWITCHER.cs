using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TIMESWITCHER : MonoBehaviour
{
    [SerializeField] private string sceneName; // Название сцены, задаваемое в инспекторе
    private static Vector3 lastPosition;

    void Start()
    {
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("LastScene")) && PlayerPrefs.GetString("LastScene") == SceneManager.GetActiveScene().name)
        {
            transform.position = new Vector3(PlayerPrefs.GetFloat("LastPosX"), PlayerPrefs.GetFloat("LastPosY"), PlayerPrefs.GetFloat("LastPosZ"));
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SavePosition();
            LoadScene();
        }
    }

    private void SavePosition()
    {
        lastPosition = transform.position;
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetFloat("LastPosX", lastPosition.x);
        PlayerPrefs.SetFloat("LastPosY", lastPosition.y);
        PlayerPrefs.SetFloat("LastPosZ", lastPosition.z);
        PlayerPrefs.Save();
    }

    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not set in the inspector!");
        }
    }
}
