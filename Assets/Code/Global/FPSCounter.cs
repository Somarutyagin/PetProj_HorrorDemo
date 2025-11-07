using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("FPS Display Settings")]
    public bool showFPS = true;
    public int fontSize = 24;
    public Color textColor = Color.white;
    public Vector2 offset = new Vector2(10, 10); // Offset from top right corner
    
    private float deltaTime = 0.0f;
    private float fps = 0.0f;
    private GUIStyle style;
    
    void Start()
    {
        // Create GUI style for FPS display
        style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
    }

    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;
    }
    
    void OnGUI()
    {
        if (showFPS)
        {
            // Calculate position for top right corner
            string fpsText = $"FPS: {fps:F1}";
            Vector2 textSize = style.CalcSize(new GUIContent(fpsText));
            float x = Screen.width - textSize.x - offset.x;
            float y = offset.y;
            
            // Display FPS on screen
            GUI.Label(new Rect(x, y, textSize.x, textSize.y), fpsText, style);
        }
    }
}
