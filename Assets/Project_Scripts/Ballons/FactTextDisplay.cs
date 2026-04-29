using UnityEngine;
using TMPro;
using System.Collections;

public class FactTextDisplay : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI factText;
    public CanvasGroup canvasGroup;
    public float displayDuration = 4f;
    public float fadeSpeed = 2f;

    private Coroutine currentRoutine;

    void Start()
    {
        canvasGroup.alpha = 0f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.SetParent(cam.transform);
            transform.localPosition = new Vector3(0f, 0f, 2f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
        }
    }

    public void ShowFact(string fact, Color accentColor)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        factText.text = fact;
        factText.color = Color.white;
        currentRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}