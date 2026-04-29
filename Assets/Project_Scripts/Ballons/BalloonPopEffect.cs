using UnityEngine;
using System.Collections;

public class BalloonPopEffect : MonoBehaviour
{
    public static BalloonPopEffect Instance;

    [Header("Audio")]
    public AudioClip[] melodicPopSounds; // Несколько нот — до, ре, ми, фа...
    private AudioSource audioSource;

    [Header("Particles")]
    public ParticleSystem popParticlesPrefab;

    [Header("Fact Display")]
    public FactTextDisplay factDisplay;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayPop(Vector3 position, string fact, Color color)
    {
        // Случайная мелодичная нота
        if (melodicPopSounds.Length > 0)
        {
            AudioClip clip = melodicPopSounds[Random.Range(0, melodicPopSounds.Length)];
            // Небольшая вариация питча для разнообразия
            audioSource.pitch = Random.Range(0.9f, 1.2f);
            audioSource.PlayOneShot(clip, 0.8f);
        }

        // Частицы
        if (popParticlesPrefab != null)
        {
            var ps = Instantiate(popParticlesPrefab, position, Quaternion.identity);
            var main = ps.main;
            main.startColor = color;
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        // Показать текст факта
        factDisplay?.ShowFact(fact, color);
    }
}