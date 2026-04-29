using UnityEngine;
using System.Collections;

public class StoppingMovingPlatform : MonoBehaviour
{
    [Header("Platform Movement")]
    public Transform pointA;
    public Transform pointB;
    [Tooltip("Скорость движения платформы")]
    public float speed = 2f;
    [Tooltip("Длина пути. 0 = использовать реальное расстояние между точками")]
    public float moveDistance = 0f;
    [Tooltip("Пауза на каждой точке (секунды)")]
    public float delayAtPoints = 1f;

    private Transform _playerTransform;
    private Vector3 _previousPosition;
    private bool _playerOnPlatform = false;

    private Vector3 _targetA;
    private Vector3 _targetB;
    private Vector3 _currentTarget;
    private bool _waiting = false;
    private bool _goingToB = true;

    private void Start()
    {
        _previousPosition = transform.position;
        RecalculateTargets();
        _currentTarget = _targetB;
    }

    // Пересчитываем позиции целей с учётом moveDistance
    private void RecalculateTargets()
    {
        if (pointA == null || pointB == null) return;

        Vector3 dir = (pointB.position - pointA.position).normalized;
        float dist = moveDistance > 0f
            ? moveDistance
            : Vector3.Distance(pointA.position, pointB.position);

        _targetA = pointA.position;
        _targetB = pointA.position + dir * dist;
    }

    private void Update()
    {
        MovePlatform();

        if (_playerOnPlatform && _playerTransform != null)
        {
            Vector3 delta = transform.position - _previousPosition;
            _playerTransform.position += delta;
        }

        _previousPosition = transform.position;
    }

    private void MovePlatform()
    {
        if (_waiting) return;

        transform.position = Vector3.MoveTowards(transform.position, _currentTarget, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _currentTarget) < 0.01f)
        {
            _waiting = true;
            StartCoroutine(WaitThenSwitch());
        }
    }

    private IEnumerator WaitThenSwitch()
    {
        yield return new WaitForSeconds(delayAtPoints);
        _goingToB = !_goingToB;
        _currentTarget = _goingToB ? _targetB : _targetA;
        _waiting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerTransform = other.transform.root;
            _playerOnPlatform = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerOnPlatform = false;
            _playerTransform = null;
        }
    }

    // Отображение пути в редакторе
    private void OnDrawGizmosSelected()
    {
        if (pointA == null || pointB == null) return;

        Vector3 dir = (pointB.position - pointA.position).normalized;
        float dist = moveDistance > 0f
            ? moveDistance
            : Vector3.Distance(pointA.position, pointB.position);

        Vector3 endPoint = pointA.position + dir * dist;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA.position, endPoint);
        Gizmos.DrawWireSphere(pointA.position, 0.15f);
        Gizmos.DrawWireSphere(endPoint, 0.15f);
    }
}
