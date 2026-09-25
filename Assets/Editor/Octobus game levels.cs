using UnityEngine;

namespace rotation_skybox
{
    public class SkyboxRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        public float rotationSpeed = 1.0f; // Скорость вращения (градусов в секунду)
        public bool pauseRotation = false; // Пауза вращения
        public bool reverseDirection = false; // Обратное направление

        [Header("3D Rotation (XYZ)")]
        public bool rotateOnAllAxes = false; // Включить вращение по всем осям X, Y, Z вместо одной
        public Vector3 rotationSpeedXYZ = new Vector3(0f, 1f, 0f); // Скорость вращения по осям X, Y, Z (градусов в секунду); используется, когда rotateOnAllAxes = true

        [Header("Advanced Settings")]
        public bool useUnscaledTime = false; // Использовать unscaled time (не зависит от Time.timeScale)

        [Header("Particle System Settings")]
        public ParticleSystem[] particleSystems; // Массив систем частиц для синхронизации
        public bool rotateParticles = true; // Включить вращение частиц
        public Vector3 particleRotationAxis = Vector3.up; // Ось вращения частиц (по умолчанию Y); используется, когда rotateOnAllAxes = false

        private Material skyboxMaterial;
        private float currentRotation = 0f; // текущий угол для режима вращения вокруг одной оси
        private Vector3 currentRotationXYZ = Vector3.zero; // текущие углы для режима вращения по X, Y, Z
        private Quaternion[] initialParticleRotations; // исходные повороты частиц
        private bool warnedAboutLimitedSkyboxRotation = false; // чтобы не спамить предупреждением каждый кадр

