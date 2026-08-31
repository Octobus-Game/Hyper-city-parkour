using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SkyboxChangerSmooth_01 : MonoBehaviour
{
    [Header("Skybox Tint")]
    [SerializeField] private Color targetTint = new Color(0.5f, 0.7f, 1f, 1f);
    [SerializeField] private float transitionDuration = 2f;

    // Skybox/Panoramic использует "_Tint" и "_Exposure"
    private static readonly int TintProp     = Shader.PropertyToID("_Tint");
    private static readonly int ExposureProp = Shader.PropertyToID("_Exposure");

    [Header("Exposure (яркость)")]
    [SerializeField] private float targetExposure   = 1.2f;
    [SerializeField] private float originalExposure = 1f;

    private Color     originalTint;
    private Material  skyboxMat;
    private Coroutine currentRoutine;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;  // ← 2D

        skyboxMat = RenderSettings.skybox;
        if (skyboxMat == null)
        {
            Debug.LogWarning("SkyboxColorTrigger: нет материала в RenderSettings!");
            return;
        }

        originalTint     = skyboxMat.GetColor(TintProp);
        originalExposure = skyboxMat.GetFloat(ExposureProp);
    }

    private void OnTriggerEnter2D(Collider2D other)      // ← 2D
    {
        if (!other.CompareTag("Player")) return;
        Transition(targetTint, targetExposure);
    }

    private void OnTriggerExit2D(Collider2D other)       // ← 2D
    {
        if (!other.CompareTag("Player")) return;
        Transition(originalTint, originalExposure);
    }

    private void Transition(Color toColor, float toExposure)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(LerpSkybox(toColor, toExposure));
    }

    private IEnumerator LerpSkybox(Color toColor, float toExposure)
    {
        if (skyboxMat == null) yield break;

        Color fromColor    = skyboxMat.GetColor(TintProp);
        float fromExposure = skyboxMat.GetFloat(ExposureProp);
        float elapsed      = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t); // smoothstep

            skyboxMat.SetColor(TintProp,     Color.Lerp(fromColor, toColor, t));
            skyboxMat.SetFloat(ExposureProp, Mathf.Lerp(fromExposure, toExposure, t));

            DynamicGI.UpdateEnvironment();
            yield return null;
        }

        skyboxMat.SetColor(TintProp,     toColor);
        skyboxMat.SetFloat(ExposureProp, toExposure);
        currentRoutine = null;
    }

    private void OnDestroy()
    {
        if (skyboxMat != null)
        {
            skyboxMat.SetColor(TintProp,     originalTint);
            skyboxMat.SetFloat(ExposureProp, originalExposure);
        }
    }
}