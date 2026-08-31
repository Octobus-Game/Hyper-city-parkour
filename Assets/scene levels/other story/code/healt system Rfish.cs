using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _currentHp;

    [Header("Regeneration Settings")]
    [SerializeField] private bool _canRegenerate = true;
    [SerializeField] private float _regenRate = 5f;
    [SerializeField] private float _regenDelay = 3f;

    [Header("UI Elements")]
    [SerializeField] private Image _hpBar;
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("Animation Settings")]
    [SerializeField] private bool _smoothHealthBar = true;
    [SerializeField] private float _healthBarAnimationSpeed = 2f;
    [SerializeField] private AnimationCurve _healthBarCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent OnDamage;
    public UnityEvent OnHeal;

    // Properties
    public float CurrentHP => _currentHp;
    public float MaxHP => _maxHp;
    public bool IsDead => _currentHp <= 0;

    // Private variables
    private float _lastDamageTime;
    private float _targetFillAmount;
    private Coroutine _healthBarAnimation;

    void Start()
    {
        _currentHp = _maxHp;
        _targetFillAmount = 1f;
        UpdateUI(true); // Force immediate update on start
    }

    void Update()
    {
        if (_canRegenerate && !IsDead && _currentHp < _maxHp &&
            Time.time - _lastDamageTime > _regenDelay)
        {
            Heal(_regenRate * Time.deltaTime);
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        _currentHp = Mathf.Max(0, _currentHp - damage);
        _lastDamageTime = Time.time;

        UpdateUI();
        OnDamage?.Invoke();

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
        UpdateUI();

        if (amount > 0)
        {
            OnHeal?.Invoke();
        }
    }

    public void SetMaxHP(float newMaxHp)
    {
        _maxHp = newMaxHp;
        _currentHp = Mathf.Min(_currentHp, _maxHp);
        UpdateUI();
    }

    public void Revive()
    {
        _currentHp = _maxHp;
        UpdateUI();
    }

    private void UpdateUI(bool forceImmediate = false)
    {
        // Update text immediately
        if (_hpText)
        {
            _hpText.text = $"{_currentHp:F0}/{_maxHp:F0}";
        }

        // Update health bar
        if (_hpBar)
        {
            _targetFillAmount = _currentHp / _maxHp;

            if (_smoothHealthBar && !forceImmediate)
            {
                // Start smooth animation
                if (_healthBarAnimation != null)
                {
                    StopCoroutine(_healthBarAnimation);
                }
                _healthBarAnimation = StartCoroutine(AnimateHealthBar());
            }
            else
            {
                // Immediate update
                _hpBar.fillAmount = _targetFillAmount;
            }
        }
    }

    private IEnumerator AnimateHealthBar()
    {
        float startFillAmount = _hpBar.fillAmount;
        float elapsed = 0f;
        float duration = 1f / _healthBarAnimationSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Apply animation curve
            float curveValue = _healthBarCurve.Evaluate(t);

            // Interpolate between start and target fill amount
            _hpBar.fillAmount = Mathf.Lerp(startFillAmount, _targetFillAmount, curveValue);

            yield return null;
        }

        // Ensure we end exactly at target
        _hpBar.fillAmount = _targetFillAmount;
        _healthBarAnimation = null;
    }

    // Additional utility methods
    public void SetSmoothAnimation(bool enabled)
    {
        _smoothHealthBar = enabled;
    }

    public void SetAnimationSpeed(float speed)
    {
        _healthBarAnimationSpeed = Mathf.Max(0.1f, speed);
    }

    public float GetHealthPercentage()
    {
        return _currentHp / _maxHp;
    }
}