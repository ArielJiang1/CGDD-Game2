using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// The "Clearview Academy" art pass, applied on top of the white box (Anti-Utopian + dark humour):
    ///  - a perfect surface with an unsettling core: clinical off-white walls over a blue-green dado,
    ///    with water stains, mould and peeling paint baked into procedural textures (3 variants)
    ///  - institutional order: flat fluorescent fixtures in a strict grid (a few flicker in neglected spots),
    ///    symmetric signage that politely regulates behaviour, cameras with blinking red LEDs, PA speakers
    ///  - absurd rituals: cheerful pastel posters and bunting against the grey
    ///  - deep red only for authority (staff uniforms, warnings, cameras)
    /// Everything is generated (no imported models), so the white box layout stays exactly as playtested.
    /// </summary>
    public static class ClearviewArt
    {
        public const string Dir = "Assets/Game/Generated/Clearview";

        // palette
        public static readonly Color OffWhite = new Color(0.90f, 0.90f, 0.86f);
        public static readonly Color PaleGray = new Color(0.78f, 0.79f, 0.77f);
        public static readonly Color BlueGreen = new Color(0.50f, 0.63f, 0.60f);
        public static readonly Color FadedYellow = new Color(0.80f, 0.74f, 0.47f);
        public static readonly Color DirtyGreen = new Color(0.40f, 0.47f, 0.36f);
        public static readonly Color DeepRed = new Color(0.55f, 0.07f, 0.06f);
        public static readonly Color PastelPink = new Color(0.97f, 0.78f, 0.82f);
        public static readonly Color PastelMint = new Color(0.72f, 0.92f, 0.83f);
        public static readonly Color PastelYellow = new Color(0.99f, 0.93f, 0.66f);
        public static readonly Color PastelBlue = new Color(0.72f, 0.84f, 0.97f);
        public static readonly Color Ink = new Color(0.12f, 0.13f, 0.14f);

        public static Material[] Walls;
        public static Material Floor, FloorAlt, Ceiling, Ground, Wood, Metal, Steel, Locker, Pillar, Board,
            Uniform, Skin, Hair, EyeBand, Authority, Warden, Apron, Tie, Tube, FixtureBody, RedLED, CamBody, Frame, DoorLeaf,
            Mattress, Blanket, Pillow, FarBuilding, FarWindow, BookA, BookB, BookC, ClockFace, Black, Ribbon, SignGlow, Concrete;
        public static Material[] Pastels;

        // ------------------------------------------------------------------ materials & textures
        public static void Prepare()
        {
            ClearviewFolders();
            Walls = new Material[3];
            for (int i = 0; i < 3; i++)
                Walls[i] = Mat("M_CV_Wall_" + i, Color.white, Tex("T_CV_Wall_" + i, 256, 384, (x, y) => WallPixel(x, y, i)), new Vector2(0.5f, 1f / 3f), 0.12f);
            Floor = Mat("M_CV_Floor", Color.white, Tex("T_CV_Floor", 256, 256, FloorPixel), new Vector2(0.5f, 0.5f), 0.35f);
            FloorAlt = Mat("M_CV_Podium", new Color(0.72f, 0.70f, 0.66f), Tex("T_CV_Floor", 256, 256, FloorPixel), new Vector2(0.5f, 0.5f), 0.3f);
            Ceiling = Mat("M_CV_Ceiling", Color.white, Tex("T_CV_Ceiling", 256, 256, CeilingPixel), new Vector2(0.5f, 0.5f), 0.05f);
            Ground = Mat("M_CV_Ground", new Color(0.42f, 0.44f, 0.41f), Tex("T_CV_Concrete", 256, 256, ConcretePixel), new Vector2(0.25f, 0.25f), 0.05f);
            Concrete = Mat("M_CV_Concrete", new Color(0.62f, 0.63f, 0.6f), Tex("T_CV_Concrete", 256, 256, ConcretePixel), new Vector2(0.5f, 0.5f), 0.05f);
            Wood = Mat("M_CV_Laminate", Color.white, Tex("T_CV_Laminate", 256, 256, LaminatePixel), Vector2.one, 0.3f);
            Board = Mat("M_CV_Board", Color.white, Tex("T_CV_Board", 256, 128, BoardPixel), new Vector2(0.25f, 0.85f), 0.1f);
            Metal = Mat("M_CV_ChairMetal", DirtyGreen, null, Vector2.one, 0.35f, 0.4f);
            Steel = Mat("M_CV_Steel", new Color(0.70f, 0.72f, 0.72f), null, Vector2.one, 0.55f, 0.7f);
            Locker = Mat("M_CV_Locker", new Color(0.52f, 0.64f, 0.62f), Tex("T_CV_Locker", 128, 256, LockerPixel), new Vector2(2f, 0.555f), 0.4f, 0.3f);
            Pillar = Mat("M_CV_Pillar", PaleGray, Tex("T_CV_Concrete", 256, 256, ConcretePixel), new Vector2(0.5f, 0.5f), 0.1f);
            Uniform = Mat("M_CV_Uniform", new Color(0.44f, 0.46f, 0.49f), null, Vector2.one, 0.15f);
            Skin = Mat("M_CV_Skin", new Color(0.86f, 0.74f, 0.64f), null, Vector2.one, 0.2f);
            Hair = Mat("M_CV_Hair", new Color(0.16f, 0.13f, 0.11f), null, Vector2.one, 0.25f);
            EyeBand = Mat("M_CV_Eyes", new Color(0.06f, 0.06f, 0.07f), null, Vector2.one, 0.6f);
            Authority = Mat("M_CV_Authority", DeepRed, null, Vector2.one, 0.25f);
            Warden = Mat("M_CV_Warden", new Color(0.11f, 0.13f, 0.22f), null, Vector2.one, 0.2f);
            Apron = Mat("M_CV_Apron", PastelMint, null, Vector2.one, 0.1f);
            Tie = Mat("M_CV_Tie", new Color(0.42f, 0.1f, 0.1f), null, Vector2.one, 0.2f);
            Tube = Mat("M_CV_Tube", new Color(0.95f, 0.98f, 1f), null, Vector2.one, 0.5f, 0f, new Color(1.6f, 1.7f, 1.75f));
            FixtureBody = Mat("M_CV_Fixture", new Color(0.82f, 0.82f, 0.79f), null, Vector2.one, 0.3f, 0.2f);
            RedLED = Mat("M_CV_RedLED", new Color(1f, 0.1f, 0.08f), null, Vector2.one, 0.6f, 0f, new Color(4f, 0.25f, 0.2f));
            CamBody = Mat("M_CV_CamBody", new Color(0.88f, 0.88f, 0.85f), null, Vector2.one, 0.5f, 0.1f);
            Frame = Mat("M_CV_Frame", new Color(0.30f, 0.36f, 0.32f), null, Vector2.one, 0.4f, 0.4f);
            DoorLeaf = Mat("M_CV_Door", new Color(0.46f, 0.55f, 0.50f), null, Vector2.one, 0.3f, 0.1f);
            Mattress = Mat("M_CV_Mattress", new Color(0.80f, 0.80f, 0.76f), null, Vector2.one, 0.05f);
            Blanket = Mat("M_CV_Blanket", new Color(0.35f, 0.42f, 0.40f), null, Vector2.one, 0.05f);
            Pillow = Mat("M_CV_Pillow", new Color(0.92f, 0.92f, 0.9f), null, Vector2.one, 0.05f);
            FarBuilding = Mat("M_CV_FarBuilding", new Color(0.12f, 0.13f, 0.15f), null, Vector2.one, 0.05f);
            FarWindow = Mat("M_CV_FarWindow", new Color(0.1f, 0.09f, 0.06f), null, Vector2.one, 0.1f, 0f, new Color(0.25f, 0.2f, 0.1f));
            BookA = Mat("M_CV_BookA", new Color(0.24f, 0.33f, 0.48f), null, Vector2.one, 0.2f);
            BookB = Mat("M_CV_BookB", new Color(0.55f, 0.2f, 0.18f), null, Vector2.one, 0.2f);
            BookC = Mat("M_CV_BookC", new Color(0.78f, 0.72f, 0.5f), null, Vector2.one, 0.2f);
            ClockFace = Mat("M_CV_ClockFace", new Color(0.95f, 0.94f, 0.9f), null, Vector2.one, 0.4f);
            Black = Mat("M_CV_Black", new Color(0.05f, 0.05f, 0.05f), null, Vector2.one, 0.3f);
            Ribbon = Mat("M_CV_Ribbon", PastelPink, null, Vector2.one, 0.2f);
            SignGlow = Mat("M_CV_SignGlow", new Color(0.8f, 0.1f, 0.08f), null, Vector2.one, 0.3f, 0f, new Color(2.2f, 0.2f, 0.15f));
            Pastels = new[]
            {
                Mat("M_CV_PastelPink", PastelPink, null, Vector2.one, 0.2f),
                Mat("M_CV_PastelMint", PastelMint, null, Vector2.one, 0.2f),
                Mat("M_CV_PastelYellow", PastelYellow, null, Vector2.one, 0.2f),
                Mat("M_CV_PastelBlue", PastelBlue, null, Vector2.one, 0.2f),
            };
            AssetDatabase.SaveAssets();
        }

        static void ClearviewFolders()
        {
            EnsureFolder(Dir);
            EnsureFolder(Dir + "/Textures");
            EnsureFolder(Dir + "/Materials");
        }

        static Shader Lit()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            return s != null ? s : Shader.Find("Standard");
        }

        public static Material Mat(string name, Color c, Texture2D tex, Vector2 tiling, float smooth, float metal = 0f, Color? emission = null)
        {
            string path = Dir + "/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Lit()); AssetDatabase.CreateAsset(m, path); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            foreach (string p in new[] { "_BaseMap", "_MainTex" })
            {
                if (!m.HasProperty(p)) continue;
                m.SetTexture(p, tex);
                m.SetTextureScale(p, tiling);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (emission.HasValue && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D Tex(string name, int w, int h, System.Func<int, int, Color> pixel)
        {
            string path = Dir + "/Textures/" + name + ".asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.name = name;
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = pixel(x, y);
            t.SetPixels(px);
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 4;
            t.Apply(true);
            if (existing != null)
            {
                EditorUtility.CopySerialized(t, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(t);
                return existing;
            }
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        // ---- noise
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xffff) / 65535f;
            }
        }

        static float Value(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float x, float y, int seed, int oct = 4)
        {
            float s = 0f, amp = 0.5f, f = 1f;
            for (int i = 0; i < oct; i++) { s += Value(x * f, y * f, seed + i * 17) * amp; f *= 2.03f; amp *= 0.5f; }
            return s / 0.9375f;
        }

        // ---- walls: 2 m x 3 m per module (128 px / m)
        static Color WallPixel(int x, int y, int variant)
        {
            float u = x / 128f, v = y / 128f;   // metres
            float heavy = variant == 0 ? 0.45f : (variant == 1 ? 0.85f : 1.35f);
            int seed = 101 + variant * 31;
            Color c;
            if (v < 0.12f) c = new Color(0.22f, 0.26f, 0.24f);                 // skirting
            else if (v < 1.05f) c = BlueGreen;                                  // dado
            else if (v < 1.12f) c = FadedYellow;                                // stripe
            else c = OffWhite;
            float n = Fbm(u * 3f, v * 3f, seed);
            c *= 0.93f + 0.09f * n;

            // water stains running down from the ceiling
            float streak = 0f;
            for (int i = 0; i < 4; i++)
            {
                float sx = Hash(i, variant, seed) * 2f;
                float width = 0.03f + Hash(i, 7, seed) * 0.09f;
                float len = (0.4f + Hash(i, 3, seed) * 1.6f) * heavy;
                float dx = Mathf.Abs(u - sx - Mathf.Sin(v * 3f + i) * 0.02f);
                if (dx < width && v > 3f - len)
                {
                    float along = (v - (3f - len)) / len;
                    streak = Mathf.Max(streak, (1f - dx / width) * along * (0.5f + 0.5f * Fbm(u * 20f, v * 4f, seed + 5)));
                }
            }
            c = Color.Lerp(c, new Color(0.66f, 0.6f, 0.42f), streak * 0.45f * Mathf.Min(1f, heavy));

            // mould - along the skirting and in the top corners
            float mould = Fbm(u * 5f, v * 5f, seed + 9);
            float zone = Mathf.Clamp01(1f - v / 0.6f) + Mathf.Clamp01((v - 2.5f) / 0.5f) * Mathf.Clamp01(1f - Mathf.Min(u, 2f - u) / 0.6f);
            float m = Mathf.Clamp01((mould - 0.62f + zone * 0.12f * heavy) * 5f) * zone;
            c = Color.Lerp(c, new Color(0.22f, 0.27f, 0.2f), m * 0.75f * Mathf.Min(1.2f, heavy));

            // peeling paint on the upper wall
            if (v > 1.2f)
            {
                float p = Fbm(u * 4f + 7f, v * 4f, seed + 21);
                float t = 0.74f - 0.05f * heavy;
                if (p > t) c = Color.Lerp(c, new Color(0.74f, 0.71f, 0.64f), Mathf.Clamp01((p - t) * 12f));
                else if (p > t - 0.012f) c *= 0.82f;   // dark edge of the flake
            }
            c.a = 1f;
            return c;
        }

        // ---- floor: 2 x 2 m, 0.5 m vinyl tiles
        static Color FloorPixel(int x, int y)
        {
            int tx = x / 64, ty = y / 64;
            bool alt = ((tx + ty) & 1) == 1;
            Color c = alt ? new Color(0.70f, 0.73f, 0.70f) : new Color(0.84f, 0.84f, 0.80f);
            int gx = x % 64, gy = y % 64;
            if (gx < 1 || gy < 1) c *= 0.7f;
            float n = Fbm(x / 40f, y / 40f, 7);
            c *= 0.9f + 0.12f * n;
            float scuff = Fbm(x / 8f, y / 60f, 13);
            if (scuff > 0.72f) c *= 0.85f;
            float dirt = Fbm(x / 90f, y / 90f, 3);
            c = Color.Lerp(c, new Color(0.5f, 0.48f, 0.4f), Mathf.Clamp01((dirt - 0.6f) * 2f) * 0.35f);
            c.a = 1f;
            return c;
        }

        // ---- ceiling: 0.5 m acoustic tiles, speckles, a ring stain now and then
        static Color CeilingPixel(int x, int y)
        {
            int gx = x % 64, gy = y % 64;
            Color c = new Color(0.86f, 0.86f, 0.83f);
            if (gx < 2 || gy < 2) return new Color(0.66f, 0.67f, 0.66f);
            if (Hash(x, y, 77) > 0.93f) c *= 0.82f;
            int tx = x / 64, ty = y / 64;
            if (Hash(tx, ty, 5) > 0.72f)
            {
                float cx = tx * 64 + 32 + (Hash(tx, ty, 6) - 0.5f) * 20f, cy = ty * 64 + 32 + (Hash(tx, ty, 8) - 0.5f) * 20f;
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / (10f + Hash(tx, ty, 9) * 14f);
                if (d < 1f) c = Color.Lerp(c, new Color(0.72f, 0.64f, 0.46f), 0.25f + 0.4f * Mathf.Clamp01((d - 0.75f) * 4f));
            }
            c.a = 1f;
            return c;
        }

        static Color ConcretePixel(int x, int y)
        {
            float n = Fbm(x / 30f, y / 30f, 41, 5);
            float s = Hash(x, y, 4) > 0.97f ? 0.85f : 1f;
            Color c = new Color(0.72f, 0.73f, 0.70f) * (0.82f + 0.25f * n) * s;
            c.a = 1f;
            return c;
        }

        static Color LaminatePixel(int x, int y)
        {
            float g = Fbm(x / 120f, y / 6f, 61, 3);
            Color c = new Color(0.76f, 0.66f, 0.51f) * (0.9f + 0.14f * g);
            if (Fbm(x / 20f, y / 20f, 66) > 0.78f) c *= 0.9f;   // worn patches
            c.a = 1f;
            return c;
        }

        static Color BoardPixel(int x, int y)
        {
            float n = Fbm(x / 25f, y / 25f, 91);
            Color c = new Color(0.14f, 0.24f, 0.2f) * (0.85f + 0.3f * n);
            float smudge = Fbm(x / 50f, y / 12f, 95);
            if (smudge > 0.6f) c = Color.Lerp(c, new Color(0.5f, 0.56f, 0.52f), (smudge - 0.6f) * 0.9f);
            if (y < 3 || y > 124 || x < 3 || x > 252) c = new Color(0.45f, 0.38f, 0.28f);
            c.a = 1f;
            return c;
        }

        static Color LockerPixel(int x, int y)
        {
            Color c = Color.white * (0.92f + 0.08f * Fbm(x / 30f, y / 30f, 31));
            if (x < 3 || x > 124) c *= 0.6f;                                // door gap
            if (y > 200 && y < 230 && x > 40 && x < 88 && (y / 4) % 2 == 0) c *= 0.55f;   // vents
            if (x > 100 && x < 110 && y > 120 && y < 150) c *= 0.45f;      // handle
            c.a = 1f;
            return c;
        }

        // ------------------------------------------------------------------ restyle the white box
        static readonly string[] Keys =
        {
            "Wall_Door_2m", "Wall_Window_2m", "Wall_2m", "Wall_1m", "Glue_Corner", "Floor_2x2", "Podium_2x2", "Ceiling_2x2_ShadowOnly", "Ground_60x40",
            "Desk_Student", "Desk_Player", "Desk_Teacher", "Cafe_Table", "Chair", "Cafe_Bench", "Cafe_Counter", "Prop_Locker", "Prop_Shelf", "Prop_BookStack",
            "Pillar", "Blackboard", "Fig_Classmate_Seated", "Fig_Teacher", "Fig_Standing_Player", "Fig_Seated_Player", "Watcher_SecurityCamera", "Marker_Waypoint", "Ruler_3m",
        };

        public static string KeyOf(Transform t, out Transform root)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                foreach (string k in Keys)
                    if (n == k || n.StartsWith(k + " ("))
                    {
                        root = p;
                        return k;
                    }
            }
            root = null;
            return null;
        }

        /// <summary>Unpack every blockout prefab instance under root (so edits never touch the kit).</summary>
        public static void Unpack(Transform root)
        {
            List<GameObject> roots = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
                GameObject r = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
                if (r != null && !roots.Contains(r)) roots.Add(r);
            }
            foreach (GameObject r in roots)
                if (r != null && PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.UnpackPrefabInstance(r, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        /// <summary>Swap the grey blockout materials for the Clearview palette, add faces / frames, remove debug visuals.</summary>
        public static void Restyle(Transform root)
        {
            Unpack(root);
            foreach (TextMesh tm in root.GetComponentsInChildren<TextMesh>(true)) Object.DestroyImmediate(tm.gameObject);
            List<GameObject> kill = new List<GameObject>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "VisionFan") kill.Add(t.gameObject);
                Transform r;
                string k = KeyOf(t, out r);
                if ((k == "Marker_Waypoint" || k == "Ruler_3m" || k == "Fig_Standing_Player" || k == "Fig_Seated_Player") && r == t) kill.Add(t.gameObject);
            }
            foreach (GameObject g in kill) if (g != null) Object.DestroyImmediate(g);

            HashSet<Transform> dressed = new HashSet<Transform>();
            foreach (Renderer rd in root.GetComponentsInChildren<Renderer>(true))
            {
                Transform r;
                string k = KeyOf(rd.transform, out r);
                if (k == null) continue;
                Material m = Pick(k, rd, r);
                if (m != null) rd.sharedMaterial = m;
                if (k == "Ceiling_2x2_ShadowOnly") rd.shadowCastingMode = ShadowCastingMode.On;
                if (r != null && !dressed.Contains(r))
                {
                    dressed.Add(r);
                    Dress(k, r);
                }
            }
        }

        static Material Pick(string k, Renderer rd, Transform r)
        {
            bool eyes = rd.gameObject.name == "Eyes";
            switch (k)
            {
                case "Wall_Door_2m": case "Wall_Window_2m": case "Wall_2m": case "Wall_1m": case "Glue_Corner":
                    Vector3 p = rd.transform.position;
                    int h = Mathf.Abs(Mathf.RoundToInt(p.x * 7.3f + p.z * 3.1f)) % 7;
                    return Walls[h < 3 ? 0 : (h < 6 ? 1 : 2)];
                case "Floor_2x2": return Floor;
                case "Podium_2x2": return FloorAlt;
                case "Ceiling_2x2_ShadowOnly": return Ceiling;
                case "Ground_60x40": return Ground;
                case "Desk_Student": case "Desk_Player": case "Desk_Teacher": case "Cafe_Table": case "Prop_Shelf": return Wood;
                case "Chair": case "Cafe_Bench": return Metal;
                case "Cafe_Counter": return Steel;
                case "Prop_Locker": return Locker;
                case "Prop_BookStack":
                    int b = Mathf.Abs(Mathf.RoundToInt(rd.transform.position.x * 13f + rd.transform.position.z * 5f)) % 3;
                    return b == 0 ? BookA : (b == 1 ? BookB : BookC);
                case "Pillar": return Pillar;
                case "Blackboard": return Board;
                case "Fig_Classmate_Seated": return eyes ? EyeBand : Uniform;
                case "Fig_Teacher": return eyes ? EyeBand : Authority;
                case "Watcher_SecurityCamera": return rd.gameObject.name == "Lens" ? RedLED : CamBody;
            }
            return null;
        }

        static void Dress(string k, Transform r)
        {
            switch (k)
            {
                case "Fig_Classmate_Seated": Seated(r, Hash(Mathf.RoundToInt(r.position.x * 10f), Mathf.RoundToInt(r.position.z * 10f), 3)); break;
                case "Fig_Teacher": Standing(r, false); break;
                case "Wall_Window_2m":
                    Box("Sill", r, new Vector3(1f, 1.0f, 0f), new Vector3(1.3f, 0.04f, 0.28f), Frame);
                    Box("Mullion_V", r, new Vector3(1f, 1.6f, 0f), new Vector3(0.04f, 1.2f, 0.05f), Frame);
                    Box("Mullion_H", r, new Vector3(1f, 1.6f, 0f), new Vector3(1.2f, 0.04f, 0.05f), Frame);
                    Box("Frame_Top", r, new Vector3(1f, 2.2f, 0f), new Vector3(1.24f, 0.05f, 0.22f), Frame);
                    break;
                case "Wall_Door_2m":
                    Box("Jamb_L", r, new Vector3(0.375f, 1.25f, 0f), new Vector3(0.06f, 2.5f, 0.24f), Frame);
                    Box("Jamb_R", r, new Vector3(1.625f, 1.25f, 0f), new Vector3(0.06f, 2.5f, 0.24f), Frame);
                    Box("Head", r, new Vector3(1f, 2.5f, 0f), new Vector3(1.31f, 0.06f, 0.24f), Frame);
                    break;
                case "Watcher_SecurityCamera":
                    GameObject led = Box("LED", r, new Vector3(0.09f, 2.88f, 0.36f), new Vector3(0.025f, 0.025f, 0.025f), RedLED);
                    led.AddComponent<BlinkingLight>().period = 1.1f;
                    foreach (Light l in r.GetComponentsInChildren<Light>(true)) { l.intensity = 2f; l.color = new Color(1f, 0.3f, 0.25f); }
                    break;
            }
        }

        static Material skirt, trousers, blush, white, sock, nose, cardigan;
        static Material[] hairs;
        static Material SkirtMat() { if (skirt == null) skirt = Mat("M_CV_Skirt", new Color(0.2f, 0.24f, 0.38f), null, Vector2.one, 0.15f); return skirt; }
        static Material TrouserMat() { if (trousers == null) trousers = Mat("M_CV_Trousers", new Color(0.27f, 0.29f, 0.33f), null, Vector2.one, 0.15f); return trousers; }
        static Material BlushMat() { if (blush == null) blush = Mat("M_CV_Blush", new Color(0.92f, 0.68f, 0.68f), null, Vector2.one, 0.3f); return blush; }
        static Material WhiteMat() { if (white == null) white = Mat("M_CV_EyeHighlight", Color.white, null, Vector2.one, 0.9f, 0f, new Color(0.7f, 0.7f, 0.7f)); return white; }
        static Material SockMat() { if (sock == null) sock = Mat("M_CV_Socks", new Color(0.96f, 0.96f, 0.94f), null, Vector2.one, 0.1f); return sock; }
        static Material NoseMat() { if (nose == null) nose = Mat("M_CV_Nose", new Color(0.93f, 0.7f, 0.6f), null, Vector2.one, 0.25f); return nose; }
        static Material CardiganMat() { if (cardigan == null) cardigan = Mat("M_CV_Cardigan", new Color(0.55f, 0.6f, 0.68f), null, Vector2.one, 0.1f); return cardigan; }
        static Material HairMat(int v)
        {
            if (hairs == null)
                hairs = new[]
                {
                    Mat("M_CV_Hair_Black", new Color(0.12f, 0.1f, 0.1f), null, Vector2.one, 0.35f),
                    Mat("M_CV_Hair_Brown", new Color(0.36f, 0.22f, 0.13f), null, Vector2.one, 0.35f),
                    Mat("M_CV_Hair_Chestnut", new Color(0.5f, 0.27f, 0.15f), null, Vector2.one, 0.35f),
                    Mat("M_CV_Hair_Sand", new Color(0.72f, 0.56f, 0.34f), null, Vector2.one, 0.35f),
                };
            return hairs[Mathf.Abs(v) % hairs.Length];
        }

        /// <summary>A primitive part (no collider). The figure's original blockout collider still does the gameplay (cover, vision).</summary>
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        /// <summary>Start a fresh dress-up group on a figure (the blockout block stays the body).</summary>
        static Transform NewLook(Transform fig)
        {
            Undress(fig);
            foreach (Transform c in fig)
                if (c.name == "Body" || c.name == "Eyes") { Renderer r = c.GetComponent<Renderer>(); if (r != null) r.enabled = true; }
            Transform look = new GameObject("Chibi").transform;   // (name kept so older scene code still finds it)
            look.SetParent(fig, false);
            return look;
        }

        static bool HasBlock(Transform fig) { return fig.Find("Body") != null; }

        public static void Undress(Transform fig)
        {
            List<GameObject> kill = new List<GameObject>();
            foreach (Transform c in fig) if (c.name == "Chibi") kill.Add(c.gameObject);
            foreach (GameObject g in kill) Object.DestroyImmediate(g);
        }

        static int Variant(Transform fig)
        {
            Vector3 p = fig.position;
            return Mathf.Abs(Mathf.RoundToInt(p.x * 7.1f) * 31 + Mathf.RoundToInt(p.z * 5.3f) * 17);
        }

        /// <summary>Bow (two lobes + knot). Used on girls' collars and hair.</summary>
        public static void Bow(Transform parent, Vector3 p, float size, Material m)
        {
            Box("Bow_L", parent, p + new Vector3(-size * 0.5f, 0f, 0f), new Vector3(size * 0.9f, size * 0.65f, size * 0.4f), m, false, new Vector3(0f, 0f, 18f));
            Box("Bow_R", parent, p + new Vector3(size * 0.5f, 0f, 0f), new Vector3(size * 0.9f, size * 0.65f, size * 0.4f), m, false, new Vector3(0f, 0f, -18f));
            Box("Bow_Knot", parent, p + new Vector3(0f, 0f, size * 0.05f), new Vector3(size * 0.35f, size * 0.5f, size * 0.45f), m);
        }

        /// <summary>Face details on a block head: ears, brows, a nose, a thin mouth. The dark eye band stays as it is.</summary>
        static void Face(Transform l, Vector3 c, float half, float front, Material hair)
        {
            Box("Face", l, c, new Vector3(half * 2f, half * 2f, front * 2f - 0.005f), Skin);
            foreach (int sgn in new[] { -1, 1 })
            {
                Box("Ear", l, c + new Vector3(sgn * (half + 0.01f), -0.01f, -0.01f), new Vector3(0.022f, 0.06f, 0.05f), Skin);
                Box("Brow", l, c + new Vector3(sgn * half * 0.36f, half * 0.34f, front + 0.004f), new Vector3(half * 0.5f, 0.012f, 0.01f), hair);
            }
            Box("Nose", l, c + new Vector3(0f, -half * 0.38f, front + 0.008f), new Vector3(0.03f, 0.04f, 0.02f), NoseMat());
            Box("Mouth", l, c + new Vector3(0f, -half * 0.68f, front + 0.003f), new Vector3(half * 0.45f, 0.008f, 0.008f), EyeBand);
        }

        /// <summary>
        /// Neck + shirt collar, designed so no face lies on the body block's faces (that flickered):
        /// a skin neck in the gap between torso and head, a collar band whose bottom sits 5 mm inside the torso and whose
        /// top sits inside the head, and two collar points tilted on the chest (a different angle, so never flush).
        /// torsoTop / chestZ / headBottom come from the white-box body (Fig_Seated: 1.10 / 0.08 / 1.12, Fig_Standing: 1.50 / 0.14 / 1.52).
        /// </summary>
        static void Collar(Transform l, float torsoTop, float chestZ, float headBottom, float pointY)
        {
            float neckH = headBottom - torsoTop + 0.03f;
            Box("Neck", l, new Vector3(0f, (torsoTop + headBottom) * 0.5f, -0.005f), new Vector3(0.1f, neckH, 0.1f), Skin);
            Box("CollarBand", l, new Vector3(0f, torsoTop + 0.012f, 0f), new Vector3(0.155f, 0.034f, 0.15f), Pillow);
            foreach (int sgn in new[] { -1, 1 })
                Box("CollarPoint", l, new Vector3(sgn * 0.034f, pointY, chestZ + 0.013f), new Vector3(0.062f, 0.028f, 0.006f), Pillow, false, new Vector3(0f, 0f, sgn * 32f));
        }

        /// <summary>Girl or boy (decided per seat, about half and half).</summary>
        public static void Seated(Transform fig, float rnd) { Seated(fig, rnd > 0.5f); }

        public static readonly Vector3 SeatedHeadTop = new Vector3(0f, 1.42f, -0.06f);

        /// <summary>
        /// A seated student on the white-box block body, with finer details: face (ears, brows, nose, mouth),
        /// a white shirt front under the uniform, arms resting forward, hands, shoes.
        /// Girls: long hair / bob / ponytail, a bow, a navy skirt, bare knees and white socks.
        /// Boys: short hair, a tie, grey trousers.
        /// </summary>
        public static void Seated(Transform fig, bool girl)
        {
            int v = Variant(fig);
            Material hair = HairMat(v);
            Transform l = NewLook(fig);
            if (!HasBlock(fig))
            {
                // a figure made from scratch (the basement): give it the same block body
                Box("Torso", l, new Vector3(0f, 0.775f, -0.04f), new Vector3(0.4f, 0.65f, 0.24f), Uniform);
                Box("Thighs", l, new Vector3(0f, 0.525f, 0.25f), new Vector3(0.4f, 0.15f, 0.34f), Uniform);
                Box("Shins", l, new Vector3(0f, 0.225f, 0.36f), new Vector3(0.4f, 0.45f, 0.12f), Uniform);
                Box("EyeBand", l, new Vector3(0f, 1.25f, 0.135f), new Vector3(0.18f, 0.05f, 0.03f), EyeBand);
            }
            Face(l, new Vector3(0f, 1.25f, 0.005f), 0.135f, 0.1225f, hair);
            Collar(l, 1.10f, 0.08f, 1.12f, 1.075f);
            Box("Shirt", l, new Vector3(0f, 0.97f, 0.083f), new Vector3(0.12f, 0.2f, 0.008f), Pillow);
            foreach (int sgn in new[] { -1, 1 })
            {
                Box("UpperArm", l, new Vector3(sgn * 0.225f, 0.88f, -0.03f), new Vector3(0.06f, 0.3f, 0.1f), Uniform);
                Box("Forearm", l, new Vector3(sgn * 0.2f, 0.75f, 0.17f), new Vector3(0.065f, 0.065f, 0.38f), Uniform, false, new Vector3(-8f, sgn * -6f, 0f));
                Box("Cuff", l, new Vector3(sgn * 0.18f, 0.765f, 0.35f), new Vector3(0.07f, 0.07f, 0.03f), Pillow);
                Box("Hand", l, new Vector3(sgn * 0.175f, 0.77f, 0.405f), new Vector3(0.06f, 0.035f, 0.08f), Skin);
            }
            Box("Shoes", l, new Vector3(0f, 0.04f, 0.4f), new Vector3(0.42f, 0.08f, 0.17f), Black);
            if (girl)
            {
                int style = v % 3;
                Box("Hair", l, new Vector3(0f, 1.38f, -0.01f), new Vector3(0.29f, 0.06f, 0.275f), hair);
                Box("Bangs", l, new Vector3(0f, 1.335f, 0.13f), new Vector3(0.27f, 0.06f, 0.02f), hair);
                if (style == 0)
                {
                    Box("HairBack", l, new Vector3(0f, 1.15f, -0.135f), new Vector3(0.29f, 0.5f, 0.05f), hair);
                    Box("HairSide_L", l, new Vector3(-0.15f, 1.19f, -0.01f), new Vector3(0.03f, 0.38f, 0.22f), hair);
                    Box("HairSide_R", l, new Vector3(0.15f, 1.19f, -0.01f), new Vector3(0.03f, 0.38f, 0.22f), hair);
                }
                else if (style == 1)
                {
                    // bob
                    Box("HairBack", l, new Vector3(0f, 1.25f, -0.135f), new Vector3(0.3f, 0.3f, 0.05f), hair);
                    Box("HairSide_L", l, new Vector3(-0.15f, 1.25f, -0.01f), new Vector3(0.03f, 0.26f, 0.24f), hair);
                    Box("HairSide_R", l, new Vector3(0.15f, 1.25f, -0.01f), new Vector3(0.03f, 0.26f, 0.24f), hair);
                    Box("Clip", l, new Vector3(0.1f, 1.33f, 0.135f), new Vector3(0.05f, 0.015f, 0.01f), Pastels[v % Pastels.Length]);
                }
                else
                {
                    // ponytail
                    Box("HairBack", l, new Vector3(0f, 1.27f, -0.13f), new Vector3(0.29f, 0.24f, 0.04f), hair);
                    Box("Tie", l, new Vector3(0f, 1.3f, -0.165f), new Vector3(0.05f, 0.04f, 0.04f), Pastels[v % Pastels.Length]);
                    Box("Ponytail", l, new Vector3(0f, 1.17f, -0.19f), new Vector3(0.08f, 0.26f, 0.06f), hair, false, new Vector3(-12f, 0f, 0f));
                }
                Bow(l, new Vector3(0f, 1.03f, 0.092f), 0.065f, Tie);
                Box("Skirt", l, new Vector3(0f, 0.53f, 0.245f), new Vector3(0.45f, 0.17f, 0.39f), SkirtMat());
                Box("SkirtFront", l, new Vector3(0f, 0.45f, 0.44f), new Vector3(0.45f, 0.2f, 0.02f), SkirtMat());
                Box("Pleat_L", l, new Vector3(-0.1f, 0.45f, 0.452f), new Vector3(0.006f, 0.2f, 0.004f), Black);
                Box("Pleat_R", l, new Vector3(0.1f, 0.45f, 0.452f), new Vector3(0.006f, 0.2f, 0.004f), Black);
                Box("Legs", l, new Vector3(0f, 0.32f, 0.36f), new Vector3(0.405f, 0.18f, 0.125f), Skin);
                Box("Socks", l, new Vector3(0f, 0.13f, 0.36f), new Vector3(0.41f, 0.22f, 0.13f), SockMat());
            }
            else
            {
                Box("Hair", l, new Vector3(0f, 1.375f, -0.015f), new Vector3(0.285f, 0.06f, 0.27f), hair);
                Box("Fringe", l, new Vector3(0.03f, 1.345f, 0.128f), new Vector3(0.22f, 0.04f, 0.02f), hair);
                Box("HairBack", l, new Vector3(0f, 1.3f, -0.12f), new Vector3(0.285f, 0.14f, 0.04f), hair);
                Box("Sideburn_L", l, new Vector3(-0.142f, 1.3f, 0.02f), new Vector3(0.02f, 0.1f, 0.1f), hair);
                Box("Sideburn_R", l, new Vector3(0.142f, 1.3f, 0.02f), new Vector3(0.02f, 0.1f, 0.1f), hair);
                Box("Tie", l, new Vector3(0f, 0.93f, 0.09f), new Vector3(0.05f, 0.24f, 0.01f), Tie);
                Box("Trousers_Thigh", l, new Vector3(0f, 0.525f, 0.25f), new Vector3(0.41f, 0.16f, 0.35f), TrouserMat());
                Box("Trousers_Shin", l, new Vector3(0f, 0.25f, 0.36f), new Vector3(0.405f, 0.4f, 0.125f), TrouserMat());
            }
            NoCoplanarFaces(fig);
        }

        /// <summary>
        /// A standing adult on the white-box block body (teacher, monitor, warden), with face details, arms, hands,
        /// a shirt front and shoes. Women: bun + skirt + dark stockings. Men: short hair + tie.
        /// </summary>
        public static void Standing(Transform fig, bool cap, bool female = false)
        {
            int v = Variant(fig) + (female ? 1 : 0);
            Material hair = HairMat(v);
            Renderer bodyR = fig.Find("Body") != null ? fig.Find("Body").GetComponent<Renderer>() : null;
            Material suit = bodyR != null && bodyR.sharedMaterial != null ? bodyR.sharedMaterial : Authority;
            Transform l = NewLook(fig);
            Face(l, new Vector3(0f, 1.66f, 0f), 0.145f, 0.145f, hair);
            if (cap)
            {
                Box("Cap", l, new Vector3(0f, 1.82f, -0.01f), new Vector3(0.31f, 0.08f, 0.31f), Warden);
                Box("CapBrim", l, new Vector3(0f, 1.79f, 0.19f), new Vector3(0.3f, 0.02f, 0.11f), Warden);
                Box("CapBadge", l, new Vector3(0f, 1.83f, 0.158f), new Vector3(0.06f, 0.04f, 0.01f), FadedYellowMat());
            }
            else
            {
                Box("Hair", l, new Vector3(0f, 1.8f, -0.01f), new Vector3(0.305f, 0.05f, 0.3f), hair);
                Box("HairBack", l, new Vector3(0f, female ? 1.66f : 1.72f, -0.15f), new Vector3(0.305f, female ? 0.28f : 0.16f, 0.03f), hair);
                Box("Fringe", l, new Vector3(female ? 0f : 0.03f, 1.77f, 0.152f), new Vector3(female ? 0.29f : 0.22f, 0.04f, 0.02f), hair);
            }
            Collar(l, 1.50f, 0.14f, 1.52f, 1.455f);
            Box("Shirt", l, new Vector3(0f, 1.3f, 0.143f), new Vector3(0.12f, 0.3f, 0.008f), Pillow);
            Box("Badge", l, new Vector3(0.12f, 1.25f, 0.145f), new Vector3(0.06f, 0.04f, 0.01f), FadedYellowMat());
            foreach (int sgn in new[] { -1, 1 })
            {
                Box("Arm", l, new Vector3(sgn * 0.26f, 1.08f, 0f), new Vector3(0.07f, 0.6f, 0.12f), suit);
                Box("Cuff", l, new Vector3(sgn * 0.26f, 0.79f, 0f), new Vector3(0.075f, 0.04f, 0.125f), Pillow);
                Box("Hand", l, new Vector3(sgn * 0.26f, 0.72f, 0f), new Vector3(0.06f, 0.1f, 0.09f), Skin);
            }
            Box("Shoes", l, new Vector3(0f, 0.04f, 0.04f), new Vector3(0.44f, 0.08f, 0.32f), Black);
            if (female)
            {
                Box("Bun", l, new Vector3(0f, 1.74f, -0.2f), new Vector3(0.15f, 0.15f, 0.11f), hair);
                Box("Skirt", l, new Vector3(0f, 0.72f, 0f), new Vector3(0.53f, 0.5f, 0.34f), suit);
                Box("Stockings", l, new Vector3(0f, 0.25f, 0.02f), new Vector3(0.46f, 0.42f, 0.29f), Black);
            }
            else
            {
                Box("Tie", l, new Vector3(0f, 1.27f, 0.15f), new Vector3(0.06f, 0.3f, 0.01f), Black);
                Box("Belt", l, new Vector3(0f, 0.9f, 0f), new Vector3(0.455f, 0.04f, 0.285f), Black);
            }
            // gaze / flash lights sit in front of the face
            foreach (Light g in fig.GetComponentsInChildren<Light>(true))
            {
                Vector3 p = g.transform.localPosition;
                if (p.z < 0.32f) g.transform.localPosition = new Vector3(p.x, p.y, 0.32f);
            }
            NoCoplanarFaces(fig);
        }

        /// <summary>
        /// Anti-flicker (z-fighting): two box parts of a figure whose faces lie in the same plane and face the same way
        /// (e.g. hair flush with the top of the head, a shirt flush with the torso) flicker when the camera moves.
        /// For every such pair the smaller part grows by a few millimetres on that axis, so its face sits clearly in
        /// front. Runs a few passes, because one fix can create a new flush pair.
        /// </summary>
        public static void NoCoplanarFaces(Transform fig, float gap = 0.004f)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                List<Transform> boxes = new List<Transform>();
                List<Part> fixedParts = new List<Part>();
                int fixes = 0;
                foreach (MeshFilter mf in fig.GetComponentsInChildren<MeshFilter>(true))
                {
                    Renderer rr = mf.GetComponent<Renderer>();
                    if (mf.sharedMesh == null || rr == null || !rr.enabled) continue;
                    if (mf.sharedMesh.name == "Cube") boxes.Add(mf.transform);
                    else AddBlockParts(mf.transform, mf.sharedMesh.name, fixedParts);   // the white-box body: never resized
                }
                // a part flush with the white-box body grows outward
                foreach (Transform b in boxes)
                    foreach (Part f in fixedParts)
                    {
                        if (Quaternion.Angle(b.rotation, f.rot) > 0.05f) continue;
                        int k = FlushAxis(f.centre, f.half, f.rot, b.position, Half(b));
                        if (k >= 0) { Grow(b, k, gap); fixes++; break; }
                    }
                for (int i = 0; i < boxes.Count; i++)
                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        Transform A = boxes[i], B = boxes[j];
                        if (Quaternion.Angle(A.rotation, B.rotation) > 0.05f) continue;   // only parts with the same orientation
                        int k = FlushAxis(A.position, Half(A), A.rotation, B.position, Half(B));
                        if (k < 0) continue;
                        Vector3 ha = Half(A), hb = Half(B);
                        Grow(ha.x * ha.y * ha.z >= hb.x * hb.y * hb.z ? B : A, k, gap);
                        fixes++;
                    }
                if (fixes == 0) break;
            }
        }

        struct Part { public Vector3 centre, half; public Quaternion rot; }

        static Vector3 Half(Transform t)
        {
            Vector3 h = t.lossyScale * 0.5f;
            return new Vector3(Mathf.Abs(h.x), Mathf.Abs(h.y), Mathf.Abs(h.z));
        }

        /// <summary>The boxes the white-box kit (BlockoutKitBuilder) welds into its figure meshes, in the mesh's local space.</summary>
        static void AddBlockParts(Transform t, string mesh, List<Part> into)
        {
            List<Vector3[]> mm = new List<Vector3[]>();
            if (mesh == "Fig_Seated")
            {
                mm.Add(new[] { new Vector3(-0.2f, 0.45f, -0.16f), new Vector3(0.2f, 1.10f, 0.08f) });
                mm.Add(new[] { new Vector3(-0.2f, 0.45f, 0.08f), new Vector3(0.2f, 0.60f, 0.42f) });
                mm.Add(new[] { new Vector3(-0.2f, 0f, 0.30f), new Vector3(0.2f, 0.45f, 0.42f) });
                mm.Add(new[] { new Vector3(-0.13f, 1.12f, -0.12f), new Vector3(0.13f, 1.38f, 0.12f) });
            }
            else if (mesh == "Fig_Seated_Eyes") mm.Add(new[] { new Vector3(-0.09f, 1.225f, 0.12f), new Vector3(0.09f, 1.275f, 0.15f) });
            else if (mesh == "Fig_Standing")
            {
                mm.Add(new[] { new Vector3(-0.225f, 0f, -0.14f), new Vector3(0.225f, 1.5f, 0.14f) });
                mm.Add(new[] { new Vector3(-0.14f, 1.52f, -0.14f), new Vector3(0.14f, 1.8f, 0.14f) });
            }
            else if (mesh == "Fig_Standing_Eyes") mm.Add(new[] { new Vector3(-0.1f, 1.575f, 0.14f), new Vector3(0.1f, 1.625f, 0.17f) });
            foreach (Vector3[] b in mm)
            {
                Vector3 c = (b[0] + b[1]) * 0.5f, h = (b[1] - b[0]) * 0.5f;
                Vector3 s = t.lossyScale;
                into.Add(new Part { centre = t.TransformPoint(c), half = new Vector3(Mathf.Abs(h.x * s.x), Mathf.Abs(h.y * s.y), Mathf.Abs(h.z * s.z)), rot = t.rotation });
            }
        }

        /// <summary>Axis (0/1/2) on which box B has a face in the same plane, facing the same way, as box A (overlapping); -1 if none.</summary>
        static int FlushAxis(Vector3 ca, Vector3 ha, Quaternion ra, Vector3 cb, Vector3 hb)
        {
            const float near = 0.0025f;
            Vector3 d = Quaternion.Inverse(ra) * (cb - ca);
            for (int k = 0; k < 3; k++)
            {
                int k1 = (k + 1) % 3, k2 = (k + 2) % 3;
                if (Mathf.Abs(d[k1]) >= ha[k1] + hb[k1] - 0.0005f || Mathf.Abs(d[k2]) >= ha[k2] + hb[k2] - 0.0005f) continue;
                foreach (int sgn in new[] { -1, 1 })
                    if (Mathf.Abs(sgn * ha[k] - (d[k] + sgn * hb[k])) < near) return k;
            }
            return -1;
        }

        /// <summary>Makes a box 2 x gap longer on one of its own axes (each face moves out by 'gap').</summary>
        static void Grow(Transform b, int k, float gap)
        {
            Vector3 ls = b.localScale;
            float per = Mathf.Abs(ls[k]) > 1e-5f ? Mathf.Abs(b.lossyScale[k] / ls[k]) : 1f;
            if (per < 1e-5f) per = 1f;
            ls[k] += Mathf.Sign(ls[k] == 0f ? 1f : ls[k]) * 2f * gap / per;
            b.localScale = ls;
        }

        static Material fy;
        static Material FadedYellowMat() { if (fy == null) fy = Mat("M_CV_Badge", FadedYellow, null, Vector2.one, 0.6f, 0.6f); return fy; }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material m, bool collider = false, Vector3? euler = null)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        // ------------------------------------------------------------------ fixtures & props
        /// <summary>Strict grid of ceiling fluorescents in a rectangle (x0,z0)-(x1,z1). 'flickerEvery' = every Nth one flickers.</summary>
        public static void Fluorescents(Transform parent, float x0, float z0, float x1, float z1, int nx, int nz, float intensity, int flickerEvery, bool alongZ = false)
        {
            Transform g = Group("Fluorescents", parent);
            int i = 0;
            for (int a = 0; a < nx; a++)
                for (int b = 0; b < nz; b++)
                {
                    float x = Mathf.Lerp(x0, x1, (a + 0.5f) / nx), z = Mathf.Lerp(z0, z1, (b + 0.5f) / nz);
                    i++;
                    Fixture(g, new Vector3(x, 2.985f, z), intensity, flickerEvery > 0 && i % flickerEvery == 0, alongZ);
                }
        }

        public static Fluorescent Fixture(Transform parent, Vector3 pos, float intensity, bool flicker, bool alongZ)
        {
            GameObject root = new GameObject(flicker ? "Fluorescent (flickers)" : "Fluorescent");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, alongZ ? 90f : 0f, 0f);
            Box("Housing", root.transform, Vector3.zero, new Vector3(1.25f, 0.05f, 0.3f), FixtureBody);
            GameObject tube = Box("Tube", root.transform, new Vector3(0f, -0.032f, 0f), new Vector3(1.15f, 0.014f, 0.2f), Tube);
            tube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            GameObject lg = new GameObject("Light");
            lg.transform.SetParent(root.transform, false);
            lg.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            Light l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = intensity;
            l.color = new Color(0.92f, 0.98f, 1f);
            l.shadows = LightShadows.None;
            Fluorescent f = root.AddComponent<Fluorescent>();
            f.lightSource = l;
            f.tube = tube.GetComponent<Renderer>();
            f.flicker = flicker;
            return f;
        }

        /// <summary>A decorative ceiling camera with a blinking red LED. yaw = where it looks.</summary>
        public static GameObject DecoCamera(Transform parent, Vector3 pos, float yaw)
        {
            GameObject root = new GameObject("Camera (decor)");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            Box("Mount", root.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.05f, 0.12f, 0.05f), CamBody);
            Box("Body", root.transform, new Vector3(0f, -0.04f, 0.12f), new Vector3(0.14f, 0.12f, 0.3f), CamBody, false, new Vector3(18f, 0f, 0f));
            Box("Lens", root.transform, new Vector3(0f, -0.09f, 0.27f), new Vector3(0.08f, 0.08f, 0.02f), Black, false, new Vector3(18f, 0f, 0f));
            GameObject led = Box("LED", root.transform, new Vector3(0.05f, 0.0f, 0.24f), new Vector3(0.02f, 0.02f, 0.02f), RedLED);
            led.AddComponent<BlinkingLight>().period = 1.3f;
            return root;
        }

        /// <summary>PA speaker on a wall. lookYaw = direction a viewer looks to face it.</summary>
        public static void Speaker(Transform parent, Vector3 wallPoint, float lookYaw)
        {
            Quaternion r = Quaternion.Euler(0f, lookYaw, 0f);
            GameObject root = new GameObject("PA Speaker");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(wallPoint - r * Vector3.forward * 0.055f, r);
            Box("Box", root.transform, Vector3.zero, new Vector3(0.36f, 0.26f, 0.1f), CamBody);
            for (int i = 0; i < 5; i++) Box("Grille", root.transform, new Vector3(0f, -0.08f + i * 0.04f, -0.051f), new Vector3(0.28f, 0.012f, 0.004f), Black);
        }

        /// <summary>Institutional wall clock (the hands really move). lookYaw = direction a viewer looks to face it.</summary>
        public static WallClock Clock(Transform parent, Vector3 wallPoint, float lookYaw, float startHour)
        {
            Quaternion r = Quaternion.Euler(0f, lookYaw, 0f);
            GameObject root = new GameObject("Wall Clock");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(wallPoint - r * Vector3.forward * 0.03f, r);
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            face.name = "Face";
            Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(root.transform, false);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            face.transform.localScale = new Vector3(0.42f, 0.015f, 0.42f);
            face.GetComponent<Renderer>().sharedMaterial = ClockFace;
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            Object.DestroyImmediate(rim.GetComponent<Collider>());
            rim.transform.SetParent(root.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rim.transform.localScale = new Vector3(0.46f, 0.012f, 0.46f);
            rim.GetComponent<Renderer>().sharedMaterial = Black;
            for (int i = 0; i < 12; i++)
            {
                Quaternion q = Quaternion.Euler(0f, 0f, i * 30f);
                Box("Tick", root.transform, q * new Vector3(0f, 0.18f, 0f) + new Vector3(0f, 0f, -0.017f), new Vector3(0.012f, i % 3 == 0 ? 0.04f : 0.022f, 0.004f), Black, false, new Vector3(0f, 0f, i * 30f));
            }
            Transform hour = new GameObject("HourHand").transform;
            hour.SetParent(root.transform, false);
            hour.localPosition = new Vector3(0f, 0f, -0.02f);
            Box("Hand", hour, new Vector3(0f, 0.055f, 0f), new Vector3(0.018f, 0.11f, 0.004f), Black);
            Transform minute = new GameObject("MinuteHand").transform;
            minute.SetParent(root.transform, false);
            minute.localPosition = new Vector3(0f, 0f, -0.024f);
            Box("Hand", minute, new Vector3(0f, 0.08f, 0f), new Vector3(0.011f, 0.16f, 0.004f), Black);
            WallClock c = root.AddComponent<WallClock>();
            c.hourHand = hour;
            c.minuteHand = minute;
            c.startHour = startHour;
            return c;
        }

        /// <summary>
        /// A printed sign / poster (world-space UI, typed in the typewriter font at runtime).
        /// wallPoint = point on the wall surface; lookYaw = direction the viewer looks to face it.
        /// </summary>
        public static Canvas Poster(Transform parent, string name, Vector3 wallPoint, float lookYaw, float w, float h,
            Color bg, Color ink, string title, string body = null, Color? stripe = null, float dim = 1f)
        {
            Quaternion r = Quaternion.Euler(0f, lookYaw, 0f);
            Canvas c = UIFactory.MakeWorldCanvas(name, parent, wallPoint - r * Vector3.forward * 0.012f, r, w, h);
            c.gameObject.AddComponent<TypewriterFont>();
            RectTransform root = (RectTransform)c.transform;
            UIFactory.AddImage(UIFactory.Stretch("BG", root), Dim(bg, dim));
            float pad = Mathf.Min(w, h) * 1000f * 0.07f;
            if (stripe.HasValue)
                UIFactory.AddImage(UIFactory.Rect("Stripe", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, h * 1000f * 0.12f)), Dim(stripe.Value, dim));
            float H = h * 1000f;
            bool hasBody = !string.IsNullOrEmpty(body);
            Text t = UIFactory.AddText(UIFactory.Rect("Title", root, new Vector2(0f, hasBody ? 0.45f : 0f), Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, stripe.HasValue ? -H * 0.06f : 0f), new Vector2(-pad * 2f, -pad)),
                title, Mathf.RoundToInt(Mathf.Min(H * (hasBody ? 0.16f : 0.3f), w * 1000f / Mathf.Max(6, LongestLine(title)) * 1.45f)), Dim(ink, dim), TextAnchor.MiddleCenter);
            t.fontStyle = FontStyle.Bold;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10;
            t.resizeTextMaxSize = t.fontSize;
            if (hasBody)
            {
                Text b = UIFactory.AddText(UIFactory.Rect("Body", root, Vector2.zero, new Vector2(1f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-pad * 2f, -pad)),
                    body, Mathf.RoundToInt(H * 0.07f), Dim(ink, dim * 0.85f), TextAnchor.UpperCenter);
                b.resizeTextForBestFit = true;
                b.resizeTextMinSize = 8;
                b.resizeTextMaxSize = b.fontSize;
            }
            return c;
        }

        static int LongestLine(string s)
        {
            int best = 0;
            foreach (string l in s.Split('\n')) best = Mathf.Max(best, l.Length);
            return best;
        }

        static Color Dim(Color c, float d) { return new Color(c.r * d, c.g * d, c.b * d, c.a); }

        /// <summary>Triangular pastel flags hung in a line (absurd celebration).</summary>
        public static void Bunting(Transform parent, Vector3 a, Vector3 b, float sag, int flags)
        {
            Transform g = Group("Bunting", parent);
            for (int i = 0; i < flags; i++)
            {
                float t = (i + 0.5f) / flags;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * sag * 4f * t * (1f - t);
                Vector3 dir = (b - a); dir.y = 0f;
                Quaternion r = Quaternion.LookRotation(Vector3.Cross(dir.normalized, Vector3.up));
                GameObject f = Box("Flag", g, p - new Vector3(0f, 0.09f, 0f), new Vector3(0.16f, 0.16f, 0.004f), Pastels[i % Pastels.Length]);
                f.transform.rotation = r * Quaternion.Euler(0f, 0f, 45f);
                f.transform.localScale = new Vector3(0.14f, 0.14f, 0.004f);
            }
            // the string
            Vector3 mid = (a + b) * 0.5f + Vector3.down * sag;
            Stringy(g, a, mid);
            Stringy(g, mid, b);
        }

        static void Stringy(Transform g, Vector3 a, Vector3 b)
        {
            GameObject s = Box("String", g, Vector3.zero, Vector3.one, Pillow);
            s.transform.position = (a + b) * 0.5f;
            s.transform.rotation = Quaternion.LookRotation(b - a);
            s.transform.localScale = new Vector3(0.006f, 0.006f, Vector3.Distance(a, b));
        }

        public static void Balloon(Transform parent, Vector3 floorPoint, float height, int colour)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "Balloon";
            Object.DestroyImmediate(b.GetComponent<Collider>());
            b.transform.SetParent(parent, false);
            b.transform.position = floorPoint + Vector3.up * height;
            b.transform.localScale = new Vector3(0.3f, 0.36f, 0.3f);
            b.GetComponent<Renderer>().sharedMaterial = Pastels[colour % Pastels.Length];
            Stringy(parent, floorPoint + Vector3.up * 0.8f, b.transform.position - Vector3.up * 0.18f);
        }

        public static Transform Group(string name, Transform parent)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        // ------------------------------------------------------------------ lighting & post
        public static void DayLighting(Light sun)
        {
            if (sun != null)
            {
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.intensity = 1.0f;
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.62f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.49f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.27f, 0.25f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.76f, 0.74f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 70f;
        }

        public static void NightLighting(Light moon)
        {
            if (moon != null)
            {
                moon.color = new Color(0.55f, 0.64f, 0.9f);
                moon.intensity = 0.32f;
                moon.shadows = LightShadows.Soft;
                RenderSettings.sun = moon;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.04f);
            RenderSettings.fogStartDistance = 6f;
            RenderSettings.fogEndDistance = 40f;
            RenderSettings.skybox = null;
        }

        /// <summary>Scene post-processing: slightly desaturated, cool, grainy, a soft vignette. Saved as an asset.</summary>
        public static void PostVolume(Transform parent, bool night)
        {
            string path = Dir + (night ? "/PP_Night.asset" : "/PP_Day.asset");
            VolumeProfile p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p != null) AssetDatabase.DeleteAsset(path);
            p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);

            Tonemapping tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);
            ColorAdjustments ca = p.Add<ColorAdjustments>(true);
            ca.saturation.Override(night ? -35f : -18f);
            ca.contrast.Override(night ? 18f : 10f);
            ca.postExposure.Override(night ? 0.2f : 0.05f);
            WhiteBalance wb = p.Add<WhiteBalance>(true);
            wb.temperature.Override(night ? -25f : -8f);
            wb.tint.Override(night ? 4f : 6f);       // a little green: "clinical"
            Bloom bl = p.Add<Bloom>(true);
            bl.threshold.Override(1.0f);
            bl.intensity.Override(night ? 1.1f : 0.55f);
            bl.scatter.Override(0.6f);
            Vignette vg = p.Add<Vignette>(true);
            vg.intensity.Override(night ? 0.38f : 0.24f);
            vg.smoothness.Override(0.6f);
            FilmGrain fg = p.Add<FilmGrain>(true);
            fg.type.Override(FilmGrainLookup.Medium3);
            fg.intensity.Override(night ? 0.55f : 0.3f);
            foreach (VolumeComponent c in p.components) { c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(c, p); }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();

            GameObject g = new GameObject(night ? "PostProcess (Night)" : "PostProcess (Day)");
            g.transform.SetParent(parent, false);
            Volume v = g.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 0f;
            v.sharedProfile = p;
        }

        public static void SetupCamera(Camera cam)
        {
            UniversalAdditionalCameraData d = cam.GetUniversalAdditionalCameraData();
            if (d != null)
            {
                d.renderPostProcessing = true;
                d.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            string parent = path.Substring(0, i);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(i + 1));
        }
    }
}
