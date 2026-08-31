using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemydrone2D_ : MonoBehaviour
{
	   [Header("Настройки стрельбы")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 5f;
    public float fireRate = 2f;
    public int bulletCount = 8;

    private float nextFireTime;

    void Update()
    {
        if (Time.time >= nextFireTime)
        {
            ShootInAllDirections();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }

    void ShootInAllDirections()
    {
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.velocity = direction * bulletSpeed;
            }
        }
    }
}
