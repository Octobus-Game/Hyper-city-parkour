
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleOrbitTrap : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float orbitRadius = 2f;
    public float orbitSpeed = 90f; // градусы в секунду
    public float orbitDuration = 10f;

    [Header("Release Settings")]
    public float releaseForce = 15f;
    public bool randomReleaseDirection = true;

    [Header("Visual Effects")]
    public ParticleSystem orbitEffect;
    public Color orbitColor = Color.cyan;

    [Header("Audio Effects")]
    public AudioClip captureSound;
    public AudioClip releaseSound;
    public AudioSource audioSource;

    private List<GameObject> orbitingBullets = new List<GameObject>();
    private List<float> bulletAngles = new List<float>();
    private List<Coroutine> releaseCoroutines = new List<Coroutine>();
    private List<Material> originalMaterials = new List<Material>();
    private List<Color> originalColors = new List<Color>();
    private List<Vector3> originalScales = new List<Vector3>();

    void Start()
    {
        // Получаем AudioSource если не назначен
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Создаем AudioSource если его нет
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        UpdateOrbitingBullets();
    }

    void UpdateOrbitingBullets()
    {
        for (int i = orbitingBullets.Count - 1; i >= 0; i--)
        {
            if (orbitingBullets[i] == null)
            {
                // Удаляем уничтоженные пули из всех списков
                RemoveBulletFromLists(i);
                continue;
            }

            // Обновляем угол орбиты
            bulletAngles[i] += orbitSpeed * Time.deltaTime;

            // Вычисляем новую позицию
            float angleInRadians = bulletAngles[i] * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angleInRadians) * orbitRadius,
                0f,
                Mathf.Sin(angleInRadians) * orbitRadius
            );

            orbitingBullets[i].transform.position = transform.position + offset;

            // Поворачиваем пулю лицом к центру орбиты
            orbitingBullets[i].transform.LookAt(transform.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем, является ли объект пулей
        if (other.CompareTag("Bullet") || other.name.Contains("Bullet"))
        {
            CaptureBullet(other.gameObject);
        }
    }

    void CaptureBullet(GameObject bullet)
    {
        // Проверяем, не захвачена ли уже эта пуля
        if (orbitingBullets.Contains(bullet))
            return;

        // Останавливаем физику пули
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.isKinematic = true;
            bulletRb.velocity = Vector3.zero;
            bulletRb.angularVelocity = Vector3.zero;
        }

        // Отключаем коллайдер пули
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }

        // Сохраняем оригинальные параметры пули
        SaveOriginalBulletProperties(bullet);

        // Применяем визуальные изменения
        ApplyOrbitVisuals(bullet);

        // Добавляем пулю в орбиту
        orbitingBullets.Add(bullet);

        // Задаем начальный угол
        float startAngle = orbitingBullets.Count == 1 ? 0f :
            (360f / orbitingBullets.Count) * (orbitingBullets.Count - 1);
        bulletAngles.Add(startAngle);

        // Запускаем корутину для освобождения пули
        Coroutine releaseCoroutine = StartCoroutine(ReleaseBulletAfterDelay(bullet, orbitDuration));
        releaseCoroutines.Add(releaseCoroutine);

        // Воспроизводим звук захвата
        if (audioSource != null && captureSound != null)
        {
            audioSource.PlayOneShot(captureSound);
        }

        // Запускаем эффект орбиты
        if (orbitEffect != null && !orbitEffect.isPlaying)
        {
            orbitEffect.Play();
        }

        Debug.Log($"Пуля {bullet.name} захвачена в орбиту!");
    }

    void SaveOriginalBulletProperties(GameObject bullet)
    {
        // Сохраняем оригинальный размер
        originalScales.Add(bullet.transform.localScale);

        // Сохраняем оригинальный материал и цвет
        Renderer bulletRenderer = bullet.GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            originalMaterials.Add(bulletRenderer.material);
            originalColors.Add(bulletRenderer.material.color);
        }
        else
        {
            originalMaterials.Add(null);
            originalColors.Add(Color.white);
        }
    }

    void ApplyOrbitVisuals(GameObject bullet)
    {
        // Изменяем размер пули
        bullet.transform.localScale *= 1.2f;

        // Изменяем цвет пули
        Renderer bulletRenderer = bullet.GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            Material bulletMat = bulletRenderer.material;
            bulletMat.color = orbitColor;

            // Добавляем свечение если материал поддерживает
            if (bulletMat.HasProperty("_EmissionColor"))
            {
                bulletMat.SetColor("_EmissionColor", orbitColor * 1.5f);
                bulletMat.EnableKeyword("_EMISSION");
            }
        }
    }

    IEnumerator ReleaseBulletAfterDelay(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bullet != null && orbitingBullets.Contains(bullet))
        {
            ReleaseBullet(bullet);
        }
    }

    void ReleaseBullet(GameObject bullet)
    {
        int index = orbitingBullets.IndexOf(bullet);
        if (index == -1) return;

        // Определяем направление выпуска
        Vector3 releaseDirection;
        if (randomReleaseDirection)
        {
            releaseDirection = Random.onUnitSphere;
            releaseDirection.y = Mathf.Abs(releaseDirection.y);
        }
        else
        {
            releaseDirection = (bullet.transform.position - transform.position).normalized;
        }

        // Восстанавливаем оригинальные свойства пули
        RestoreOriginalBulletProperties(bullet, index);

        // Восстанавливаем физику пули
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.isKinematic = false;
            bulletRb.velocity = releaseDirection * releaseForce;
            bulletRb.angularVelocity = Random.insideUnitSphere * 2f;
        }

        // Включаем коллайдер
        Collider bulletCollider = bullet.GetComponent<Collider>();
        if (bulletCollider != null)
        {
            bulletCollider.enabled = true;
        }

        // Удаляем пулю из всех списков
        RemoveBulletFromLists(index);

        // Останавливаем эффект орбиты если нет пуль
        if (orbitingBullets.Count == 0 && orbitEffect != null)
        {
            orbitEffect.Stop();
        }

        // Воспроизводим звук освобождения
        if (audioSource != null && releaseSound != null)
        {
            audioSource.PlayOneShot(releaseSound);
        }

        Debug.Log($"Пуля {bullet.name} освобождена из орбиты!");
    }

    void RestoreOriginalBulletProperties(GameObject bullet, int index)
    {
        // Восстанавливаем размер
        if (index < originalScales.Count)
        {
            bullet.transform.localScale = originalScales[index];
        }

        // Восстанавливаем материал и цвет
        if (index < originalMaterials.Count && index < originalColors.Count)
        {
            Renderer bulletRenderer = bullet.GetComponent<Renderer>();
            if (bulletRenderer != null && originalMaterials[index] != null)
            {
                bulletRenderer.material = originalMaterials[index];
                bulletRenderer.material.color = originalColors[index];

                // Отключаем свечение
                if (bulletRenderer.material.HasProperty("_EmissionColor"))
                {
                    bulletRenderer.material.SetColor("_EmissionColor", Color.black);
                    bulletRenderer.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    void RemoveBulletFromLists(int index)
    {
        orbitingBullets.RemoveAt(index);
        bulletAngles.RemoveAt(index);
        releaseCoroutines.RemoveAt(index);

        if (index < originalMaterials.Count)
            originalMaterials.RemoveAt(index);
        if (index < originalColors.Count)
            originalColors.RemoveAt(index);
        if (index < originalScales.Count)
            originalScales.RemoveAt(index);
    }

    // Публичные методы
    public void ReleaseAllBullets()
    {
        for (int i = orbitingBullets.Count - 1; i >= 0; i--)
        {
            if (orbitingBullets[i] != null)
            {
                ReleaseBullet(orbitingBullets[i]);
            }
        }
    }

    public void SetOrbitRadius(float radius)
    {
        orbitRadius = Mathf.Max(0.5f, radius);
    }

    public void SetOrbitSpeed(float speed)
    {
        orbitSpeed = speed;
    }

    public void SetOrbitDuration(float duration)
    {
        orbitDuration = Mathf.Max(1f, duration);
    }

    // Геттеры
    public int OrbitingBulletsCount => orbitingBullets.Count;
    public bool HasOrbitingBullets => orbitingBullets.Count > 0;

    void OnDisable()
    {
        StopAllCoroutines();
        ReleaseAllBullets();
    }

    void OnDrawGizmosSelected()
    {
        // Рисуем радиус орбиты
        Gizmos.color = Color.yellow;

        float segments = 32f;
        float deltaAngle = 360f / segments;
        Vector3 previousPoint = transform.position + Vector3.forward * orbitRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * deltaAngle * Mathf.Deg2Rad;
            Vector3 currentPoint = transform.position + new Vector3(
                Mathf.Sin(angle) * orbitRadius,
                0f,
                Mathf.Cos(angle) * orbitRadius
            );

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }

        // Рисуем орбитирующие пули
        if (Application.isPlaying && orbitingBullets.Count > 0)
        {
            Gizmos.color = Color.red;
            foreach (GameObject bullet in orbitingBullets)
            {
                if (bullet != null)
                {
                    Gizmos.DrawSphere(bullet.transform.position, 0.1f);
                }
            }
        }
    }
}