using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private Vector3 speed      = new Vector3(0f, 30f, 0f); // градусов/сек
    [SerializeField] private Space   space      = Space.Self;
    [SerializeField] private bool    randomizeY = false; // случайная начальная Y-ротация

    private void Start()
    {
        if (randomizeY)
            transform.Rotate(0f, Random.Range(0f, 360f), 0f, Space.Self);
    }

    private void Update()
    {
        transform.Rotate(speed * Time.deltaTime, space);
    }
}
