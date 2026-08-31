using UnityEngine;
using System.Collections.Generic;


public class DeformableObject : MonoBehaviour
{
    [Header("Деформация")]
    public float deformationStrength = 1f;
    public float minForceThreshold = 5f; 
    public float maxDeformationDistance = 2f;
    public AnimationCurve deformationFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Восстановление")]
    public bool canRecover = true;
    public float recoverySpeed = 1f;
    public float recoveryDelay = 2f;

    [Header("Отладка")]
    public bool showGizmos = true;

    private Mesh originalMesh;
    private Mesh deformedMesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private MeshFilter meshFilter;
    private Rigidbody rb;
    private bool isRecovering = false;
    private float recoveryTimer = 0f;

    private List<DeformationPoint> activeDeformations = new List<DeformationPoint>();

    [System.Serializable]
    public class DeformationPoint
    {
        public Vector3 position;
        public float intensity;
        public float radius;
        public float timeCreated;

        public DeformationPoint(Vector3 pos, float inten, float rad)
        {
            position = pos;
            intensity = inten;
            radius = rad;
            timeCreated = Time.time;
        }
    }

    void Start()
    {
        InitializeDeformation();
    }

    void InitializeDeformation()
    {
        meshFilter = GetComponent<MeshFilter>();
        rb = GetComponent<Rigidbody>();

        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter не найден на " + gameObject.name);
            return;
        }

        // Сохраняем оригинальный меш
        originalMesh = meshFilter.mesh;
        originalVertices = originalMesh.vertices;

        // Создаем копию для деформации
        deformedMesh = Instantiate(originalMesh);
        deformedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        meshFilter.mesh = deformedMesh;
    }

    void Update()
    {
        if (canRecover && activeDeformations.Count > 0)
        {
            HandleRecovery();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= minForceThreshold)
        {
            Vector3 collisionPoint = collision.contacts[0].point;
            Vector3 localPoint = transform.InverseTransformPoint(collisionPoint);

            ApplyDeformation(localPoint, impactForce);
        }
    }

    public void ApplyDeformation(Vector3 localPosition, float force)
    {
        float deformationRadius = Mathf.Clamp(force * 0.1f, 0.1f, maxDeformationDistance);
        float deformationIntensity = force * deformationStrength * 0.01f;

        // Добавляем новую точку деформации
        activeDeformations.Add(new DeformationPoint(localPosition, deformationIntensity, deformationRadius));

        // Применяем деформацию
        ApplyDeformationToMesh();

        // Сбрасываем таймер восстановления
        recoveryTimer = 0f;
        isRecovering = false;
    }

    void ApplyDeformationToMesh()
    {
        // Сбрасываем к оригинальным вершинам
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

        // Применяем все активные деформации
        foreach (var deformation in activeDeformations)
        {
            ApplyDeformationPoint(deformation);
        }

        // Обновляем меш
        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();
    }

    void ApplyDeformationPoint(DeformationPoint deformation)
    {
        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 vertex = deformedVertices[i];
            float distance = Vector3.Distance(vertex, deformation.position);

            if (distance <= deformation.radius)
            {
                // Вычисляем силу деформации на основе расстояния
                float normalizedDistance = distance / deformation.radius;
                float falloffMultiplier = deformationFalloff.Evaluate(1f - normalizedDistance);

                // Направление деформации (внутрь объекта)
                Vector3 deformDirection = (vertex - deformation.position).normalized;
                if (deformDirection.magnitude < 0.1f)
                    deformDirection = Vector3.up; // Fallback направление

                // Применяем деформацию
                float deformAmount = deformation.intensity * falloffMultiplier;
                deformedVertices[i] = vertex - deformDirection * deformAmount;
            }
        }
    }

    void HandleRecovery()
    {
        recoveryTimer += Time.deltaTime;

        if (recoveryTimer >= recoveryDelay && !isRecovering)
        {
            isRecovering = true;
        }

        if (isRecovering)
        {
            bool needsUpdate = false;

            // Уменьшаем интенсивность деформаций
            for (int i = activeDeformations.Count - 1; i >= 0; i--)
            {
                activeDeformations[i].intensity -= recoverySpeed * Time.deltaTime;

                if (activeDeformations[i].intensity <= 0)
                {
                    activeDeformations.RemoveAt(i);
                    needsUpdate = true;
                }
                else
                {
                    needsUpdate = true;
                }
            }

            if (needsUpdate)
            {
                ApplyDeformationToMesh();
            }

            if (activeDeformations.Count == 0)
            {
                isRecovering = false;
                recoveryTimer = 0f;
            }
        }
    }

    // Публичный метод для мгновенного восстановления
    public void RestoreOriginalShape()
    {
        activeDeformations.Clear();
        System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);
        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();
        isRecovering = false;
        recoveryTimer = 0f;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || activeDeformations == null) return;

        Gizmos.color = Color.red;
        foreach (var deformation in activeDeformations)
        {
            Vector3 worldPos = transform.TransformPoint(deformation.position);
            Gizmos.DrawWireSphere(worldPos, deformation.radius);
        }
    }

    void OnDestroy()
    {
        if (deformedMesh != null)
        {
            DestroyImmediate(deformedMesh);
        }
    }
}

// Дополнительный компонент для управления столкновениями
public class CollisionForceDetector : MonoBehaviour
{
    [Header("Настройки столкновений")]
    public float forceMultiplier = 1f;
    public bool showCollisionEffects = true;
    public GameObject impactEffectPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        DeformableObject deformable = GetComponent<DeformableObject>();
        if (deformable == null) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            float impactForce = collision.relativeVelocity.magnitude * forceMultiplier;

            // Преобразуем мировые координаты в локальные
            Vector3 localPoint = transform.InverseTransformPoint(contact.point);

            // Применяем деформацию
            deformable.ApplyDeformation(localPoint, impactForce);

            // Показываем эффект столкновения
            if (showCollisionEffects && impactEffectPrefab != null)
            {
                GameObject effect = Instantiate(impactEffectPrefab, contact.point,
                    Quaternion.LookRotation(contact.normal));
                Destroy(effect, 2f);
            }

            Debug.Log($"Столкновение с силой {impactForce:F2} в точке {contact.point}");
        }
    }
}

// Утилитарный класс для настройки деформируемых объектов
public static class DeformationUtility
{
    public static void SetupDeformableObject(GameObject obj, float deformationStrength = 1f,
        float minForce = 5f, bool canRecover = true)
    {
        // Добавляем необходимые компоненты
        if (obj.GetComponent<Rigidbody>() == null)
            obj.AddComponent<Rigidbody>();

        if (obj.GetComponent<MeshCollider>() == null)
        {
            MeshCollider collider = obj.AddComponent<MeshCollider>();
            collider.convex = true; // Нужно для Rigidbody
        }

        // Настраиваем деформацию
        DeformableObject deformable = obj.GetComponent<DeformableObject>();
        if (deformable == null)
            deformable = obj.AddComponent<DeformableObject>();

        deformable.deformationStrength = deformationStrength;
        deformable.minForceThreshold = minForce;
        deformable.canRecover = canRecover;

        // Добавляем детектор столкновений
        if (obj.GetComponent<CollisionForceDetector>() == null)
            obj.AddComponent<CollisionForceDetector>();
    }
}