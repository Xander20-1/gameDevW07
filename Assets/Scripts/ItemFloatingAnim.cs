using UnityEngine;

public class ItemFloatingAnim : MonoBehaviour
{
    public float rotateSpeed = 60f;
    public float floatAmplitude = 0.15f;
    public float floatFrequency = 1.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Rotasi item
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Bobbing naik-turun halus
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
