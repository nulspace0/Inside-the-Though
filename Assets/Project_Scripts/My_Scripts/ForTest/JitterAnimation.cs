using UnityEngine;
using System.Collections;

public class JitterAnimation : MonoBehaviour
{
    [Tooltip("Максимальное смещение по каждой оси")]
    public Vector3 jitterAmount = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("Скорость анимации")]
    public float speed = 10f;

    private Vector3 originalPosition;
    private Coroutine jitterCoroutine;

    void OnEnable()
    {
        originalPosition = transform.localPosition;
        jitterCoroutine = StartCoroutine(Jitter());
    }

    void OnDisable()
    {
        if (jitterCoroutine != null)
        {
            StopCoroutine(jitterCoroutine);
            jitterCoroutine = null;
        }
    }

    IEnumerator Jitter()
    {
        while (true)
        {
            float randomX = Random.Range(-jitterAmount.x, jitterAmount.x);
            float randomY = Random.Range(-jitterAmount.y, jitterAmount.y);
            float randomZ = Random.Range(-jitterAmount.z, jitterAmount.z);

            transform.localPosition = originalPosition + new Vector3(randomX, randomY, randomZ);

            yield return new WaitForSeconds(1f / speed);
        }
    }
}