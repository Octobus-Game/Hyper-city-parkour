using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class zahita : MonoBehaviour
{
    public GameObject activatableObject;  // Объект, который активируется по нажатию F
    public GameObject targetObject;       // Объект, который отключается при касании

    private bool isActivated = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            activatableObject.SetActive(true);
            isActivated = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActivated) return;

        if (other.gameObject == targetObject)
        {
            StartCoroutine(DisableTemporarily(targetObject, 10f));
        }
    }

    IEnumerator DisableTemporarily(GameObject obj, float delay)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);
    }
}
