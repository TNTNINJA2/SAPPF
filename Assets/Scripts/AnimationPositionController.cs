using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPositionController : MonoBehaviour
{
    [SerializeField] GameObject characterRender;

    public Vector3 targetPos;
     Vector3 oldTargetPos;

    Rigidbody2D rb2D;

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
    }
    public void ResetTargetPos()
    {
        oldTargetPos = Vector3.zero;
    }

    private void Update()
    {
        if (targetPos != oldTargetPos)
        {
            Vector3 deltaPos = targetPos - oldTargetPos;
            rb2D.velocity = new Vector3(deltaPos.x * transform.localScale.x, deltaPos.y, deltaPos.z) / Time.deltaTime;
        }

        oldTargetPos = targetPos;
    }

}
