using UnityEngine;
using UnityEngine.Events;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public class Mirror_bullet : NetworkBehaviour
{

    public enum WeaponType { Gun, Knife }
    [SerializeField] private WeaponType currentWeaponType = WeaponType.Gun;

    [Header("Gun Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireRate = 0.5f;

    [Header("Knife Settings")]
    [SerializeField] private float knifeRange = 1f;
    [SerializeField] private float knifeDamage = 25f;
    [SerializeField] private float knifeAttackRate = 0.8f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip knifeSwingSound;
    [SerializeField] private AudioClip knifeHitSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem knifeSlashEffect;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float knifeShakeMagnitude = 0.05f;
    [SerializeField] private Transform cameraTransform;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string shootAnimationTrigger = "Shoot";
    [SerializeField] private string knifeAttackTrigger = "KnifeAttack";

    [Header("Weapon Models")]
    [SerializeField] private GameObject gunModel;
    [SerializeField] private GameObject knifeModel;

    private float nextFireTime = 0f;
    private Vector3 originalCameraPosition;
    private Coroutine shakeCoroutine;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (cameraTransform == null && isLocalPlayer)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                cameraTransform = Camera.main?.transform;
            }
        }

        if (cameraTransform != null)
        {
            originalCameraPosition = cameraTransform.localPosition;
        }

        UpdateWeaponModels();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(WeaponType.Gun);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(WeaponType.Knife);
        }

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            if (currentWeaponType == WeaponType.Gun)
            {
                nextFireTime = Time.time + fireRate;
                CmdShoot(firePoint.position, firePoint.rotation, firePoint.forward);

                if (cameraTransform != null)
                {
                    if (shakeCoroutine != null)
                    {
                        StopCoroutine(shakeCoroutine);
                    }
                    shakeCoroutine = StartCoroutine(CameraShake(shakeMagnitude));
                }
            }
            else if (currentWeaponType == WeaponType.Knife)
            {
                nextFireTime = Time.time + knifeAttackRate;
                CmdKnifeAttack();

                if (cameraTransform != null)
                {
                    if (shakeCoroutine != null)
                    {
                        StopCoroutine(shakeCoroutine);
                    }
                    shakeCoroutine = StartCoroutine(CameraShake(knifeShakeMagnitude));
                }
            }
        }
    }

    void SwitchWeapon(WeaponType newWeaponType)
    {
        currentWeaponType = newWeaponType;
        UpdateWeaponModels();
        Debug.Log($"Switched to {currentWeaponType}");
    }

    void UpdateWeaponModels()
    {
        if (gunModel != null)
        {
            gunModel.SetActive(currentWeaponType == WeaponType.Gun);
        }
        if (knifeModel != null)
        {
            knifeModel.SetActive(currentWeaponType == WeaponType.Knife);
        }
    }

    [Command]
    void CmdShoot(Vector3 position, Quaternion rotation, Vector3 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, rotation);

        //                     SyncVar          
        BulletMover mover = bullet.GetComponent<BulletMover>();
        if (mover != null)
        {
            mover.moveDirection = direction;
            mover.moveSpeed = bulletSpeed;
        }

        //                  SyncVar'                                   
        NetworkServer.Spawn(bullet);

        RpcPlayShootEffects();
    }

    [Command]
    void CmdKnifeAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(firePoint.position, knifeRange, playerLayer);

        bool hitSomeone = false;

        foreach (Collider col in hitColliders)
        {
            if (col.gameObject == gameObject) continue;

            Healt_mirror_Player healthComponent = col.GetComponent<Healt_mirror_Player>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(knifeDamage);
                hitSomeone = true;
                Debug.Log($"Knife hit {col.gameObject.name} for {knifeDamage} damage");
            }
        }

        RpcPlayKnifeEffects(hitSomeone);
    }

    [ClientRpc]
    void RpcPlayShootEffects()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (animator != null)
        {
            animator.SetTrigger(shootAnimationTrigger);
        }
    }

    [ClientRpc]
    void RpcPlayKnifeEffects(bool hitTarget)
    {
        if (knifeSwingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(knifeSwingSound);
        }

        if (hitTarget && knifeHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(knifeHitSound);
        }

        if (knifeSlashEffect != null)
        {
            knifeSlashEffect.Play();
        }

        if (animator != null)
        {
            animator.SetTrigger(knifeAttackTrigger);
        }
    }

    IEnumerator CameraShake(float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = originalCameraPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalCameraPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (currentWeaponType == WeaponType.Knife && firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, knifeRange);
        }
    }
}