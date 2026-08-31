using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class positioncamera : MonoBehaviour
{
    public Text positionText; // Позиция
    public Transform targetObject; // Объект

    void Update()
    {
        if (targetObject == null)
            targetObject = this.transform;

        Vector3 pos = targetObject.position;
        positionText.text = $"Position: X={pos.x:F2}, Y={pos.y:F2}, Z={pos.z:F2}";
    }
}
