using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(IInputProvider))]
public class PlayerMovement : MonoBehaviour
{
    private const float groundCheckDistance = 1.2f;
    private const float walkSpeed = 2f;
    private const float runSpeed = 3.5f;
    private const float mouseSensitivity = 1.2f;
    private const float jumpForce = 6f;

    private Rigidbody rb;
    private IInputProvider inputProvider;
    private Camera playerCamera;

    private bool isGrounded;
    
    // Outline system variables
    private GameObject currentOutlinedObject;
    private GameObject[] outlineObjects;
    private Material outlineMaterial;
    
    [Header("Outline Settings")]
    private Color outlineColor = Color.white; // White glow
    private float outlineScale = 1.15f; // Scale factor for the outline
    
    // Crosshair system variables
    private GameObject crosshairCanvas;
    private Image crosshairCenterDot; // Center dot (only visible when looking at interactable)
    private Image crosshairOuterCircle; // Outer circle (always visible)
    private bool isLookingAtInteractable = false;
    private const float crosshairDetectionRange = 3f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputProvider = GetComponent<IInputProvider>();
        playerCamera = GetComponentInChildren<Camera>();
        
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize outline material - DISABLED
        // InitializeOutlineMaterial();
        
        // Initialize crosshair
        InitializeCrosshair();
    }

    private void Start()
    {
        // Ensure rigidbody is properly initialized
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Check ground detection on start
        isGrounded = IsGrounded();
    }

    private void Update()
    {
        HandleLook();
        isGrounded = IsGrounded();
        HandleJump();
    }
    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleLook()
    {
        if (inputProvider == null || playerCamera == null) return;
        
        Vector2 lookInput = inputProvider.GetLookInput();
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        float camRotationX = playerCamera.transform.localRotation.eulerAngles.x - mouseY;

        playerCamera.transform.localRotation = Quaternion.Euler(camRotationX, 0f, 0f);
        
        // Handle outline detection for items - DISABLED
        // HandleItemOutline();
        
        // Update crosshair state based on interactable detection
        UpdateCrosshairState();
    }

    private void HandleMovement()
    {
        if (inputProvider == null) return;
        
        Vector2 moveInput = inputProvider.GetMovementInput();
        bool isRunning = inputProvider.IsRunPressed() && moveInput.magnitude > 0;
        
        // Calculate movement direction
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        moveDirection.y = 0; // Keep movement horizontal
        
        // Calculate speed
        float speed = isRunning ? runSpeed : walkSpeed;
        
        // Apply movement
        if (moveInput.magnitude > 0)
        {
            // Simple direct movement
            Vector3 movement = moveDirection * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }
    }


    private bool IsGrounded()
    {
        // More reliable ground check with multiple raycasts
        Vector3 origin = transform.position;
        float checkDistance = groundCheckDistance;
        
        // Check center
        if (Physics.Raycast(origin, Vector3.down, checkDistance))
            return true;
            
        // Check corners for better edge detection
        float offset = 0.3f;
        Vector3[] corners = {
            origin + new Vector3(offset, 0, offset),
            origin + new Vector3(-offset, 0, offset),
            origin + new Vector3(offset, 0, -offset),
            origin + new Vector3(-offset, 0, -offset)
        };
        
        foreach (Vector3 corner in corners)
        {
            if (Physics.Raycast(corner, Vector3.down, checkDistance))
                return true;
        }
        
        return false;
    }

    private void HandleJump()
    {
        if (inputProvider == null) return;
        
        if (inputProvider.IsJumpPressed() && isGrounded)
        {
            Debug.Log("Jumping!");
            
            // Reset vertical velocity before jumping for consistent jump height
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            // Apply jump force
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        else if (inputProvider.IsJumpPressed() && !isGrounded)
        {
            Debug.Log("Cannot jump - not grounded!");
        }
    }
    
    private void InitializeOutlineMaterial()
    {
        // Create outline material using a simple shader that will be used for the outline effect
        Shader outlineShader = Shader.Find("Unlit/Color");
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Standard");
        }
        
        outlineMaterial = new Material(outlineShader);
        
        // Set up for outline rendering
        if (outlineShader.name == "Standard")
        {
            //     opaque rendering mode for solid color
            outlineMaterial.SetFloat("_Mode", 0);
            outlineMaterial.SetInt("_ZWrite", 0);
            outlineMaterial.DisableKeyword("_ALPHATEST_ON");
            outlineMaterial.DisableKeyword("_ALPHABLEND_ON");
            outlineMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            outlineMaterial.renderQueue = 1998; // Render before original
            
            // Set material properties
            outlineMaterial.SetFloat("_Metallic", 0f);
            outlineMaterial.SetFloat("_Smoothness", 0f);
            outlineMaterial.SetColor("_Color", outlineColor); // Use _Color property for Standard shader
        }
        else
        {
            // For Unlit/Color shader, use regular color property
            outlineMaterial.color = outlineColor;
        }
        
        Debug.Log($"Cartoon outline material created with shader: {outlineMaterial.shader.name}");
    }
    
    private void InitializeCrosshair()
    {
        // Create Canvas for crosshair
        crosshairCanvas = new GameObject("CrosshairCanvas");
        Canvas canvas = crosshairCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Ensure it's on top
        
        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = crosshairCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // Add GraphicRaycaster
        crosshairCanvas.AddComponent<GraphicRaycaster>();
        
        // Create outer circle (always visible)
        GameObject outerCircleObj = new GameObject("CrosshairOuterCircle");
        outerCircleObj.transform.SetParent(crosshairCanvas.transform, false);
        
        crosshairOuterCircle = outerCircleObj.AddComponent<Image>();
        crosshairOuterCircle.color = Color.white;
        
        RectTransform outerRect = crosshairOuterCircle.rectTransform;
        outerRect.sizeDelta = new Vector2(48, 48); // Outer circle size
        outerRect.anchorMin = new Vector2(0.5f, 0.5f);
        outerRect.anchorMax = new Vector2(0.5f, 0.5f);
        outerRect.anchoredPosition = Vector2.zero;
        
        CreateCircleTexture(crosshairOuterCircle, 48, true); // Create hollow circle texture
        
        // Create center dot (only visible when looking at interactable)
        GameObject centerDotObj = new GameObject("CrosshairCenterDot");
        centerDotObj.transform.SetParent(crosshairCanvas.transform, false);
        
        crosshairCenterDot = centerDotObj.AddComponent<Image>();
        crosshairCenterDot.color = Color.white;
        
        RectTransform centerRect = crosshairCenterDot.rectTransform;
        centerRect.sizeDelta = new Vector2(8, 8); // Small center dot
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.anchoredPosition = Vector2.zero;
        
        CreateCircleTexture(crosshairCenterDot, 8, false); // Create filled circle texture
        
        // Initially hide the center dot
        crosshairCenterDot.gameObject.SetActive(false);
    }
    
    private void CreateCircleTexture(Image targetImage, int size, bool hollow)
    {
        // Create a circle texture (filled or hollow)
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 2f;
        float innerRadius = hollow ? outerRadius - 3f : 0f; // Hollow circle has inner radius (thicker ring)
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pixelPos = new Vector2(x, y);
                float distance = Vector2.Distance(pixelPos, center);
                
                if (hollow)
                {
                    // Create hollow circle (ring)
                    if (distance <= outerRadius && distance >= innerRadius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    // Create filled circle
                    if (distance <= outerRadius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Create sprite from texture
        Sprite circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        targetImage.sprite = circleSprite;
    }
    
    private void UpdateCrosshairState()
    {
        if (playerCamera == null || crosshairCenterDot == null) return;
        
        // Cast a ray from the camera center to detect interactables
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hasInteractable = false;
        
        if (Physics.Raycast(ray, out RaycastHit hit, crosshairDetectionRange))
        {
            // Check for Item component
            Item item = hit.collider.GetComponent<Item>();
            if (item != null)
            {
                hasInteractable = true;
            }
            
            // Check for CoffeeMachine (need to get held item from Player class)
            CoffeeMachine coffeeMachine = hit.collider.GetComponent<CoffeeMachine>();
            if (coffeeMachine != null && Player.Instance != null && Player.Instance.heldItem is Cup)
            {
                hasInteractable = true;
            }
            
            // Check for Lid on placed cup
            if (Player.Instance != null && Player.Instance.placedCup != null)
            {
                Cup placedCup = Player.Instance.placedCup;
                if (hit.collider.gameObject == placedCup.GetLid())
                {
                    hasInteractable = true;
                }
            }
        }
        
        // Update crosshair visibility based on interactable detection
        if (hasInteractable != isLookingAtInteractable)
        {
            isLookingAtInteractable = hasInteractable;
            crosshairCenterDot.gameObject.SetActive(isLookingAtInteractable);
        }
    }
    
    private void HandleItemOutline()
    {
        // Cast a ray from the camera center
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            // Check if the hit object has an Item component
            Item item = hit.collider.GetComponent<Item>();
            
            if (item != null)
            {
                // If we're looking at a different item, remove previous outline
                if (currentOutlinedObject != hit.collider.gameObject)
                {
                    Debug.Log($"Detected item: {item.name}, applying outline");
                    RemoveOutline();
                    
                    // Apply outline to new item
                    ApplyOutline(hit.collider.gameObject);
                }
            }
            else
            {
                // Not looking at an item, remove outline
                if (currentOutlinedObject != null)
                {
                    Debug.Log("Looking at non-item object, removing outline");
                    RemoveOutline();
                }
            }
        }
        else
        {
            // Not hitting anything, remove outline
            if (currentOutlinedObject != null)
            {
                Debug.Log("Not hitting anything, removing outline");
                RemoveOutline();
            }
        }
    }
    
    private void ApplyOutline(GameObject targetObject)
    {
        if (targetObject == null) 
        {
            Debug.LogWarning("ApplyOutline: targetObject is null");
            return;
        }
        
        if (outlineMaterial == null)
        {
            Debug.LogError("ApplyOutline: outlineMaterial is null! Make sure InitializeOutlineMaterial() was called.");
            return;
        }
        
        // Get all renderers from the object and its children
        Renderer[] allRenderers = targetObject.GetComponentsInChildren<Renderer>();
        
        if (allRenderers == null || allRenderers.Length == 0)
        {
            Debug.LogWarning($"ApplyOutline: No Renderer components found on {targetObject.name} or its children");
            return;
        }
        
        // Store current object
        currentOutlinedObject = targetObject;
        outlineObjects = new GameObject[allRenderers.Length];
        
        // Create simple outline objects for each renderer
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                // Create outline object as child of the renderer
                GameObject outlineObj = new GameObject($"Outline_{allRenderers[i].name}");
                outlineObj.transform.SetParent(allRenderers[i].transform);
                outlineObj.transform.localPosition = Vector3.zero;
                outlineObj.transform.localRotation = Quaternion.identity;
                outlineObj.transform.localScale = Vector3.one * outlineScale; // Configurable scale for visible outline
                
                // Copy mesh filter
                MeshFilter originalMeshFilter = allRenderers[i].GetComponent<MeshFilter>();
                if (originalMeshFilter != null)
                {
                    MeshFilter outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
                    outlineMeshFilter.mesh = originalMeshFilter.mesh;
                }
                
                // Add renderer with outline material
                Renderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
                outlineRenderer.material = outlineMaterial;
                
                // Configure for outline rendering
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                
                // Enable back face culling to make the outline visible as a border
                outlineRenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                
                outlineObjects[i] = outlineObj;
                
                Debug.Log($"Created simple outline object for {allRenderers[i].name}");
            }
        }
        
        Debug.Log($"Applied simple outline to {targetObject.name} with {allRenderers.Length} renderer(s)");
    }
    
    private void RemoveOutline()
    {
        if (outlineObjects != null)
        {
            // Destroy all outline objects
            for (int i = 0; i < outlineObjects.Length; i++)
            {
                if (outlineObjects[i] != null)
                {
                    DestroyImmediate(outlineObjects[i]);
                }
            }
            Debug.Log($"Removed outline from {currentOutlinedObject?.name}, destroyed {outlineObjects.Length} outline object(s)");
        }
        
        // Clear references
        currentOutlinedObject = null;
        outlineObjects = null;
    }
    
    private void OnDestroy()
    {
        // Clean up outline material - DISABLED
        // if (outlineMaterial != null)
        // {
        //     DestroyImmediate(outlineMaterial);
        // }
        
        // Remove any active outline - DISABLED
        // RemoveOutline();
        
        // Clean up crosshair
        if (crosshairCanvas != null)
        {
            DestroyImmediate(crosshairCanvas);
        }
    }
}
