// PlayerController.cs
// Handles player movement, jumping, wall sliding, and wall jumping.

using UnityEditor;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
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
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float killbounceforce = 10f;
    #endregion

    #region Private Variables
    private int maxJumps = 1;
    private int jumpsRemaining;
    private bool candash = true;
    private float horizontal;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isWallJumping;
    private bool isCrouched;
    private bool isdashing;
    private float facingDirection = 1f; // 1 for right, -1 for left
    private Vector3 NormalSize = new Vector3(1f, 2f, 1f);
    private Vector3 CrouchSize = new Vector3(1f, 1f, 1f);
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
        if (!isCrouched)
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
    }

    public void crouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            startcrouch();
            isCrouched = true;
        }
        else if (context.canceled && isCrouched)
        {
            stopcrouch();
            isCrouched = false;
        }
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Performdash();
        }
    }
    #endregion

    #region Movement
    private void HandleHorizontalMovement()
    {
        if (isWallJumping|isdashing)
        {
            return; // Skip horizontal movement during wall jump to preserve impulse
        }

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpsRemaining--; // Decrease available jumps
        candash = true; // Allow dashing again after a jump
    }

    private void WallJump()
    {
        isWallJumping = true;
        float jumpDir = -transform.localScale.x; // Direction away from the wall

        rb.linearVelocity = Vector2.zero; // Reset velocity for a clean impulse
        rb.AddForce(new Vector2(wallJumpDirection.x * jumpDir, wallJumpDirection.y) * wallJumpForce, ForceMode2D.Impulse);
        transform.localScale = new Vector3(jumpDir, transform.localScale.y, transform.localScale.z); // Face away from the wall while preserving height

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
            transform.localScale = new Vector3(1, transform.localScale.y, transform.localScale.z); // Face right without changing height
            facingDirection = 1f;
        }
        else if (horizontal < 0f)
        {
            transform.localScale = new Vector3(-1, transform.localScale.y, transform.localScale.z); // Face left without changing height
            facingDirection = -1f;
        }
    }

    private void Slide()
    {
        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, wallSlideSpeed, wallSlideSpeed)); // Limit downward speed
        }
    }

    private void startcrouch()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - 0.5f, transform.localPosition.z); // Move player down to match crouch
            transform.localScale = new Vector3(CrouchSize.x, CrouchSize.y, CrouchSize.z); // Keep facing but do not scale height
        }
    }

    private void stopcrouch()
    {
        if (transform.localScale==NormalSize)
        {
            return; // Already at normal size, no need to adjust
        }
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + 0.5f, transform.localPosition.z); // Move player up to match standing position
            transform.localScale = new Vector3(NormalSize.x, NormalSize.y, NormalSize.z); // Keep facing but do not scale height
        }
    }
    private void Performdash()
    {
        if (!candash || isWallSliding)
        {
            return; // Cannot dash if already dashed or while wall sliding
        }
        isdashing = true;
        rb.AddForce(new Vector2(facingDirection * dashForce, 0), ForceMode2D.Impulse);
        candash = false; // Prevent dashing again until reset
        Invoke(nameof(Stopdash), 0.2f); // Dash lasts for 0.2 seconds
    }

    private void Stopdash()
    {
        isdashing = false;
    }
    #endregion

    #region State Checks
    private void UpdateGroundedState()
    {
        // Check if the player is touching the ground using a capsule-shaped overlap
        isGrounded = Physics2D.OverlapCapsule(
            groundCheck.position,
            new Vector2(2f, 0.4f),
            CapsuleDirection2D.Horizontal,
            0f,
            groundLayer);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps; // Reset jumps when grounded
            candash = true; // Allow dashing again when grounded   
        }
    }

    private void UpdateWallState()
    {
        // Check if the player is touching a wall using a capsule-shaped overlap
        isTouchingWall = Physics2D.OverlapCapsule(
            wallCheck.position,
            new Vector2(0.06f, 2f),
            CapsuleDirection2D.Vertical,
            0f,
            wallLayer);

        isWallSliding = isTouchingWall && !isGrounded && !isCrouched; // Wall sliding only when not grounded
    }
    #endregion

    #region Enemy Interaction
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Killable"))
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
        else if (collision.gameObject.CompareTag("Damageable"))
        {
            // Player takes damage from the enemy
            TakeDamage();
        }
    }

    private void StompEnemy(GameObject enemy)
    {
        // Apply an upward force to the player for a bounce effect
        rb.AddForce(Vector2.up  * killbounceforce, ForceMode2D.Impulse);
        // Destroy the enemy or apply damage to it
        Destroy(enemy);
    }

    private void TakeDamage()
    {
        Destroy(gameObject); // For demonstration, destroy the player on damage
    }
    #endregion
}

