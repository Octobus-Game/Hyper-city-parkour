using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cameracollision : MonoBehaviour
{
    public Transform target; // Цель камеры (например, персонаж)
    public float minDistance = 1.0f; // Минимальное расстояние до цели
    public float maxDistance = 5.0f; // Максимальное расстояние до цели
    public float smoothSpeed = 10.0f; // Скорость сглаживания
    public LayerMask collisionMask; // Слои, с которыми будет проверяться столкновение

    private Vector3 offset;

    void Start()
    {
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset.normalized * maxDistance;
        RaycastHit hit;

        if (Physics.Raycast(target.position, offset.normalized, out hit, maxDistance, collisionMask))
        {
            float distance = Mathf.Clamp(hit.distance, minDistance, maxDistance);
            desiredPosition = target.position + offset.normalized * distance;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}
