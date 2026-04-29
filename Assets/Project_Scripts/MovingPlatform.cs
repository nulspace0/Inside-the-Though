using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Параметры движения")]
    public float moveSpeed = 5f; // Скорость движения платформы
    public float moveDistance = 5f; // Расстояние, на которое платформа движется вперед и назад
    public float delayBeforeMove = 1f; // Задержка перед движением (в секундах)

    private Vector3 startPosition; // Начальная позиция
    private Vector3 targetPosition; // Целевая позиция
    private bool movingForward = true; // Направление движения
    private bool waiting = false; // Флаг ожидания

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + transform.forward * moveDistance;
    }

    void Update()
    {
        MovePlatform();
    }

    void MovePlatform()
    {
        // Двигаем платформу к целевой позиции
        if (!waiting)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Проверяем, достигла ли платформа целевой позиции
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                waiting = true; // Устанавливаем флаг ожидания
                StartCoroutine(WaitAndChangeDirection()); // Запускаем корутину ожидания
            }
        }
    }

    IEnumerator WaitAndChangeDirection()
    {
        yield return new WaitForSeconds(delayBeforeMove); // Ждем заданное время

        // Меняем направление
        if (movingForward)
        {
            targetPosition = startPosition; // Назад к стартовой позиции
        }
        else
        {
            targetPosition = startPosition + transform.forward * moveDistance; // Вперед
        }

        movingForward = !movingForward; // Инвертируем направление
        waiting = false; // Сбрасываем флаг ожидания
    }
}