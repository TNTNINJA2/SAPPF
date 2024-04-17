using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitboxDetector : MonoBehaviour
{
    PlayerController playerController;
    [SerializeField] CircleCollider2D hitbox;
    [SerializeField] LayerMask targetLayers;
    // Start is called before the first frame update
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
                    playerController.HitEnemy(hit);
                }
                else
                {

                }
            }
        }
    }
}
