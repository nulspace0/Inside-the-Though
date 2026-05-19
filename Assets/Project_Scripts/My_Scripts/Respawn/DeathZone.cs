using Unity.XR.CoreUtils;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        XROrigin xrOrigin = col.GetComponentInParent<XROrigin>()
                         ?? FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null) return;
        if (RespawnManager.Instance == null) return;

        Vector3 respawnPos = RespawnManager.Instance.GetBestRespawnPosition();
        if (respawnPos == Vector3.zero) return;

        TeleportPlayer(xrOrigin, respawnPos);
        Debug.Log($"[DeathZone] Телепорт в {respawnPos}");
    }

    private void TeleportPlayer(XROrigin xrOrigin, Vector3 position)
    {
        // Компенсируем горизонтальное смещение камеры от XROrigin (room-scale)
        Vector3 camPos   = xrOrigin.Camera.transform.position;
        Vector3 rigPos   = xrOrigin.transform.position;
        Vector3 xzOffset = new Vector3(camPos.x - rigPos.x, 0f, camPos.z - rigPos.z);

        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        xrOrigin.transform.position = position - xzOffset;
        if (cc != null) cc.enabled = true;
    }

    private void OnDrawGizmos()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;
        Gizmos.color  = new Color(1, 0, 0, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
    }
}
