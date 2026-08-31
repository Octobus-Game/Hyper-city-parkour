using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MirrorTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Array of possible teleport destinations (assigned manually in Inspector)")]
    public Transform[] teleportDestinations;

    [Header("Tag-Based Destinations")]
    [Tooltip("Автоматически добавлять объекты с этим тегом в список точек телепортации")]
    public bool useTaggedDestinations = true;

    [Tooltip("Тег объектов на сцене, которые будут использоваться как точки телепортации")]
    public string destinationTag = "Start position";

    [Header("Cooldown Settings")]
    [Tooltip("Время перезарядки перед следующей телепортацией (в секундах)")]
    public float cooldown = 2f;
    private float nextTeleportTime = 0f;

    [Header("Optional Settings")]
    [Tooltip("Tag to identify the player (default: 'Player')")]
    public string playerTag = "Player";

    [Tooltip("Choose random destination or cycle through them")]
    public bool randomDestination = true;

    [Tooltip("Enable to see debug messages in console")]
    public bool showDebugLogs = false;

    [Header("Audio Settings")]
    [Tooltip("Звук, воспроизводимый при телепортации")]
    public AudioClip teleportSound;

    [Tooltip("Громкость звука (0 = тихо, 1 = полная громкость)")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Tooltip("Воспроизводить звук в точке назначения, а не у зеркала")]
    public bool playAtDestination = false;

    private AudioSource audioSource;
    private int currentDestinationIndex = 0;
    private Transform[] allDestinations;

    private void Start()
    {
        // Получаем или добавляем AudioSource на этот объект
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        RefreshDestinations();
    }

    private bool TagExists(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return false;

        try
        {
            GameObject.FindGameObjectsWithTag(tag);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    public void RefreshDestinations()
    {
        var destinations = new List<Transform>();

        if (teleportDestinations != null)
        {
            foreach (Transform dest in teleportDestinations)
            {
                if (dest != null)
                    destinations.Add(dest);
            }
        }

        if (useTaggedDestinations && !string.IsNullOrEmpty(destinationTag))
        {
            if (!TagExists(destinationTag))
            {
                Debug.LogWarning($"Mirror Teleport: Тег '{destinationTag}' не определён в проекте! " +
                    $"Добавьте его в Edit → Project Settings → Tags and Layers → Tags, " +
                    $"или выключите 'Use Tagged Destinations', или укажите другой тег.");
            }
            else
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(destinationTag);

                foreach (GameObject obj in taggedObjects)
                {
                    if (obj != null && !destinations.Contains(obj.transform))
                        destinations.Add(obj.transform);
                }

                if (showDebugLogs)
                    Debug.Log($"Mirror Teleport: Найдено {taggedObjects.Length} объектов с тегом '{destinationTag}'.");
            }
        }

        allDestinations = destinations.ToArray();

        if (showDebugLogs)
            Debug.Log($"Mirror Teleport: Итого точек телепортации: {allDestinations.Length}.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextTeleportTime)
        {
            if (showDebugLogs)
                Debug.Log($"Mirror Teleport: Зеркало на перезарядке. Осталось: {nextTeleportTime - Time.time:F1} сек.");
            return;
        }

        if (other.CompareTag(playerTag))
        {
            TeleportPlayer(other.gameObject);
            nextTeleportTime = Time.time + cooldown;
        }
    }

    private void TeleportPlayer(GameObject player)
    {
        if (allDestinations == null || allDestinations.Length == 0)
        {
            Debug.LogWarning("Mirror Teleport: Нет доступных точек телепортации!");
            return;
        }

        Transform destination = GetNextDestination();

        if (destination == null)
        {
            Debug.LogWarning("Mirror Teleport: Выбранная точка назначения равна null!");
            return;
        }

        // Саму телепортацию откладываем на один кадр через корутину.
        // Причина бага: скрипт движения вызывает characterController.Move()
        // в Update(), а OnTriggerEnter срабатывает прямо ВНУТРИ этого вызова
        // (пока капсула контроллера проходит сквозь триггер). Наше изменение
        // transform.position происходит "внутри" ещё не завершившегося Move(),
        // и когда Move() потом доигрывает свой внутренний расчёт, он
        // перезаписывает позицию обратно — почти к исходной точке. Ждём
        // кадр — и Move() этого кадра уже гарантированно завершён, поэтому
        // никто не перезапишет позицию после нас.
        StartCoroutine(TeleportRoutine(player, destination));
    }

    private IEnumerator TeleportRoutine(GameObject player, Transform destination)
{
    yield return null;
    if (player == null) yield break;

    var playerLogic = player.GetComponent<P90brush.PlayerLogic_mirror>();
    if (playerLogic != null)
    {
        playerLogic.Teleport(destination.position, resetVelocity: false);
        player.transform.rotation = destination.rotation;
    }
    else
    {
        // fallback для игроков без этого компонента
        player.transform.position = destination.position;
        player.transform.rotation = destination.rotation;
    }

    PlayTeleportSound(destination.position);
}
    
private void PlayTeleportSound(Vector3 destinationPosition)
    {
        if (teleportSound == null)
            return;

        if (playAtDestination)
        {
            // Воспроизводим звук в точке назначения (3D звук)
            AudioSource.PlayClipAtPoint(teleportSound, destinationPosition, soundVolume);
        }
        else
        {
            // Воспроизводим звук у зеркала через AudioSource компонент
            audioSource.clip = teleportSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
    }

    private Transform GetNextDestination()
    {
        if (randomDestination)
        {
            int randomIndex = Random.Range(0, allDestinations.Length);
            return allDestinations[randomIndex];
        }
        else
        {
            Transform destination = allDestinations[currentDestinationIndex];
            currentDestinationIndex = (currentDestinationIndex + 1) % allDestinations.Length;
            return destination;
        }
    }

    private void OnDrawGizmos()
    {
        if (teleportDestinations != null && teleportDestinations.Length > 0)
        {
            for (int i = 0; i < teleportDestinations.Length; i++)
            {
                if (teleportDestinations[i] != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, teleportDestinations[i].position);

                    Gizmos.color = Color.Lerp(Color.green, Color.yellow, (float)i / teleportDestinations.Length);
                    Gizmos.DrawWireSphere(teleportDestinations[i].position, 0.5f);
                }
            }
        }

        if (useTaggedDestinations && !string.IsNullOrEmpty(destinationTag) && TagExists(destinationTag))
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(destinationTag);
            for (int i = 0; i < taggedObjects.Length; i++)
            {
                if (taggedObjects[i] != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(transform.position, taggedObjects[i].transform.position);
                    Gizmos.DrawWireSphere(taggedObjects[i].transform.position, 0.5f);
                }
            }
        }
    }
}