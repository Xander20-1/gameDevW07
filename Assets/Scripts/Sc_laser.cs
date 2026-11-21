using UnityEngine;

public class Sc_laser : MonoBehaviour
{
    private LineRenderer lr;
    private Animator heroAnim;

    [Header("Challenge 2 - Aim Only Laser")]
    [Tooltip("Jika dicentang, laser hanya muncul saat karakter melakukan Aim (stat_aim = true)")]
    public bool onlyOnAim = true;

    public float maxDistance = 100f;

    void Start()
    {
        lr = this.GetComponent<LineRenderer>();
        FindHeroAnimator();
    }

    void Update()
    {
        if (lr == null) return;

        // Challenge 2: Laser hanya aktif saat aiming
        if (onlyOnAim)
        {
            if (heroAnim == null) FindHeroAnimator();

            bool isAiming = (heroAnim != null && heroAnim.GetBool("stat_aim"));
            lr.enabled = isAiming;

            if (!isAiming) return;
        }
        else
        {
            lr.enabled = true;
        }

        // Posisi awal laser di barrel senjata
        lr.SetPosition(0, this.transform.position);

        RaycastHit hit;
        if (Physics.Raycast(this.transform.position, this.transform.forward, out hit, maxDistance))
        {
            if (hit.collider != null)
            {
                lr.SetPosition(1, hit.point);
            }
        }
        else
        {
            lr.SetPosition(1, this.transform.position + this.transform.forward * maxDistance);
        }
    }

    private void FindHeroAnimator()
    {
        heroAnim = GetComponentInParent<Animator>();
        if (heroAnim == null)
        {
            GameObject hero = GameObject.Find("hero");
            if (hero == null) hero = GameObject.Find("Hero");
            if (hero != null) heroAnim = hero.GetComponent<Animator>();
        }
    }
}
