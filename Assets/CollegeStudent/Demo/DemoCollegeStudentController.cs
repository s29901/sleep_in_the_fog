using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DemoCollegeStudentController : MonoBehaviour
{

    private Vector2 followSpot;
    public float speed = 5f;
    public float persectiveScale;
    public Animator  anim; 
    public SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Start()
    {
        EnsureCursorVisible();
        followSpot = transform.position;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        //followSpot = PlayerMemory.HasSavedPosition ? PlayerMemory.LastPosition : transform.position;

        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /* if (PlayerMemory.HasSavedPosition)
     {
         transform.position = PlayerMemory.LastPosition;
         followSpot = PlayerMemory.LastPosition;
     }
     else
     {
         followSpot = transform.position;
     }
  }*/

    
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -5f;
    public float maxY = 5f;

         // ← не забудь вверху!
         private void EnsureCursorVisible()
         {
             if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
             {
                 Cursor.visible = true;
                 Cursor.lockState = CursorLockMode.None;
             }
         }


    void Update()
    {
        EnsureCursorVisible();
        var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

       /* if (Input.GetMouseButtonDown(0))
        {
            // НЕ ставим точку, если клик по UI
            if (EventSystem.current.IsPointerOverGameObject()) return;

            followSpot = new Vector2(mousePosition.x, mousePosition.y);
        }*/

        Vector2 direction = followSpot - rb.position;

        anim.SetFloat("MoveX", direction.x);
        anim.SetFloat("MoveY", direction.y);
        anim.SetBool("IsMoving", direction.magnitude > 0.1f);

        if (direction.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (direction.x < -0.01f)
            spriteRenderer.flipX = true;
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }



    void FixedUpdate()
    {
        Debug.Log("followSpot: " + followSpot + " | position: " + transform.position);
        float distance = Vector2.Distance(rb.position, followSpot);
        float step = speed * Time.fixedDeltaTime;

        if (distance > step)
        {
            Vector2 direction = (followSpot - rb.position).normalized;
            rb.MovePosition(rb.position + direction * step);
        }
        else
        {
            rb.MovePosition(followSpot);
        }
    }


}

