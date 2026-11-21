using UnityEngine;

public class Sc_hero2 : MonoBehaviour
{
    Animator anim;
    [Range(0, 1f)] public float distanceToGround = 0.135f;
    public LayerMask layermask;

    [Header("Movement")]
    public float moveSpeed = 1.25f; // Kecepatan gerak maju/mundur
    public float rotationSpeed = 120f; // Kecepatan putar pada poros karakter (derajat/detik)

    private bool isWalkingBySpace = false;

    void Start()
    {
        anim = this.GetComponent<Animator>();
    }

    void Update()
    {
        // 1. Tombol Spasi untuk toggle animasi jalan/idle manual (Langkah 6 halaman 3 & 4 PDF)
        bool spacePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spacePressed = true;
        }
#endif
        try
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                spacePressed = true;
            }
        }
        catch {}

        if (spacePressed && anim != null)
        {
            isWalkingBySpace = !isWalkingBySpace;
            anim.SetBool("isWalk", isWalkingBySpace);
        }

        // 2. Input pergerakan karakter
        float h = 0f;
        float v = 0f;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (h == 0f) h = Input.GetAxisRaw("Horizontal");
            if (v == 0f) v = Input.GetAxisRaw("Vertical");
        }
        catch {}
#endif
        if (h == 0f && v == 0f)
        {
            try
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
            catch {}
        }

        // 3. Rotasi berputar tepat pada porosnya menggunakan tombol A/D (atau Panah Kiri/Kanan)
        if (Mathf.Abs(h) > 0.01f)
        {
            transform.Rotate(0f, h * rotationSpeed * Time.deltaTime, 0f, Space.Self);
        }

        // 4. Gerak maju / mundur mengikuti arah hadap karakter menggunakan tombol W/S (atau Panah Atas/Bawah)
        if (Mathf.Abs(v) > 0.01f)
        {
            transform.Translate(0f, 0f, v * moveSpeed * Time.deltaTime, Space.Self);

            if (anim != null)
            {
                anim.SetBool("isWalk", true);
            }
        }
        else
        {
            if (anim != null)
            {
                anim.SetBool("isWalk", isWalkingBySpace);
            }
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;

        bool isWalking = anim.GetBool("isWalk");

        // LeftFoot (Langkah 7 Halaman 4 PDF)
        RaycastHit hit;
        Ray ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, distanceToGround + 1f, layermask) && hit.transform.tag == "walkable")
        {
            Vector3 footPosition = hit.point;
            footPosition.y += distanceToGround;

            Vector3 currentPos = anim.GetIKPosition(AvatarIKGoal.LeftFoot);
            if (!isWalking || currentPos.y <= footPosition.y + 0.05f)
            {
                anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
                anim.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                anim.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
            else
            {
                anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
            }
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
        }

        // RightFoot (Challenge Modul Halaman 5 PDF)
        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, distanceToGround + 1f, layermask) && hit.transform.tag == "walkable")
        {
            Vector3 footPosition = hit.point;
            footPosition.y += distanceToGround;

            Vector3 currentPos = anim.GetIKPosition(AvatarIKGoal.RightFoot);
            if (!isWalking || currentPos.y <= footPosition.y + 0.05f)
            {
                anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
                anim.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                anim.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
            else
            {
                anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
            }
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
        }
    }
}
