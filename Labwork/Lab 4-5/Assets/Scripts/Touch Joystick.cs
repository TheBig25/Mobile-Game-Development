using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private bool isDynamic = true;
    [SerializeField] private bool autoHide = true;

    [Header("References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Visual")]
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.3f;

    public Vector2 InputDirection { get; private set; }
    public float InputMagnitude { get; private set; }
    public bool IsActive { get; private set; }

    private RectTransform baseRect;
    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 fixedPosition;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCamera = canvas.worldCamera;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        fixedPosition = background.anchoredPosition;

        if (autoHide)
            canvasGroup.alpha = inactiveAlpha;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsActive = true;

        if (autoHide)
            canvasGroup.alpha = activeAlpha;

        if (isDynamic)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                baseRect, eventData.position, uiCamera, out localPoint);
            background.anchoredPosition = localPoint;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, uiCamera, out localPoint);

        Vector2 clampedInput = localPoint / (background.sizeDelta * 0.5f);

        InputMagnitude = Mathf.Min(clampedInput.magnitude, 1f);

        if (clampedInput.magnitude > 1f)
            clampedInput = clampedInput.normalized;

        InputDirection = clampedInput;

        Vector2 handlePos = clampedInput * handleRange;
        handle.anchoredPosition = handlePos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsActive = false;
        InputDirection = Vector2.zero;
        InputMagnitude = 0f;
        handle.anchoredPosition = Vector2.zero;

        if (isDynamic)
            background.anchoredPosition = fixedPosition;

        if (autoHide)
            canvasGroup.alpha = inactiveAlpha;
    }

    public Vector2 GetInput()
    {
        return InputDirection;
    }

    public float GetHorizontal()
    {
        return InputDirection.x;
    }

    public float GetVertical()
    {
        return InputDirection.y;
    }

    public void SetDynamic(bool dynamic)
    {
        isDynamic = dynamic;
    }

    public void SetHandleRange(float range)
    {
        handleRange = Mathf.Max(10f, range);
    }

    private void OnDisable()
    {
        InputDirection = Vector2.zero;
        InputMagnitude = 0f;
        IsActive = false;
        handle.anchoredPosition = Vector2.zero;

        if (canvasGroup != null)
            canvasGroup.alpha = inactiveAlpha;
    }
}