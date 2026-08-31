using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CUTSCENESNOWW : MonoBehaviour
{
    private void OnTriggerEnter(Collider myCollider)
    {

        if (myCollider.tag == ("Player"))
        {
            SceneManager.LoadScene("6 scene snoww");
        }
    }

}
