// @role Builds every on-screen panel, text, button and world-space poster canvas in code (no prefabs), with the right font for English or Chinese
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Small helpers for building uGUI by code (used by the scene builder AND at runtime by the HUD / phone).
    /// Fonts: in the editor only the built-in font can be saved into scenes; at runtime UI uses a typewriter
    /// (monospace) font taken from the computer (Courier New / Menlo...), falling back to the built-in one.
    /// </summary>
    public static class UIFactory
    {
        static Font font, mono, cjk;
        static bool monoTried, cjkTried;

        /// <summary>A Chinese font from the computer (PingFang on a Mac, YaHei on Windows...). Runtime only.</summary>
        public static Font CjkFont
        {
            get
            {
                if (cjk == null && !cjkTried)
                {
                    cjkTried = true;
                    try
                    {
                        cjk = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Hiragino Sans GB", "Heiti SC", "STHeiti", "Microsoft YaHei", "Noto Sans CJK SC", "Source Han Sans SC", "Arial Unicode MS" }, 32);
                    }
                    catch (System.Exception) { cjk = null; }
                }
                return cjk != null ? cjk : DefaultFont;
            }
        }

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Typewriter font (runtime only). Falls back to the built-in font.</summary>
        public static Font MonoFont
        {
            get
            {
                if (mono == null && !monoTried)
                {
                    monoTried = true;
                    try
                    {
                        mono = Font.CreateDynamicFontFromOSFont(new[] { "Courier New", "Courier", "Menlo", "Monaco", "Consolas", "Liberation Mono", "DejaVu Sans Mono" }, 32);
                    }
                    catch (System.Exception) { mono = null; }
                }
                return mono != null ? mono : DefaultFont;
            }
        }

        /// <summary>The font new Text gets: typewriter while playing, built-in while building scenes in the editor.</summary>
        public static Font TextFont { get { return Application.isPlaying ? (Loc.Chinese ? CjkFont : MonoFont) : DefaultFont; } }

        public static Canvas MakeCanvas(string name, int sortOrder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            CanvasScaler s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        /// <summary>A canvas that lives in the 3D world (posters, signs). 1 px = 1 mm.</summary>
        public static Canvas MakeWorldCanvas(string name, Transform parent, Vector3 worldPos, Quaternion rot, float widthMeters, float heightMeters)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, true);
            Canvas c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(widthMeters * 1000f, heightMeters * 1000f);
            rt.position = worldPos;
            rt.rotation = rot;
            rt.localScale = Vector3.one * 0.001f;
            CanvasScaler s = go.GetComponent<CanvasScaler>();
            s.dynamicPixelsPerUnit = TypewriterFont.PosterPixelsPerUnit;   // see TypewriterFont: bigger overflows the CJK font atlas
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Rect anchored to the parent's top-left corner; pos is (x right, y down) in pixels.</summary>
        public static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h)
        {
            return Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        }

        public static RectTransform Centered(string name, Transform parent, float x, float y, float w, float h)
        {
            Vector2 c = new Vector2(0.5f, 0.5f);
            return Rect(name, parent, c, c, c, new Vector2(x, y), new Vector2(w, h));
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        public static Image AddImage(RectTransform rt, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text AddText(RectTransform rt, string text, int size, Color color, TextAnchor align)
        {
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = TextFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static readonly Dictionary<Text, int> baseSizes = new Dictionary<Text, int>();
        static readonly Dictionary<Text, string> fitted = new Dictionary<Text, string>();

        /// <summary>Height the text would need in this box (layout units), wrapping to the box width.</summary>
        public static float PreferredHeight(Text t, string full, int fontSize = -1)
        {
            TextGenerationSettings g = t.GetGenerationSettings(new Vector2(t.rectTransform.rect.width, 0f));
            if (fontSize > 0) g.fontSize = fontSize;
            g.resizeTextForBestFit = false;
            return t.cachedTextGeneratorForLayout.GetPreferredHeight(full ?? "", g) / Mathf.Max(0.01f, t.pixelsPerUnit);
        }

        /// <summary>
        /// Picks the biggest font size (from the text's own size down to 'min') at which 'full' fits inside the box.
        /// Use the FULL string even while it is being typed, so the size never jumps. (Chinese lines are longer.)
        /// </summary>
        public static void FitFont(Text t, string full, int min = 11)
        {
            if (t == null) return;
            int max;
            if (!baseSizes.TryGetValue(t, out max)) { max = t.fontSize; baseSizes[t] = max; }
            string last;
            if (fitted.TryGetValue(t, out last) && last == full) return;
            fitted[t] = full;
            float h = t.rectTransform.rect.height;
            int size = max;
            while (size > min && PreferredHeight(t, full, size) > h) size--;
            t.fontSize = size;
        }

        public static void AddShadow(Graphic g, Color c, float distance)
        {
            Shadow s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = c;
            s.effectDistance = new Vector2(distance, -distance);
        }

        public static Button MakeButton(string name, Transform parent, string label, float x, float y, float w, float h)
        {
            RectTransform rt = Centered(name, parent, x, y, w, h);
            Image img = AddImage(rt, new Color(0.93f, 0.90f, 0.82f, 1f));
            img.raycastTarget = true;
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 0.82f, 0.45f);
            cb.pressedColor = new Color(0.8f, 0.65f, 0.35f);
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            b.colors = cb;
            AddText(Stretch("Label", rt), label, 34, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleCenter);
            return b;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.UI.InputSystemUIInputModule m = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            m.AssignDefaultActions();
#endif
        }
    }
}
