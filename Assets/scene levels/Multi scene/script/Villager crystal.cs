using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Villagercrystal : MonoBehaviour
{
    public GameObject objectToSpawn; // Объект, который будет появляться
    public Transform spawnPoint;    // Точка появления объекта

    public GameObject bulletPrefab; // Префаб пули
    public Transform firePoint; // Точка стрельбы
    public float bulletSpeed = 20f; // Скорость пули
    public float fireRate = 0.5f; // Задержка между выстрелами
    public float cooldownTime = 5f; // Время блокировки после выстрела
    public ParticleSystem muzzleFlash; // Частицы выстрела
    public AudioClip shootSound; // Звук выстрела
    public AudioSource audioSource; // Источник звука
    public Transform target; // Цель для самонаводки пуль

    private float nextFireTime = 0f; // Время для следующего выстрела
    private bool isCooldown = false; // Флаг блокировки оружия

    private bool canSpawn = true;   // Флаг, разрешающий создание объекта
    private bool timerRunning = false; // Флаг, сигнализирующий о работе таймера

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canSpawn && !timerRunning)
        {
            StartCoroutine(SpawnObjectWithCooldown());
        }

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && !isCooldown)
        {
            Shoot();
            StartCooldown();
        }
    }

    private IEnumerator SpawnObjectWithCooldown()
    {
        // Создаём объект
        Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation);

        // Запускаем таймер на 5 секунд
        timerRunning = true;
        yield return new WaitForSeconds(5f);

        // После таймера блокируем спавн на 10 секунд
        canSpawn = false;
        timerRunning = false;
        yield return new WaitForSeconds(10f);

        // Разрешаем спавн снова
        canSpawn = true;
    }

    void Shoot()
    {
        // Создание пули
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (target != null)
            {
                Vector3 direction = (target.position - firePoint.position).normalized;
                rb.velocity = direction * bulletSpeed; // Задаем направление к цели
            }
            else
            {
                rb.velocity = firePoint.forward * bulletSpeed; // Задаем скорость пули
            }
        }

        // Активация эффекта частиц
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Воспроизведение звука выстрела
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        nextFireTime = Time.time + fireRate; // Установка времени для следующего выстрела
    }

    void StartCooldown()
    {
        isCooldown = true; // Активируем блокировку оружия
        Invoke(nameof(EndCooldown), cooldownTime); // Запускаем таймер блокировки
    }

    void EndCooldown()
    {
        isCooldown = false; // Снимаем блокировку оружия
    }
}
