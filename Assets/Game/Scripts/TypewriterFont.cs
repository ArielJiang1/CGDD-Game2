using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Scenes built in the editor can only save the built-in font and English text. At runtime this swaps every
    /// UI Text under this object to the typewriter (monospace) font - or, in Chinese, translates the text
    /// (LocTable) and uses a Chinese font.
    /// </summary>
    public class TypewriterFont : MonoBehaviour
    {
        // World-space posters are laid out at 1000 units per metre. Rendering their glyphs at 3x that (the old setting)
        // made a banner title a 450-900 px glyph, and with Chinese the font atlas overflowed:
        // "Failed to update dynamic font texture ... (max size 4096)". 0.5 = 500 texture px per metre of text: still sharp.
        public const float PosterPixelsPerUnit = 0.5f;

        void Awake()
        {
            CanvasScaler cs = GetComponent<CanvasScaler>();
            Canvas canvas = GetComponent<Canvas>();
            if (cs != null && canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                cs.dynamicPixelsPerUnit = Mathf.Min(cs.dynamicPixelsPerUnit, PosterPixelsPerUnit);
            bool zh = Loc.Chinese;
            Font f = zh ? UIFactory.CjkFont : UIFactory.MonoFont;
            foreach (Text t in GetComponentsInChildren<Text>(true))
            {
                if (zh) t.text = Loc.T(t.text);
                if (f != null && t.font == UIFactory.DefaultFont) t.font = f;
            }
        }
    }
}
