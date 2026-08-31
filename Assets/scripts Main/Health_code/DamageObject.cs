using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Количество урона")]
    public float damageAmount = 10f;

    [Tooltip("Наносить урон постоянно при контакте?")]
    public bool continuousDamage = false;
}