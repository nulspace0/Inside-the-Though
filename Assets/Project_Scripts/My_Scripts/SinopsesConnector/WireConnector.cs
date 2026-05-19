using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WireConnector : MonoBehaviour
{
    [Header("Параметры соединения")]
    public string connectionTag = "WireEnd";

    [Header("Объекты для активации при соединении")]
    public GameObject[] objectsToActivate;

    [Header("Объекты для деактивирования при соединении")]
    public GameObject[] objectsToDeactivate;

    [Header("Физика провода")]
    public GameObject wireRoot;

    [Header("Объект с XRGrabInteractable")]
    [Tooltip("Перетащи сюда Bone.007 (или объект с XRGrabInteractable)")]
    public XRGrabInteractable grabTarget;

    [Header("Стабилизация физики")]
    [Tooltip("Линейное затухание рибодей костей (выше = меньше болтанки)")]
    public float boneDrag = 5f;
    [Tooltip("Угловое затухание рибодей костей")]
    public float boneAngularDrag = 10f;
    [Tooltip("Максимальная скорость костей провода")]
    public float maxBoneVelocity = 2f;
    [Tooltip("Максимальный угол отклонения провода от начального")]
    public float maxSwingAngle = 60f;

    [Header("Фиксация вращения при захвате")]
    [Tooltip("Зафиксировать вращение X при захвате")]
    public bool freezeRotationX = false;
    [Tooltip("Зафиксировать вращение Z при захвате")]
    public bool freezeRotationZ = false;
    [Tooltip("Плавность возврата к целевому вращению при захвате")]
    public float rotationCorrectSpeed = 5f;
    [Tooltip("Целевой локальный эйлер при захвате (Y игнорируется для свободного поворота)")]
    public Vector3 targetGrabRotationEuler = Vector3.zero;

    private bool isConnected = false;
    private bool isGrabbed = false;
    private GameObject otherWireEnd = null;
    private Transform originalParent;
    private Transform fixedAnchor;
    private GameObject anchorObject;

    private List<Rigidbody> wireRigidbodies = new List<Rigidbody>();
    private List<Joint> wireJoints = new List<Joint>();
    private List<RigidbodyConstraints> originalConstraints = new List<RigidbodyConstraints>();

    // XR Interactable на корне провода (для события захвата)
    private XRGrabInteractable grabInteractable;
    private Rigidbody rootRigidbody;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        else Debug.LogError("На объекте " + gameObject.name + " отсутствует Collider!");

        GameObject root = wireRoot != null ? wireRoot : gameObject;

        wireRigidbodies.AddRange(root.GetComponentsInChildren<Rigidbody>());
        wireJoints.AddRange(root.GetComponentsInChildren<Joint>());

        // Сохраняем оригинальные constraints и применяем стабилизацию
        foreach (var rb in wireRigidbodies)
        {
            if (rb != null)
            {
                originalConstraints.Add(rb.constraints);
                rb.linearDamping = boneDrag;
                rb.angularDamping = boneAngularDrag;
                rb.maxLinearVelocity = maxBoneVelocity;
                rb.maxAngularVelocity = maxBoneVelocity;
            }
            else originalConstraints.Add(RigidbodyConstraints.None);
        }

        // Берём XRGrabInteractable из явно указанного поля (Bone.007)
        grabInteractable = grabTarget;
        if (grabInteractable == null)
            grabInteractable = GetComponentInParent<XRGrabInteractable>(); // фолбэк

        // Rigidbody берём с того же объекта что и XRGrabInteractable
        rootRigidbody = grabInteractable != null
            ? grabInteractable.GetComponent<Rigidbody>()
            : root.GetComponent<Rigidbody>();

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void FixedUpdate()
    {
        // Ограничиваем скорость костей чтобы не разлетались
        foreach (var rb in wireRigidbodies)
        {
            if (rb != null && !rb.isKinematic)
            {
                if (rb.linearVelocity.magnitude > maxBoneVelocity)
                    rb.linearVelocity = rb.linearVelocity.normalized * maxBoneVelocity;
                if (rb.angularVelocity.magnitude > maxBoneVelocity)
                    rb.angularVelocity = rb.angularVelocity.normalized * maxBoneVelocity;
            }
        }

        // Плавная коррекция вращения при захвате
        if (isGrabbed && rootRigidbody != null)
        {
            Vector3 euler = rootRigidbody.rotation.eulerAngles;

            if (freezeRotationX)
                euler.x = Mathf.LerpAngle(euler.x, targetGrabRotationEuler.x, Time.fixedDeltaTime * rotationCorrectSpeed);
            if (freezeRotationZ)
                euler.z = Mathf.LerpAngle(euler.z, targetGrabRotationEuler.z, Time.fixedDeltaTime * rotationCorrectSpeed);

            rootRigidbody.MoveRotation(Quaternion.Euler(euler));
        }
    }

    // ─── Захват / отпускание ────────────────────────────────────────────────

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        // Записываем текущий Y как целевой, чтобы не принудительно крутить его
        if (rootRigidbody != null)
            targetGrabRotationEuler.y = rootRigidbody.rotation.eulerAngles.y;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }

    // ─── Триггеры ───────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!isConnected && other.CompareTag(connectionTag))
        {
            WireConnector otherConnector = other.GetComponent<WireConnector>();
            if (other.gameObject != this.gameObject && otherConnector != null && !otherConnector.isConnected)
                ConnectTo(other.gameObject);
        }
    }

    // Разъединение намеренно отключено — соединение постоянное

    // ─── Соединение ─────────────────────────────────────────────────────────

    void ConnectTo(GameObject otherEnd)
    {
        isConnected = true;
        otherWireEnd = otherEnd;

        foreach (GameObject obj in objectsToActivate)
            if (obj != null) obj.SetActive(true);

        foreach (GameObject obj in objectsToDeactivate)
            if (obj != null) obj.SetActive(false);

        originalParent = transform.parent;
        Vector3 worldPos = transform.position;
        Quaternion worldRot = transform.rotation;

        // Полностью замораживаем физику провода
        foreach (var rb in wireRigidbodies)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        foreach (var joint in wireJoints)
            if (joint != null) joint.connectedBody = null;

        // Якорь для фиксации позиции
        anchorObject = new GameObject($"Anchor_{gameObject.name}_to_{otherEnd.name}");
        anchorObject.transform.position = worldPos;
        anchorObject.transform.rotation = worldRot;
        fixedAnchor = anchorObject.transform;
        transform.SetParent(fixedAnchor, true);

        // Игнорируем коллизии между концами
        Collider thisCol = GetComponent<Collider>();
        Collider otherCol = otherEnd.GetComponent<Collider>();
        if (thisCol != null && otherCol != null)
            Physics.IgnoreCollision(thisCol, otherCol, true);

        // Блокируем grab — провод больше нельзя вытащить
        if (grabInteractable != null)
            grabInteractable.enabled = false;

        Debug.Log($"[WireConnector] {gameObject.name} соединён с {otherEnd.name} (постоянно)");
    }

    void Disconnect()
    {
        if (!isConnected) return;
        isConnected = false;

        transform.SetParent(originalParent, true);

        if (anchorObject != null) { Destroy(anchorObject); anchorObject = null; fixedAnchor = null; }

        foreach (GameObject obj in objectsToActivate)
            if (obj != null) obj.SetActive(false);

        foreach (GameObject obj in objectsToDeactivate)
            if (obj != null) obj.SetActive(true);

        // Восстанавливаем физику с ограничителями скорости
        for (int i = 0; i < wireRigidbodies.Count; i++)
        {
            var rb = wireRigidbodies[i];
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.constraints = originalConstraints[i];
                rb.linearDamping = boneDrag;
                rb.angularDamping = boneAngularDrag;
            }
        }

        if (otherWireEnd != null)
        {
            Collider thisCol = GetComponent<Collider>();
            Collider otherCol = otherWireEnd.GetComponent<Collider>();
            if (thisCol != null && otherCol != null)
                Physics.IgnoreCollision(thisCol, otherCol, false);
        }

        otherWireEnd = null;
        Debug.Log($"[WireConnector] {gameObject.name} разъединён");
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = isConnected ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}