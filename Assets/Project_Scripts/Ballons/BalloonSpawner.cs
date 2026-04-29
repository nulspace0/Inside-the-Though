using UnityEngine;
using System.Collections;

public class BalloonSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject balloonPrefab;
    public float spawnInterval = 5f;
    public int maxBalloons = 8;

    [Header("Зона спавна вокруг игрока")]
    [Tooltip("Минимальное расстояние от игрока")]
    public float minRadius = 1.0f;
    [Tooltip("Максимальное расстояние от игрока")]
    public float maxRadius = 2.5f;
    [Tooltip("Высота спавна относительно этого объекта")]
    public float spawnHeight = 1.2f;

    [Header("Facts")]
    [TextArea(2, 4)]
    public string[] brainFacts = {
        "🧠 Мозг на 73% состоит из воды",
        "⚡ Нейроны передают сигнал со скоростью 431 км/ч",
        "🔗 В мозге около 86 миллиардов нейронов",
        "💤 Во сне мозг очищается от токсинов",
        "🎵 Музыка активирует больше зон мозга, чем любой другой стимул",
        "📚 Мозг продолжает развиваться до 25 лет",
        "🌙 Мозг активнее ночью, чем днём",
        "❤️ Мозг чувствует социальную боль так же, как физическую",
    };

    private int currentBalloonCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentBalloonCount < maxBalloons)
                SpawnBalloon();
        }
    }

    void SpawnBalloon()
    {
        // Случайное направление вокруг игрока
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(minRadius, maxRadius);

        Vector3 offset = new Vector3(
            Mathf.Sin(angle) * radius,
            spawnHeight,
            Mathf.Cos(angle) * radius
        );

        // Спавним в мировых координатах — шар не двигается вместе с игроком
        Vector3 spawnPos = transform.position + offset;

        GameObject balloonObj = Instantiate(balloonPrefab, spawnPos, Quaternion.identity);

        Balloon balloon = balloonObj.GetComponent<Balloon>();
        if (balloon != null)
            balloon.fact = brainFacts[Random.Range(0, brainFacts.Length)];

        currentBalloonCount++;
        balloonObj.AddComponent<BalloonTracker>().spawner = this;
    }

    public void OnBalloonDestroyed() => currentBalloonCount--;

    // Показывает зону спавна в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, minRadius);
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * spawnHeight, maxRadius);
    }
}

public class BalloonTracker : MonoBehaviour
{
    public BalloonSpawner spawner;
    void OnDestroy() => spawner?.OnBalloonDestroyed();
}
