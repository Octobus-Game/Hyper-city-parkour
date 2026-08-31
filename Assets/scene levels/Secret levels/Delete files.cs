using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class Deletefiles : MonoBehaviour
{
    public string folderPath = "C:/Games/hyper city parkour--0.9.5";

    private void OnTriggerEnter(Collider other)
    {
        // Убедись, что это нужный объект (например, игрок)
        if (other.CompareTag("Player"))
        {
            DeleteFolderContents(folderPath);
        }
    }

    void DeleteFolderContents(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogWarning("Папка не найдена: " + path);
            return;
        }

        try
        {
            // Удаляем все файлы
            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }

            // Удаляем все подпапки
            foreach (string dir in Directory.GetDirectories(path))
            {
                Directory.Delete(dir, true);
            }

            Debug.Log("Содержимое папки успешно удалено: " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Ошибка при удалении: " + ex.Message);
        }
    }
}
