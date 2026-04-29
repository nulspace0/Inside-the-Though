using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // ��������� ��� ������� � XR Interaction Toolkit

// [RequireComponent(typeof(XRGrabInteractable))] // �����������, ���� ������ ������ ����� �� ������������� �������
public class FixedWireEndToConnector : MonoBehaviour
{
    [Tooltip("�����, ������� ����� ������������� � ��������� �������/�������� � �����")]
    public Transform rootBoneOfWire; // ��������� ������� � Inspector (��������, RootBoneOfWire)

    [Tooltip("������, ������������ ��������� ������� � �������� ����� �������� (Connector NonMotion)")]
    public Transform wireAnchorPoint; // ��������� ������� � Inspector (��������, Connector NonMotion)

    private Vector3 initialWorldPosition; // ��������� ������� ����� � ����
    private Quaternion initialWorldRotation; // ��������� �������� ����� � ����
    private bool isFixed = false; // ���� ��� ������������ ��������� ��������

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Start()
    {
        if (rootBoneOfWire == null || wireAnchorPoint == null)
        {
            Debug.LogError("FixedWireEndToConnector: ���������� ������� rootBoneOfWire � wireAnchorPoint!");
            enabled = false; // ��������� ������, ���� ������ �� ������
            return;
        }

        // ��������� ��������� ���������� ������� � �������� �����
        // ��� �������� ����� "�������������" �����
        initialWorldPosition = wireAnchorPoint.position; // ���������� ������� Connector NonMotion
        initialWorldRotation = wireAnchorPoint.rotation; // ���������� �������� Connector NonMotion
        isFixed = true; // ����� ������������� ��������

        // �������� ��������� �������, ���� �� ����
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            // ����������� �� ������� ������ � ��������� ������� � ����������� ������ ����������
            grabInteractable.selectEntered.AddListener(OnGrabStarted);
            grabInteractable.selectExited.AddListener(OnGrabEnded);
        }
        else
        {
            Debug.LogWarning("FixedWireEndToConnector: XRGrabInteractable �� ������ �� " + gameObject.name + ". �������� ����� ����������.");
            // ���� ������� ���, �������� ����� ���������� (��� ���� � ���������� �������)
        }
    }

    // ���������� ��� ������ �������
    // ���������� BaseInteractionEventArgs
    private void OnGrabStarted(BaseInteractionEventArgs args)
    {
        // ������ �� ������ ��� ������ �������, �������� ��� �������
        // �� ����� �������� ������, ���� ����� �������� ��������� ��� �������
    }

    // ���������� ��� ��������� �������
    // ���������� BaseInteractionEventArgs
    private void OnGrabEnded(BaseInteractionEventArgs args)
    {
        // ��� ���������� ����� ������, ����� �� �������� ������� ����� � �������
        // ��� �������� ��� ����, ���� ����-��� ��� "������" ��������.
        // � ������ ������, �������� �������� �������� ������, ���� ������ �������.
        // ���� �� ������, ����� �������� ������������ ��� ����������, ���������� isFixed = false;
        // isFixed = false; // ������: ��������� �������� ��� ����������
    }

    void LateUpdate()
    {
        if (isFixed && rootBoneOfWire != null)
        {
            // ���������� ������� ����� � � ��������� ����� � ����
            rootBoneOfWire.position = initialWorldPosition;
            // ���������� �������� ����� � � ��������� �������� � ����
            rootBoneOfWire.rotation = initialWorldRotation;
        }
    }

    void OnDestroy()
    {
        // ������� �� ������� ��� ����������� �������
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabStarted);
            grabInteractable.selectExited.RemoveListener(OnGrabEnded);
        }
    }
}