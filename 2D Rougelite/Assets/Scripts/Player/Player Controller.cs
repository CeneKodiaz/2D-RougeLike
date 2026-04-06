// PlayerController.cs
// Handles player movement, jumping, wall sliding, and wall jumping.

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    #region Inspector Variables
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private int wallJumpForce = 4;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(2, 1.5f);
    [SerializeField] private float wallJumpDuration = 0.2f;
    [SerializeField] private float wallSlideSpeed = -2f;
    #endregion

    #region Private Variables
    private int maxJumps = 1;
    private int jumpsRemaining;
    private float horizontal;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    #endregion

    #region Update Loops
    private void FixedUpdate()
    {
        HandleHorizontalMovement();
        Slide();
    }

    private void Update()
    {
        UpdateGroundedState();
        UpdateWallState();
    }
    #endregion

    #region Input Functions
    public void Move(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
        UpdateFacing();
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started && jumpsRemaining > 0)
        {
            Jump();
        }

        if (isWallSliding && context.started)
        {
            WallJump();
        }
    }
    #endregion

    #region Movement
    private void HandleHorizontalMovement()
    {
        if (isWallJumping)
        {
            return; // Skip horizontal movement during wall jump to preserve impulse
        }

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpsRemaining--; // Decrease available jumps
    }

    private void WallJump()
    {
        isWallJumping = true;
        float jumpDir = -transform.localScale.x; // Direction away from the wall

        rb.linearVelocity = Vector2.zero; // Reset velocity for a clean impulse
        rb.AddForce(new Vector2(wallJumpDirection.x * jumpDir, wallJumpDirection.y) * wallJumpForce, ForceMode2D.Impulse);
        transform.localScale = new Vector3(jumpDir, 1, 1); // Face away from the wall

        Invoke(nameof(StopWallJump), wallJumpDuration); // Schedule end of wall jump
    }

    private void StopWallJump()
    {
        isWallJumping = false;
        UpdateFacing(); // Restore facing based on current input
    }

    private void UpdateFacing()
    {
        if (horizontal > 0f)
        {
            transform.localScale = Vector3.one; // Face right
        }
        else if (horizontal < 0f)
        {
            transform.localScale = new Vector3(-1, 1, 1); // Face left
        }
    }

    private void Slide()
    {
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, wallSlideSpeed, wallSlideSpeed)); // Limit downward speed
        }
    }
    #endregion

    #region State Checks
    private void UpdateGroundedState()
    {
        // Check if the player is touching the ground using a capsule-shaped overlap
        isGrounded = Physics2D.OverlapCapsule(
            groundCheck.position,
            new Vector2(1f, 0.4f),
            CapsuleDirection2D.Horizontal,
            0f,
            groundLayer);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps; // Reset jumps when grounded
        }
    }

    private void UpdateWallState()
    {
        // Check if the player is touching a wall using a capsule-shaped overlap
        isTouchingWall = Physics2D.OverlapCapsule(
            wallCheck.position,
            new Vector2(0.06f, 1f),
            CapsuleDirection2D.Vertical,
            0f,
            wallLayer);

        isWallSliding = isTouchingWall && !isGrounded; // Wall sliding only when not grounded
    }
    #endregion

    #region Enemy Interaction
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Evil"))
        {
            // Check if the player is above the enemy's top
            float enemyTop = collision.transform.position.y + collision.collider.bounds.size.y / 2;
            if (transform.position.y > enemyTop)
            {
                // Player is above the enemy, perform a stomp
                StompEnemy(collision.gameObject);
            }
            else
            {
                // Player is below or at the same level as the enemy, take damage
                TakeDamage();
            }
        }
    }

    private void StompEnemy(GameObject enemy)
    {
        // Apply an upward force to the player for a bounce effect
        rb.AddForce(Vector2.up * jumpForce * 2, ForceMode2D.Impulse);
        // Destroy the enemy or apply damage to it
        Destroy(enemy);
    }

    private void TakeDamage()
    {
        Destroy(gameObject); // For demonstration, destroy the player on damage
    }
    #endregion
}

