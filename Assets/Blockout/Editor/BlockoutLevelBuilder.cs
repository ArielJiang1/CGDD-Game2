using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Blockout
{
    /// <summary>
    /// Menu: Blockout > 2. Build School Level Scene
    ///
    /// Assembles a FIRST-DRAFT white box from the kit prefabs (every piece stays a prefab instance):
    ///   - Ch.1 "The Lesson"     : classroom 10 x 12 m, player seat = back corner by the windows,
    ///                             teacher with a patrol loop (board = safe window, scan = danger)
    ///   - Corridor              : 4 m wide (2x the book's 2 m minimum), lockers
    ///   - Ch.4 "The Cafeteria"  : 16 x 12 m, pillars as cover, sweeping security camera
    ///   - Metrics Zoo           : every module in a row with its measured size (book: "metrics zoo")
    ///
    /// This is a starting point, not an answer. The Level Design Book: "99% of the time, your blockout
    /// will not survive a playtest." Walk it, then move things by hand.
    ///
    /// Layout: +Z = front of the classroom (blackboard). X 0..10 classroom, 10..14 corridor, 14..30 cafeteria.
    /// </summary>
    public static class BlockoutLevelBuilder
    {
        const string SceneDir = BlockoutKitBuilder.Root + "/Scenes";
        const string ScenePath = SceneDir + "/Blockout_School.unity";

        const string W = "Wall_2m";
        const string D = "Wall_Door_2m";
        const string N = "Wall_Window_2m";

        [MenuItem("Blockout/2. Build School Level Scene", false, 2)]
        public static void BuildLevel()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string path = ScenePath;
            if (System.IO.File.Exists(path))
            {
                int choice = EditorUtility.DisplayDialogComplex("Blockout",
                    path + " already exists.\n\nOverwriting will throw away hand edits made in that scene.",
                    "Overwrite", "Cancel", "Save as a new copy");
                if (choice == 1) return;
                if (choice == 2) path = AssetDatabase.GenerateUniqueAssetPath(path);
            }

            if (BlockoutKitBuilder.LoadPrefab("Wall_2m") == null) BlockoutKitBuilder.BuildKit();

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject("BLOCKOUT_School").transform;

            BuildLighting(root);
            P("Ground_60x40", root, 0, 0, 0);
            BuildClassroom(root);
            BuildCorridor(root);
            BuildCafeteria(root);
            BuildZoo(root);
            BuildShotCameras(root);

            BlockoutKitBuilder.EnsureFolder(SceneDir);
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[Blockout] Scene saved: " + path +
                      "\nNext: drag the course FlyCamera prefab (StudentTool folder) into the scene and press Play." +
                      "\nTo walk at player scale instead, enable 'Player_Walker' (Corridor group) and disable FlyCamera.");
        }

        /// <summary>Hide every floating text label (for clean screenshots / video), or show them again.</summary>
        [MenuItem("Blockout/3. Show or Hide Labels", false, 3)]
        public static void ToggleLabels()
        {
            bool haveTarget = false, target = false;
            int count = 0;
            foreach (TextMesh tm in Resources.FindObjectsOfTypeAll<TextMesh>())
            {
                if (EditorUtility.IsPersistent(tm) || !tm.gameObject.scene.IsValid()) continue;
                if (!haveTarget) { target = !tm.gameObject.activeSelf; haveTarget = true; }
                Undo.RecordObject(tm.gameObject, "Show or Hide Labels");
                tm.gameObject.SetActive(target);
                count++;
            }
            if (count > 0) EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[Blockout] " + (target ? "Showing " : "Hiding ") + count + " labels.");
        }

        // ------------------------------------------------------------------ Ch.1 classroom
        static void BuildClassroom(Transform root)
        {
            Transform room = G("Ch1_Classroom_TheLesson", root);

            Transform shell = G("Shell", room);
            FloorRect(G("Floor", shell), 0, 0, 5, 6);
            CeilingRect(G("Ceiling (shadow only)", shell), 0, 0, 5, 6);
            Transform walls = G("Walls", shell);
            WallRun(walls, 0, 0, false, W, W, W, W, W);        // back wall (south)
            WallRun(walls, 0, 12, false, W, W, W, W, W);       // front wall (north, blackboard)
            WallRun(walls, 0, 0, true, W, N, N, N, N, W);      // window wall (west) - sunlight side
            WallRun(walls, 10, 0, true, D, W, W, W, W, D);     // corridor wall (east): back door + front door
            Glue(walls, 0, 0); Glue(walls, 10, 0); Glue(walls, 0, 12); Glue(walls, 10, 12);

            Transform front = G("TeacherZone", room);
            for (int i = 0; i < 3; i++) P("Podium_2x2", front, 2 + i * 2, 0, 9.9f);
            P("Desk_Teacher", front, 5, 0.15f, 10.4f);
            P("Blackboard", front, 5, 0, 11.9f);                // landmark, visible from both doors

            // 3 pairs of desks, 5 rows. Aisles 1.3 / 1.6 / 1.6 / 1.3 m (teacher walks them).
            Transform desks = G("Desks_Chairs", room);
            Transform people = G("Classmates (cover)", room);
            Transform props = G("Props", room);
            Transform player = G("PLAYER_Seat", room);
            float[] cols = { 1.65f, 2.35f, 4.65f, 5.35f, 7.65f, 8.35f };
            System.Random rng = new System.Random(7);
            for (int r = 0; r < 5; r++)
            {
                float dz = 2.4f + r * 1.3f, cz = dz - 0.55f;
                for (int c = 0; c < cols.Length; c++)
                {
                    float x = cols[c];
                    bool isPlayer = r == 0 && c == 0;
                    P(isPlayer ? "Desk_Player" : "Desk_Student", isPlayer ? player : desks, x, 0, dz);
                    P("Chair", isPlayer ? player : desks, x, 0, cz);
                    if (isPlayer) continue;

                    bool mustFill = (r == 1 && c == 0) || (r == 0 && c == 1);   // classmate in front + beside the player
                    if (mustFill || rng.NextDouble() < 0.7) P("Fig_Classmate_Seated", people, x, 0, cz);
                    if (rng.NextDouble() < 0.3) P("Prop_BookStack", props, x + (rng.NextDouble() < 0.5 ? -0.17f : 0.17f), 0.75f, dz + 0.05f);
                }
            }
            P("Fig_Seated_Player", player, 1.65f, 0, 1.85f);
            P("Prop_BookStack", player, 1.48f, 0.75f, 2.48f);      // the player's own cover on their desk
            P("Prop_Shelf", props, 7f, 0, 0.3f);

            // Teacher + patrol. Rotate a waypoint to change where the teacher looks while waiting there.
            Transform gp = G("Watcher_Teacher", room);
            Transform wpRoot = G("Patrol_Waypoints", gp);
            List<BlockoutWaypoint> wps = new List<BlockoutWaypoint>();
            wps.Add(WP(wpRoot, "WP0_Board", 5.0f, 0.15f, 11.2f, 0f, 5f, "SAFE: writing on board"));
            wps.Add(WP(wpRoot, "WP1_Scan", 3.2f, 0.15f, 10.4f, 180f, 3f, "DANGER: scanning class"));
            wps.Add(WP(wpRoot, "WP2_AisleTop", 3.5f, 0f, 8.3f, 180f, 0f, ""));
            wps.Add(WP(wpRoot, "WP3_BackAisle", 3.5f, 0f, 1.0f, -90f, 1.5f, "Behind the back row"));
            wps.Add(WP(wpRoot, "WP4_BehindPlayer", 0.65f, 0f, 1.0f, 0f, 1.5f, "Right behind YOU"));
            wps.Add(WP(wpRoot, "WP5_WindowAisle", 0.65f, 0f, 8.3f, 90f, 0f, ""));
            wps.Add(WP(wpRoot, "WP6_ToBoard", 3.0f, 0.15f, 11.2f, 90f, 0f, ""));

            GameObject teacher = P("Fig_Teacher", gp, 5.0f, 0.15f, 11.2f, 0f);
            if (teacher != null)
            {
                TeacherPatrol patrol = teacher.GetComponent<TeacherPatrol>();
                if (patrol == null) patrol = teacher.AddComponent<TeacherPatrol>();
                patrol.waypoints = wps.ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(patrol);
            }

            Label(room, "CH.1  THE LESSON", new Vector3(5f, 3.4f, -0.3f), 0f, 0.12f, Color.white);
        }

        // ------------------------------------------------------------------ corridor
        static void BuildCorridor(Transform root)
        {
            Transform cor = G("Corridor", root);
            Transform shell = G("Shell", cor);
            FloorRect(G("Floor", shell), 10, 0, 2, 6);
            CeilingRect(G("Ceiling (shadow only)", shell), 10, 0, 2, 6);
            Transform walls = G("Walls", shell);
            WallRun(walls, 10, 0, false, W, W);                  // south end
            WallRun(walls, 10, 12, false, N, N);                 // north end: windows = light at the end of the hall
            WallRun(walls, 14, 0, true, W, W, D, W, W, W);       // cafeteria door at z 4..6
            Glue(walls, 14, 0); Glue(walls, 14, 12);

            Transform props = G("Lockers (cover)", cor);
            for (float z = 3.25f; z <= 8.76f; z += 0.5f) P("Prop_Locker", props, 10.35f, 0, z, 90f);
            for (float z = 7.25f; z <= 11.26f; z += 0.5f) P("Prop_Locker", props, 13.65f, 0, z, -90f);

            P("Fig_Standing_Player", cor, 12f, 0, 3f, 0f);       // scale figure
            P("Ruler_3m", cor, 11.2f, 0, 11.4f, 0f);

            GameObject walker = P("Player_Walker", cor, 12f, 0, 1.2f, 0f);
            if (walker != null) walker.SetActive(false);         // enable to playtest on foot

            Label(cor, "CORRIDOR", new Vector3(12f, 3.4f, -0.3f), 0f, 0.1f, Color.white);
        }

        // ------------------------------------------------------------------ Ch.4 cafeteria
        static void BuildCafeteria(Transform root)
        {
            Transform caf = G("Ch4_Cafeteria", root);
            Transform shell = G("Shell", caf);
            FloorRect(G("Floor", shell), 14, 0, 8, 6);
            CeilingRect(G("Ceiling (shadow only)", shell), 14, 0, 8, 6);
            Transform walls = G("Walls", shell);
            WallRun(walls, 14, 0, false, W, N, N, N, N, N, N, W);
            WallRun(walls, 14, 12, false, W, W, W, W, W, W, W, W);
            WallRun(walls, 30, 0, true, W, N, N, N, N, W);
            Glue(walls, 30, 0); Glue(walls, 30, 12);

            Transform furn = G("Tables_Benches", caf);
            Transform people = G("Students (cover)", caf);
            System.Random rng = new System.Random(11);
            float[] xs = { 18f, 22f, 26f };
            float[] zs = { 3f, 6f, 9f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    P("Cafe_Table", furn, x, 0, z);
                    P("Cafe_Bench", furn, x, 0, z - 0.625f);
                    P("Cafe_Bench", furn, x, 0, z + 0.625f);
                    if (rng.NextDouble() < 0.5) P("Fig_Classmate_Seated", people, x + (float)(rng.NextDouble() * 1.6 - 0.8), 0, z - 0.625f, 0f);
                    if (rng.NextDouble() < 0.5) P("Fig_Classmate_Seated", people, x + (float)(rng.NextDouble() * 1.6 - 0.8), 0, z + 0.625f, 180f);
                }
            }
            for (int i = 0; i < 6; i++) P("Cafe_Counter", furn, 17f + i * 2f, 0, 11.5f);

            Transform cover = G("Pillars (cover vs camera)", caf);
            P("Pillar", cover, 20f, 0, 4.5f); P("Pillar", cover, 24f, 0, 4.5f);
            P("Pillar", cover, 20f, 0, 7.5f); P("Pillar", cover, 24f, 0, 7.5f);

            Transform gp = G("Watcher_Camera", caf);
            P("Watcher_SecurityCamera", gp, 29.6f, 0, 11.6f, -135f);
            P("Fig_Standing_Player", caf, 15.2f, 0, 5f, 90f);     // scale figure at the entrance

            Label(caf, "CH.4  THE CAFETERIA", new Vector3(22f, 3.4f, -0.3f), 0f, 0.12f, Color.white);
        }

        // ------------------------------------------------------------------ metrics zoo
        static void BuildZoo(Transform root)
        {
            Transform zoo = G("MetricsZoo", root);
            ZooRow(zoo, -6f, 0f, "Floor_2x2", "Podium_2x2", "Wall_2m", "Wall_1m", "Wall_Door_2m", "Wall_Window_2m", "Glue_Corner", "Pillar", "Ruler_3m");
            ZooRow(zoo, -10f, 0f, "Desk_Student", "Desk_Player", "Chair", "Desk_Teacher", "Blackboard", "Prop_Shelf", "Prop_Locker", "Prop_BookStack", "Cafe_Table", "Cafe_Bench", "Cafe_Counter");
            ZooRow(zoo, -15f, 180f, "Fig_Standing_Player", "Fig_Seated_Player", "Fig_Classmate_Seated", "Fig_Teacher", "Watcher_SecurityCamera", "Marker_Waypoint");
            Label(zoo, "METRICS ZOO  (1 grid square = 1 m)", new Vector3(14f, 0.05f, -18.5f), 0f, 0.14f, Color.white, 90f);
        }

        static void ZooRow(Transform parent, float z, float yaw, params string[] names)
        {
            // Each exhibit gets a slot wide enough for BOTH the object and its label, so labels never overlap.
            const float charSize = 0.035f;
            const float charWidth = charSize * 64f * 0.058f;   // approx. world width of one character at fontSize 64
            float x = 0f;
            foreach (string n in names)
            {
                GameObject go = P(n, parent, 0, 0, z, yaw);
                if (go == null) continue;
                Bounds b = RendererBounds(go);
                string size = string.Format("{0:0.00} x {1:0.00} x {2:0.00} m", b.size.x, b.size.y, b.size.z);
                float labelWidth = Mathf.Max(n.Length, size.Length) * charWidth;
                float slot = Mathf.Max(b.size.x, labelWidth) + 0.8f;
                float centerX = x + slot * 0.5f;
                go.transform.position += new Vector3(centerX - b.center.x, 0f, 0f);
                Label(parent, n + "\n" + size, new Vector3(centerX, 0.05f, z - 1.7f), 0f, charSize, Color.white, 90f);
                x += slot;
            }
        }

        static Bounds RendererBounds(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.one * 0.3f);
            bool first = true;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "VisionFan") continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------------ lighting & cameras
        static void BuildLighting(Transform root)
        {
            // Robert Yang (GDC 2018): blockout first, then global light + dominant mood.
            // Low warm sun through the west windows; everything else is ambient.
            Transform g = G("Lighting", root);
            GameObject sun = new GameObject("Sun (through west windows)");
            sun.transform.SetParent(g, false);
            sun.transform.rotation = Quaternion.Euler(30f, 65f, 0f);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.2f;
            l.color = new Color(1f, 0.93f, 0.80f);
            l.shadows = LightShadows.Soft;
            l.lightmapBakeType = LightmapBakeType.Realtime;
            RenderSettings.sun = l;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
        }

        static void BuildShotCameras(Transform root)
        {
            // Disabled cameras for the screenshots. Enable ONE, look at the Game view, take the shot, disable again.
            Transform g = G("ScreenshotCameras (enable one at a time)", root);
            ShotCam(g, "Shot_PlayerSeat_POV", new Vector3(1.65f, 1.25f, 2.03f), new Vector3(1.65f, 1.0f, 12f));
            ShotCam(g, "Shot_Teacher_POV_fromBoard", new Vector3(5f, 1.75f, 10.9f), new Vector3(3f, 0.6f, 1.5f));
            ShotCam(g, "Shot_Classroom_Overview", new Vector3(-4f, 9f, -5f), new Vector3(5f, 0f, 6f));
            ShotCam(g, "Shot_Cafeteria_Overview", new Vector3(22f, 12f, -7f), new Vector3(22f, 0f, 6f));
            ShotCam(g, "Shot_WholeLevel_Top", new Vector3(15f, 32f, -14f), new Vector3(15f, 0f, 5f));
            ShotCam(g, "Shot_MetricsZoo", new Vector3(14f, 5f, -24f), new Vector3(14f, 0.8f, -10f));
        }

        static void ShotCam(Transform parent, string name, Vector3 pos, Vector3 target)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(target - pos);
            Camera c = go.AddComponent<Camera>();
            c.fieldOfView = 60f;
            c.nearClipPlane = 0.05f;
            c.depth = 10f;
            go.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers
        static GameObject P(string prefab, Transform parent, float x, float y, float z, float yaw = 0f)
        {
            GameObject asset = BlockoutKitBuilder.LoadPrefab(prefab);
            if (asset == null) { Debug.LogError("[Blockout] Missing prefab: " + prefab + " (run Blockout > 1. Build Module Kit)"); return null; }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.position = new Vector3(x, y, z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static Transform G(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Lays 2 m wall modules from (x,z). alongZ=false runs toward +X, true toward +Z.</summary>
        static void WallRun(Transform parent, float x, float z, bool alongZ, params string[] modules)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                float d = i * BlockoutMetrics.Module;
                if (alongZ) P(modules[i], parent, x, 0, z + d, -90f);
                else P(modules[i], parent, x + d, 0, z, 0f);
            }
        }

        static void Glue(Transform parent, float x, float z) { P("Glue_Corner", parent, x, 0, z); }

        static void FloorRect(Transform parent, float x0, float z0, int nx, int nz)
        {
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    P("Floor_2x2", parent, x0 + i * 2f, 0, z0 + j * 2f);
        }

        static void CeilingRect(Transform parent, float x0, float z0, int nx, int nz)
        {
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    P("Ceiling_2x2_ShadowOnly", parent, x0 + i * 2f, 0, z0 + j * 2f);
        }

        static BlockoutWaypoint WP(Transform parent, string name, float x, float y, float z, float yaw, float wait, string note)
        {
            GameObject go = P("Marker_Waypoint", parent, x, y, z, yaw);
            if (go == null) return null;
            go.name = name;
            BlockoutWaypoint wp = go.GetComponent<BlockoutWaypoint>();
            wp.waitSeconds = wait;
            wp.note = note;
            PrefabUtility.RecordPrefabInstancePropertyModifications(wp);
            if (!string.IsNullOrEmpty(note))
            {
                // Stagger heights (2.0 / 2.6 m) by waypoint number so neighbouring labels don't overlap.
                float height = (parent.childCount % 2 == 0) ? 2.6f : 2.0f;
                Label(go.transform, name.Substring(0, 3) + " " + note + " (" + wait + "s)", new Vector3(x, y + height, z), 0f, 0.028f, new Color(1f, 0.6f, 0.2f));
            }
            return wp;
        }

        static Font legacyFont;
        static Font LegacyFont()
        {
            if (legacyFont != null) return legacyFont;
#if UNITY_2022_2_OR_NEWER
            legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            legacyFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            return legacyFont;
        }

        static void Label(Transform parent, string text, Vector3 worldPos, float yaw, float charSize, Color color, float pitch = 0f)
        {
            GameObject go = new GameObject("Label: " + text.Split('\n')[0]);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            TextMesh tm = go.AddComponent<TextMesh>();
            Font f = LegacyFont();
            if (f != null)
            {
                tm.font = f;
                go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
        }
    }
}
