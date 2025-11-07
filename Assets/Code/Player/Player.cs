using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    private Rigidbody rb;
    public Item heldItem { get; private set; }
    public Cup placedCup { get; private set; }
    private IInputProvider inputProvider;
    private Camera playerCamera;

    [SerializeField] private AudioSource ambientSound;
    // Enhanced pickup system variables
    private const float pickupRange = 3f;
    [SerializeField] private LayerMask itemLayerMask = -1;
    [SerializeField] private LayerMask applianceLayerMask = -1;
    [SerializeField] private LayerMask NPCLayerMask = -1;
    private Transform itemHoldPosition; // Position where held items appear
    
    private GameObject interactionPromptUI;
    private Text interactionPromptText;
    
    [Header("Pickup Animation")]
    public float pickupDelay = 0.5f;
    public AnimationCurve pickupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Item currentTargetItem;
    private NPCLogic currentTargetNPC;
    private bool isPickingUp = false;
    
    // Coffee machine interaction variables
    private CoffeeMachine currentTargetCoffeeMachine;
    private bool isLookingAtLid;
    private bool wasBreawing = false;
    private bool isChase = false;

    private void Awake()
    {
        Instance = this;
        inputProvider = GetComponent<IInputProvider>();
        playerCamera = GetComponentInChildren<Camera>();
        
        if (inputProvider == null)
        {
            Debug.LogError("Player is missing IInputProvider component! Please add InputHandler component.");
        }
        
        // Initialize UI elements
        InitializeInteractionUI();
        
        // Create default hold position if not assigned
        if (itemHoldPosition == null)
        {
            GameObject holdPos = new GameObject("ItemHoldPosition");
            holdPos.transform.SetParent(playerCamera.transform);
            // Position item in left bottom corner of view
            holdPos.transform.localPosition = new Vector3(0.45f, -0.3f, 0.6f);
            // Slight rotation to face player
            holdPos.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            itemHoldPosition = holdPos.transform;
        }
    }

    private void Update()
    {
        if (!isPickingUp)
        {
            HandleItemDetection();
            HandleInteractionInput();
        }
    }
    
    private void HandleItemDetection()
    {
        // Cast ray from camera center forward
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // First check for coffee machine interaction (using appliance layer)
        if (Physics.Raycast(ray, out hit, pickupRange, applianceLayerMask))
        {
            CoffeeMachine coffeeMachine = hit.collider.GetComponent<CoffeeMachine>();
            if (coffeeMachine != null)
            {
                // Check if we can interact with coffee machine
                bool canInteract = false;
                
                if (heldItem is Cup cup)
                {
                    // Can only place cup if coffee machine is empty and cup hasn't been filled
                    if (coffeeMachine.CurrentBrewingCup == null && !cup.HasBeenFilled)
                    {
                        canInteract = true;
                    }
                }
                else if (coffeeMachine.CurrentBrewingCup != null && !coffeeMachine.IsBrewing)
                {
                    // Can pick up brewed cup
                    canInteract = true;
                }
                else if (coffeeMachine.IsBrewing)
                {
                    // Show brewing status
                    wasBreawing = true;
                    canInteract = true;
                }
                
                if (canInteract && coffeeMachine != currentTargetCoffeeMachine)
                {
                    currentTargetCoffeeMachine = coffeeMachine;
                    currentTargetItem = null; // Clear item target
                    ShowCoffeeMachinePrompt(coffeeMachine);
                }
                else if (canInteract && wasBreawing && !coffeeMachine.IsBrewing)
                {
                    wasBreawing = false;
                    ShowCoffeeMachinePrompt(coffeeMachine);
                }
                return;
            }
        }
        
        // If we were looking at a coffee machine but now we're not, clear it
        if (currentTargetCoffeeMachine != null)
        {
            currentTargetCoffeeMachine = null;
            HidePrompt();
        }

        if (Physics.Raycast(ray, out hit, pickupRange, NPCLayerMask))
        {
            // Check for lid interaction on placed cup
            if (heldItem is Cup cup && cup.HasBeenFilled)
            {
                NPCLogic npc = hit.collider.GetComponent<NPCLogic>();
                currentTargetNPC = npc;
                ShowNPCPrompt();

                return;
            }
        }

        // If we were looking at a NPC but now we're not, clear it
        if (currentTargetNPC != null)
        {
            currentTargetNPC = null;
            HidePrompt();
        }

        // Then check for regular items (using item layer)
        if (Physics.Raycast(ray, out hit, pickupRange, itemLayerMask))
        {
            // Check for lid interaction on placed cup
            if (placedCup != null && hit.collider.gameObject == placedCup.GetLid())
            {
                isLookingAtLid = true;
                currentTargetItem = null;
                currentTargetCoffeeMachine = null;
                ShowLidPrompt();
                return;
            }
            
            // Regular item detection
            Item item = hit.collider.GetComponent<Item>();
            
            if (item != null && item != currentTargetItem)
            {
                // New item detected
                currentTargetItem = item;
                currentTargetCoffeeMachine = null;
                isLookingAtLid = false;
                ShowInteractionPrompt(item);
            }
        }
        else
        {
            // No object in range - clear all targets
            if (currentTargetItem != null || currentTargetCoffeeMachine != null || isLookingAtLid)
            {
                currentTargetItem = null;
                currentTargetCoffeeMachine = null;
                isLookingAtLid = false;
                HidePrompt();
            }
        }
    }
    
    private void HandleInteractionInput()
    {
        if (inputProvider == null) return;
        
        if (inputProvider.IsInteractionPressed())
        {
            if (currentTargetCoffeeMachine != null)
            {
                // Call the coffee machine's interact method
                currentTargetCoffeeMachine.Interact();
                
                // Clear targets and hide prompt
                currentTargetCoffeeMachine = null;
                HidePrompt();
            }
            else if (isLookingAtLid && placedCup != null)
            {
                // Remove lid from placed cup
                RemoveLidFromCup(placedCup);
            }
            else if (currentTargetNPC != null)
            {
                //throw at a cup at NPC
                ThrowItem();
                currentTargetNPC.StartChase();
                ShowRunPrompt();
                isChase = true;

                ambientSound.Stop();
            }
            else if (currentTargetItem != null)
            {
                // Regular item pickup
                StartCoroutine(PickUpItemCoroutine(currentTargetItem));
            }
        }
    }

    public void PickUpItem(Item item)
    {
        // Legacy method for compatibility - now calls the enhanced version
        StartCoroutine(PickUpItemCoroutine(item));
    }
    
    public IEnumerator PickUpItemCoroutine(Item item)
    {
        if (isPickingUp || item == null) yield break;
        
        isPickingUp = true;
        HidePrompt();
        
        // Disable physics on the item
        Rigidbody itemRb = item.GetComponent<Rigidbody>();
        Collider itemCollider = item.GetComponent<Collider>();
        
        if (itemRb != null) itemRb.isKinematic = true;
        if (itemCollider != null) itemCollider.enabled = false;
        
        // Store original position and rotation
        Vector3 startPosition = item.transform.position;
        Quaternion startRotation = item.transform.rotation;
        Vector3 targetPosition = itemHoldPosition.position;
        Quaternion targetRotation = itemHoldPosition.rotation;
        
        // Animate pickup
        float elapsedTime = 0f;
        while (elapsedTime < pickupDelay)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / pickupDelay;
            float curveValue = pickupCurve.Evaluate(progress);
            
            item.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            item.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, curveValue);
            
            yield return null;
        }
        
        // Final positioning
        item.transform.position = targetPosition;
        item.transform.rotation = targetRotation;
        item.transform.SetParent(itemHoldPosition);
        
        // Reset local transform to ensure item is exactly at hold position
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        
        // Set as held item
        heldItem = item;
        currentTargetItem = null;
        
        isPickingUp = false;
    }

    private void ThrowItem()
    {
        if (heldItem != null)
        {
            // Re-enable physics
            Rigidbody itemRb = heldItem.GetComponent<Rigidbody>();
            Collider itemCollider = heldItem.GetComponent<Collider>();

            if (itemRb != null) itemRb.isKinematic = false;
            if (itemCollider != null) itemCollider.enabled = true;

            // Detach from player/NPC
            heldItem.transform.SetParent(null);

            // Apply throw force in the forward direction
            if (itemRb != null)
            {
                itemRb.AddForce(transform.forward * 10f, ForceMode.Impulse);
            }

            heldItem = null;
        }
    }

    private void InitializeInteractionUI()
    {
        // Create interaction prompt UI if not assigned
        if (interactionPromptUI == null)
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("InteractionCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // Higher than crosshair
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Create prompt panel
            GameObject promptPanel = new GameObject("InteractionPrompt");
            promptPanel.transform.SetParent(canvasObj.transform, false);
            
            Image panelImage = promptPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            
            RectTransform panelRect = promptPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.1f);
            panelRect.anchorMax = new Vector2(0.5f, 0.1f);
            panelRect.sizeDelta = new Vector2(300, 50);
            panelRect.anchoredPosition = Vector2.zero;
            
            // Create text
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(promptPanel.transform, false);
            
            interactionPromptText = textObj.AddComponent<Text>();
            interactionPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            interactionPromptText.fontSize = 16;
            interactionPromptText.color = Color.white;
            interactionPromptText.alignment = TextAnchor.MiddleCenter;
            interactionPromptText.text = "Press E to pick up";
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            interactionPromptUI = promptPanel;
        }
        
        // Hide initially
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }
    
    private void ShowInteractionPrompt(Item item)
    {
        ShowPrompt("Press E to pick up", item);
    }
    private void ShowLidPrompt()
    {
        ShowPrompt("Press E to remove lid");
    }
    private void ShowNPCPrompt()
    {
        ShowPrompt("Press E to throw the cup");
    }
    private void ShowRunPrompt()
    {
        ShowPrompt("Press shift to run");
    }
    private void ShowPrompt(string text, Item item = null)
    {
        if (interactionPromptUI != null && interactionPromptText != null)
        {
            interactionPromptText.text = item == null ? text : $"text {item.name}";
            interactionPromptUI.SetActive(true);
        }
    }
    private void HidePrompt()
    {
        if (interactionPromptUI != null && !isChase)
        {
            interactionPromptUI.SetActive(false);
        }
    }
    
    private void ShowCoffeeMachinePrompt(CoffeeMachine coffeeMachine)
    {
        if (interactionPromptUI != null && interactionPromptText != null)
        {
            if (heldItem is Cup cup)
            {
                if (coffeeMachine.CurrentBrewingCup == null && !cup.HasBeenFilled)
                {
                    interactionPromptText.text = "Press E to place cup on coffee machine";
                }
                else if (cup.HasBeenFilled)
                {
                    interactionPromptText.text = "This cup already has coffee";
                }
                else
                {
                    interactionPromptText.text = "Coffee machine is occupied";
                }
            }
            else if (coffeeMachine.CurrentBrewingCup != null && !coffeeMachine.IsBrewing)
            {
                interactionPromptText.text = "Press E to pick up brewed cup";
            }
            else if (coffeeMachine.IsBrewing)
            {
                interactionPromptText.text = "Coffee is brewing...";
            }
            interactionPromptUI.SetActive(true);
        }
    }

    private void RemoveLidFromCup(Cup cup)
    {
        GameObject lid = cup.GetLid();
        if (lid == null)
        {
            Debug.LogError("Cup doesn't have a lid assigned!");
            return;
        }
        
        // Detach lid from cup
        lid.transform.SetParent(null);
        
        // Add Lid component so it can be picked up
        if (lid.GetComponent<Lid>() == null)
        {
            lid.AddComponent<Lid>();
        }
        
        // Enable physics on lid
        Rigidbody lidRb = lid.GetComponent<Rigidbody>();
        Collider lidCollider = lid.GetComponent<Collider>();
        
        if (lidRb != null) lidRb.isKinematic = false;
        if (lidCollider != null) lidCollider.enabled = true;
        
        // Position lid slightly above cup
        lid.transform.position = cup.transform.position + Vector3.up * 0.2f;
        
        // Clear targets and hide prompt
        isLookingAtLid = false;
        HidePrompt();
    }
    
    public void DropItem()
    {
        if (heldItem != null)
        {
            // Re-enable physics
            Rigidbody itemRb = heldItem.GetComponent<Rigidbody>();
            Collider itemCollider = heldItem.GetComponent<Collider>();
            
            if (itemRb != null) itemRb.isKinematic = false;
            if (itemCollider != null) itemCollider.enabled = true;
            
            // Detach from player
            heldItem.transform.SetParent(null);
            
            // Drop in front of player
            Vector3 dropPosition = transform.position + transform.forward * 1f;
            heldItem.transform.position = dropPosition;
            
            heldItem = null;
        }
    }
    
    // Public methods for external classes to interact with player state
    public void ClearHeldItem()
    {
        heldItem = null;
    }
    
    public void SetPlacedCup(Cup cup)
    {
        placedCup = cup;
    }
}
