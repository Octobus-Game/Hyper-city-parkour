using System.Collections.Generic;
using UnityEngine;

namespace MeshDestroy
{
    public class MeshDestroyer : MonoBehaviour
    {
        [Header("Разрушение")]
        [SerializeField] private int cutCascades = 1;
        [SerializeField] private float explodeForce = 0f;
        [SerializeField] private bool destroyOnClick = true;

        [Header("Разрушение при столкновении")]
        [SerializeField] private bool destroyOnCollision = false;
        [SerializeField] private LayerMask collisionLayers = -1;
        [SerializeField] private string[] collisionTags = new string[] { "Player" };
        [SerializeField] private float minCollisionForce = 1f;
        [SerializeField] private bool useVelocityThreshold = true;
        [SerializeField] private float velocityThreshold = 2f;

        [Header("Массив разрушаемых объектов")]
        [SerializeField] private bool enableObjectArrayCollision = false;
        [SerializeField] private MeshDestroyer[] destructibleObjects = new MeshDestroyer[0];
        [SerializeField] private float arrayCollisionVelocity = 5f;
        [SerializeField] private bool destroyBothObjects = true;

        [Header("Тряска камеры")]
        [SerializeField] private bool enableCameraShake = true;
        [SerializeField] private float shakeIntensity = 1f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeRadius = 20f;
        [SerializeField] private AnimationCurve shakeIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [SerializeField] private bool scaleShakeWithDistance = true;
        [SerializeField] private bool scaleShakeWithVelocity = true;
        [SerializeField] private float velocityShakeMultiplier = 0.1f;
        [SerializeField] private bool enableSounds = true;
        [SerializeField] private AudioClip[] destructionSounds = new AudioClip[0];
        [SerializeField] private AudioClip[] collisionSounds = new AudioClip[0];
        [SerializeField] private AudioClip[] highSpeedCollisionSounds = new AudioClip[0];
        [SerializeField] private float soundVolume = 1f;
        [SerializeField] private float soundPitch = 1f;
        [SerializeField] private float pitchVariation = 0.1f;
        [SerializeField] private bool use3DSound = true;
        [SerializeField] private float maxSoundDistance = 50f;

        [Header("Настройки частей")]
        [SerializeField] private bool addPhysics = true;
        [SerializeField] private float fragmentLifetime = 10f;

        private static readonly List<List<PartMesh>> s_listsPool = new List<List<PartMesh>>();
        private Rigidbody cachedRigidbody;
        private AudioSource audioSource;
        private bool isDestroyed = false;

        // Статический словарь для отслеживания всех разрушаемых объектов
        private static Dictionary<MeshDestroyer, bool> s_allDestructibleObjects = new Dictionary<MeshDestroyer, bool>();

        // Статический менеджер тряски камеры
        private static CameraShakeManager s_cameraShakeManager;

        void Start()
        {
            cachedRigidbody = GetComponent<Rigidbody>();

            // Инициализируем менеджер тряски камеры
            if (s_cameraShakeManager == null && enableCameraShake)
            {
                InitializeCameraShakeManager();
            }

            // Настраиваем AudioSource для звуков
            if (enableSounds)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                SetupAudioSource();
            }

            // Регистрируем этот объект в глобальном списке
            if (!s_allDestructibleObjects.ContainsKey(this))
            {
                s_allDestructibleObjects.Add(this, false);
            }

            // Если нужно разрушение при столкновении, убеждаемся что есть коллайдер
            if (destroyOnCollision && GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"MeshDestroyer на {gameObject.name}: Для разрушения при столкновении нужен Collider!");
            }

