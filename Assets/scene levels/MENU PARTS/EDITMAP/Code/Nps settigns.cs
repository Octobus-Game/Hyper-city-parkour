using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AIToggleManager : MonoBehaviour
{
    [Header("UI Toggle References")]
    public Toggle agentToggle;      // Toggle для активации AGENT скрипта
    public Toggle selvelToggle;     // Toggle для активации SELVEL_AI скрипта

    [Header("AI Script References")]
    public AGENT agentScript;           // Ссылка на скрипт AGENT
    public SELVEL_AI selvelAIScript;    // Ссылка на скрипт SELVEL_AI

    [Header("Auto Find Scripts (Optional)")]
    public bool autoFindScripts = true; // Автоматически найти скрипты на объекте

    private void Start()
    {
        // Если включен автопоиск скриптов, пытаемся найти их на том же объекте
        if (autoFindScripts)
        {
            if (agentScript == null)
                agentScript = GetComponent<AGENT>();

            if (selvelAIScript == null)
                selvelAIScript = GetComponent<SELVEL_AI>();
        }

        // Проверяем наличие всех необходимых компонентов
        ValidateComponents();

        // Подписываемся на события Toggle
        if (agentToggle != null)
            agentToggle.onValueChanged.AddListener(OnAgentToggleChanged);

        if (selvelToggle != null)
            selvelToggle.onValueChanged.AddListener(OnSelvelToggleChanged);

        // Устанавливаем начальное состояние
        InitializeToggles();
    }

    private void ValidateComponents()
    {
        // Проверяем наличие Toggle компонентов
        if (agentToggle == null)
            Debug.LogWarning("Agent Toggle не назначен в AIToggleManager на объекте " + gameObject.name);

        if (selvelToggle == null)
            Debug.LogWarning("Selvel Toggle не назначен в AIToggleManager на объекте " + gameObject.name);

        // Проверяем наличие AI скриптов
        if (agentScript == null)
            Debug.LogWarning("AGENT скрипт не найден в AIToggleManager на объекте " + gameObject.name);

        if (selvelAIScript == null)
            Debug.LogWarning("SELVEL_AI скрипт не найден в AIToggleManager на объекте " + gameObject.name);
    }

    private void InitializeToggles()
    {
        // Устанавливаем начальное состояние Toggle в соответствии с активностью скриптов
        if (agentToggle != null && agentScript != null)
        {
            agentToggle.isOn = agentScript.enabled;
        }

        if (selvelToggle != null && selvelAIScript != null)
        {
            selvelToggle.isOn = selvelAIScript.enabled;
        }

        // По умолчанию активируем только один скрипт (AGENT)
        if (agentScript != null && selvelAIScript != null)
        {
            if (!agentScript.enabled && !selvelAIScript.enabled)
            {
                agentScript.enabled = true;
                if (agentToggle != null)
                    agentToggle.isOn = true;
            }
        }
    }

    // Обработчик для Toggle AGENT скрипта
    private void OnAgentToggleChanged(bool isOn)
    {
        if (agentScript == null) return;

        if (isOn)
        {
            // Активируем AGENT скрипт
            agentScript.enabled = true;

            // Деактивируем SELVEL_AI скрипт
            if (selvelAIScript != null)
            {
                selvelAIScript.enabled = false;

                // Обновляем состояние второго Toggle без вызова события
                if (selvelToggle != null)
                {
                    selvelToggle.onValueChanged.RemoveListener(OnSelvelToggleChanged);
                    selvelToggle.isOn = false;
                    selvelToggle.onValueChanged.AddListener(OnSelvelToggleChanged);
                }
            }

            Debug.Log("AGENT скрипт активирован");
        }
        else
        {
            // Деактивируем AGENT скрипт
            agentScript.enabled = false;
            Debug.Log("AGENT скрипт деактивирован");
        }
    }

    // Обработчик для Toggle SELVEL_AI скрипта
    private void OnSelvelToggleChanged(bool isOn)
    {
        if (selvelAIScript == null) return;

        if (isOn)
        {
            // Активируем SELVEL_AI скрипт
            selvelAIScript.enabled = true;

            // Деактивируем AGENT скрипт
            if (agentScript != null)
            {
                agentScript.enabled = false;

                // Обновляем состояние первого Toggle без вызова события
                if (agentToggle != null)
                {
                    agentToggle.onValueChanged.RemoveListener(OnAgentToggleChanged);
                    agentToggle.isOn = false;
                    agentToggle.onValueChanged.AddListener(OnAgentToggleChanged);
                }
            }

            Debug.Log("SELVEL_AI скрипт активирован");
        }
        else
        {
            // Деактивируем SELVEL_AI скрипт
            selvelAIScript.enabled = false;
            Debug.Log("SELVEL_AI скрипт деактивирован");
        }
    }

    // Публичные методы для программного управления
    public void ActivateAgent()
    {
        if (agentToggle != null)
            agentToggle.isOn = true;
    }

    public void ActivateSelvelAI()
    {
        if (selvelToggle != null)
            selvelToggle.isOn = true;
    }

    public void DeactivateBoth()
    {
        if (agentToggle != null)
            agentToggle.isOn = false;

        if (selvelToggle != null)
            selvelToggle.isOn = false;
    }

    // Получение текущего активного скрипта
    public string GetActiveAI()
    {
        if (agentScript != null && agentScript.enabled)
            return "AGENT";
        else if (selvelAIScript != null && selvelAIScript.enabled)
            return "SELVEL_AI";
        else
            return "None";
    }

    private void OnDestroy()
    {
        // Отписываемся от событий при уничтожении объекта
        if (agentToggle != null)
            agentToggle.onValueChanged.RemoveListener(OnAgentToggleChanged);

        if (selvelToggle != null)
            selvelToggle.onValueChanged.RemoveListener(OnSelvelToggleChanged);
    }
}