using UnityEngine;
using System;

/// <summary>
/// Detects swipe gestures on mobile (touch) and keyboard input for desktop testing.
/// Fires events for lane changes, jump, and roll.
/// </summary>
public class SwipeInput : MonoBehaviour
{
    public static SwipeInput Instance { get; private set; }

    public event Action OnSwipeLeft;
    public event Action OnSwipeRight;
    public event Action OnSwipeUp;
    public event Action OnSwipeDown;
    public event Action OnDoubleTap;

    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isSwiping;

    private float lastTapTime;
    private const float DoubleTapWindow = 0.3f;

    private RunnerTuning tuning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        tuning = GameManager.Instance.Tuning;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        HandleTouchInput();
        HandleKeyboardInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchStartPos = touch.position;
                touchStartTime = Time.time;
                isSwiping = true;
                break;

            case TouchPhase.Ended:
                if (!isSwiping) break;
                isSwiping = false;

                float duration = Time.time - touchStartTime;
                if (duration > tuning.swipeMaxTime)
                    break;

                Vector2 delta = touch.position - touchStartPos;
                float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
                float normalizedDistance = delta.magnitude / screenDiagonal;

                if (normalizedDistance < tuning.swipeThreshold)
                {
                    // It's a tap, check for double tap
                    if (Time.time - lastTapTime < DoubleTapWindow)
                    {
                        OnDoubleTap?.Invoke();
                        lastTapTime = 0f;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                    }
                    break;
                }

                // Determine swipe direction
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    // Horizontal swipe
                    if (delta.x > 0)
                        OnSwipeRight?.Invoke();
                    else
                        OnSwipeLeft?.Invoke();
                }
                else
                {
                    // Vertical swipe
                    if (delta.y > 0)
                        OnSwipeUp?.Invoke();
                    else
                        OnSwipeDown?.Invoke();
                }
                break;

            case TouchPhase.Canceled:
                isSwiping = false;
                break;
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            OnSwipeLeft?.Invoke();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            OnSwipeRight?.Invoke();
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            OnSwipeUp?.Invoke();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            OnSwipeDown?.Invoke();
        if (Input.GetKeyDown(KeyCode.E))
            OnDoubleTap?.Invoke();
    }
}
