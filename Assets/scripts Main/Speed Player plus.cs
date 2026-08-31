using System.Collections;
using UnityEngine;
using System.Reflection;

public class SpeedBooster : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float jumpMultiplier = 1.5f;
    [SerializeField] private float boostDuration = 5f;

    [Header("Visual & Audio")]
    [SerializeField] private ParticleSystem boostEffect;
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float soundVolume = 0.7f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 1f;
    [SerializeField] private bool oneTimeUse = false;

    private bool isOnCooldown = false;
    private bool isUsed = false;

    // Хелперы для доступа к приватным [SerializeField] полям
    private static float GetField(object obj, string name)
    {
        FieldInfo f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return f != null ? (float)f.GetValue(obj) : 0f;
    }

    private static void SetField(object obj, string name, float value)
    {
        FieldInfo f = obj.GetType().GetField(name,
            BindingFlags.NonPublic | BindingFlags.Instance);
        f?.SetValue(obj, value);
    }

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOnCooldown && !isUsed)
        {
            CharacterMovement0 playerMovement = other.GetComponent<CharacterMovement0>();
            if (playerMovement != null)
                StartCoroutine(ApplyBoost(playerMovement));
        }
    }

    private IEnumerator ApplyBoost(CharacterMovement0 playerMovement)
    {
        isOnCooldown = true;
        if (oneTimeUse) isUsed = true;

        // Читаем оригинальные значения
        float originalMaxSpeed = GetField(playerMovement, "_maxSpeed");
        float originalJumpPower = GetField(playerMovement, "_jumpPower");

        // Применяем буст
        SetField(playerMovement, "_maxSpeed", originalMaxSpeed * speedMultiplier);
        SetField(playerMovement, "_jumpPower", originalJumpPower * jumpMultiplier);

        PlayBoostEffects();
        Debug.Log($"[SpeedBooster] MaxSpeed: {originalMaxSpeed} -> {originalMaxSpeed * speedMultiplier}, " +
                  $"JumpPower: {originalJumpPower} -> {originalJumpPower * jumpMultiplier}");

        yield return new WaitForSeconds(boostDuration);

        // Восстанавливаем
        SetField(playerMovement, "_maxSpeed", originalMaxSpeed);
        SetField(playerMovement, "_jumpPower", originalJumpPower);
        Debug.Log("[SpeedBooster] Boost ended. Values restored.");

        if (!oneTimeUse)
        {
            yield return new WaitForSeconds(cooldownTime);
            isOnCooldown = false;
        }
        else
        {
            if (boostEffect != null) boostEffect.Stop();
        }
    }

    private void PlayBoostEffects()
    {
        if (boostEffect != null) boostEffect.Play();

        if (boostSound != null && audioSource != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(boostSound);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOnCooldown ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}