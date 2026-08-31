using UnityEngine;

namespace rotation_skybox
{
    public class SkyboxRotator : MonoBehaviour
    {
        [Header("Rotation Settings")]
        public float rotationSpeed = 1.0f; // Скорость вращения (градусы в секунду)
        public bool pauseRotation = false; // Пауза вращения
        public bool reverseDirection = false; // Обратное направление

        [Header("Advanced Settings")]
        public bool useUnscaledTime = false; // Использовать unscaled time (не зависит от Time.timeScale)

        [Header("Particle System Settings")]
        public ParticleSystem[] particleSystems; // Массив систем частиц для синхронизации
        public bool rotateParticles = true; // Включить вращение частиц
        public Vector3 particleRotationAxis = Vector3.up; // Ось вращения частиц (по умолчанию Y)

        private Material skyboxMaterial;
        private float currentRotation = 0f;
        private Quaternion[] initialParticleRotations; // Сохраняем начальные повороты

        void Start()
        {
            // Получаем материал skybox
            skyboxMaterial = RenderSettings.skybox;

            // Проверяем, что skybox материал существует
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

            // Инициализируем массив для хранения начальных поворотов
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
                // Выбираем тип времени
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                // Учитываем направление вращения
                float speed = reverseDirection ? -rotationSpeed : rotationSpeed;

                // Увеличиваем угол поворота
                currentRotation += speed * deltaTime;

                // Ограничиваем угол в пределах 0-360 для оптимизации
                if (currentRotation >= 360f)
                    currentRotation -= 360f;
                else if (currentRotation < 0f)
                    currentRotation += 360f;

                // Применяем поворот к skybox материалу
                skyboxMaterial.SetFloat("_Rotation", currentRotation);

                // Вращаем системы частиц
                if (rotateParticles && particleSystems != null)
                {
                    RotateParticleSystems();
                }
            }
        }

        void RotateParticleSystems()
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i] != null && initialParticleRotations != null && i < initialParticleRotations.Length)
                {
                    // Создаем поворот вокруг заданной оси
                    Quaternion rotation = Quaternion.AngleAxis(currentRotation, particleRotationAxis.normalized);

                    // Применяем поворот относительно начальной ротации
                    particleSystems[i].transform.rotation = rotation * initialParticleRotations[i];
                }
            }
        }

        void OnDestroy()
        {
            // Сбрасываем поворот при уничтожении объекта
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetFloat("_Rotation", 0f);
            }

            // Возвращаем начальные повороты систем частиц
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

            // Создаем новый массив с дополнительным элементом
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

            // Создаем новый массив без удаленного элемента
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

        public void SetPause(bool pause)
        {
            pauseRotation = pause;
        }
    }
}