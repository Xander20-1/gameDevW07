using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 1.4f, -2.6f);
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null)
        {
            GameObject hero = GameObject.Find("Hero");
            if (hero != null) target = hero.transform;
            else return;
        }

        // Posisi kamera relatif di belakang arah hadap karakter
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Kamera selalu menatap ke arah karakter
        transform.LookAt(target.position + Vector3.up * 1.0f);
    }
}
