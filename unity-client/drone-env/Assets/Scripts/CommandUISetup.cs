using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically creates and sets up the command input UI elements.
/// This includes the canvas, input field, and buttons for natural language drone control.
/// </summary>
public class CommandUISetup : MonoBehaviour
{
    private CommandInputUI commandInputUI;

    void Start()
    {
        commandInputUI = gameObject.AddComponent<CommandInputUI>();
        CreateSimpleUI();
    }

    private void CreateSimpleUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("CommandCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create panel
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 150);

        // Create input field
        GameObject inputGO = new GameObject("Input");
        inputGO.transform.SetParent(panelGO.transform, false);
        TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
        Image inputImage = inputGO.AddComponent<Image>();
        inputImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject inputTextGO = new GameObject("Text");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        TextMeshProUGUI inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 14;
        inputText.color = Color.white;

        RectTransform inputTextRect = inputTextGO.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.sizeDelta = new Vector2(-20, -10);

        inputField.textComponent = inputText;
        inputField.interactable = true;

        RectTransform inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.4f);
        inputRect.anchorMax = new Vector2(0.9f, 0.8f);

        // Create buttons
        Button sendButton = CreateButton("Send", new Color(0f, 1f, 0f, 1f), panelGO, new Vector2(-100f, -60f));
        Button closeButton = CreateButton("Close", new Color(1f, 0f, 0f, 1f), panelGO, new Vector2(100f, -60f));

        // Assign to CommandInputUI
        commandInputUI.commandCanvas = canvas;
        commandInputUI.commandInput = inputField;
        commandInputUI.sendButton = sendButton;
        commandInputUI.closeButton = closeButton;

        // Setup button listeners
        sendButton.onClick.AddListener(() => commandInputUI.SendCommand());
        closeButton.onClick.AddListener(() => commandInputUI.HideUI());

        // Make buttons interactable
        sendButton.interactable = true;
        closeButton.interactable = true;

        // Start with UI hidden
        canvasGO.SetActive(false);

        // Ensure EventSystem exists
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// Creates a styled button with TextMeshPro text.
    /// </summary>
    /// <param name="text">The text to display on the button</param>
    /// <param name="color">The background color of the button</param>
    /// <param name="parent">The parent GameObject to attach the button to</param>
    /// <param name="position">The anchored position of the button</param>
    /// <returns>The created Button component</returns>
    private Button CreateButton(string text, Color color, GameObject parent, Vector2 position)
    {
        GameObject buttonGO = new GameObject(text + "Button");
        buttonGO.transform.SetParent(parent.transform, false);
        Button button = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(80f, 30f);

        return button;
    }
}
