using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

public class Lesser_Soul_AI : MonoBehaviour
{
    [SerializeField] private Transform GroundCheck;
    [SerializeField] private LayerMask GroundLayer;
    [SerializeField] private Transform WallCheck;
    [SerializeField] private LayerMask WallLayer;
    [SerializeField] private float Movespeed = 2f;
    private Rigidbody2D rb;
    private int direction = 1;
    public bool CanRoam;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Roam();
        CanRoam = Physics2D.OverlapBox(GroundCheck.position, new Vector2(0.1f, 0.1f), 0f, GroundLayer)&&!Physics2D.OverlapBox(WallCheck.position, new Vector2(0.1f, 0.1f), 0f, WallLayer);
    }
    void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        direction *= -1;
        CanRoam = true; // Prevent immediate roaming after flipping
    }

    void Roam()
    {
    if(CanRoam)
    {
        rb.linearVelocity = new Vector2(Movespeed * direction, rb.linearVelocity.y);
    }
    if(!CanRoam)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        Flip();
    }
    }
}
