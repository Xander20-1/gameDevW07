using UnityEngine;

public class Sc_hero : MonoBehaviour
{
    [Header("Movement Settings")]
    public float kecepatan = 3f;
    public float rotasiKecepatan = 10f;

    [Header("LookAt / Inverse Kinematic (Meet 7)")]
    public Transform tubuhChar;
    public Transform targetLook = null;

    [Header("Challenge 3 - Anti Twist Settings")]
    [Tooltip("Sudut maksimal pandangan hero menghadap item (mencegah badan terputar ke belakang)")]
    public float maxLookAngle = 70f;
    [Tooltip("Kecepatan lerp rotasi pandangan agar mulus")]
    public float lookSmoothSpeed = 5f;
    [Tooltip("Gunakan Animator IK LookAt (weight clamping resmi Unity) atau Bone Clamp")]
    public bool useAnimatorIK = true;

    private Animator anim;
    private float currentIKWeight = 0f;
    private Quaternion initialBoneLocalRot;
    private bool isAimingHeld = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim != null)
        {
            // Mencari tulang dada / chest sesuai modul slide 15
            tubuhChar = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (tubuhChar == null)
            {
                tubuhChar = anim.GetBoneTransform(HumanBodyBones.Head);
            }
        }

        if (tubuhChar != null)
        {
            initialBoneLocalRot = tubuhChar.localRotation;
        }
    }

    void Update()
    {
        // 1. Input Gerak (Sesuai Modul Slide 5)
        float h = 0f;
        float v = 0f;

        // Dukungan Input Manager & Input System
        try
        {
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }
        catch {}

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1f;
        }
#endif

        if (h != 0 || v != 0)
        {
            Vector3 targetDirection = new Vector3(h, 0f, v);
            if (Camera.main != null)
            {
                targetDirection = Camera.main.transform.TransformDirection(targetDirection);
                targetDirection.y = 0.0f;
            }

            if (targetDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, Time.deltaTime * rotasiKecepatan);
            }

            if (Camera.main != null)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
                this.transform.position += camFwd * v * Time.deltaTime * kecepatan;
                this.transform.position += camRight * h * Time.deltaTime * kecepatan;
            }
            else
            {
                this.transform.position += targetDirection.normalized * Time.deltaTime * kecepatan;
            }

            if (anim != null)
            {
                anim.SetBool("stat_jalan", true);
            }
        }
        else
        {
            if (anim != null)
            {
                anim.SetBool("stat_jalan", false);
            }
        }

        // 2. Input Aim (Sesuai Modul Slide 9)
        bool rightMouseDown = false;
        bool rightMouseUp = false;
        bool rightMouseHeld = false;

        try
        {
            rightMouseDown = Input.GetMouseButtonDown(1);
            rightMouseUp = Input.GetMouseButtonUp(1);
            rightMouseHeld = Input.GetMouseButton(1);
        }
        catch {}

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse.rightButton.wasPressedThisFrame) rightMouseDown = true;
            if (mouse.rightButton.wasReleasedThisFrame) rightMouseUp = true;
            if (mouse.rightButton.isPressed) rightMouseHeld = true;
        }
#endif

        if (rightMouseDown)
        {
            isAimingHeld = true;
            if (anim != null) anim.SetBool("stat_aim", true);
        }
        else if (rightMouseUp)
        {
            isAimingHeld = false;
            if (anim != null) anim.SetBool("stat_aim", false);
        }
        else if (rightMouseHeld && anim != null && !anim.GetBool("stat_aim"))
        {
            anim.SetBool("stat_aim", true);
        }
    }

    // 3. Deteksi Target Item (Sesuai Modul Slide 15)
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger with tag: " + other.gameObject.tag);
        if (other.gameObject.CompareTag("item"))
        {
            targetLook = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("item"))
        {
            if (targetLook == other.transform)
            {
                targetLook = null;
            }
        }
    }

    // 4. Challenge 3: Solusi Mencegah Tubuh 'Twist' (Terpelintir 180 Derajat / Rusak)
    // Solusi A (LateUpdate Bone LookAt dengan Angle Clamping)
    private void LateUpdate()
    {
        if (useAnimatorIK) return; // Jika memakai OnAnimatorIK, LateUpdate dilewati

        if (tubuhChar != null && targetLook != null)
        {
            Vector3 dirToTarget = targetLook.position - tubuhChar.position;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            // Mencegah Twist: Hanya menoleh jika target berada di depan hero (dalam sudut maxLookAngle)
            if (angle <= maxLookAngle)
            {
                // Hitung arah pandang ter-clamp
                Quaternion lookRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
                tubuhChar.rotation = Quaternion.Slerp(tubuhChar.rotation, lookRotation, Time.deltaTime * lookSmoothSpeed);
            }
            // Jika target berada di belakang hero (sudut > maxLookAngle), tidak memutar badan ke belakang
        }
    }

    // Solusi B (Unity Inverse Kinematic LookAt dengan clampWeight resmi)
    private void OnAnimatorIK(int layerIndex)
    {
        if (!useAnimatorIK || anim == null) return;

        bool canLook = false;
        if (targetLook != null)
        {
            Vector3 dirToTarget = targetLook.position - (transform.position + Vector3.up * 1.5f);
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            // Hanya look at jika dalam jarak dan sudut pandang wajar di depan karakter
            if (angle <= maxLookAngle)
            {
                canLook = true;
            }
        }

        float targetWeight = canLook ? 1.0f : 0.0f;
        currentIKWeight = Mathf.MoveTowards(currentIKWeight, targetWeight, Time.deltaTime * lookSmoothSpeed);

        if (currentIKWeight > 0.001f && targetLook != null)
        {
            anim.SetLookAtPosition(targetLook.position);
            // Parameter SetLookAtWeight:
            // weight: bobot total
            // bodyWeight: 0.2f (agar tulang belakang tidak melintir)
            // headWeight: 0.8f (fokus di kepala)
            // eyesWeight: 1.0f (mata melihat)
            // clampWeight: 0.5f -> KUNCI Challenge 3: Membatasi rotasi tulang agar tidak twist
            anim.SetLookAtWeight(currentIKWeight, 0.2f, 0.8f, 1.0f, 0.5f);
        }
        else
        {
            anim.SetLookAtWeight(0f);
        }
    }
}
