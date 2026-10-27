using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SafeVR.Editor
{
    public static class SafeVRUIBuilder
    {
        public static GameObject CreateCanvas(string name)
        {
            GameObject canvasObj = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            return canvasObj;
        }

        public static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = panel.GetComponent<Image>();
            img.color = color;
            return panel;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Color normalColor, Color textColor, float fontSize)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = normalColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = normalColor;
            cb.highlightedColor = normalColor * 1.25f;
            cb.pressedColor = normalColor * 0.8f;
            cb.selectedColor = normalColor;
            btn.colors = cb;

            CreateText(btnObj.transform, "Label", label, fontSize, FontStyles.Bold, textColor, Vector2.zero, size, TextAlignmentOptions.Center);
            return btn;
        }
    }
}
