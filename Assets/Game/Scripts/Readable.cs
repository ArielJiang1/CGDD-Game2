// @role A poster / sign / label the player can read by walking up and looking at it: a thought, a Search keyword, or the poster answering back
// @calls Loc
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Put on a world-space poster canvas. When the player is close enough and keeps the crosshair on it for a moment,
    /// the level director "reads" it once:
    ///   reaction  - what the player notices (small print, a clue, a joke that belongs to this school)
    ///   keyword   - a word for the phone's Search app (Chapter 5)
    ///   altTitle / altBody - the poster changes after you read it (it reacts to being read)
    /// Text is stored in English; Chinese comes from LocTable.
    /// </summary>
    public class Readable : MonoBehaviour
    {
        [TextArea] public string reaction;
        public string keyword;
        [TextArea] public string altTitle;
        [TextArea] public string altBody;
        [Tooltip("How close you must be (m). 0 = worked out from the poster's size.")]
        public float range;
        [Tooltip("Size of the thing being read (m). 0 = worked out from the poster's size. Set it for things that aren't posters (a tree, a bell).")]
        public float radius;
        [HideInInspector] public bool read;

        /// <summary>Centre of the poster and half its diagonal (world units).</summary>
        public Vector3 Centre { get { return transform.position; } }

        public float Radius
        {
            get
            {
                if (radius > 0f) return radius;
                RectTransform rt = transform as RectTransform;
                if (rt == null) return 0.3f;
                Vector2 s = Vector2.Scale(rt.sizeDelta, new Vector2(rt.lossyScale.x, rt.lossyScale.y));
                return Mathf.Max(0.12f, s.magnitude * 0.5f);
            }
        }

        public float Range { get { return range > 0f ? range : Mathf.Clamp(1.6f + Radius * 2.2f, 1.8f, 6f); } }

        /// <summary>The poster answers back: swap its printed text (with a glitch).</summary>
        public void React()
        {
            foreach (Text t in GetComponentsInChildren<Text>(true))
            {
                if (t.name == "Title" && !string.IsNullOrEmpty(altTitle)) t.text = Loc.T(altTitle);
                if (t.name == "Body" && !string.IsNullOrEmpty(altBody)) t.text = Loc.T(altBody);
            }
        }

        public bool Reacts { get { return !string.IsNullOrEmpty(altTitle) || !string.IsNullOrEmpty(altBody); } }
    }
}
