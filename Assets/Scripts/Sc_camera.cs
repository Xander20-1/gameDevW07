using UnityEngine;

public class Sc_camera : MonoBehaviour
{
    public float turnSpeed = 4.0f;
    private Transform posHero;
    private Vector3 offset;

    void Start()
    {
        FindTarget();
    }

    void LateUpdate()
    {
        if (posHero == null)
        {
            FindTarget();
            if (posHero == null) return;
        }

        float mouseX = 0f;
        try
        {
            mouseX = Input.GetAxis("Mouse X");
        }
        catch {}

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mouseX = UnityEngine.InputSystem.Mouse.current.delta.x.ReadValue() * 0.1f;
        }
#endif

        offset = Quaternion.AngleAxis(mouseX * turnSpeed, Vector3.up) * offset;
        transform.position = posHero.position + offset;
        transform.LookAt(posHero.position);
    }

    private void FindTarget()
    {
        GameObject hero = GameObject.Find("hero");
        if (hero == null) hero = GameObject.Find("Hero");

        if (hero != null)
        {
            Transform point = hero.transform.Find("char_point_cam");
            if (point != null)
            {
                posHero = point;
            }
            else
            {
                posHero = hero.transform;
            }

            offset = new Vector3(posHero.localPosition.x, posHero.localPosition.y, posHero.localPosition.z - 3.2f);
            if (offset.magnitude < 1f)
            {
                offset = new Vector3(0f, 0.4f, -3.2f);
            }
        }
    }
}
