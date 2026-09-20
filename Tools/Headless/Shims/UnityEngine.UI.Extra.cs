// Minimal shim of the uGUI surface UIFactory/the screens under Assets/_Project/Scripts/Runtime/UI
// use, added when the headless harness's own project files (Plunderspell.Headless.csproj etc.) were
// reconstructed after a gitignore rule had swallowed them -- see docs/Decisions.md, "The headless
// harness's own project files were never committed". Nothing here does real layout; it exists so
// this code compiles and constructs headlessly. No test exercises these UI screens at runtime (the
// one that tries, UIScreenshotPlayModeTests, self-skips without a real graphics device), so
// behavioural fidelity beyond "the fields and methods exist" was not needed to prove that.
using System.Collections.Generic;

namespace UnityEngine
{
    public class RectTransform : Transform
    {
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; } = Vector2.one;
        public Vector2 anchoredPosition { get; set; }
        public Vector2 pivot { get; set; } = new Vector2(0.5f, 0.5f);
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public class RectOffset
    {
        public int left;
        public int right;
        public int top;
        public int bottom;

        public RectOffset() { }

        public RectOffset(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }
    }

    public class MaterialPropertyBlock
    {
        public void SetColor(int nameID, Color color) { }
        public void SetColor(string name, Color color) { }
        public void SetFloat(int nameID, float value) { }
        public void SetFloat(string name, float value) { }
        public void Clear() { }
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction<T0>(T0 arg0);
}

namespace UnityEngine.UI
{
    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }

        public ScaleMode uiScaleMode;
        public Vector2 referenceResolution;
        public float matchWidthOrHeight;
    }

    public class GraphicRaycaster : Behaviour
    {
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding = new RectOffset();
        public TextAnchor childAlignment;
    }

    public class VerticalLayoutGroup : LayoutGroup
    {
        public float spacing;
        public bool childControlWidth;
        public bool childControlHeight;
        public bool childForceExpandWidth;
        public bool childForceExpandHeight;
    }

    public class GridLayoutGroup : LayoutGroup
    {
        public Vector2 cellSize;
        public Vector2 spacing;
    }

    public class HorizontalLayoutGroup : LayoutGroup
    {
        public float spacing;
        public bool childControlWidth;
        public bool childControlHeight;
        public bool childForceExpandWidth;
        public bool childForceExpandHeight;
    }
}
