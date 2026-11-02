// @role A poster that changes when you come close and look at it: a too-perfect smile, a glitch, an inedible lunch, a mirror (you, in uniform), a red X, or Voss
// @calls AudioSynth, UIFactory, GameSpeed, Loc
// @tune readFirst:float:0-5:seconds of looking before it starts to change (0 = from the text length)
// @tune changeSeconds:float:0-5:seconds the change takes (0 = by kind)
// Drawn from plain rectangles at runtime; pure atmosphere, no gameplay effect.
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Put on a world-space poster canvas (GameSceneBuilder.AddEggs picks the posters by name). When the player stands
    /// near and looks at it, an overlay fades in over the poster; walk away and it fades back. Everything is drawn
    /// with rectangles at runtime, so the scenes stay plain.
    /// </summary>
    public class PosterEgg : MonoBehaviour
    {
        public enum Kind { Smile, Glitch, Lunch, Mirror, RedX, Voss }

        /// <summary>Which posters (by name) do what. Used by the scene builder, and again at runtime so new entries work without a rebuild.</summary>
        public static readonly System.Collections.Generic.Dictionary<string, Kind> Table = new System.Collections.Generic.Dictionary<string, Kind>
        {
            { "Poster_Smile", Kind.Smile }, { "Poster_WholeFace", Kind.Smile },
            // glitches: the screens, the incidents, the stairs that don't exist - and every poster that insists on a date
            { "Sign_Devices", Kind.Glitch }, { "Sign_LiveAddress", Kind.Glitch }, { "Banner_DaysSinceIncident", Kind.Glitch }, { "Sign_NotHere", Kind.Glitch },
            { "Poster_Catalogue", Kind.Glitch }, { "Poster_BannedBooks", Kind.Glitch }, { "Poster_Founding", Kind.Glitch },
            { "Sign_LightsOut", Kind.Glitch }, { "Banner_PictureDay", Kind.Glitch }, { "Poster_Counting", Kind.Glitch }, { "Sign_Repair", Kind.Glitch },
            { "Poster_Menu", Kind.Lunch }, { "Poster_Privilege", Kind.Lunch },
            { "Poster_Uniform", Kind.Mirror }, { "Poster_CitizenCard", Kind.Mirror }, { "Photo_1967", Kind.Mirror },
            { "Poster_CardsReady", Kind.RedX },
            { "Display_Yearbook1980", Kind.Voss }, { "Poster_InspectedDesk", Kind.Voss },
        };

        /// <summary>Adds the egg to every matching poster in the loaded scene that doesn't have one yet.</summary>
        public static void InstallAll()
        {
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Kind k;
                if (c.renderMode != RenderMode.WorldSpace || !Table.TryGetValue(c.name, out k) || c.GetComponent<PosterEgg>() != null) continue;
                c.gameObject.AddComponent<PosterEgg>().kind = k;
            }
        }

        /// <summary>The player's body lives on this layer: the player's camera skips it, mirrors show it.</summary>
        public const int MirrorLayer = 30;

        public Kind kind;
        [Tooltip("How close you must be (m). 0 = worked out from the poster's size.")]
        public float range;
        [Tooltip("Seconds to change completely while you look.")]
        public float changeSeconds;
        [Tooltip("Seconds of looking before it starts to change (time to read the text). 0 = from the text length.")]
        public float readFirst;
        float looked;

        RectTransform root, layer;
        CanvasGroup group;
        Text[] texts;
        Color[] textColors;
        string[] textStrings;
        Vector2[] textPos;
        float p, W, H;
        bool everNear;
        Transform cam;

        // per kind
        RectTransform[] eyes;
        Vector2[] eyeRest;
        RectTransform fly;
        RectTransform[] bars;
        Text[] ghosts;
        RectTransform slice;
        Camera mirrorCam;
        RenderTexture rt;
        float glitchT;

        void Start()
        {
            root = transform as RectTransform;
            if (root == null) { enabled = false; return; }
            W = root.sizeDelta.x;
            H = root.sizeDelta.y;
            texts = GetComponentsInChildren<Text>(true);
            textColors = new Color[texts.Length];
            textStrings = new string[texts.Length];
            textPos = new Vector2[texts.Length];
            for (int i = 0; i < texts.Length; i++) { textColors[i] = texts[i].color; textStrings[i] = texts[i].text; textPos[i] = texts[i].rectTransform.anchoredPosition; }
            if (range <= 0f) range = Mathf.Clamp(1.8f + Mathf.Max(W, H) * 0.0012f, 2f, 3.6f);
            if (changeSeconds <= 0f) changeSeconds = kind == Kind.Mirror || kind == Kind.Voss ? 1.4f : (kind == Kind.Glitch ? 0.3f : 1f);
            // first you get to read it: about a second per short line, more for longer posters
            int chars = 0;
            foreach (Text t in texts) chars += t.text.Length;
            if (readFirst <= 0f) readFirst = Mathf.Clamp(0.3f + chars * (Loc.Chinese ? 0.02f : 0.008f), 0.4f, 1.2f);

            if (kind != Kind.Glitch)
            {
                layer = UIFactory.Stretch("Egg_" + kind, root);
                layer.SetAsLastSibling();
                group = layer.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = group.blocksRaycasts = false;
                Image bg = null;
                foreach (Image im in GetComponentsInChildren<Image>(true)) if (im.name == "BG") { bg = im; break; }
                UIFactory.AddImage(UIFactory.Stretch("Cover", layer), bg != null ? bg.color : new Color(0.9f, 0.9f, 0.86f));
            }
            switch (kind)
            {
                case Kind.Smile: BuildSmile(); break;
                case Kind.Lunch: BuildLunch(); break;
                case Kind.Mirror: BuildMirror(); break;
                case Kind.RedX: BuildRedX(); break;
                case Kind.Voss: BuildVoss(); break;
                case Kind.Glitch: BuildGlitch(); break;
            }
        }

        void OnDestroy() { if (rt != null) rt.Release(); }

        // ------------------------------------------------------------------ drawing helpers (pixels, from the centre)
        RectTransform R(string name, float x, float y, float w, float h, Color c, float rot = 0f, Transform parent = null)
        {
            RectTransform r = UIFactory.Centered(name, parent != null ? parent : layer, x, y, w, h);
            r.localRotation = Quaternion.Euler(0f, 0f, rot);
            UIFactory.AddImage(r, c);
            return r;
        }

        float S { get { return Mathf.Min(W, H); } }   // a square drawing area that fits the poster

        // ------------------------------------------------------------------ the smile: symmetric, wide, the eyes don't smile
        void BuildSmile()
        {
            // a plain, perfectly symmetric smiley: two big blank eyes, one huge curved mouth, two dimples. Too wide. Too still.
            float s = S * 0.92f;
            Color ink = new Color(0.1f, 0.1f, 0.12f), mouth = new Color(0.45f, 0.08f, 0.1f), blush = new Color(0.98f, 0.62f, 0.62f, 0.6f);
            R("Face", 0f, 0f, s, s, new Color(1f, 0.9f, 0.55f));
            eyes = new[] { R("Eye_L", -s * 0.17f, s * 0.16f, s * 0.085f, s * 0.19f, ink), R("Eye_R", s * 0.17f, s * 0.16f, s * 0.085f, s * 0.19f, ink) };
            eyeRest = new[] { eyes[0].anchoredPosition, eyes[1].anchoredPosition };
            int n = 21;
            float rad = s * 0.34f, cy = s * 0.04f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Lerp(195f, 345f, i / (float)(n - 1)) * Mathf.Deg2Rad;
                Vector2 q = new Vector2(Mathf.Cos(a) * rad, cy + Mathf.Sin(a) * rad * 0.85f);
                // thick in the middle, thinner towards the corners
                float thick = s * Mathf.Lerp(0.045f, 0.075f, Mathf.Sin(Mathf.PI * i / (n - 1)));
                R("Mouth", q.x, q.y, s * 0.075f, thick, mouth, Mathf.Atan2(Mathf.Cos(a) * 0.85f, -Mathf.Sin(a)) * Mathf.Rad2Deg);
            }
            float ex = Mathf.Cos(195f * Mathf.Deg2Rad) * rad, ey = cy + Mathf.Sin(195f * Mathf.Deg2Rad) * rad * 0.85f;
            R("Dimple_L", ex - s * 0.035f, ey + s * 0.035f, s * 0.018f, s * 0.07f, mouth, 25f);
            R("Dimple_R", -ex + s * 0.035f, ey + s * 0.035f, s * 0.018f, s * 0.07f, mouth, -25f);
            R("Blush_L", -s * 0.3f, -s * 0.02f, s * 0.11f, s * 0.05f, blush);
            R("Blush_R", s * 0.3f, -s * 0.02f, s * 0.11f, s * 0.05f, blush);
        }

        // ------------------------------------------------------------------ lunch: grey tray, grey food, one fly
        void BuildLunch()
        {
            float w = W * 0.86f, h = H * 0.62f;
            R("Tray", 0f, -H * 0.05f, w, h, new Color(0.55f, 0.56f, 0.55f));
            R("Slot_A", -w * 0.22f, -H * 0.05f + h * 0.18f, w * 0.48f, h * 0.5f, new Color(0.45f, 0.46f, 0.45f));
            R("Slot_B", w * 0.25f, -H * 0.05f + h * 0.18f, w * 0.4f, h * 0.5f, new Color(0.45f, 0.46f, 0.45f));
            R("Slot_C", 0f, -H * 0.05f - h * 0.27f, w * 0.9f, h * 0.36f, new Color(0.45f, 0.46f, 0.45f));
            // grey-green mush
            for (int i = 0; i < 5; i++)
                R("Mush", -w * 0.22f + (i - 2) * w * 0.06f, -H * 0.05f + h * (0.12f + (i % 2) * 0.08f), w * 0.16f, h * 0.2f, new Color(0.52f, 0.56f, 0.38f), i * 17f);
            // a grey slab with spots
            R("Slab", w * 0.25f, -H * 0.05f + h * 0.18f, w * 0.3f, h * 0.3f, new Color(0.62f, 0.55f, 0.5f), -6f);
            for (int i = 0; i < 4; i++) R("Spot", w * (0.18f + 0.05f * i), -H * 0.05f + h * (0.12f + 0.06f * (i % 2)), w * 0.03f, w * 0.03f, new Color(0.35f, 0.3f, 0.26f));
            // pale soup, with a hair in it
            R("Soup", 0f, -H * 0.05f - h * 0.27f, w * 0.8f, h * 0.26f, new Color(0.78f, 0.76f, 0.62f));
            R("Hair", w * 0.05f, -H * 0.05f - h * 0.26f, w * 0.3f, 2.5f, new Color(0.12f, 0.1f, 0.08f), 12f);
            fly = R("Fly", 0f, 0f, w * 0.035f, w * 0.03f, new Color(0.08f, 0.08f, 0.09f));
            R("Wing_L", -w * 0.022f, w * 0.015f, w * 0.03f, w * 0.014f, new Color(0.85f, 0.88f, 0.9f, 0.8f), 25f, fly);
            R("Wing_R", w * 0.022f, w * 0.015f, w * 0.03f, w * 0.014f, new Color(0.85f, 0.88f, 0.9f, 0.8f), -25f, fly);
        }

        // ------------------------------------------------------------------ the mirror: it shows you (in uniform)
        void BuildMirror()
        {
            int tw = 256, th = Mathf.Max(64, Mathf.RoundToInt(256f * H / Mathf.Max(1f, W)));
            rt = new RenderTexture(tw, th, 16);
            rt.name = "Mirror_" + name;
            RectTransform pic = UIFactory.Stretch("Mirror", layer);
            pic.offsetMin = new Vector2(S * 0.04f, S * 0.04f);
            pic.offsetMax = new Vector2(-S * 0.04f, -S * 0.04f);
            RawImage ri = pic.gameObject.AddComponent<RawImage>();
            ri.texture = rt;
            ri.uvRect = new Rect(1f, 0f, -1f, 1f);   // mirrored
            ri.raycastTarget = false;
            R("Sheen", W * 0.2f, H * 0.2f, W * 0.05f, H * 0.9f, new Color(1f, 1f, 1f, 0.08f), -25f);
            GameObject g = new GameObject("MirrorCamera");
            g.transform.SetParent(transform, false);
            mirrorCam = g.AddComponent<Camera>();
            mirrorCam.targetTexture = rt;
            mirrorCam.fieldOfView = 42f;
            mirrorCam.nearClipPlane = 0.05f;
            mirrorCam.farClipPlane = 30f;
            mirrorCam.enabled = false;
        }

        // ------------------------------------------------------------------ red X
        void BuildRedX()
        {
            Color red = new Color(0.78f, 0.08f, 0.06f);
            float diag = Mathf.Sqrt(W * W + H * H) * 0.82f, ang = Mathf.Atan2(H, W) * Mathf.Rad2Deg;
            bars = new[] { R("Bar_A", 0f, 0f, diag, S * 0.16f, red, ang), R("Bar_B", 0f, 0f, diag, S * 0.16f, red, -ang) };
            layer.GetComponentInChildren<Image>().color = new Color(0.97f, 0.93f, 0.86f);
        }

        // ------------------------------------------------------------------ Voss: stern eyes, a full beard, a red tie
        void BuildVoss()
        {
            float s = S * 0.92f;
            Color skin = new Color(0.86f, 0.74f, 0.64f), beard = new Color(0.2f, 0.15f, 0.12f), suit = new Color(0.15f, 0.15f, 0.18f), ink = new Color(0.08f, 0.08f, 0.09f);
            R("Backdrop", 0f, 0f, W, H, new Color(0.3f, 0.35f, 0.32f));
            R("Shoulders", 0f, -s * 0.42f, s * 0.95f, s * 0.3f, suit);
            R("Shirt", 0f, -s * 0.33f, s * 0.2f, s * 0.14f, new Color(0.94f, 0.93f, 0.88f));
            R("Tie", 0f, -s * 0.4f, s * 0.06f, s * 0.22f, new Color(0.55f, 0.07f, 0.06f));
            R("Head", 0f, s * 0.06f, s * 0.46f, s * 0.56f, skin);
            R("Hair", 0f, s * 0.31f, s * 0.48f, s * 0.1f, beard);
            R("Beard", 0f, -s * 0.12f, s * 0.48f, s * 0.22f, beard);
            R("Jaw_L", -s * 0.22f, -s * 0.02f, s * 0.05f, s * 0.2f, beard);
            R("Jaw_R", s * 0.22f, -s * 0.02f, s * 0.05f, s * 0.2f, beard);
            R("Mouth", 0f, -s * 0.07f, s * 0.12f, s * 0.018f, new Color(0.45f, 0.2f, 0.18f));
            R("Moustache", 0f, -s * 0.035f, s * 0.2f, s * 0.035f, beard);
            R("Nose", 0f, s * 0.03f, s * 0.05f, s * 0.08f, new Color(0.8f, 0.64f, 0.55f));
            eyes = new[] { R("Eye_L", -s * 0.1f, s * 0.12f, s * 0.07f, s * 0.025f, ink), R("Eye_R", s * 0.1f, s * 0.12f, s * 0.07f, s * 0.025f, ink) };
            eyeRest = new[] { eyes[0].anchoredPosition, eyes[1].anchoredPosition };
            R("Brow_L", -s * 0.1f, s * 0.17f, s * 0.1f, s * 0.025f, beard, -10f);
            R("Brow_R", s * 0.1f, s * 0.17f, s * 0.1f, s * 0.025f, beard, 10f);
        }

        // ------------------------------------------------------------------ glitch: colour ghosts, jitter, a torn slice
        void BuildGlitch()
        {
            ghosts = new Text[texts.Length * 2];
            for (int i = 0; i < texts.Length; i++)
                for (int k = 0; k < 2; k++)
                {
                    Text src = texts[i];
                    RectTransform r = Instantiate(src.rectTransform, src.rectTransform.parent);
                    r.name = src.name + (k == 0 ? "_GhostR" : "_GhostC");
                    r.SetSiblingIndex(src.rectTransform.GetSiblingIndex());
                    Text g = r.GetComponent<Text>();
                    g.color = k == 0 ? new Color(1f, 0.1f, 0.2f, 0f) : new Color(0.1f, 0.9f, 1f, 0f);
                    ghosts[i * 2 + k] = g;
                }
            slice = UIFactory.Rect("Slice", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, H * 0.08f));
            UIFactory.AddImage(slice, new Color(0.9f, 0.95f, 1f, 0f));
        }

        // ------------------------------------------------------------------ every frame
        void Update()
        {
            if (cam == null) { Camera c = Camera.main; if (c == null) return; cam = c.transform; c.cullingMask &= ~(1 << MirrorLayer); }
            Vector3 to = transform.position - cam.position;
            float d = to.magnitude;
            // near + roughly looking at it (within 38 degrees of the view centre) = the reading clock runs
            bool near = d < range && Vector3.Dot(transform.forward, to) > 0f && Vector3.Angle(cam.forward, to) < 38f;
            float before = p;
            looked = near ? looked + GameSpeed.Dt : Mathf.Max(0f, looked - Time.deltaTime * 0.5f);
            bool changing = near && looked >= readFirst;
            p = Mathf.MoveTowards(p, changing ? 1f : 0f, (changing ? GameSpeed.Dt : Time.deltaTime) / (changing ? changeSeconds : 2.2f));
            if (changing && !everNear && kind == Kind.Glitch) { everNear = true; AudioSource.PlayClipAtPoint(AudioSynth.Glitch(), transform.position, 0.18f); }
            if (p <= 0f && before <= 0f && kind != Kind.Glitch) { if (mirrorCam != null) mirrorCam.enabled = false; return; }
            float v = p * p * (3f - 2f * p);
            if (group != null) group.alpha = v;

            switch (kind)
            {
                case Kind.Smile:
                case Kind.Voss:
                    // the eyes follow you (just a little)
                    Vector3 local = transform.InverseTransformPoint(cam.position);
                    float follow = Mathf.Clamp(local.x / Mathf.Max(1f, W), -1f, 1f) * S * 0.035f;   // canvas units
                    for (int i = 0; i < eyes.Length; i++) eyes[i].anchoredPosition = eyeRest[i] + new Vector2(follow, 0f);
                    break;
                case Kind.Lunch:
                    float t = Time.time * 1.7f;
                    fly.anchoredPosition = new Vector2(Mathf.Sin(t * 1.3f) * W * 0.25f + Mathf.Sin(t * 4.1f) * W * 0.03f, Mathf.Sin(t * 0.9f + 1f) * H * 0.18f);
                    fly.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 40f);
                    break;
                case Kind.Mirror:
                    mirrorCam.enabled = v > 0.01f;
                    Transform mc = mirrorCam.transform;
                    mc.position = transform.position - transform.forward * 0.03f;
                    Vector3 face = cam.position + Vector3.down * 0.12f;
                    mc.rotation = Quaternion.LookRotation(face - mc.position, Vector3.up);
                    break;
                case Kind.RedX:
                    for (int i = 0; i < texts.Length; i++) texts[i].color = Color.Lerp(textColors[i], new Color(0.78f, 0.08f, 0.06f), Mathf.Clamp01(p * 2f));
                    if (group != null) group.alpha = Mathf.Clamp01((p - 0.35f) / 0.65f);
                    float g2 = Mathf.Clamp01((p - 0.35f) / 0.65f);
                    bars[0].localScale = new Vector3(Mathf.Clamp01(g2 * 2f), 1f, 1f);
                    bars[1].localScale = new Vector3(Mathf.Clamp01(g2 * 2f - 1f), 1f, 1f);
                    break;
                case Kind.Glitch:
                    UpdateGlitch(v);
                    break;
            }
        }

        /// <summary>Does the centre of the screen (the crosshair) point at this poster's rectangle?</summary>
        bool CrosshairOnPoster()
        {
            Plane plane = new Plane(transform.forward, transform.position);
            Ray ray = new Ray(cam.position, cam.forward);
            float enter;
            if (!plane.Raycast(ray, out enter)) return false;
            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(enter));   // canvas units (px)
            float mx = W * 0.5f + 40f, my = H * 0.5f + 40f;                       // a little forgiving at the edges
            return Mathf.Abs(local.x) <= mx && Mathf.Abs(local.y) <= my;
        }

        void UpdateGlitch(float v)
        {
            glitchT -= Time.deltaTime;
            bool burst = v > 0.05f && glitchT < 0f;
            if (burst) glitchT = Random.Range(0.05f, 0.25f);
            if (v <= 0.001f)
            {
                for (int i = 0; i < texts.Length; i++) { texts[i].rectTransform.anchoredPosition = textPos[i]; texts[i].text = textStrings[i]; }
                for (int i = 0; i < ghosts.Length; i++) ghosts[i].color = new Color(ghosts[i].color.r, ghosts[i].color.g, ghosts[i].color.b, 0f);
                slice.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                return;
            }
            if (!burst) return;
            for (int i = 0; i < texts.Length; i++)
            {
                // a translated poster: keep whatever text it shows now (Readable may have swapped it)
                if (texts[i].text != Scrambled(textStrings[i])) textStrings[i] = texts[i].text;
                float jx = Random.Range(-1f, 1f) * W * 0.02f * v;
                texts[i].rectTransform.anchoredPosition = textPos[i] + new Vector2(jx, Random.Range(-1f, 1f) * H * 0.01f * v);
                texts[i].text = Random.value < 0.18f * v ? Scrambled(textStrings[i]) : textStrings[i];
                for (int k = 0; k < 2; k++)
                {
                    Text g = ghosts[i * 2 + k];
                    g.text = texts[i].text;
                    g.rectTransform.anchoredPosition = texts[i].rectTransform.anchoredPosition + new Vector2((k == 0 ? -1f : 1f) * W * Random.Range(0.005f, 0.02f) * v, 0f);
                    Color c = g.color; c.a = 0.55f * v; g.color = c;
                }
            }
            Image si = slice.GetComponent<Image>();
            if (Random.value < 0.4f * v)
            {
                slice.anchoredPosition = new Vector2(Random.Range(-1f, 1f) * W * 0.04f, Random.Range(-0.45f, 0.45f) * H);
                si.color = new Color(Random.value, Random.value, Random.value, 0.5f * v);
            }
            else si.color = new Color(1f, 1f, 1f, 0f);
        }

        static string Scrambled(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            char[] c = s.ToCharArray();
            int seed = s.Length * 31;
            for (int i = 0; i < c.Length; i++)
                if (c[i] != '\n' && c[i] != ' ' && ((i * 7 + seed) % 5 == 0)) c[i] = "#%&@!?/\\01"[(i + seed) % 10];
            return new string(c);
        }
    }
}
