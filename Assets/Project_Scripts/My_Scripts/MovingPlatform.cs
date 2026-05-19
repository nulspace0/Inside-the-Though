using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float moveDistance = 5f;
    public float delayBeforeMove = 1f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingForward = true;
    private bool waiting = false;

    private Transform _playerTransform;
    private Vector3 _previousPosition;
    private bool _playerOnPlatform = false;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + transform.forward * moveDistance;
        _previousPosition = transform.position;
    }

    void Update()
    {
        MovePlatform();

        if (_playerOnPlatform && _playerTransform != null)
        {
            Vector3 delta = transform.position - _previousPosition;
            _playerTransform.position += delta;
        }

        _previousPosition = transform.position;
    }

    void MovePlatform()
    {
        if (!waiting)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                waiting = true;
                StartCoroutine(WaitAndChangeDirection());
            }
        }
    }

    IEnumerator WaitAndChangeDirection()
    {
        yield return new WaitForSeconds(delayBeforeMove);

        if (movingForward)
            targetPosition = startPosition;
        else
            targetPosition = startPosition + transform.forward * moveDistance;

        movingForward = !movingForward;
        waiting = false;
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
}