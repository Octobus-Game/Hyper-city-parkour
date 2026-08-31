
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cloth_player
{
    public class Cloth_smena : MonoBehaviour
    {
        public GameObject[] _objects; // Массив объектов, между которыми будет происходить переключение
        public Button[] _buttons; // Массив кнопок UI

        [Header("Дополнительные объекты для отключения")]
        public GameObject[] _objectsToDisable; // Массив объектов, которые будут отключаться
        public Button[] _disableButtons; // Массив кнопок для отключения объектов

        private int currentActiveIndex = -1; // Индекс активного объекта

        void Start()
        {
            // Устанавливаем на все кнопки обработчики событий для переключения
            for (int i = 0; i < _buttons.Length; i++)
            {
                int index = i; // Локальная копия индекса, чтобы избежать замыканий
                _buttons[i].onClick.AddListener(() => OnButtonClick(index));
            }

            // Устанавливаем на кнопки отключения обработчики событий
            for (int i = 0; i < _disableButtons.Length; i++)
            {
                int index = i; // Локальная копия индекса, чтобы избежать замыканий
                _disableButtons[i].onClick.AddListener(() => OnDisableButtonClick(index));
            }
        }

        // Метод, который вызывается при нажатии на кнопку переключения
        void OnButtonClick(int index)
        {
            // Если объект с таким индексом уже активен, не делаем ничего
            if (currentActiveIndex == index) return;

            // Если активен объект, скрываем его
            if (currentActiveIndex >= 0)
            {
                _objects[currentActiveIndex].SetActive(false);
            }

            // Активируем новый объект
            _objects[index].SetActive(true);

            // Обновляем текущий активный объект
            currentActiveIndex = index;
        }

        // Метод, который вызывается при нажатии на кнопку отключения
        void OnDisableButtonClick(int index)
        {
            // Проверяем, что индекс находится в допустимых пределах
            if (index >= 0 && index < _objectsToDisable.Length)
            {
                // Отключаем объект
                _objectsToDisable[index].SetActive(false);
            }
        }

        // Дополнительный метод для отключения всех объектов из массива _objectsToDisable
        public void DisableAllObjects()
        {
            foreach (GameObject obj in _objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        // Дополнительный метод для включения всех объектов из массива _objectsToDisable
        public void EnableAllObjects()
        {
            foreach (GameObject obj in _objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}