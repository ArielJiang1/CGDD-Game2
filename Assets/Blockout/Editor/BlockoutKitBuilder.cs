using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using B = Blockout.BlockoutMeshBuilder;
using M = Blockout.BlockoutMetrics;

namespace Blockout
{
    /// <summary>
    /// Menu: Blockout > 1. Build Module Kit (Prefabs)
    ///
    /// Generates the building-block modules (meshes, materials, grid texture) and saves each one
    /// as a prefab in Assets/Blockout/Prefabs. Safe to run again: assets are updated in place,
    /// so scenes that already use the prefabs keep working.
    ///
    /// PIVOT CONVENTIONS (keep these if you later replace a mesh with a Blender model):
    ///   Floor_/Podium_/Ceiling_  : pivot at the MIN corner (x=0,z=0), top surface at y=0 (floor) - snap to a 2 m grid.
    ///   Wall_*                   : pivot at the START of the wall, on its centre line, at floor level; wall runs along +X.
    ///   Everything else          : pivot at bottom-centre; FRONT faces +Z.
    ///
    /// COLOUR LANGUAGE (David Shaver, GDC 2018 "Invisible Intuition": greybox should not be a
    /// single colour - use consistent shape and colour language):
    ///   light grey  = floor            off-white = walls / structure
    ///   blue        = COVER (anything that blocks the watcher's view - classmates, book stacks, lockers, pillars)
    ///   yellow      = player           red       = authority / watchers (teacher, camera) and their gaze
    ///   green       = landmark (blackboard)       orange    = patrol route markers, rulers
    ///   beige       = ordinary furniture
    /// </summary>
    public static class BlockoutKitBuilder
    {
        public const string Root = "Assets/Blockout";
        public const string GenRoot = Root + "/Generated";
        public const string MeshDir = GenRoot + "/Meshes";
        public const string MatDir = GenRoot + "/Materials";
        public const string TexDir = GenRoot + "/Textures";
        public const string PrefabDir = Root + "/Prefabs";

        static Dictionary<string, Material> mats;

        [MenuItem("Blockout/1. Build Module Kit (Prefabs)", false, 1)]
        public static void BuildKitMenu()
        {
            BuildKit();
            EditorUtility.DisplayDialog("Blockout",
                "Module kit built.\n\nPrefabs: " + PrefabDir + "\n\nNext: Blockout > 2. Build School Level Scene", "OK");
        }

        public static GameObject LoadPrefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
        }

