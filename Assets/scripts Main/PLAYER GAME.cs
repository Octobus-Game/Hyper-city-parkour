using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLAYERGAME : MonoBehaviour
{
    public GameObject player1; // Первый игрок
    public GameObject player2; // Второй игрок

    public MeshRenderer player1Model; // Модель первого игрока
    public MeshRenderer player2Model; // Модель второго игрока

    void Update()
    {
        // Проверяем нажатие клавиш
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchToPlayer1();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchToPlayer2();
        }
    }

    // Метод для переключения на первого игрока
    void SwitchToPlayer1()
    {
        player1.SetActive(true);
        player2.SetActive(false);

        // Первый игрок активен, второй игрок видит модель первого
        player1Model.enabled = true;
        player2Model.enabled = false;
    }

    // Метод для переключения на второго игрока
    void SwitchToPlayer2()
    {
        player1.SetActive(false);
        player2.SetActive(true);

        // Второй игрок активен, первый игрок видит модель второго
        player1Model.enabled = false;
        player2Model.enabled = true;
    }
}