        void Start()
        {
            // Получаем материал skybox
            skyboxMaterial = RenderSettings.skybox;

            // Проверяем, что материал skybox назначен
            if (skyboxMaterial == null)
            {
                Debug.LogError("Skybox material not found! Make sure you have a skybox material assigned in Lighting settings.");
                enabled = false;
                return;
            }

            // Проверяем, что материал поддерживает вращение
            if (!skyboxMaterial.HasProperty("_Rotation"))
            {
                Debug.LogError("Skybox material doesn't support rotation! Use a material with _Rotation property (like 6 Sided or Cubemap skybox).");
                enabled = false;
                return;
            }

            // Инициализируем массив для хранения начальных поворотов частиц
            if (particleSystems != null && particleSystems.Length > 0)
            {
                initialParticleRotations = new Quaternion[particleSystems.Length];
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null)
                    {
                        initialParticleRotations[i] = particleSystems[i].transform.rotation;
                    }
                }
            }
        }

        void Update()
        {
            if (skyboxMaterial != null && !pauseRotation)
            {
                // Получаем дельта тайм
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                if (rotateOnAllAxes)
                {
                    // Режим вращения по всем осям X, Y, Z
                    Vector3 speed = reverseDirection ? -rotationSpeedXYZ : rotationSpeedXYZ;
                    currentRotationXYZ += speed * deltaTime;

                    currentRotationXYZ.x = NormalizeAngle(currentRotationXYZ.x);
                    currentRotationXYZ.y = NormalizeAngle(currentRotationXYZ.y);
                    currentRotationXYZ.z = NormalizeAngle(currentRotationXYZ.z);

                    ApplySkyboxRotationXYZ();
                }
                else
                {
                    // Определяем направление вращения
                    float speed = reverseDirection ? -rotationSpeed : rotationSpeed;

                    // Накапливаем угол вращения
                    currentRotation += speed * deltaTime;
                    currentRotation = NormalizeAngle(currentRotation);

                    // Применяем текущий угол к материалу skybox
                    skyboxMaterial.SetFloat("_Rotation", currentRotation);
                }

                // Вращаем системы частиц
                if (rotateParticles && particleSystems != null)
                {
                    RotateParticleSystems();
                }
            }
        }

        float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        void ApplySkyboxRotationXYZ()
        {
            // Стандартные скайбокс-шейдеры Unity (Skybox/6 Sided, Skybox/Cubemap, Skybox/Procedural)
            // поддерживают только один параметр _Rotation (вращение вокруг вертикальной оси Y).
            // Если используется кастомный шейдер с отдельными осями — задействуем их.
            if (skyboxMaterial.HasProperty("_RotationX") &&
                skyboxMaterial.HasProperty("_RotationY") &&
                skyboxMaterial.HasProperty("_RotationZ"))
            {
                skyboxMaterial.SetFloat("_RotationX", currentRotationXYZ.x);
                skyboxMaterial.SetFloat("_RotationY", currentRotationXYZ.y);
                skyboxMaterial.SetFloat("_RotationZ", currentRotationXYZ.z);
            }
            else
            {
                // Фолбэк: стандартный шейдер умеет вращать только вокруг Y
                skyboxMaterial.SetFloat("_Rotation", currentRotationXYZ.y);

                if (!warnedAboutLimitedSkyboxRotation)
                {
                    Debug.LogWarning("Skybox material only exposes _Rotation (single axis). Applying Y-axis rotation only. " +
                                     "For true X/Y/Z skybox rotation use a custom shader exposing _RotationX/_RotationY/_RotationZ properties.");
                    warnedAboutLimitedSkyboxRotation = true;
                }
            }
        }

        void RotateParticleSystems()
        {
            // В режиме XYZ используем полный поворот по трём осям сразу,
            // иначе — поворот вокруг одной заданной оси (как раньше)
            Quaternion rotation = rotateOnAllAxes
                ? Quaternion.Euler(currentRotationXYZ)
                : Quaternion.AngleAxis(currentRotation, particleRotationAxis.normalized);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null && initialParticleRotations != null && i < initialParticleRotations.Length)
                {
                    // Применяем поворот относительно начального поворота
                    particleSystems[i].transform.rotation = rotation * initialParticleRotations[i];
                }
            }
        }

        void OnDestroy()
        {
            // Сбрасываем вращение при уничтожении объекта
            if (skyboxMaterial != null)
            {
                if (skyboxMaterial.HasProperty("_RotationX") &&
                    skyboxMaterial.HasProperty("_RotationY") &&
                    skyboxMaterial.HasProperty("_RotationZ"))
                {
                    skyboxMaterial.SetFloat("_RotationX", 0f);
                    skyboxMaterial.SetFloat("_RotationY", 0f);
                    skyboxMaterial.SetFloat("_RotationZ", 0f);
                }
                skyboxMaterial.SetFloat("_Rotation", 0f);
            }

            // Восстанавливаем исходные повороты систем частиц
            if (particleSystems != null && initialParticleRotations != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null && i < initialParticleRotations.Length)
                    {
                        particleSystems[i].transform.rotation = initialParticleRotations[i];
                    }
                }
            }
        }

        // Публичные методы для управления из других скриптов

        public void AddParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;

            // Создаём новый массив с дополнительным элементом
            int newLength = (particleSystems == null) ? 1 : particleSystems.Length + 1;
            ParticleSystem[] newArray = new ParticleSystem[newLength];
            Quaternion[] newRotations = new Quaternion[newLength];

            // Копируем существующие элементы
            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    newArray[i] = particleSystems[i];
                    if (initialParticleRotations != null && i < initialParticleRotations.Length)
                    {
                        newRotations[i] = initialParticleRotations[i];
                    }
                }
            }

            // Добавляем новую систему частиц
            newArray[newLength - 1] = ps;
            newRotations[newLength - 1] = ps.transform.rotation;

            particleSystems = newArray;
            initialParticleRotations = newRotations;
        }

        public void RemoveParticleSystem(ParticleSystem ps)
        {
            if (particleSystems == null || ps == null) return;

            int index = System.Array.IndexOf(particleSystems, ps);
            if (index < 0) return;

            // Создаём новый массив без удаляемого элемента
            ParticleSystem[] newArray = new ParticleSystem[particleSystems.Length - 1];
            Quaternion[] newRotations = new Quaternion[particleSystems.Length - 1];

            int newIndex = 0;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (i != index)
                {
                    newArray[newIndex] = particleSystems[i];
                    if (initialParticleRotations != null && i < initialParticleRotations.Length)
                    {
                        newRotations[newIndex] = initialParticleRotations[i];
                    }
                    newIndex++;
                }
            }

            particleSystems = newArray;
            initialParticleRotations = newRotations;
        }

        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = speed;
        }

        public void SetRotationSpeedXYZ(Vector3 speed)
        {
            rotationSpeedXYZ = speed;
        }

        public void SetPause(bool pause)
        {
            pauseRotation = pause;
        }

        public void SetRotateOnAllAxes(bool enable)
        {
            rotateOnAllAxes = enable;
        }
    }
}