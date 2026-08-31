using UnityEngine;

public class DamageTrigger : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _damageInterval = 1f;
    [SerializeField] private bool _destroyOnHit = false;
    [SerializeField] private string _targetTag = "Player";

    private float _lastDamageTime;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_targetTag))
            DealDamage(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(_targetTag) && Time.time - _lastDamageTime >= _damageInterval)
            DealDamage(other.gameObject);
    }

    private void DealDamage(GameObject target)
    {
        var healthSystem = target.GetComponent<HealthSystem>();
        if (healthSystem)
        {
            healthSystem.TakeDamage(_damage);
            _lastDamageTime = Time.time;
            if (_destroyOnHit) Destroy(gameObject);
        }
    }
}