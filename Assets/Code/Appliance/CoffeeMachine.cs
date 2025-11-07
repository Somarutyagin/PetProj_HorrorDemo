using UnityEngine;
public class CoffeeMachine : Appliance
{
    private AudioSource audioSource;
    [SerializeField] public Transform cupStandPosition; // Position where cup will be placed
    private Cup currentBrewingCup;
    private bool isBrewing = false;
    private Vector3 originalLidPosition;
    private Quaternion originalLidRotation;
    private Transform originalLidParent;
    private int originalLidSiblingIndex;
    
    // Public properties for external access
    public Cup CurrentBrewingCup => currentBrewingCup;
    public bool IsBrewing => isBrewing;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    public override void Interact()
    {
        if (Player.Instance.heldItem is Cup cup)
        {
            // Only place cup if coffee machine is empty and cup hasn't been filled before
            if (currentBrewingCup == null && !cup.HasBeenFilled)
            {
                // Place cup on coffee machine
                PlaceCupOnCoffeeMachine(cup);
                StartBrewing();
            }
        }
        else if (currentBrewingCup != null && !isBrewing)
        {
            // Cup is ready, pick it up with lid
            PickUpBrewedCup();
        }
    }
    
    private void StartBrewing()
    {
        if (currentBrewingCup != null && !isBrewing)
        {
            isBrewing = true;
            audioSource.Play();
            
            // Start coroutine to handle brewing completion
            StartCoroutine(HandleBrewingCompletion());
        }
    }
    
    private System.Collections.IEnumerator HandleBrewingCompletion()
    {
        // Wait for audio to finish
        yield return new WaitForSeconds(audioSource.clip.length);
        
        // Mark cup as filled with coffee
        if (currentBrewingCup != null)
        {
            currentBrewingCup.MarkAsFilled();
        }
        
        // Brewing is complete
        isBrewing = false;
        // Cup is now ready to be picked up
    }
    
    private void PickUpBrewedCup()
    {
        if (currentBrewingCup != null)
        {
            // Reconnect cup with lid and pick up
            Cup cup = currentBrewingCup;
            currentBrewingCup = null;
            
            // Clear placed cup reference
            Player.Instance.SetPlacedCup(null);
            
            // Reconnect with lid (if player has lid)
            if (Player.Instance.heldItem is Lid lid)
            {
                // Remove lid from player's hands
                Player.Instance.ClearHeldItem();
                
                // Reconnect lid to cup with original position, rotation, and complete hierarchy
                lid.transform.SetParent(originalLidParent);
                lid.transform.localPosition = originalLidPosition;
                lid.transform.localRotation = originalLidRotation;
                lid.transform.SetSiblingIndex(originalLidSiblingIndex);
                
                // Disable lid physics
                Rigidbody lidRb = lid.GetComponent<Rigidbody>();
                Collider lidCollider = lid.GetComponent<Collider>();
                if (lidRb != null) lidRb.isKinematic = true;
                if (lidCollider != null) lidCollider.enabled = false;
            }
            
            // Pick up the cup
            Player.Instance.StartCoroutine(Player.Instance.PickUpItemCoroutine(cup));
        }
    }
    
    private void PlaceCupOnCoffeeMachine(Cup cup)
    {
        if (cupStandPosition == null)
        {
            Debug.LogError("Coffee machine doesn't have a cup stand position assigned!");
            return;
        }
        
        // Remove cup from player's hands
        Player.Instance.ClearHeldItem();
        
        // Position cup on coffee machine
        cup.transform.SetParent(transform);
        cup.transform.position = cupStandPosition.position;
        cup.transform.rotation = cupStandPosition.rotation;
        
        // Disable physics on cup
        Rigidbody cupRb = cup.GetComponent<Rigidbody>();
        Collider cupCollider = cup.GetComponent<Collider>();
        
        if (cupRb != null) cupRb.isKinematic = true;
        if (cupCollider != null) cupCollider.enabled = false;
        
        // Store reference to placed cup
        Player.Instance.SetPlacedCup(cup);
        currentBrewingCup = cup;
        
        // Automatically pick up the lid from the cup
        PickUpLidFromCup(cup);
    }
    
    private void PickUpLidFromCup(Cup cup)
    {
        GameObject lid = cup.GetLid();
        if (lid == null)
        {
            Debug.LogError("Cup doesn't have a lid assigned!");
            return;
        }
        
        // Store original lid position, rotation, and complete hierarchy information
        originalLidPosition = lid.transform.localPosition;
        originalLidRotation = lid.transform.localRotation;
        originalLidParent = lid.transform.parent;
        originalLidSiblingIndex = lid.transform.GetSiblingIndex();
        
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
        
        // Automatically pick up the lid
        Player.Instance.StartCoroutine(Player.Instance.PickUpItemCoroutine(lid.GetComponent<Lid>()));
    }
}