using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit1 : MonoBehaviour
{
   public void Quitbutton()
    {
        Application.Quit();
    }

    public void Menubutton()
    {
        SceneManager.LoadScene("Menu");
    }

}
