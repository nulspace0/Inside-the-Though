using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private Image        progressBarFill;   // Image с Fill Amount
    [SerializeField] private TextMeshProUGUI statusText;     // Текст статуса загрузки
    [SerializeField] private TextMeshProUGUI percentText;    // "42%"
    [SerializeField] private TextMeshProUGUI tipText;        // Подсказка снизу (необязательно)

    [Header("Настройки")]
    [SerializeField] private string nextSceneName = "Head";  // Сцена для загрузки
    [SerializeField] private float  minLoadTime   = 4.5f;    // Минимальное время (сек)
    [SerializeField] private float  messageDelay  = 0.9f;    // Пауза между сообщениями

    // ── Шуточные сообщения загрузки ──────────────────────────────────────
    private readonly List<string> _messages = new()
    {
        "Загрузка нейронных связей...",
        "Будим сонные нейроны...",
        "Загрузка синапсов: 17 из 86 миллиардов...",
        "Калибровка серого вещества...",
        "Настройка префронтальной коры...",
        "Загрузка мыслей (очень много)...",
        "Синхронизация полушарий...",
        "Считаем извилины... потеряли счёт...",
        "Загрузка воспоминаний из детства...",
        "Фильтрация ненужных синопсов...",
        "Подключение к подсознанию...",
        "Анализ философских вопросов...",
        "Загрузка инстинктов выживания...",
        "Оптимизация потока сознания...",
        "Дефрагментация памяти...",
        "Запрос разрешения у мозжечка...",
        "Загрузка чувства юмора...",
        "Активация зоны Брока...",
        "Почти готово... наверное...",
    };

    private readonly List<string> _tips = new()
    {
        "Твой мозг обрабатывает ~11 миллионов бит информации в секунду.",
        "Нейрон может передавать сигналы со скоростью до 120 м/с.",
        "В мозге около 86 миллиардов нейронов.",
        "Мозг потребляет ~20% всей энергии тела.",
        "Во сне мозг \"очищается\" от токсинов через глимфатическую систему.",
        "Мозг на 73% состоит из воды.",
    };

    // ── Приватные поля ────────────────────────────────────────────────────
    private float          _progress      = 0f;
    private int            _msgIndex      = 0;
    private AsyncOperation _loadOperation;

    // ─────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Случайный порядок сообщений
        Shuffle(_messages);

        // Случайный совет
        if (tipText != null)
            tipText.text = _tips[Random.Range(0, _tips.Count)];

        SetProgress(0f);
        StartCoroutine(LoadScene());
        StartCoroutine(CycleMessages());
    }

    // ── Асинхронная загрузка сцены ────────────────────────────────────────
    private IEnumerator LoadScene()
    {
        _loadOperation = SceneManager.LoadSceneAsync(nextSceneName);
        _loadOperation.allowSceneActivation = false; // Не переключаемся сразу

        float elapsed    = 0f;
        float fakeTarget = 0f;   // куда "едет" прогресс для плавности

        while (!_loadOperation.isDone)
        {
            elapsed += Time.deltaTime;

            // Реальный прогресс Unity (0..0.9) + время ожидания
            float realProgress  = _loadOperation.progress / 0.9f;   // 0..1
            float timeProgress  = Mathf.Clamp01(elapsed / minLoadTime);
            float targetProgress = Mathf.Min(realProgress, timeProgress);

            // Плавно догоняем цель
            fakeTarget   = Mathf.MoveTowards(fakeTarget, targetProgress, Time.deltaTime * 0.35f);
            _progress    = fakeTarget;

            SetProgress(_progress);

            // Когда и сцена готова, и минимальное время прошло — переходим
            if (_progress >= 0.999f && elapsed >= minLoadTime && _loadOperation.progress >= 0.9f)
            {
                SetProgress(1f);
                yield return new WaitForSeconds(0.4f);
                _loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // ── Цикл сообщений ────────────────────────────────────────────────────
    private IEnumerator CycleMessages()
    {
        while (true)
        {
            if (statusText != null)
            {
                string msg = _messages[_msgIndex % _messages.Count];
                yield return StartCoroutine(TypeText(statusText, msg, 0.03f));
                yield return new WaitForSeconds(messageDelay);
                _msgIndex++;
            }
            else
            {
                yield return null;
            }
        }
    }

    // ── Эффект печатания текста ───────────────────────────────────────────
    private IEnumerator TypeText(TextMeshProUGUI tmp, string fullText, float charDelay)
    {
        tmp.text = "";
        foreach (char c in fullText)
        {
            tmp.text += c;
            yield return new WaitForSeconds(charDelay);
        }
    }

    // ── Обновление UI прогресса ───────────────────────────────────────────
    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (progressBarFill != null) progressBarFill.fillAmount = value;
        if (percentText     != null) percentText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // ── Перемешать список ─────────────────────────────────────────────────
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
