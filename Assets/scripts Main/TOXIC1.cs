using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TOXIC1 : MonoBehaviour
{
    private void OnTriggerEnter(Collider myCollider)
    {

        if (myCollider.tag == ("Player"))
        {
            SceneManager.LoadScene("TOXIC");
        }
    }
}
