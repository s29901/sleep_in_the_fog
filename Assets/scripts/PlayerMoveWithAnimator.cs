using UnityEngine;

public class PlayerMoveWithAnimator : MonoBehaviour
{
    public float speed = 3f;

    private Animator animator;
    private SpriteRenderer sr;

    void Start()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");

        transform.Translate(Vector2.right * moveX * speed * Time.deltaTime);

        animator.SetFloat("Speed", Mathf.Abs(moveX));

        if (moveX > 0) sr.flipX = false;
        else if (moveX < 0) sr.flipX = true;
    }
}