        public static void BuildKit()
        {
            EnsureFolder(MeshDir);
            EnsureFolder(MatDir);
            EnsureFolder(TexDir);
            EnsureFolder(PrefabDir);

            Texture2D grid = GetOrCreateGridTexture();
            mats = new Dictionary<string, Material>();
            MakeMat("M_Floor",     new Color(0.80f, 0.80f, 0.78f), grid, 0f);
            MakeMat("M_FloorAlt",  new Color(0.62f, 0.62f, 0.60f), grid, 0f);
            MakeMat("M_Ground",    new Color(0.45f, 0.47f, 0.45f), grid, 0f);
            MakeMat("M_Wall",      new Color(0.95f, 0.93f, 0.88f), grid, 0f);
            MakeMat("M_Furniture", new Color(0.78f, 0.68f, 0.52f), grid, 0f);
            MakeMat("M_Cover",     new Color(0.30f, 0.52f, 0.85f), grid, 0f);
            MakeMat("M_Player",    new Color(1.00f, 0.80f, 0.10f), grid, 0.15f);
            MakeMat("M_Authority", new Color(0.88f, 0.20f, 0.18f), grid, 0.1f);
            MakeMat("M_Landmark",  new Color(0.16f, 0.45f, 0.32f), grid, 0f);
            MakeMat("M_Path",      new Color(1.00f, 0.55f, 0.10f), null, 0.4f);
            MakeMat("M_Eye",       new Color(0.20f, 0.95f, 1.00f), null, 0.8f);
            MakeMat("M_VisionFan", new Color(1.00f, 0.45f, 0.40f), null, 0.35f);

            BuildStructure();
            BuildFurniture();
            BuildFigures();
            BuildWatchers();
            BuildMarkersAndTools();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Blockout] Module kit built -> " + PrefabDir);
        }

        // ------------------------------------------------------------------ structure
        static void BuildStructure()
        {
            float h = M.WallThickness * 0.5f, H = M.WallHeight, S = M.Module;

            Simple("Floor_2x2", new B().Box(0, -M.FloorThickness, 0, S, 0, S), "M_Floor");
            Simple("Podium_2x2", new B().Box(0, 0, 0, S, M.StepHeight, S), "M_FloorAlt");
            Simple("Wall_2m", new B().Box(0, 0, -h, S, H, h), "M_Wall");
            Simple("Wall_1m", new B().Box(0, 0, -h, 1f, H, h), "M_Wall");

            float side = (S - M.DoorWidth) * 0.5f;   // doorway = a gap, no frame (book: "just leave a gap")
            Simple("Wall_Door_2m", new B()
                .Box(0, 0, -h, side, H, h)
                .Box(S - side, 0, -h, S, H, h)
                .Box(side, M.DoorHeight, -h, S - side, H, h), "M_Wall");

            Simple("Wall_Window_2m", new B()         // window = doorway-like gap + waist-height wall
                .Box(0, 0, -h, 0.4f, H, h)
                .Box(S - 0.4f, 0, -h, S, H, h)
                .Box(0.4f, 0, -h, S - 0.4f, M.WindowSill, h)
                .Box(0.4f, M.WindowTop, -h, S - 0.4f, H, h), "M_Wall");

            Simple("Glue_Corner", new B().Box(-h, 0, -h, h, H, h), "M_Wall");
            Simple("Pillar", new B().Box(-0.3f, 0, -0.3f, 0.3f, H, 0.3f), "M_Cover");

            // Invisible ceiling that still casts shadows: the room gets its light only through
            // the windows (Robert Yang: light defines the space), but a fly camera can still see in.
            GameObject ceiling = Piece("Ceiling_2x2_ShadowOnly", null,
                new B().Box(0, H, 0, S, H + 0.2f, S), "Ceiling_2x2", "M_Wall", false);
            ceiling.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            SavePrefab(ceiling, "Ceiling_2x2_ShadowOnly");

            Simple("Ground_60x40", new B().Box(-10f, -0.3f, -20f, 50f, -0.01f, 20f), "M_Ground");
        }

        // ------------------------------------------------------------------ furniture & props
        static B DeskMesh()
        {
            // front (+Z) modesty panel hides the player's lap from the teacher = a built-in hiding spot
            return new B()
                .Box(-0.35f, 0.72f, -0.25f, 0.35f, 0.75f, 0.25f)   // top
                .Box(-0.35f, 0f, -0.25f, -0.31f, 0.72f, 0.25f)     // left side
                .Box(0.31f, 0f, -0.25f, 0.35f, 0.72f, 0.25f)       // right side
                .Box(-0.31f, 0.30f, 0.21f, 0.31f, 0.72f, 0.25f);   // front panel
        }

        static void BuildFurniture()
        {
            SavePrefab(Piece("Desk_Student", null, DeskMesh(), "Desk", "M_Furniture", true), "Desk_Student");
            SavePrefab(Piece("Desk_Player", null, DeskMesh(), "Desk", "M_Player", true), "Desk_Player");

            // Chair = the ONLY thing the player can sit on. One seat point (the pivot), facing +Z (the desk).
            GameObject chair = Piece("Chair", null, new B()
                .Box(-0.22f, 0f, -0.22f, 0.22f, 0.45f, 0.22f)
                .Box(-0.22f, 0.45f, -0.22f, 0.22f, 0.85f, -0.16f), "Chair", "M_Furniture", true);
            BlockoutSeat seatComp = chair.AddComponent<BlockoutSeat>();
            seatComp.eyeHeight = M.EyeSeated;   // same eye height as every seated NPC figure
            SavePrefab(chair, "Chair");

            Simple("Desk_Teacher", new B().Box(-0.7f, 0f, -0.35f, 0.7f, 0.9f, 0.35f), "M_Furniture");
            Simple("Blackboard", new B().Box(-2f, 0.9f, -0.05f, 2f, 2.1f, 0f), "M_Landmark");
            Simple("Prop_Shelf", new B().Box(-1f, 0f, -0.2f, 1f, 1.2f, 0.2f), "M_Cover");
            Simple("Prop_Locker", new B().Box(-0.25f, 0f, -0.25f, 0.25f, 1.8f, 0.25f), "M_Cover");
            Simple("Prop_BookStack", new B().Box(-0.15f, 0f, -0.11f, 0.15f, 0.25f, 0.11f), "M_Cover");

            Simple("Cafe_Table", new B()
                .Box(-1.2f, 0.72f, -0.4f, 1.2f, 0.76f, 0.4f)
                .Box(-1.0f, 0f, -0.3f, -0.95f, 0.72f, 0.3f)
                .Box(0.95f, 0f, -0.3f, 1.0f, 0.72f, 0.3f), "M_Furniture");
            Simple("Cafe_Bench", new B().Box(-1.2f, 0f, -0.175f, 1.2f, 0.45f, 0.175f), "M_Furniture");
            Simple("Cafe_Counter", new B().Box(-1f, 0f, -0.4f, 1f, 1.0f, 0.4f), "M_Furniture");
        }

        // ------------------------------------------------------------------ scale figures
        static B StandingBody()
        {
            return new B()
                .Box(-0.225f, 0f, -0.14f, 0.225f, 1.5f, 0.14f)
                .Box(-0.14f, 1.52f, -0.14f, 0.14f, M.PlayerHeight, 0.14f);
        }

        static B StandingEyes()
        {
            // cyan visor strip at eye height: shows exactly where this figure is LOOKING
            return new B().Box(-0.1f, M.EyeStanding - 0.025f, 0.14f, 0.1f, M.EyeStanding + 0.025f, 0.17f);
        }

        static B SeatedBody()
        {
            // pivot = centre of the chair seat on the floor, seat height 0.45 m
            return new B()
                .Box(-0.2f, 0.45f, -0.16f, 0.2f, 1.10f, 0.08f)   // torso
                .Box(-0.2f, 0.45f, 0.08f, 0.2f, 0.60f, 0.42f)    // thighs
                .Box(-0.2f, 0f, 0.30f, 0.2f, 0.45f, 0.42f)       // shins
                .Box(-0.13f, 1.12f, -0.12f, 0.13f, 1.38f, 0.12f); // head
        }

        static B SeatedEyes()
        {
            return new B().Box(-0.09f, M.EyeSeated - 0.025f, 0.12f, 0.09f, M.EyeSeated + 0.025f, 0.15f);
        }

        static GameObject Figure(string name, B body, B eyes, string meshName, string mat)
        {
            GameObject root = new GameObject(name);
            Piece("Body", root.transform, body, meshName, mat, true);
            Piece("Eyes", root.transform, eyes, meshName + "_Eyes", "M_Eye", false);
            return root;
        }

        static void BuildFigures()
        {
            SavePrefab(Figure("Fig_Standing_Player", StandingBody(), StandingEyes(), "Fig_Standing", "M_Player"), "Fig_Standing_Player");
            SavePrefab(Figure("Fig_Seated_Player", SeatedBody(), SeatedEyes(), "Fig_Seated", "M_Player"), "Fig_Seated_Player");
            SavePrefab(Figure("Fig_Classmate_Seated", SeatedBody(), SeatedEyes(), "Fig_Seated", "M_Cover"), "Fig_Classmate_Seated");
        }

        // ------------------------------------------------------------------ watchers
        static void AddGazeLight(Transform parent, Vector3 localPos, float pitch, float angle, float range, float intensity)
        {
            // A red spot light as the watcher's gaze: its shadows show the blind spots behind cover.
            GameObject go = new GameObject("GazeLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = angle;
            l.range = range;
            l.intensity = intensity;
            l.color = new Color(1f, 0.35f, 0.3f);
            l.shadows = LightShadows.Soft;
            l.lightmapBakeType = LightmapBakeType.Realtime;
        }

        static void BuildWatchers()
        {
            // Teacher (Challenge 1). TeacherPatrol waypoints are assigned in the scene.
            GameObject teacher = Figure("Fig_Teacher", StandingBody(), StandingEyes(), "Fig_Standing", "M_Authority");
            teacher.AddComponent<TeacherPatrol>();
            Piece("VisionFan", teacher.transform, new B().Fan(90f, 5f, 24, 0.03f), "Fan_Teacher_90deg_5m", "M_VisionFan", false, false);
            AddGazeLight(teacher.transform, new Vector3(0f, M.EyeStanding, 0.18f), 25f, 80f, 7f, 2f);
            SavePrefab(teacher, "Fig_Teacher");

            // Security camera (Challenge 4). Root sits on the floor so the vision fan lies on the floor.
            GameObject cam = new GameObject("Watcher_SecurityCamera");
            cam.AddComponent<SweepRotate>();
            Piece("CameraBody", cam.transform, new B()
                .Box(-0.04f, 2.85f, -0.04f, 0.04f, 3.0f, 0.04f)       // mount
                .Box(-0.12f, 2.70f, -0.05f, 0.12f, 2.90f, 0.40f),    // body, looks along +Z
                "SecurityCamera_Body", "M_Authority", false);
            Piece("Lens", cam.transform, new B().Box(-0.07f, 2.73f, 0.40f, 0.07f, 2.87f, 0.43f), "SecurityCamera_Lens", "M_Eye", false);
            Piece("VisionFan", cam.transform, new B().Fan(50f, 11f, 24, 0.03f), "Fan_Camera_50deg_11m", "M_VisionFan", false, false);
            AddGazeLight(cam.transform, new Vector3(0f, 2.8f, 0.45f), 20f, 50f, 18f, 3f);
            SavePrefab(cam, "Watcher_SecurityCamera");
        }

        // ------------------------------------------------------------------ markers & tools
        static void BuildMarkersAndTools()
        {
            GameObject wp = Piece("Marker_Waypoint", null, new B()
                .Box(-0.25f, 0f, -0.25f, 0.25f, 0.03f, 0.25f)
                .Box(-0.06f, 0f, 0.25f, 0.06f, 0.03f, 0.55f),     // arrow tab = facing direction
                "Marker_Waypoint", "M_Path", false, false);
            wp.AddComponent<BlockoutWaypoint>();
            SavePrefab(wp, "Marker_Waypoint");

            // 3 m ruler: ticks every 0.5 m, wide ticks every 1 m
            B ruler = new B().Box(-0.03f, 0f, -0.03f, 0.03f, 3f, 0.03f);
            for (int i = 1; i <= 6; i++)
            {
                float y = i * 0.5f, w = (i % 2 == 0) ? 0.15f : 0.09f;
                ruler.Box(-w, y - 0.012f, -0.03f, w, y + 0.012f, 0.03f);
            }
            GameObject rulerGo = Piece("Ruler_3m", null, ruler, "Ruler_3m", "M_Path", false);
            Piece("EyeMarks", rulerGo.transform, new B()
                .Box(0.03f, M.EyeStanding - 0.01f, -0.03f, 0.3f, M.EyeStanding + 0.01f, 0.03f)
                .Box(0.03f, M.EyeSeated - 0.01f, -0.03f, 0.3f, M.EyeSeated + 0.01f, 0.03f),
                "Ruler_EyeMarks", "M_Eye", false);
            SavePrefab(rulerGo, "Ruler_3m");

            // First-person walker for playtesting at real player size
            GameObject walker = new GameObject("Player_Walker");
            CharacterController cc = walker.AddComponent<CharacterController>();
            cc.height = M.PlayerHeight;
            cc.radius = M.PlayerWidth * 0.5f;
            cc.center = new Vector3(0f, M.PlayerHeight * 0.5f, 0f);
            cc.stepOffset = 0.2f;              // podium (0.15) yes, benches/chairs (0.45) no
            cc.slopeLimit = 45f;
            GameObject eye = new GameObject("Eye");
            eye.transform.SetParent(walker.transform, false);
            eye.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            Camera c = eye.AddComponent<Camera>();
            c.fieldOfView = 70f;
            c.nearClipPlane = 0.05f;
            eye.AddComponent<AudioListener>();
            BlockoutWalker walkerComp = walker.AddComponent<BlockoutWalker>();
            walkerComp.eye = eye.transform;
            walkerComp.eyeStanding = 1.5f;     // book: 1.5-1.7 m; playtest said 1.6 felt too high
            walkerComp.lookSensitivity = 5f;
            SavePrefab(walker, "Player_Walker");
        }

        // ------------------------------------------------------------------ helpers
        static void Simple(string name, B b, string mat)
        {
            SavePrefab(Piece(name, null, b, name, mat, true), name);
        }

        static GameObject Piece(string name, Transform parent, B b, string meshName, string mat, bool colliders, bool castShadows = true)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(b, meshName);
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mats[mat];
            if (!castShadows) r.shadowCastingMode = ShadowCastingMode.Off;
            if (colliders)
            {
                foreach (Bounds box in b.Boxes)
                {
                    BoxCollider col = go.AddComponent<BoxCollider>();
                    col.center = box.center;
                    col.size = box.size;
                }
            }
            return go;
        }

        static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        static Mesh SaveMesh(B b, string name)
        {
            Mesh mesh = b.ToMesh(name);
            string path = MeshDir + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(mesh);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Shader LitShader()
        {
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null)
            {
                string n = rp.GetType().Name;
                Shader s = null;
                if (n.Contains("Universal")) s = Shader.Find("Universal Render Pipeline/Lit");
                else if (n.Contains("HD")) s = Shader.Find("HDRP/Lit");
                if (s != null) return s;
            }
            return Shader.Find("Standard");
        }

        static void MakeMat(string name, Color col, Texture tex, float emission)
        {
            Shader shader = LitShader();
            Material m = new Material(shader);
            m.name = name;
            foreach (string p in new[] { "_BaseColor", "_Color" })
                if (m.HasProperty(p)) m.SetColor(p, col);
            if (tex != null)
                foreach (string p in new[] { "_BaseMap", "_MainTex", "_BaseColorMap" })
                    if (m.HasProperty(p)) m.SetTexture(p, tex);
            foreach (string p in new[] { "_Glossiness", "_Smoothness" })
                if (m.HasProperty(p)) m.SetFloat(p, 0.1f);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", col * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            string path = MatDir + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                EditorUtility.CopySerialized(m, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(m);
                mats[name] = existing;
                return;
            }
            AssetDatabase.CreateAsset(m, path);
            mats[name] = m;
        }

        static Texture2D GetOrCreateGridTexture()
        {
            // 1 texture tile = 1 m. Strong line every metre, faint line every 0.5 m.
            string path = TexDir + "/T_BlockoutGrid_1m.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int S = 256;
            Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, true);
            tex.name = "T_BlockoutGrid_1m";
            Color32[] px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    bool major = x < 3 || x >= S - 3 || y < 3 || y >= S - 3;
                    bool minor = (x >= S / 2 - 1 && x <= S / 2) || (y >= S / 2 - 1 && y <= S / 2);
                    byte c = major ? (byte)150 : (minor ? (byte)205 : (byte)238);
                    px[y * S + x] = new Color32(c, c, c, 255);
                }
            }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;
            tex.Apply(true);
            AssetDatabase.CreateAsset(tex, path);
            return tex;
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
