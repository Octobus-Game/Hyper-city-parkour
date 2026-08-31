using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBJ : MonoBehaviour
{
    public GameObject gizmoPrefab; // Assign a prefab with three axes
    private GameObject activeGizmo;

    void OnMouseDown() // Detects when the object is clicked
    {
        if (activeGizmo == null)
        {
            activeGizmo = Instantiate(gizmoPrefab, transform.position, Quaternion.identity);
            activeGizmo.transform.parent = transform; // Attach gizmo to the object
        }
        else
        {
            Destroy(activeGizmo);
        }
    }
}
