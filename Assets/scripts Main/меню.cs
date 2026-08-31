using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class level: MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("1 уровень");
    }

    public void Exit()
    {
        Application.Quit();
    }





}

