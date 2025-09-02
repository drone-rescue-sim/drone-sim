using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateDroneUI : MonoBehaviour
{
    [ContextMenu("Create Drone UI Setup")]
    public void CreateUI()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("DroneCommandCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Panel for input
        GameObject panelGO = new GameObject("CommandPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 200);
        panelRect.anchoredPosition = new Vector2(0, 0);

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        // Create Input Field
        GameObject inputGO = new GameObject("CommandInput");
        inputGO.transform.SetParent(panelGO.transform, false);
        TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
        RectTransform inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.7f);
        inputRect.anchorMax = new Vector2(0.5f, 0.7f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(350, 40);
        inputRect.anchoredPosition = new Vector2(0, 0);

        // Add background image to input
        Image inputBG = inputGO.AddComponent<Image>();
        inputBG.color = Color.white;

        // Create text component for input
        GameObject inputTextGO = new GameObject("InputText");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        TextMeshProUGUI inputText = inputTextGO.AddComponent<TextMeshProUGUI>();
        RectTransform inputTextRect = inputTextGO.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.sizeDelta = Vector2.zero;
        inputTextRect.anchoredPosition = Vector2.zero;
        inputText.color = Color.black;
        inputText.fontSize = 24;
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.text = "Type drone command here...";

        inputField.textComponent = inputText;

        // Create Send Button
        GameObject buttonGO = new GameObject("SendButton");
        buttonGO.transform.SetParent(panelGO.transform, false);
        Button button = buttonGO.AddComponent<Button>();
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(150, 40);
        buttonRect.anchoredPosition = new Vector2(0, 0);

        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = Color.green;

        // Create button text
        GameObject buttonTextGO = new GameObject("ButtonText");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;
        buttonTextRect.anchoredPosition = Vector2.zero;
        buttonText.color = Color.white;
        buttonText.fontSize = 20;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.text = "Send Command";

        // Create Status Text
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI statusText = statusGO.AddComponent<TextMeshProUGUI>();
        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.2f);
        statusRect.anchorMax = new Vector2(0.5f, 0.2f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(350, 30);
        statusRect.anchoredPosition = new Vector2(0, 0);
        statusText.color = Color.yellow;
        statusText.fontSize = 16;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.text = "Ready to receive commands";

        // Create Help Text
        GameObject helpGO = new GameObject("HelpText");
        helpGO.transform.SetParent(panelGO.transform, false);
        TextMeshProUGUI helpText = helpGO.AddComponent<TextMeshProUGUI>();
        RectTransform helpRect = helpGO.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(0.5f, 0.05f);
        helpRect.anchorMax = new Vector2(0.5f, 0.05f);
        helpRect.pivot = new Vector2(0.5f, 0.5f);
        helpRect.sizeDelta = new Vector2(350, 60);
        helpRect.anchoredPosition = new Vector2(0, 0);
        helpText.color = Color.white;
        helpText.fontSize = 12;
        helpText.alignment = TextAlignmentOptions.Center;
        helpText.text = "Commands: move_forward, move_backward, move_left, move_right,\nascend, descend, turn_left, turn_right\n\nPress Tab to toggle UI";

        // Create DroneCommandUI component
        DroneCommandUI commandUI = canvasGO.AddComponent<DroneCommandUI>();
        commandUI.commandInputField = inputField;
        commandUI.sendButton = button;
        commandUI.statusText = statusText;
        commandUI.inputPanel = panelGO;

        // Add SimpleHTTPServer
        SimpleHTTPServer httpServer = canvasGO.AddComponent<SimpleHTTPServer>();
        httpServer.commandUI = commandUI;

        Debug.Log("Drone UI created! Now assign a drone with PA_DroneController component to DroneCommandUI.droneObject");
    }
}
