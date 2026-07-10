using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]private bool isMoving = false;
    [SerializeField]private float cellSize;
    [SerializeField]private float moveSpeed = 25f;
    [SerializeField]private LayerMask wallLayer;
    [SerializeField]private Rigidbody2D rb;
    [SerializeField]private AudioSource audioSource;
    [SerializeField]private AudioClip moveAudio;
    [SerializeField]private AudioClip collisionAudio;

    private float raycastLength;
    private Vector2 moveInput;
    private CinemachineImpulseSource impulseSource;

    public float swipeThreshold = 50f;
    // Internal touch tracking variables
    private Vector2 touchStartPosition;
    private bool isTouching = false;

    private void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        raycastLength = cellSize;
    }
    void Update()
    {
        if(isMoving || moveInput == Vector2.zero) return;   
        if(moveInput == Vector2.up ||  moveInput == Vector2.down || moveInput == Vector2.left || moveInput == Vector2.right)
        {
            if (Physics2D.Raycast(transform.position, moveInput, raycastLength, wallLayer)) {
                isMoving = false;
                moveInput = Vector2.zero; 
            }
            else
            {
                StartCoroutine(MoveCoroutine());
            }
        }
    }

    private void OnMove(InputValue inputValue)
    {
        if (!isMoving)
        {
            moveInput = inputValue.Get<Vector2>();
        }
    }

    private void OnPrimaryContact(InputValue value)
    {
        if (isMoving) return;

        bool isPressed = value.isPressed;

        if (isPressed)
        {
            // Finger touched the screen: record initial point
            touchStartPosition = GetCurrentPointerPosition();
            isTouching = true;
        }
        else if (isTouching)
        {
            // Finger lifted off the screen: calculate swipe delta
            Vector2 touchEndPosition = GetCurrentPointerPosition();
            Vector2 swipeDelta = touchEndPosition - touchStartPosition;

            // Verify if the movement met your minimum swipe distance
            if (swipeDelta.magnitude >= swipeThreshold)
            {
                moveInput = ConvertDeltaToCardinalDirection(swipeDelta);
            }

            isTouching = false;
        }
    }

    private Vector2 GetCurrentPointerPosition()
    {
        // Safely fetches current position from the active screen pointer
        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    private Vector2 ConvertDeltaToCardinalDirection(Vector2 delta)
    {
        // Compare horizontal distance vs vertical distance to find dominant direction
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            return delta.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return delta.y > 0 ? Vector2.up : Vector2.down;
        }
    }
    private IEnumerator MoveCoroutine()
    {
        isMoving = true;
        GameManager.Instance.AddSwipes();
        PlaySound(moveAudio, 0.3f);
        while(!Physics2D.Raycast(transform.position, moveInput, raycastLength, wallLayer))
        {
            Vector2 targetPos = rb.position + (moveInput * cellSize);
            while (Vector2.Distance(transform.position, targetPos) > 0.001f)
            {
                Vector2 nextStep = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                rb.MovePosition(nextStep);
                yield return null;
            }
            rb.MovePosition(targetPos);
        }
        CameraShakeManager.Instance.CameraShake(impulseSource);
        PlaySound(collisionAudio, 0.5f);
        isMoving = false;
        moveInput = Vector2.zero;
    }

    private void PlaySound(AudioClip audioClip, float volume)
    {
        audioSource.pitch = Random.Range(0.7f, 1);
        audioSource.PlayOneShot(audioClip, volume);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Path"))
        {
            // Grab the PathTile component and paint it
            PathTile tile = collision.GetComponent<PathTile>();
            if (tile != null)
            {
                tile.Paint();
            }
        }
    }
}
