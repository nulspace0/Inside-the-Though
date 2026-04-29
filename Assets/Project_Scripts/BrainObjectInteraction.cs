using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(Collider), typeof(Renderer))]
public class BrainObjectInteraction : MonoBehaviour
{
    public Material defaultMaterial;
    public Material highlightedMaterial;

    [Header("Subtle Shake (on hover)")]
    public float subtleShakeDuration = 0.3f;
    public float subtleShakeIntensity = 0.02f;

    [Header("Strong Shake (on trigger press)")]
    public float strongShakeDuration = 5f;
    public float strongShakeIntensity = 0.08f;

    [Header("Scene Transition")]
    public string targetSceneName = "Mozjechok"; // ← должно совпадать с именем .unity файла
    public float delayBeforeSceneLoad = 3f;

    private Renderer objectRenderer;
    private Vector3 originalPosition;
    private bool isHighlighted = false;
    private Coroutine currentShake = null;

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        originalPosition = transform.position;
        if (defaultMaterial == null)
            defaultMaterial = objectRenderer.material;
    }

    void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
            interactable.selectEntered.AddListener(OnSelect);
        }
        else
        {
            Debug.LogError("Объект должен иметь XRBaseInteractable.");
        }
    }

    void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
            interactable.selectEntered.RemoveListener(OnSelect);
        }
        if (currentShake != null)
            StopCoroutine(currentShake);
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (isHighlighted) return;
        isHighlighted = true;
        if (highlightedMaterial != null)
            objectRenderer.material = highlightedMaterial;
        if (currentShake != null) StopCoroutine(currentShake);
        currentShake = StartCoroutine(Shake(subtleShakeDuration, subtleShakeIntensity));
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        isHighlighted = false;
        objectRenderer.material = defaultMaterial;
        if (currentShake != null) StopCoroutine(currentShake);
        transform.position = originalPosition;
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        if (!isHighlighted) return;

        isHighlighted = false;
        objectRenderer.material = defaultMaterial;
        if (currentShake != null) StopCoroutine(currentShake);

        currentShake = StartCoroutine(Shake(strongShakeDuration, strongShakeIntensity));
        StartCoroutine(LoadTargetSceneAfterDelay(delayBeforeSceneLoad));
    }

    IEnumerator Shake(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Vector3 jitter = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * intensity * (1f - elapsed / duration);
            transform.position = originalPosition + jitter;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    IEnumerator LoadTargetSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        LoadSceneByName(targetSceneName);
    }

    void LoadSceneByName(string sceneName)
    {
#if UNITY_EDITOR
        // В редакторе: ищем сцену по имени файла в проекте
        string[] guids = AssetDatabase.FindAssets($"t:SceneAsset {sceneName}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
        else
        {
            Debug.LogError($"Сцена '{sceneName}' не найдена в проекте (проверьте имя файла .unity)!");
            return;
        }
#else
        // В билде: используем стандартную загрузку
        if (SceneIsInBuild(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Сцена '{sceneName}' не добавлена в билд! Добавьте её в Build Profile → Included Scenes.");
        }
#endif
    }

#if !UNITY_EDITOR
    bool SceneIsInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return true;
        }
        return false;
    }
#endif
}