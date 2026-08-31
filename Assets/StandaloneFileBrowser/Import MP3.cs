using SFB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB; // Пространство имён библиотеки
using System.IO;

public class ImportMP3 : MonoBehaviour
{
    public Button openButton;
    public Text filePathText; // UI текст для вывода пути
    public AudioSource audioSource;

    void Start()
    {
        openButton.onClick.AddListener(OpenFileDialog);
    }

    void OpenFileDialog()
    {
        var extensions = new[] {
            new ExtensionFilter("Audio Files", "mp3"),
        };

        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open MP3 File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            filePathText.text = paths[0];

            // Загружаем и проигрываем mp3 (если нужно)
            StartCoroutine(LoadAndPlay(paths[0]));
        }
    }

    System.Collections.IEnumerator LoadAndPlay(string path)
    {
        var www = new WWW("file://" + path);
        yield return www;

        AudioClip clip = www.GetAudioClip(false, false, AudioType.MPEG);
        audioSource.clip = clip;
        audioSource.Play();
    }
}
