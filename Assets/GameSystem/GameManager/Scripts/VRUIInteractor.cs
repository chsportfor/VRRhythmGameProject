using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

public class VRUIInteractor : MonoBehaviour
{
    [Header("Aiming Settings")]
    public float maxRayDistance = 10f;
    public LayerMask interactableLayer = ~0; // Raycast against all layers

    [Header("Visuals")]
    [SerializeField] private Color rayColorNormal = new Color(0f, 0.75f, 1f, 0.5f);
    [SerializeField] private Color rayColorHover = new Color(0f, 1f, 0.6f, 0.8f);
    
    private LineRenderer lineRenderer;
    private VRButton currentHoveredButton;
    private bool isPointerActive = true;

    private void Awake()
    {
        // Setup LineRenderer for laser visual
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.useWorldSpace = true;
        
        // Create a simple glowing shader for the laser
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            lineRenderer.material = new Material(lineShader);
        }
        
        lineRenderer.startColor = rayColorNormal;
        lineRenderer.endColor = rayColorNormal;
    }

    public void SetPointerActive(bool active)
    {
        isPointerActive = active;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = active;
        }
        if (!active && currentHoveredButton != null)
        {
            currentHoveredButton.OnHoverExit();
            currentHoveredButton = null;
        }
    }

    private void Update()
    {
        if (!isPointerActive) return;

        // 1. Determine Ray direction based on VR controller anchor or fallback camera
        Ray ray;
        bool isVR = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) || 
                    OVRInput.IsControllerConnected(OVRInput.Controller.Active);

        if (isVR)
        {
            // Raycast forward from controller
            ray = new Ray(transform.position, transform.forward);
        }
        else
        {
            // Editor fallback: Mouse cursor raycast from Main Camera
            if (Camera.main != null)
            {
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            }
            else
            {
                ray = new Ray(transform.position, transform.forward);
            }
        }

        // 2. Perform Raycast
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, maxRayDistance, interactableLayer);
        Vector3 hitPoint = ray.origin + ray.direction * maxRayDistance;
        VRButton hitButton = null;

        if (hasHit)
        {
            hitPoint = hit.point;
            hitButton = hit.collider.GetComponent<VRButton>();
        }

        // 3. Render Laser Line
        if (lineRenderer != null && lineRenderer.enabled)
        {
            // Draw from actual controller/camera transform or ray origin
            Vector3 startPos = isVR ? transform.position : ray.origin + ray.direction * 0.1f;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, hitPoint);
            
            Color activeColor = hitButton != null ? rayColorHover : rayColorNormal;
            lineRenderer.startColor = activeColor;
            lineRenderer.endColor = activeColor;
        }

        // 4. Handle Hover States
        if (hitButton != currentHoveredButton)
        {
            if (currentHoveredButton != null)
            {
                currentHoveredButton.OnHoverExit();
            }

            currentHoveredButton = hitButton;

            if (currentHoveredButton != null)
            {
                currentHoveredButton.OnHoverEnter();
            }
        }

        // 5. Handle Click Trigger
        bool clicked = false;
        
        // VR Controller Click: Right Controller's Index Trigger (방아쇠 트리거)
        if (isVR)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || 
                OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                clicked = true;
            }
        }
        else
        {
            // Keyboard/Mouse Click in Editor
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
        }

        if (clicked && currentHoveredButton != null)
        {
            currentHoveredButton.OnClick();
        }
    }
}

[RequireComponent(typeof(BoxCollider))]
public class VRButton : MonoBehaviour
{
    [Header("Colors")]
    public Color normalColor = new Color(0.12f, 0.18f, 0.32f, 0.8f);
    public Color hoverColor = new Color(0.18f, 0.42f, 0.95f, 0.95f);
    public Color clickColor = new Color(0f, 0.85f, 1f, 1f);

    [Header("Hover Scaling")]
    public Vector3 hoverScaleMultiplier = new Vector3(1.05f, 1.05f, 1f);
    public float animationSpeed = 12f;

    [Header("Events")]
    public UnityEvent onClickEvent = new UnityEvent();

    private UnityEngine.UI.Image backgroundImage;
    private BoxCollider boxCollider;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private bool isHovered = false;

    private void Awake()
    {
        backgroundImage = GetComponent<UnityEngine.UI.Image>();
        boxCollider = GetComponent<BoxCollider>();
        originalScale = transform.localScale;
        targetScale = originalScale;
        targetColor = normalColor;

        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }

        // Auto-configure the BoxCollider size based on RectTransform if it is a UI element
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null && boxCollider != null)
        {
            boxCollider.size = new Vector3(rect.rect.width, rect.rect.height, 10f);
            boxCollider.center = Vector3.zero;
        }
    }

    private void Update()
    {
        // Smoothly animate scale and color
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, Time.deltaTime * animationSpeed);
        }
    }

    public void Setup(Color normColor, Color hvrColor, System.Action clickAction)
    {
        normalColor = normColor;
        hoverColor = hvrColor;
        targetColor = normalColor;
        
        onClickEvent.RemoveAllListeners();
        if (clickAction != null)
        {
            onClickEvent.AddListener(new UnityAction(clickAction));
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    public void OnHoverEnter()
    {
        isHovered = true;
        targetScale = Vector3.Scale(originalScale, hoverScaleMultiplier);
        targetColor = hoverColor;
    }

    public void OnHoverExit()
    {
        isHovered = false;
        targetScale = originalScale;
        targetColor = normalColor;
    }

    public void OnClick()
    {
        StartCoroutine(ClickRoutine());
    }

    private IEnumerator ClickRoutine()
    {
        targetColor = clickColor;
        if (backgroundImage != null) backgroundImage.color = clickColor;
        
        yield return new WaitForSeconds(0.12f);
        
        targetColor = isHovered ? hoverColor : normalColor;
        onClickEvent?.Invoke();
    }
}
