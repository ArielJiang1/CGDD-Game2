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
        void Awake()
        {
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
