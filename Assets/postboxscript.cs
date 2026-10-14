
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;

public class postboxscript : MonoBehaviour
{
    [Header("PIN Settings")]
    public string correctPin = "1234";
    public int maxPinLength = 4;

    [Header("Keypad Appearance")]
    public Vector2 panelSize = new Vector2(320, 440);

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private Canvas canvas;
    private Text pinDisplay;
    private string enteredPin = "";
    private bool keypadOpen = false;

    private Font uiFont;

    private void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError(
                "[PostBox] Add a Collider to the post box."
            );
        }

        if (Camera.main == null)
        {
            Debug.LogError(
                "[PostBox] No MainCamera found. Tag your camera MainCamera."
            );
        }

        CreateKeypad();

        if (canvas != null)
            canvas.gameObject.SetActive(false);

        Log("Ready. Click the post box to open the keypad.");
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        // Don't interpret UI button clicks as clicks on the 3D box.
        if (keypadOpen)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(
            mouse.position.ReadValue()
        );

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                OpenKeypad();
            }
        }
    }

    private void CreateKeypad()
    {
        // Ensure a UI EventSystem exists.
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("Generated EventSystem");

            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            Log("Created EventSystem with Input System UI module.");
        }

        // Create Canvas.
        GameObject canvasObj = new GameObject(
            "Generated PIN Keypad",
            typeof(RectTransform)
        );

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Built-in Unity UI font.
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Create background panel.
        GameObject panel = CreateUIObject(
            "Keypad Panel", canvas.transform
        );

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.10f, 0.12f, 0.16f, 0.98f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;

        // Title and PIN display.
        CreateLabel(
            "POST BOX", panel.transform,
            new Vector2(0, 175), 26, Color.white
        );

        pinDisplay = CreateLabel(
            "", panel.transform,
            new Vector2(0, 125), 30, Color.green
        );

        // Digit buttons arranged in a 3-column grid.
        for (int i = 1; i <= 9; i++)
        {
            int digit = i;
            int index = i - 1;

            float x = (index % 3 - 1) * 85;
            float y = 65 - (index / 3) * 65;

            CreateButton(
                digit.ToString(),
                panel.transform,
                new Vector2(x, y),
                () => PressDigit(digit)
            );
        }

        // Bottom row.
        CreateButton(
            "Clear", panel.transform,
            new Vector2(-85, -135), ClearPin
        );

        CreateButton(
            "0", panel.transform,
            new Vector2(0, -135), () => PressDigit(0)
        );

        CreateButton(
            "Enter", panel.transform,
            new Vector2(85, -135), SubmitPin
        );

        CreateButton(
            "Close", panel.transform,
            new Vector2(0, -200), CloseKeypad
        );

        UpdatePinDisplay();

        Log("Keypad created successfully.");
    }

    private GameObject CreateUIObject(
        string objectName, Transform parent)
    {
        GameObject obj = new GameObject(
            objectName, typeof(RectTransform)
        );

        obj.transform.SetParent(parent, false);

        return obj;
    }

    private Text CreateLabel(
        string labelText,
        Transform parent,
        Vector2 position,
        int fontSize,
        Color color)
    {
        GameObject obj = CreateUIObject(labelText, parent);

        Text label = obj.AddComponent<Text>();
        label.font = uiFont;
        label.text = labelText;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(270, 45);
        rect.anchoredPosition = position;

        return label;
    }

    private void CreateButton(
        string buttonText,
        Transform parent,
        Vector2 position,
        UnityAction action)
    {
        GameObject obj = CreateUIObject(
            buttonText + " Button", parent
        );

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.22f, 0.27f, 0.35f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        // Button label.
        Text label = CreateLabel(
            buttonText,
            obj.transform,
            Vector2.zero,
            22,
            Color.white
        );

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Button size and location.
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(75, 52);
        rect.anchoredPosition = position;
    }

    public void OpenKeypad()
    {
        if (canvas == null)
        {
            Debug.LogError("[PostBox] Keypad Canvas is missing.");
            return;
        }

        enteredPin = "";
        UpdatePinDisplay();

        canvas.gameObject.SetActive(true);
        keypadOpen = true;

        Log("Keypad opened.");
    }

    public void PressDigit(int digit)
    {
        if (!keypadOpen)
            return;

        if (digit < 0 || digit > 9)
            return;

        if (enteredPin.Length >= maxPinLength)
        {
            Log("PIN is full. Press Clear to try again.");
            return;
        }

        enteredPin += digit.ToString();

        UpdatePinDisplay();

        Log("Digit entered. PIN length: " + enteredPin.Length);
    }

    public void SubmitPin()
    {
        if (!keypadOpen)
            return;

        if (enteredPin == correctPin)
        {
            Debug.Log("Right");
            Log("Correct PIN entered!");
        }
        else
        {
            Debug.Log("Wrong PIN");
            Log("Incorrect PIN.");
        }
    }

    public void ClearPin()
    {
        if (!keypadOpen)
            return;

        enteredPin = "";
        UpdatePinDisplay();

        Log("PIN cleared.");
    }

    public void CloseKeypad()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        keypadOpen = false;
        enteredPin = "";

        Log("Keypad closed.");
    }

    private void UpdatePinDisplay()
    {
        if (pinDisplay != null)
        {
            pinDisplay.text = new string(
                '*', enteredPin.Length
            );
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log("[PostBox] " + message, this);
    }
}
