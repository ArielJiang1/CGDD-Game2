using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Scenes built in the editor can only save the built-in font. At runtime this swaps every UI Text under
    /// this object to the typewriter (monospace) font, for the "typed in a professional, almost robotic way" look.
    /// </summary>
    public class TypewriterFont : MonoBehaviour
    {
        void Awake()
        {
            Font mono = UIFactory.MonoFont;
            if (mono == null) return;
            foreach (Text t in GetComponentsInChildren<Text>(true))
            {
                if (t.font == UIFactory.DefaultFont) t.font = mono;
            }
        }
    }
}
