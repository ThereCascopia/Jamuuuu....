using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlmanacItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // References to required components
    private RectTransform rectTransform;
    private Image iconImage;
    private Text itemNameText;

    // The parent almanac system
    private AlmanacSystem almanacSystem;

    // Animation properties
    private Vector3 originalScale;
    private float hoverScale = 1.05f;
    private float clickScale = 0.95f;
    private float animationSpeed = 8f;
    private bool isAnimating = false;

    // Color properties
    private Color originalColor;
    private Color hoverColor = new Color(1f, 0.9f, 0.7f);

    // Item data reference
    private AlmanacSystem.AlmanacItemData itemData;

    // Debug properties
    [SerializeField] private bool debugMode = false;

    void Awake()
    {
        // Get components
        rectTransform = GetComponent<RectTransform>();

        // Find the icon image (handles both "Icon" and "imgcard" naming)
        iconImage = transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = transform.Find("imgcard")?.GetComponent<Image>();
        }

        // Find the name text (handles both "Nama" and other text components)
        itemNameText = transform.Find("Nama")?.GetComponent<Text>();
        if (itemNameText == null)
        {
            // Try to find any Text component as a fallback
            itemNameText = GetComponentInChildren<Text>();
        }

        // Get reference to AlmanacSystem
        almanacSystem = FindAnyObjectByType<AlmanacSystem>();

        // Store original properties
        originalScale = rectTransform.localScale;
        if (iconImage != null)
        {
            originalColor = iconImage.color;
        }

        // Add shadow to text if it doesn't have one
        if (itemNameText != null && itemNameText.GetComponent<Shadow>() == null)
        {
            Shadow shadow = itemNameText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        // Ensure button is properly configured for clicks
        EnsureButtonIsClickable();
    }

    // Ensure this object can be clicked
    private void EnsureButtonIsClickable()
    {
        // Make sure we have a Button component (Unity UI Button)
        Button button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            if (debugMode) Debug.Log($"Added Button component to {gameObject.name}");
        }

        // Make sure button has a target graphic
        if (button.targetGraphic == null)
        {
            if (iconImage != null)
            {
                button.targetGraphic = iconImage;
            }
            else
            {
                Image image = GetComponentInChildren<Image>();
                if (image != null)
                {
                    button.targetGraphic = image;
                }
            }

            if (debugMode) Debug.Log($"Set Button target graphic for {gameObject.name}");
        }

        // Ensure all Image components are raycast targets
        Image[] images = GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (!img.raycastTarget)
            {
                img.raycastTarget = true;
                if (debugMode) Debug.Log($"Enabled raycast target on {img.gameObject.name}");
            }
        }

        // Add Box Collider if needed for physics raycasts
        if (GetComponent<BoxCollider2D>() == null && GetComponent<BoxCollider>() == null)
        {
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.size = rectTransform.sizeDelta;
            if (debugMode) Debug.Log($"Added BoxCollider2D to {gameObject.name}");
        }
    }

    // Set the item data for this button
    public void SetItemData(AlmanacSystem.AlmanacItemData data)
    {
        this.itemData = data;

        // Update visuals
        if (iconImage != null && data.gambar != null)
        {
            iconImage.sprite = data.gambar;
            iconImage.preserveAspect = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text = data.nama;
        }

        // Make sure the button is clickable after setting data
        EnsureButtonIsClickable();

        if (debugMode) Debug.Log($"Set item data: {data.nama} and ensured button is clickable");
    }

    // Mouse hover enter effect
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (debugMode) Debug.Log($"Pointer entered: {gameObject.name}");

        // Scale effect
        StopAllCoroutines();
        StartCoroutine(ScaleAnimation(hoverScale));

        // Color effect (if we have an image)
        if (iconImage != null)
        {
            iconImage.color = hoverColor;
        }

        // Play sound effect
        PlayHoverSound();
    }

    // Mouse hover exit effect
    public void OnPointerExit(PointerEventData eventData)
    {
        if (debugMode) Debug.Log($"Pointer exited: {gameObject.name}");

        // Scale effect
        StopAllCoroutines();
        StartCoroutine(ScaleAnimation(1.0f));

        // Color effect (if we have an image)
        if (iconImage != null)
        {
            iconImage.color = originalColor;
        }
    }

    // Click effect
    public void OnPointerClick(PointerEventData eventData)
    {
        if (debugMode) Debug.Log($"Button clicked: {gameObject.name}");

        // Quick down-up animation
        StopAllCoroutines();
        StartCoroutine(ClickAnimation());

        // Play click sound
        PlayClickSound();

        // Show item details if we have itemData and AlmanacSystem
        if (itemData != null && almanacSystem != null)
        {
            almanacSystem.ShowItemDetail(itemData);
        }
        else
        {
            if (debugMode)
            {
                if (itemData == null) Debug.LogWarning($"Item data is null for {gameObject.name}");
                if (almanacSystem == null) Debug.LogWarning("AlmanacSystem reference is null");
            }
        }
    }

    // Smooth scale animation
    private System.Collections.IEnumerator ScaleAnimation(float targetScale)
    {
        isAnimating = true;
        Vector3 startScale = rectTransform.localScale;
        Vector3 endScale = originalScale * targetScale;
        float elapsedTime = 0f;
        float duration = 0.15f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration);
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        rectTransform.localScale = endScale;
        isAnimating = false;
    }

    // Quick click animation
    private System.Collections.IEnumerator ClickAnimation()
    {
        isAnimating = true;

        // First scale down
        Vector3 startScale = rectTransform.localScale;
        Vector3 clickedScale = originalScale * clickScale;
        float elapsedTime = 0;
        float clickDuration = 0.1f;

        while (elapsedTime < clickDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / clickDuration);
            rectTransform.localScale = Vector3.Lerp(startScale, clickedScale, t);
            yield return null;
        }

        // Then scale back up to hover scale
        startScale = rectTransform.localScale;
        Vector3 endScale = originalScale * hoverScale;
        elapsedTime = 0;

        while (elapsedTime < clickDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / clickDuration);
            rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        rectTransform.localScale = endScale;
        isAnimating = false;
    }

    // Sound effects (requires AudioSource on this object or parent)
    private void PlayHoverSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.volume = 0.5f;
            audioSource.pitch = 1.2f;
            audioSource.Play();
        }
    }

    private void PlayClickSound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.volume = 0.7f;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }
    }
}