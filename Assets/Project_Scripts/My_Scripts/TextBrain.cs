using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Collider))]
public class HoverTextDisplay : MonoBehaviour
{
    [Header("Text Configuration")]
    public GameObject textPrefab; // Укажите ваш префаб текста
    public string displayText = "Interactive Object";

    [Header("Text Position Offset")]
    public Vector3 offset = new Vector3(0f, 0.5f, 0f);

    private GameObject textObject;
    private TextMesh textMesh;
    private Transform textTransform;
    private Camera mainCamera;

    private HashSet<XRBaseInteractor> activeInteractors = new HashSet<XRBaseInteractor>();

    void Start()
    {
        if (textPrefab != null)
        {
            textObject = Instantiate(textPrefab, transform);
            textTransform = textObject.transform;
            textMesh = textObject.GetComponent<TextMesh>();

            if (textMesh != null)
            {
                textMesh.text = displayText;
            }

            textTransform.localPosition = offset;
            textTransform.localScale = Vector3.one * 0.1f; // Уменьшаем масштаб
            textObject.SetActive(false);
        }

        mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateTextVisibility();
    }

    void LateUpdate()
    {
        if (textObject != null && mainCamera != null && textObject.activeSelf)
        {
            // Поворачиваем текст к камере
            textTransform.LookAt(mainCamera.transform);
            // Инвертируем поворот по Y для правильной ориентации
            textTransform.Rotate(0, 180, 0);
        }
    }

    void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
    }

    void OnDisable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
        activeInteractors.Clear();
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor != null && !activeInteractors.Contains(interactor))
        {
            activeInteractors.Add(interactor);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor != null && activeInteractors.Contains(interactor))
        {
            activeInteractors.Remove(interactor);
        }
    }

    private void UpdateTextVisibility()
    {
        bool shouldShowText = activeInteractors.Count > 0;
        if (textObject != null)
        {
            textObject.SetActive(shouldShowText);
        }
    }
}