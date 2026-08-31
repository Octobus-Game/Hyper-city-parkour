using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class STRELBA1 : MonoBehaviour
{
    public GameObject bulletPrefab; // Префаб пули
    public Transform firePoint; // Точка стрельбы
    public float bulletSpeed = 20f; // Скорость пули
    public float fireRate = 0.5f; // Задержка между выстрелами
    private float nextFireTime = 0f;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime) // "Fire1" — левая кнопка мыши
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation); // Создание пули
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = firePoint.forward * bulletSpeed; // Задаем скорость пули
        }
    }
}
