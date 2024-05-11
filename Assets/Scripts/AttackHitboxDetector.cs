using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AttackHitboxDetector;

public class AttackHitboxDetector : MonoBehaviour
{
    PlayerController playerController;
    [SerializeField] CircleCollider2D hitbox;
    [SerializeField] LayerMask targetLayers;
    public List<int> nums = new List<int>();

    [SerializeField]
    public Hitbox[] hitboxes;

    [SerializeField]
    public Hitbox hitbox2;


    public enum HitboxType
    {
        circle,
        rectangle
    }

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }


    // Update is called once per frame
    void Update()
    {
        if (hitbox.enabled)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y), hitbox.radius, targetLayers);
            foreach (Collider2D hit in hits)
            {
                if (hit.gameObject != gameObject && hit.gameObject != transform.parent.gameObject)
                {
                    PlayerController target = hit.GetComponent<PlayerController>();
                    if (target != null) playerController.state.OnHit(target);
                }
                else
                {

                }
            }

        }
    }

    private void OnDrawGizmos()
    {
        if (hitbox.enabled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hitbox.transform.position, hitbox.radius);
        }

        foreach (Hitbox hitbox in hitboxes)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + new Vector3(hitbox.position.x, hitbox.position.y, 0), hitbox.radius);
        }

    }
}

[System.Serializable]
public struct Hitbox
{
    [SerializeField]
    public bool enabled;
    [SerializeField]
    public Vector2 position;
    [SerializeField]
    public HitboxType type;
    [SerializeField]
    public float radius;
    [SerializeField]
    public float width;
    [SerializeField]
    public float height;
}

