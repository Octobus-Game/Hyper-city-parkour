using System;
using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    [Header("IK Settings")]
    public Transform[] joints;          // Массив суставов цепи
    public Transform target;            // Целевая точка
    public Transform endEffector;       // Конечный эффектор

    [Header("Parameters")]
    public int maxIterations = 10;      // Максимальное количество итераций
    public float tolerance = 0.01f;     // Допустимая погрешность
    public bool enableIK = true;        // Включить/выключить IK

    [Header("Joint Constraints")]
    public bool useConstraints = true;
    public Vector2[] angleConstraints;  // Ограничения углов для каждого сустава

    void Start()
    {
        // Инициализация ограничений углов, если не заданы
        if (angleConstraints == null || angleConstraints.Length != joints.Length)
        {
            angleConstraints = new Vector2[joints.Length];
            for (int i = 0; i < joints.Length; i++)
            {
                angleConstraints[i] = new Vector2(-180f, 180f);
            }
        }
    }

    void Update()
    {
        if (enableIK && target != null && endEffector != null)
        {
            SolveIK();
        }
    }

    void SolveIK()
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            // Проверяем, достигли ли мы цели
            float distance = Vector3.Distance(endEffector.position, target.position);
            if (distance < tolerance)
                break;

            // Проходим по суставам в обратном порядке (от конца к началу)
            for (int i = joints.Length - 1; i >= 0; i--)
            {
                SolveJoint(joints[i], i);
            }
        }
    }

    void SolveJoint(Transform joint, int jointIndex)
    {
        // Векторы от сустава к конечному эффектору и к цели
        Vector3 toEnd = endEffector.position - joint.position;
        Vector3 toTarget = target.position - joint.position;

        // Вычисляем угол между векторами
        float angle = Vector3.SignedAngle(toEnd, toTarget, Vector3.forward);

        // Применяем ограничения углов
        if (useConstraints)
        {
            float currentAngle = joint.eulerAngles.z;
            float newAngle = currentAngle + angle;

            // Нормализуем углы к диапазону -180, 180
            newAngle = NormalizeAngle(newAngle);

            // Применяем ограничения
            newAngle = Mathf.Clamp(newAngle, angleConstraints[jointIndex].x, angleConstraints[jointIndex].y);

            // Вычисляем итоговый поворот
            angle = newAngle - currentAngle;
        }

        // Поворачиваем сустав
        joint.Rotate(0, 0, angle);
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    // Метод FABRIK (Forward And Backward Reaching Inverse Kinematics)
    public void SolveFABRIK()
    {
        if (joints.Length < 2) return;

        Vector3[] positions = new Vector3[joints.Length + 1];
        float[] distances = new float[joints.Length];

        // Сохраняем исходные позиции и расстояния
        for (int i = 0; i < joints.Length; i++)
        {
            positions[i] = joints[i].position;
            if (i < joints.Length - 1)
            {
                distances[i] = Vector3.Distance(joints[i].position, joints[i + 1].position);
            }
        }
        positions[joints.Length] = endEffector.position;
        distances[joints.Length - 1] = Vector3.Distance(joints[joints.Length - 1].position, endEffector.position);

        float totalLength = 0;
        for (int i = 0; i < distances.Length; i++)
            totalLength += distances[i];

        // Проверяем, достижима ли цель
        float targetDistance = Vector3.Distance(positions[0], target.position);
        if (targetDistance > totalLength)
        {
            // Цель недостижима - вытягиваем цепь в направлении цели
            Vector3 direction = (target.position - positions[0]).normalized;
            for (int i = 1; i < positions.Length; i++)
            {
                positions[i] = positions[i - 1] + direction * distances[i - 1];
            }
        }
        else
        {
            // Применяем алгоритм FABRIK
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // Обратный проход
                positions[positions.Length - 1] = target.position;
                for (int i = positions.Length - 2; i >= 0; i--)
                {
                    Vector3 direction = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + direction * distances[i];
                }

                // Прямой проход
                positions[0] = joints[0].position;
                for (int i = 0; i < positions.Length - 1; i++)
                {
                    Vector3 direction = (positions[i + 1] - positions[i]).normalized;
                    positions[i + 1] = positions[i] + direction * distances[i];
                }

                // Проверяем сходимость
                if (Vector3.Distance(positions[positions.Length - 1], target.position) < tolerance)
                    break;
            }
        }

        // Применяем вычисленные позиции к суставам
        for (int i = 0; i < joints.Length; i++)
        {
            if (i < joints.Length - 1)
            {
                Vector3 direction = (positions[i + 1] - positions[i]).normalized;
                joints[i].rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        if (joints == null || joints.Length == 0) return;

        // Рисуем кости
        Gizmos.color = Color.blue;
        for (int i = 0; i < joints.Length - 1; i++)
        {
            if (joints[i] != null && joints[i + 1] != null)
            {
                Gizmos.DrawLine(joints[i].position, joints[i + 1].position);
                Gizmos.DrawWireSphere(joints[i].position, 0.1f);
            }
        }

        // Рисуем последний сустав и конечный эффектор
        if (joints[joints.Length - 1] != null)
            Gizmos.DrawWireSphere(joints[joints.Length - 1].position, 0.1f);

        if (endEffector != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(endEffector.position, 0.15f);
            if (joints.Length > 0)
                Gizmos.DrawLine(joints[joints.Length - 1].position, endEffector.position);
        }

        // Рисуем цель
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.2f);
        }

        // Рисуем линию от конечного эффектора к цели
        if (target != null && endEffector != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(endEffector.position, target.position);
        }
    }
}

// Дополнительный класс для настройки конкретного сустава
[System.Serializable]
public class IKJoint
{
    public Transform transform;
    public Vector2 angleConstraint = new Vector2(-180f, 180f);
    public float weight = 1f;
    public bool locked = false;

    public IKJoint(Transform t)
    {
        transform = t;
    }
}