            // Добавляем объекты из массива в глобальный список
            if (enableObjectArrayCollision)
            {
                foreach (var obj in destructibleObjects)
                {
                    if (obj != null && !s_allDestructibleObjects.ContainsKey(obj))
                    {
                        s_allDestructibleObjects.Add(obj, false);
                    }
                }
            }
        }

        void OnDestroy()
        {
            // Удаляем объект из глобального списка при уничтожении
            if (s_allDestructibleObjects.ContainsKey(this))
            {
                s_allDestructibleObjects.Remove(this);
            }
        }

        void Update()
        {
            if (!isDestroyed && destroyOnClick && Input.GetMouseButtonDown(0))
            {
                DestroyMesh();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (isDestroyed) return;

            // Проверяем столкновение с объектами из массива
            if (enableObjectArrayCollision)
            {
                var otherDestroyer = collision.gameObject.GetComponent<MeshDestroyer>();
                if (otherDestroyer != null && IsInDestructibleArray(otherDestroyer))
                {
                    HandleArrayObjectCollision(collision, otherDestroyer);
                    return;
                }
            }

            // Обычная логика столкновений
            if (!destroyOnCollision)
            {
                // Воспроизводим звук обычного столкновения даже если разрушение отключено
                if (enableSounds)
                {
                    Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                    PlaySound(collisionSounds, contactPoint);
                }

                // Тряска камеры при обычном столкновении
                if (enableCameraShake)
                {
                    Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
                    TriggerCameraShake(contactPoint, collision.relativeVelocity.magnitude, 0.5f);
                }

                return;
            }

            // Проверяем слой объекта
            if (!IsLayerInMask(collision.gameObject.layer, collisionLayers)) return;

            // Проверяем тег объекта
            if (collisionTags.Length > 0 && !IsTagInArray(collision.gameObject.tag, collisionTags)) return;

            // Проверяем силу столкновения
            float collisionMagnitude = collision.relativeVelocity.magnitude;

            if (useVelocityThreshold)
            {
                if (collisionMagnitude < velocityThreshold) return;
            }
            else
            {
                float impactForce = collision.impulse.magnitude / Time.fixedDeltaTime;
                if (impactForce < minCollisionForce) return;
            }

            // Устанавливаем точку взрыва как точку контакта
            if (collision.contacts.Length > 0)
            {
                Vector3 contactPoint = collision.contacts[0].point;

                // Воспроизводим звук столкновения перед разрушением
                if (enableSounds)
                {
                    PlaySound(collisionSounds, contactPoint);
                }

                // Тряска камеры при столкновении с разрушением
                if (enableCameraShake)
                {
                    TriggerCameraShake(contactPoint, collision.relativeVelocity.magnitude, 1.5f);
                }

                DestroyMesh(contactPoint);
            }
            else
            {
                // Воспроизводим звук столкновения перед разрушением
                if (enableSounds)
                {
                    PlaySound(collisionSounds, transform.position);
                }

                // Тряска камеры при столкновении с разрушением
                if (enableCameraShake)
                {
                    TriggerCameraShake(transform.position, collision.relativeVelocity.magnitude, 1.5f);
                }

                DestroyMesh();
            }
        }

        private void HandleArrayObjectCollision(Collision collision, MeshDestroyer otherDestroyer)
        {
            float collisionMagnitude = collision.relativeVelocity.magnitude;
            Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

            // Воспроизводим звук высокоскоростного столкновения
            if (enableSounds && collisionMagnitude >= arrayCollisionVelocity)
            {
                PlaySound(highSpeedCollisionSounds, contactPoint);
            }
            else if (enableSounds)
            {
                PlaySound(collisionSounds, contactPoint);
            }

            // Тряска камеры при столкновении
            if (enableCameraShake)
            {
                float shakeMultiplier = collisionMagnitude >= arrayCollisionVelocity ? 2f : 1f;
                TriggerCameraShake(contactPoint, collisionMagnitude, shakeMultiplier);
            }

            // Проверяем превышение пороговой скорости
            if (collisionMagnitude >= arrayCollisionVelocity)
            {
                contactPoint = collision.contacts.Length > 0 ?
                    collision.contacts[0].point :
                    Vector3.Lerp(transform.position, otherDestroyer.transform.position, 0.5f);

                Debug.Log($"Высокоскоростное столкновение между {gameObject.name} и {otherDestroyer.gameObject.name}! Скорость: {collisionMagnitude:F2}");

                // Разрушаем оба объекта если включена соответствующая опция
                if (destroyBothObjects)
                {
                    // Разрушаем текущий объект
                    DestroyMesh(contactPoint);

                    // Разрушаем другой объект
                    if (!otherDestroyer.isDestroyed)
                    {
                        otherDestroyer.DestroyMesh(contactPoint);
                    }
                }
                else
                {
                    // Разрушаем только текущий объект
                    DestroyMesh(contactPoint);
                }
            }
        }

        private bool IsInDestructibleArray(MeshDestroyer otherDestroyer)
        {
            // Проверяем, есть ли объект в нашем массиве
            for (int i = 0; i < destructibleObjects.Length; i++)
            {
                if (destructibleObjects[i] == otherDestroyer)
                    return true;
            }

            // Также проверяем, есть ли мы в массиве другого объекта
            if (otherDestroyer.enableObjectArrayCollision)
            {
                for (int i = 0; i < otherDestroyer.destructibleObjects.Length; i++)
                {
                    if (otherDestroyer.destructibleObjects[i] == this)
                        return true;
                }
            }

            return false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!destroyOnCollision || isDestroyed) return;

            // Проверяем слой объекта
            if (!IsLayerInMask(other.gameObject.layer, collisionLayers)) return;

            // Проверяем тег объекта
            if (collisionTags.Length > 0 && !IsTagInArray(other.gameObject.tag, collisionTags)) return;

            // Для триггеров не проверяем силу столкновения
            DestroyMesh(other.transform.position);
        }

        private bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private bool IsTagInArray(string tag, string[] tagArray)
        {
            for (int i = 0; i < tagArray.Length; i++)
            {
                if (tagArray[i] == tag) return true;
            }
            return false;
        }

        // Публичные методы для управления массивом объектов
        public void AddDestructibleObject(MeshDestroyer obj)
        {
            if (obj == null || obj == this) return;

            // Создаем новый массив с дополнительным элементом
            var newArray = new MeshDestroyer[destructibleObjects.Length + 1];
            System.Array.Copy(destructibleObjects, newArray, destructibleObjects.Length);
            newArray[destructibleObjects.Length] = obj;
            destructibleObjects = newArray;

            // Добавляем в глобальный список
            if (!s_allDestructibleObjects.ContainsKey(obj))
            {
                s_allDestructibleObjects.Add(obj, false);
            }
        }

        public void RemoveDestructibleObject(MeshDestroyer obj)
        {
            if (obj == null) return;

            var list = new List<MeshDestroyer>(destructibleObjects);
            if (list.Remove(obj))
            {
                destructibleObjects = list.ToArray();
            }
        }

        public void ClearDestructibleObjects()
        {
            destructibleObjects = new MeshDestroyer[0];
        }

        public MeshDestroyer[] GetDestructibleObjects()
        {
            return (MeshDestroyer[])destructibleObjects.Clone();
        }

        public static MeshDestroyer[] GetAllDestructibleObjects()
        {
            var result = new MeshDestroyer[s_allDestructibleObjects.Count];
            int index = 0;
            foreach (var kvp in s_allDestructibleObjects)
            {
                if (kvp.Key != null)
                {
                    result[index++] = kvp.Key;
                }
            }

            // Обрезаем массив до актуального размера
            if (index < result.Length)
            {
                var trimmed = new MeshDestroyer[index];
                System.Array.Copy(result, trimmed, index);
                return trimmed;
            }

            return result;
        }

        #region Sound System
        private void SetupAudioSource()
        {
            if (audioSource == null) return;

            audioSource.volume = soundVolume;
            audioSource.pitch = soundPitch;
            audioSource.playOnAwake = false;

            if (use3DSound)
            {
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = maxSoundDistance;
                audioSource.minDistance = 1f;
            }
            else
            {
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        private void PlaySound(AudioClip[] soundArray, Vector3 position)
        {
            if (!enableSounds || soundArray == null || soundArray.Length == 0) return;

            // Выбираем случайный звук из массива
            AudioClip clipToPlay = soundArray[Random.Range(0, soundArray.Length)];
            if (clipToPlay == null) return;

            if (use3DSound)
            {
                // Создаем временный AudioSource для 3D звука в точке столкновения
                GameObject tempAudioObject = new GameObject("TempAudio");
                tempAudioObject.transform.position = position;

                AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
                tempAudioSource.clip = clipToPlay;
                tempAudioSource.volume = soundVolume;
                tempAudioSource.pitch = soundPitch + Random.Range(-pitchVariation, pitchVariation);
                tempAudioSource.spatialBlend = 1f;
                tempAudioSource.rolloffMode = AudioRolloffMode.Linear;
                tempAudioSource.maxDistance = maxSoundDistance;
                tempAudioSource.minDistance = 1f;
                tempAudioSource.playOnAwake = false;

                tempAudioSource.Play();

                // Уничтожаем временный объект после окончания звука
                Destroy(tempAudioObject, clipToPlay.length + 0.1f);
            }
            else if (audioSource != null && !isDestroyed)
            {
                // Используем основной AudioSource для 2D звука
                audioSource.pitch = soundPitch + Random.Range(-pitchVariation, pitchVariation);
                audioSource.PlayOneShot(clipToPlay, soundVolume);
            }
            else
            {
                // Если основной AudioSource недоступен, создаем временный 2D звук
                AudioSource.PlayClipAtPoint(clipToPlay, Camera.main ? Camera.main.transform.position : Vector3.zero, soundVolume);
            }
        }

        // Публичные методы для воспроизведения звуков извне
        public void PlayDestructionSound()
        {
            if (enableSounds)
                PlaySound(destructionSounds, transform.position);
        }

        public void PlayCollisionSound()
        {
            if (enableSounds)
                PlaySound(collisionSounds, transform.position);
        }

        public void PlayHighSpeedCollisionSound()
        {
            if (enableSounds)
                PlaySound(highSpeedCollisionSounds, transform.position);
        }
        #endregion

        #region Camera Shake System
        private void InitializeCameraShakeManager()
        {
            if (Camera.main != null)
            {
                s_cameraShakeManager = Camera.main.GetComponent<CameraShakeManager>();
                if (s_cameraShakeManager == null)
                {
                    s_cameraShakeManager = Camera.main.gameObject.AddComponent<CameraShakeManager>();
                }
            }
            else
            {
                Debug.LogWarning("MeshDestroyer: Не найдена основная камера для тряски. Убедитесь что у камеры есть тег MainCamera.");
            }
        }

        private void TriggerCameraShake(Vector3 epicenter, float velocity, float multiplier = 1f)
        {
            if (!enableCameraShake || s_cameraShakeManager == null) return;

            float finalIntensity = shakeIntensity * multiplier;
            float finalDuration = shakeDuration;

            // Масштабируем тряску в зависимости от скорости
            if (scaleShakeWithVelocity)
            {
                finalIntensity *= 1f + (velocity * velocityShakeMultiplier);
            }

            // Масштабируем тряску в зависимости от расстояния до камеры
            if (scaleShakeWithDistance && Camera.main != null)
            {
                float distance = Vector3.Distance(Camera.main.transform.position, epicenter);
                if (distance > shakeRadius)
                {
                    return; // Слишком далеко для тряски
                }

                float distanceScale = 1f - (distance / shakeRadius);
                finalIntensity *= distanceScale;
                finalDuration *= distanceScale;
            }

            s_cameraShakeManager.StartShake(finalIntensity, finalDuration, shakeIntensityCurve);
        }

        // Публичные методы для ручного управления тряской
        public void TriggerCameraShake()
        {
            TriggerCameraShake(transform.position, 5f, 1f);
        }

        public void TriggerCameraShake(float intensity, float duration)
        {
            if (enableCameraShake && s_cameraShakeManager != null)
            {
                s_cameraShakeManager.StartShake(intensity, duration, shakeIntensityCurve);
            }
        }
        #endregion

        // Вложенный класс для управления тряской камеры
        public class CameraShakeManager : MonoBehaviour
        {
            private Vector3 originalPosition;
            private bool isShaking = false;
            private float shakeTimer = 0f;
            private float shakeDuration = 0f;
            private float shakeIntensity = 0f;
            private AnimationCurve intensityCurve;

            private void Start()
            {
                originalPosition = transform.localPosition;
            }

            private void Update()
            {
                if (isShaking)
                {
                    UpdateShake();
                }
            }

            public void StartShake(float intensity, float duration, AnimationCurve curve)
            {
                if (!isShaking)
                {
                    originalPosition = transform.localPosition;
                }

                // Если уже трясем, берем максимальные значения
                shakeIntensity = Mathf.Max(shakeIntensity, intensity);
                shakeDuration = Mathf.Max(shakeDuration, duration);
                shakeTimer = 0f;
                intensityCurve = curve ?? AnimationCurve.EaseInOut(0, 1, 1, 0);
                isShaking = true;
            }

            private void UpdateShake()
            {
                shakeTimer += Time.unscaledDeltaTime;

                if (shakeTimer >= shakeDuration)
                {
                    // Заканчиваем тряску
                    isShaking = false;
                    transform.localPosition = originalPosition;
                    return;
                }

                // Вычисляем текущую интенсивность по кривой
                float normalizedTime = shakeTimer / shakeDuration;
                float currentIntensity = shakeIntensity * intensityCurve.Evaluate(normalizedTime);

                // Применяем случайное смещение
                Vector3 randomOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * currentIntensity;

                transform.localPosition = originalPosition + randomOffset;
            }

            public void StopShake()
            {
                isShaking = false;
                transform.localPosition = originalPosition;
            }
        }

        public void DestroyMesh()
        {
            DestroyMesh(transform.position);
        }

        public void DestroyMesh(Vector3 explosionCenter)
        {
            if (isDestroyed) return;

            // Воспроизводим звук разрушения
            if (enableSounds)
            {
                PlaySound(destructionSounds, explosionCenter);
            }

            // Тряска камеры при разрушении
            if (enableCameraShake)
            {
                TriggerCameraShake(explosionCenter, 10f, 2f); // Сильная тряска при разрушении
            }

            isDestroyed = true;
            var meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter?.mesh) return;

            var originalMesh = meshFilter.mesh;
            var parts = GetPooledPartsList();
            var subParts = GetPooledPartsList();

            // Создаем основную часть
            var mainPart = CreateMainPart(originalMesh);
            parts.Add(mainPart);

            // Выполняем разрезы
            for (int cascade = 0; cascade < cutCascades; cascade++)
            {
                ProcessCascade(parts, subParts);
                SwapLists(ref parts, ref subParts);
                subParts.Clear();
            }

            // Создаем GameObject'ы для каждой части
            CreateFragments(parts, explosionCenter);

            ReturnPartsListToPool(parts);
            ReturnPartsListToPool(subParts);

            Destroy(gameObject);
        }

        private PartMesh CreateMainPart(Mesh originalMesh)
        {
            return new PartMesh
            {
                Vertices = originalMesh.vertices,
                Normals = originalMesh.normals,
                UV = originalMesh.uv,
                Triangles = GetAllTriangles(originalMesh),
                Bounds = originalMesh.bounds
            };
        }

        private int[][] GetAllTriangles(Mesh mesh)
        {
            var triangles = new int[mesh.subMeshCount][];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                triangles[i] = mesh.GetTriangles(i);
            }
            return triangles;
        }

        private void ProcessCascade(List<PartMesh> parts, List<PartMesh> subParts)
        {
            foreach (var part in parts)
            {
                var plane = GenerateRandomPlane(part.Bounds);
                subParts.Add(CutMesh(part, plane, true));
                subParts.Add(CutMesh(part, plane, false));
            }
        }

        private Plane GenerateRandomPlane(Bounds bounds)
        {
            bounds.Expand(0.5f);
            var randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
            return new Plane(Random.onUnitSphere, randomPoint);
        }

        private PartMesh CutMesh(PartMesh original, Plane plane, bool leftSide)
        {
            var result = new PartMesh();
            var edgeVertices = new List<EdgeVertex>();

            for (int submesh = 0; submesh < original.Triangles.Length; submesh++)
            {
                ProcessSubmesh(original, result, plane, leftSide, submesh, edgeVertices);
            }

            CreateEdgeTriangles(result, edgeVertices, leftSide ? -plane.normal : plane.normal);
            result.FillArrays();
            return result;
        }

        private void ProcessSubmesh(PartMesh original, PartMesh result, Plane plane, bool leftSide,
                                   int submesh, List<EdgeVertex> edgeVertices)
        {
            var triangles = original.Triangles[submesh];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                ProcessTriangle(original, result, plane, leftSide, submesh, triangles, i, edgeVertices);
            }
        }

        private void ProcessTriangle(PartMesh original, PartMesh result, Plane plane, bool leftSide,
                                   int submesh, int[] triangles, int startIndex, List<EdgeVertex> edgeVertices)
        {
            // Получаем индексы вершин треугольника
            var indices = new int[] { triangles[startIndex], triangles[startIndex + 1], triangles[startIndex + 2] };

            // Определяем с какой стороны плоскости находятся вершины
            var sides = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                sides[i] = plane.GetSide(original.Vertices[indices[i]]) == leftSide;
            }

            int sideCount = (sides[0] ? 1 : 0) + (sides[1] ? 1 : 0) + (sides[2] ? 1 : 0);

            switch (sideCount)
            {
                case 0: return; // Треугольник полностью с другой стороны
                case 3: // Треугольник полностью на нужной стороне
                    AddFullTriangle(original, result, submesh, indices);
                    break;
                case 1: // Одна вершина на нужной стороне
                case 2: // Две вершины на нужной стороне
                    CutTriangle(original, result, plane, submesh, indices, sides, sideCount, edgeVertices);
                    break;
            }
        }

        private void AddFullTriangle(PartMesh original, PartMesh result, int submesh, int[] indices)
        {
            result.AddTriangle(submesh,
                original.Vertices[indices[0]], original.Vertices[indices[1]], original.Vertices[indices[2]],
                original.Normals[indices[0]], original.Normals[indices[1]], original.Normals[indices[2]],
                original.UV[indices[0]], original.UV[indices[1]], original.UV[indices[2]]);
        }

        private void CutTriangle(PartMesh original, PartMesh result, Plane plane, int submesh,
                               int[] indices, bool[] sides, int sideCount, List<EdgeVertex> edgeVertices)
        {
            // Находим одиночную вершину (которая отличается от двух других)
            int singleIndex = sides[1] == sides[2] ? 0 : sides[0] == sides[2] ? 1 : 2;

            // Вычисляем точки пересечения с плоскостью
            var (cutPoint1, lerp1) = GetPlaneIntersection(original, plane, indices, singleIndex, (singleIndex + 1) % 3);
            var (cutPoint2, lerp2) = GetPlaneIntersection(original, plane, indices, singleIndex, (singleIndex + 2) % 3);

            // Интерполируем UV координаты
            var cutUV1 = Vector2.Lerp(original.UV[indices[singleIndex]], original.UV[indices[(singleIndex + 1) % 3]], lerp1);
            var cutUV2 = Vector2.Lerp(original.UV[indices[singleIndex]], original.UV[indices[(singleIndex + 2) % 3]], lerp2);

            // Добавляем точки для создания граней
            edgeVertices.Add(new EdgeVertex { position = cutPoint1, uv = cutUV1 });
            edgeVertices.Add(new EdgeVertex { position = cutPoint2, uv = cutUV2 });

            if (sideCount == 1)
            {
                // Создаем один треугольник
                var cutNormal1 = Vector3.Lerp(original.Normals[indices[singleIndex]], original.Normals[indices[(singleIndex + 1) % 3]], lerp1);
                var cutNormal2 = Vector3.Lerp(original.Normals[indices[singleIndex]], original.Normals[indices[(singleIndex + 2) % 3]], lerp2);

                result.AddTriangle(submesh,
                    original.Vertices[indices[singleIndex]], cutPoint1, cutPoint2,
                    original.Normals[indices[singleIndex]], cutNormal1, cutNormal2,
                    original.UV[indices[singleIndex]], cutUV1, cutUV2);
            }
            else // sideCount == 2
            {
                // Создаем два треугольника
                var cutNormal1 = Vector3.Lerp(original.Normals[indices[singleIndex]], original.Normals[indices[(singleIndex + 1) % 3]], lerp1);
                var cutNormal2 = Vector3.Lerp(original.Normals[indices[singleIndex]], original.Normals[indices[(singleIndex + 2) % 3]], lerp2);

                result.AddTriangle(submesh,
                    cutPoint1, original.Vertices[indices[(singleIndex + 1) % 3]], original.Vertices[indices[(singleIndex + 2) % 3]],
                    cutNormal1, original.Normals[indices[(singleIndex + 1) % 3]], original.Normals[indices[(singleIndex + 2) % 3]],
                    cutUV1, original.UV[indices[(singleIndex + 1) % 3]], original.UV[indices[(singleIndex + 2) % 3]]);

                result.AddTriangle(submesh,
                    cutPoint1, original.Vertices[indices[(singleIndex + 2) % 3]], cutPoint2,
                    cutNormal1, original.Normals[indices[(singleIndex + 2) % 3]], cutNormal2,
                    cutUV1, original.UV[indices[(singleIndex + 2) % 3]], cutUV2);
            }
        }

        private (Vector3 point, float lerp) GetPlaneIntersection(PartMesh original, Plane plane, int[] indices, int fromIndex, int toIndex)
        {
            var from = original.Vertices[indices[fromIndex]];
            var to = original.Vertices[indices[toIndex]];
            var direction = to - from;

            var ray = new Ray(from, direction);
            plane.Raycast(ray, out float distance);

            float lerp = distance / direction.magnitude;
            Vector3 point = from + direction.normalized * distance;

            return (point, lerp);
        }

        private void CreateEdgeTriangles(PartMesh result, List<EdgeVertex> edgeVertices, Vector3 normal)
        {
            if (edgeVertices.Count < 3) return;

            // Простая триангуляция граней (можно улучшить)
            for (int i = 1; i < edgeVertices.Count - 1; i++)
            {
                result.AddTriangle(0,
                    edgeVertices[0].position, edgeVertices[i].position, edgeVertices[i + 1].position,
                    normal, normal, normal,
                    edgeVertices[0].uv, edgeVertices[i].uv, edgeVertices[i + 1].uv);
            }
        }

        private void CreateFragments(List<PartMesh> parts, Vector3 explosionCenter)
        {
            foreach (var part in parts)
            {
                var fragment = part.CreateGameObject(this, addPhysics);

                if (explodeForce > 0 && fragment.TryGetComponent<Rigidbody>(out var rb))
                {
                    // Вычисляем направление взрыва от центра взрыва к центру фрагмента
                    Vector3 explosionDirection = (part.Bounds.center - explosionCenter).normalized;
                    if (explosionDirection.magnitude < 0.1f) // Если фрагмент очень близко к центру
                    {
                        explosionDirection = Random.onUnitSphere;
                    }

                    rb.AddForce(explosionDirection * explodeForce, ForceMode.Impulse);

                    // Добавляем случайное вращение
                    rb.AddTorque(Random.onUnitSphere * explodeForce * 0.5f, ForceMode.Impulse);
                }

                if (fragmentLifetime > 0)
                {
                    Destroy(fragment, fragmentLifetime);
                }
            }
        }

        #region Pooling
        private static List<PartMesh> GetPooledPartsList()
        {
            if (s_listsPool.Count > 0)
            {
                var list = s_listsPool[s_listsPool.Count - 1];
                s_listsPool.RemoveAt(s_listsPool.Count - 1);
                list.Clear();
                return list;
            }
            return new List<PartMesh>();
        }

        private static void ReturnPartsListToPool(List<PartMesh> list)
        {
            if (s_listsPool.Count < 10) // Ограничиваем размер пула
            {
                list.Clear();
                s_listsPool.Add(list);
            }
        }

        private static void SwapLists(ref List<PartMesh> a, ref List<PartMesh> b)
        {
            (a, b) = (b, a);
        }
        #endregion

        private struct EdgeVertex
        {
            public Vector3 position;
            public Vector2 uv;
        }

        public class PartMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<List<int>> triangles = new List<List<int>>();
            private readonly List<Vector2> uvs = new List<Vector2>();

            public Vector3[] Vertices { get; set; }
            public Vector3[] Normals { get; set; }
            public int[][] Triangles { get; set; }
            public Vector2[] UV { get; set; }
            public Bounds Bounds { get; set; } = new Bounds();

            public void AddTriangle(int submesh, Vector3 v1, Vector3 v2, Vector3 v3,
                                  Vector3 n1, Vector3 n2, Vector3 n3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
            {
                // Расширяем список подмешей при необходимости
                while (triangles.Count <= submesh)
                    triangles.Add(new List<int>());

                // Добавляем вершины
                int baseIndex = vertices.Count;
                vertices.AddRange(new[] { v1, v2, v3 });
                normals.AddRange(new[] { n1, n2, n3 });
                uvs.AddRange(new[] { uv1, uv2, uv3 });

                // Добавляем индексы треугольника
                triangles[submesh].AddRange(new[] { baseIndex, baseIndex + 1, baseIndex + 2 });

                // Обновляем bounds - ИСПРАВЛЕНА ОШИБКА!
                Bounds.Encapsulate(v1);
                Bounds.Encapsulate(v2);
                Bounds.Encapsulate(v3);
            }

            public void FillArrays()
            {
                Vertices = vertices.ToArray();
                Normals = normals.ToArray();
                UV = uvs.ToArray();
                Triangles = new int[triangles.Count][];

                for (int i = 0; i < triangles.Count; i++)
                    Triangles[i] = triangles[i].ToArray();
            }

            public GameObject CreateGameObject(MeshDestroyer original, bool addPhysics = true)
            {
                var go = new GameObject($"{original.name}_Fragment");
                go.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
                go.transform.localScale = original.transform.localScale;

                // Создаем меш
                var mesh = new Mesh { name = $"{original.name}_Fragment_Mesh" };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);

                for (int i = 0; i < triangles.Count; i++)
                {
                    if (triangles[i].Count > 0)
                        mesh.SetTriangles(triangles[i], i);
                }

                mesh.RecalculateBounds();
                Bounds = mesh.bounds;

                // Добавляем компоненты
                var renderer = go.AddComponent<MeshRenderer>();
                var originalRenderer = original.GetComponent<MeshRenderer>();
                if (originalRenderer)
                    renderer.materials = originalRenderer.materials;

                var filter = go.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                if (addPhysics && vertices.Count >= 4) // Нужно минимум 4 вершины для выпуклого коллайдера
                {
                    var collider = go.AddComponent<MeshCollider>();
                    collider.convex = true;
                    collider.sharedMesh = mesh;

                    var rb = go.AddComponent<Rigidbody>();

                    // Копируем настройки разрушения (НЕ копируем массив объектов для фрагментов)
                    var meshDestroyer = go.AddComponent<MeshDestroyer>();
                    meshDestroyer.cutCascades = original.cutCascades;
                    meshDestroyer.explodeForce = original.explodeForce;
                    meshDestroyer.destroyOnClick = original.destroyOnClick;
                    meshDestroyer.destroyOnCollision = original.destroyOnCollision;
                    meshDestroyer.collisionLayers = original.collisionLayers;
                    meshDestroyer.collisionTags = original.collisionTags;
                    meshDestroyer.minCollisionForce = original.minCollisionForce;
                    meshDestroyer.useVelocityThreshold = original.useVelocityThreshold;
                    meshDestroyer.velocityThreshold = original.velocityThreshold;
                    meshDestroyer.addPhysics = original.addPhysics;
                    meshDestroyer.fragmentLifetime = original.fragmentLifetime;
                    // Не копируем настройки массива объектов для фрагментов
                    meshDestroyer.enableObjectArrayCollision = false;
                }

                return go;
            }
        }
    }
}