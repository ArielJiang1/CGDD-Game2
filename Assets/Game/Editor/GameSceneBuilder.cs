using System.Collections.Generic;
using Blockout;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace PhoneGame
{
    /// <summary>
    /// Menu: Game > Build All Scenes
    ///
    ///  Title.unity              - title page (New Game / Continue / Chapters / Quit)
    ///  Level01_Lesson.unity     - Ch.1, COPY of Blockout_School (classroom + corridor), Clearview art pass
    ///  Level02_Lunch.unity      - Ch.2, COPY of Blockout_School (corridor + cafeteria, STAFF door, camera, monitor)
    ///  Level03_LightsOut.unity  - Ch.3, a new dormitory white box (room 304 + hallway, built from the same kit)
    ///  Level04_Inspector.unity  - Ch.4, the Ch.1 classroom again, decorated for Inspection Day
    ///  End.unity                - between chapters / game over / the two endings
    /// and puts them in that order in File > Build Profiles.
    /// Assets/Blockout/Scenes/Blockout_School.unity itself is never modified.
    /// </summary>
    public static class GameSceneBuilder
    {
        const string SceneDir = "Assets/Game/Scenes";
        const string TitlePath = SceneDir + "/Title.unity";
        const string L1Path = SceneDir + "/Level01_Lesson.unity";
        const string L2Path = SceneDir + "/Level02_Lunch.unity";
        const string L3Path = SceneDir + "/Level03_LightsOut.unity";
        const string L4Path = SceneDir + "/Level04_Inspector.unity";
        const string EndPath = SceneDir + "/End.unity";
        const string BlockoutPath = "Assets/Blockout/Scenes/Blockout_School.unity";
        const string PrefabDir = "Assets/Blockout/Prefabs/";

        static readonly string[] AllPaths = { TitlePath, L1Path, L2Path, L3Path, L4Path, EndPath };

        /// <summary>
        /// Bump this whenever the scene-building code or serialized defaults change. Unity compares it on load
        /// (GameSceneAutoRebuild) and offers to rebuild out-of-date scenes, so nobody plays an old build by accident.
        /// </summary>
        public const int ScenesVersion = 16;

        [MenuItem("Game/Build All Scenes (Title, 4 Chapters, End)", false, 1)]
        public static void BuildAllMenu() { BuildAll(false); }

        public static void BuildAll(bool skipConfirm)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BlockoutPath) == null)
            {
                EditorUtility.DisplayDialog("Game", "Can't find " + BlockoutPath + ".\nRun Blockout > 2. Build School Level Scene first.", "OK");
                return;
            }
            bool any = false;
            foreach (string p in AllPaths) any |= AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null;
            if (any && !skipConfirm && !EditorUtility.DisplayDialog("Game",
                    "The game scenes already exist in " + SceneDir + ".\nRebuild them? Hand edits in those scenes will be lost.\n(Blockout_School is not touched.)",
                    "Rebuild", "Cancel")) return;

            try
            {
                EditorUtility.DisplayProgressBar("Game", "Generating Clearview textures & materials...", 0.05f);
                EnsureFolder(SceneDir);
                ClearviewArt.Prepare();
                MakeMaterials();
                EditorUtility.DisplayProgressBar("Game", "Title & End", 0.15f);
                BuildMenuScene(TitlePath, true);
                BuildMenuScene(EndPath, false);
                EditorUtility.DisplayProgressBar("Game", "Chapter 1 - The Lesson", 0.3f);
                if (!BuildClassroom(L1Path, false)) return;
                EditorUtility.DisplayProgressBar("Game", "Chapter 2 - Lunch", 0.5f);
                if (!BuildLunch()) return;
                EditorUtility.DisplayProgressBar("Game", "Chapter 3 - Lights Out", 0.7f);
                BuildLightsOut();
                EditorUtility.DisplayProgressBar("Game", "Chapter 4 - Inspection Day", 0.85f);
                if (!BuildClassroom(L4Path, true)) return;
                SetBuildScenes();
                GameSceneAutoRebuild.MarkBuilt();
            }
            finally { EditorUtility.ClearProgressBar(); }

            EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Game",
                "Done.\n\nTitle, 4 chapters and End are in " + SceneDir + " and in Build Profiles.\n\n" +
                "Press Play here in the Title scene to play from the start.\n(Title > Chapters lets you jump to any chapter.)", "OK");
        }

        // ------------------------------------------------------------------ Title / End (they build their own UI)
        static void BuildMenuScene(string path, bool title)
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.07f, 0.075f, 0.085f);
            go.AddComponent<AudioListener>();
            if (title) new GameObject("TitleScreen").AddComponent<TitleScreen>();
            else new GameObject("EndScreen").AddComponent<EndScreen>();
            UIFactory.EnsureEventSystem();
            EditorSceneManager.SaveScene(s, path);
        }

        // ------------------------------------------------------------------ shared: copy the white box
        static GameObject CopyBlockout(string path, string rootName, out Scene s)
        {
            s = default(Scene);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) AssetDatabase.DeleteAsset(path);
            if (!AssetDatabase.CopyAsset(BlockoutPath, path))
            {
                EditorUtility.DisplayDialog("Game", "Could not copy " + BlockoutPath, "OK");
                return null;
            }
            s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject level = null;
            foreach (GameObject go in s.GetRootGameObjects())
            {
                if (go.name == "BLOCKOUT_School") { level = go; continue; }
                if (go.GetComponentInChildren<Camera>(true) != null) Object.DestroyImmediate(go);   // FlyCamera, extra cameras
            }
            if (level == null)
            {
                EditorUtility.DisplayDialog("Game", "BLOCKOUT_School root not found in the copied scene.", "OK");
                return null;
            }
            level.name = rootName;

            // every camera nested anywhere inside the level (FlyCamera, Player_Walker, screenshot cameras...)
            List<GameObject> cams = new List<GameObject>();
            foreach (Camera c in level.GetComponentsInChildren<Camera>(true))
            {
                GameObject g = c.gameObject;
                if (PrefabUtility.IsPartOfPrefabInstance(g)) g = PrefabUtility.GetOutermostPrefabInstanceRoot(g);
                if (g == level) continue;
                if (!cams.Contains(g)) cams.Add(g);
            }
            foreach (GameObject g in cams) if (g != null) Object.DestroyImmediate(g);

            List<GameObject> remove = new List<GameObject>();
            foreach (Transform child in level.transform)
                if (child.name == "MetricsZoo" || child.name.StartsWith("ScreenshotCameras")) remove.Add(child.gameObject);
            foreach (GameObject g in remove) Object.DestroyImmediate(g);
            return level;
        }

        static Light FindSun(GameObject level)
        {
            foreach (Light l in level.GetComponentsInChildren<Light>(true)) if (l.type == LightType.Directional) return l;
            return null;
        }

        // ------------------------------------------------------------------ Chapters 1 & 4: the classroom
        static bool BuildClassroom(string path, bool inspection)
        {
            Scene s;
            GameObject level = CopyBlockout(path, inspection ? "LEVEL04_InspectionDay" : "LEVEL01_TheLesson", out s);
            if (level == null) return false;

            Transform caf = level.transform.Find("Ch4_Cafeteria");
            if (caf != null) Object.DestroyImmediate(caf.gameObject);

            Transform classroom = level.transform.Find("Ch1_Classroom_TheLesson");
            Transform corridor = level.transform.Find("Corridor");
            if (classroom == null) { EditorUtility.DisplayDialog("Game", "Ch1_Classroom_TheLesson not found.", "OK"); return false; }

            ClearviewArt.Restyle(level.transform);
            ClearviewArt.DayLighting(FindSun(level));
            ClearviewArt.PostVolume(level.transform, false);

            if (corridor != null)
            {
                GameObject block = new GameObject("Blocker_CafeteriaDoor");
                block.transform.SetParent(corridor, false);
                block.transform.position = new Vector3(14f, 1.5f, 5f);
                block.AddComponent<BoxCollider>().size = new Vector3(0.4f, 3f, 2.2f);
                ClosedDoor(corridor, new Vector3(14f, 0f, 5f), true, "CAFETERIA\nOPENS 12:40", 90f);
            }

            Transform seat = classroom.Find("PLAYER_Seat");

            // --- the player's seat: swap the desk for one with a cubby
            Vector3 seatPos = new Vector3(1.65f, 0f, 1.85f);
            Vector3 deskPos = new Vector3(1.65f, 0f, 2.4f);
            if (seat != null)
            {
                Transform chair = seat.Find("Chair");
                if (chair != null) seatPos = chair.position;
                Transform oldDesk = seat.Find("Desk_Player");
                if (oldDesk != null) { deskPos = oldDesk.position; Object.DestroyImmediate(oldDesk.gameObject); }
            }
            DeskParts desk = BuildCubbyDesk(seat != null ? seat : classroom, deskPos);

            // --- the student in front + the chalk triangle on their chair
            Interactable front = null;
            Transform frontFig = Nearest(classroom, "Fig_Classmate_Seated", new Vector3(seatPos.x, 0f, seatPos.z + 1.3f), 0.3f);
            Transform frontChair = Nearest(classroom, "Chair", new Vector3(seatPos.x, 0f, seatPos.z + 1.3f), 0.3f);
            if (!inspection)
            {
                if (frontFig != null) front = frontFig.gameObject.AddComponent<Interactable>();
                else Debug.LogWarning("[Game] No classmate found in front of the player's seat.");
                if (frontChair != null) ChalkTriangle(frontChair, frontChair.position + new Vector3(0f, 0.66f, -0.2215f), 0.12f);
            }

            // --- teacher: NavMesh agent instead of the fixed patrol
            TeacherPatrol patrol = Object.FindFirstObjectByType<TeacherPatrol>();
            TeacherAI teacherAI = null;
            TeacherVision vision = null;
            if (patrol != null)
            {
                GameObject t = patrol.gameObject;
                Transform wps = patrol.transform.parent != null ? patrol.transform.parent.Find("Patrol_Waypoints") : null;
                if (wps != null) Object.DestroyImmediate(wps.gameObject);
                Object.DestroyImmediate(patrol);
                SetLayer(t, 2);   // Ignore Raycast: he is not baked into the NavMesh as an obstacle
                t.transform.position = new Vector3(3.6f, 0.15f, 11.3f);
                AddAgent(t, 1.2f);
                teacherAI = t.AddComponent<TeacherAI>();
                vision = t.GetComponent<TeacherVision>();
                if (vision == null) vision = t.AddComponent<TeacherVision>();
                vision.eyeHeight = BlockoutMetrics.EyeStanding;
                vision.viewRange = 12f;
                Footsteps fs = t.AddComponent<Footsteps>();
                fs.volume = 0.4f;
                fs.maxDistance = 14f;
                if (inspection) teacherAI.lectureWeight = 0.5f;
            }
            else Debug.LogWarning("[Game] No teacher (TeacherPatrol) found.");

            NavMeshSurface surface = MakeSurface(classroom.gameObject);

            DecorateClassroom(classroom, inspection);
            if (corridor != null) DecorateCorridor(corridor);

            // --- player
            PhoneController phone;
            Transform exposure;
            // Ch.1: you arrive at the FRONT door (by the teacher) and have to walk past the whole class
            PlayerRig rig = MakeRig(inspection ? seatPos : new Vector3(11.7f, 0f, 11.0f), inspection ? 0f : -90f, out phone, out exposure);
            AudioClip bell = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/SchoolBell.wav");

            GameObject seatPoint = new GameObject("SeatPoint");
            seatPoint.transform.SetParent(classroom, false);
            seatPoint.transform.SetPositionAndRotation(seatPos, Quaternion.identity);
            GameObject stand = new GameObject("TeacherTakesPhoneHere");
            stand.transform.SetParent(classroom, false);
            stand.transform.position = new Vector3(0.7f, 0f, seatPos.z + 0.7f);

            if (!inspection)
            {
                Level01Director dir = rig.gameObject.AddComponent<Level01Director>();
                dir.rig = rig;
                dir.phone = phone;
                dir.teacher = teacherAI;
                dir.vision = vision;
                dir.navSurface = surface;
                dir.seatPoint = seatPoint.transform;
                dir.phoneExposurePoint = exposure;
                dir.cubbyZone = desk.cubbyZone;
                dir.books = desk.books;
                dir.loosePanel = desk.loosePanel;
                dir.hiddenPhone = desk.hiddenPhone;
                dir.frontStudent = front;
                dir.frontChair = frontChair;
                dir.takePhoneStandPoint = stand.transform;
                dir.bellClip = bell;
            }
            else
            {
                // yesterday the books were already pushed aside; the panel was put back
                desk.books.transform.localPosition += new Vector3(-0.23f, 0f, 0f);
                Object.DestroyImmediate(desk.books);
                Level04Director dir = rig.gameObject.AddComponent<Level04Director>();
                dir.rig = rig;
                dir.phone = phone;
                dir.teacher = teacherAI;
                dir.vision = vision;
                dir.navSurface = surface;
                dir.seatPoint = seatPoint.transform;
                dir.phoneExposurePoint = exposure;
                dir.cubbyZone = desk.cubbyZone;
                dir.loosePanel = desk.loosePanel;
                dir.hiddenPhone = desk.hiddenPhone;
                dir.takePhoneStandPoint = stand.transform;
                dir.bellClip = bell;
                Basement b = BuildBasement(level.transform);
                dir.basementSeat = b.seat;
                dir.basementDoorHinge = b.doorHinge;
                dir.vossInDoorway = b.voss;
            }

            EditorSceneManager.SaveScene(s);
            return true;
        }

        class Basement { public Transform seat, doorHinge; public GameObject voss; }

        /// <summary>
        /// Ending "LIGHTS": the room at the bottom of the stairs, finally seen. Far from the classroom (x 40, z -12),
        /// warm instead of fluorescent, hand-made instead of printed. Students on crates, talking out loud.
        /// </summary>
        static Basement BuildBasement(Transform level)
        {
            Basement b = new Basement();
            Transform r = ClearviewArt.Group("BASEMENT (behind the stairs - ending LIGHTS)", level);
            const float cx = 43f, cz = -11.5f, w = 6f, d = 5f;
            Material con = ClearviewArt.Concrete;
            ClearviewArt.Box("Floor", r, new Vector3(cx, -0.05f, cz), new Vector3(w, 0.1f, d), ClearviewArt.Floor, true);
            ClearviewArt.Box("Rug", r, new Vector3(cx, 0.006f, cz + 0.2f), new Vector3(3.2f, 0.01f, 2.4f), ClearviewArt.Pastels[0]);
            ClearviewArt.Box("Ceiling", r, new Vector3(cx, 2.85f, cz), new Vector3(w, 0.1f, d), con, true);
            ClearviewArt.Box("Wall_S", r, new Vector3(cx, 1.4f, cz - d * 0.5f), new Vector3(w, 2.8f, 0.1f), con, true);
            ClearviewArt.Box("Wall_W", r, new Vector3(cx - w * 0.5f, 1.4f, cz), new Vector3(0.1f, 2.8f, d), con, true);
            ClearviewArt.Box("Wall_E", r, new Vector3(cx + w * 0.5f, 1.4f, cz), new Vector3(0.1f, 2.8f, d), con, true);
            float nz = cz + d * 0.5f;   // north wall with the door (x cx-0.5 .. cx+0.5)
            ClearviewArt.Box("Wall_N_L", r, new Vector3((cx - w * 0.5f + cx - 0.5f) * 0.5f, 1.4f, nz), new Vector3(w * 0.5f - 0.5f, 2.8f, 0.1f), con, true);
            ClearviewArt.Box("Wall_N_R", r, new Vector3((cx + 0.5f + cx + w * 0.5f) * 0.5f, 1.4f, nz), new Vector3(w * 0.5f - 0.5f, 2.8f, 0.1f), con, true);
            ClearviewArt.Box("Wall_N_Top", r, new Vector3(cx, 2.45f, nz), new Vector3(1.0f, 0.8f, 0.1f), con, true);
            foreach (float x in new[] { cx - w * 0.5f + 0.06f, cx + w * 0.5f - 0.06f })
                ClearviewArt.Box("Band", r, new Vector3(x, 0.55f, cz), new Vector3(0.01f, 1.1f, d), ClearviewArt.Locker);
            // the stairs' landing beyond the door: cold light behind him
            ClearviewArt.Box("Landing", r, new Vector3(cx, -0.05f, nz + 0.9f), new Vector3(1.6f, 0.1f, 1.7f), con, true);
            ClearviewArt.Box("Landing_Back", r, new Vector3(cx, 1.4f, nz + 1.75f), new Vector3(1.6f, 2.8f, 0.1f), con, true);
            ClearviewArt.Box("Landing_L", r, new Vector3(cx - 0.8f, 1.4f, nz + 0.9f), new Vector3(0.1f, 2.8f, 1.7f), con, true);
            ClearviewArt.Box("Landing_R", r, new Vector3(cx + 0.8f, 1.4f, nz + 0.9f), new Vector3(0.1f, 2.8f, 1.7f), con, true);
            Light cold = new GameObject("Stairwell light (cold)").AddComponent<Light>();
            cold.transform.SetParent(r, false);
            cold.transform.position = new Vector3(cx, 2.3f, nz + 1.2f);
            cold.type = LightType.Point; cold.range = 3.5f; cold.intensity = 1.6f; cold.color = new Color(0.8f, 0.92f, 1f);
            Transform hinge = new GameObject("Door_Hinge").transform;
            hinge.SetParent(r, false);
            hinge.position = new Vector3(cx - 0.5f, 0f, nz);
            hinge.rotation = Quaternion.Euler(0f, -100f, 0f);   // wide open (towards the landing)
            ClearviewArt.Box("Leaf", hinge, new Vector3(0.5f, 1.04f, 0f), new Vector3(0.98f, 2.06f, 0.05f), ClearviewArt.DoorLeaf, true);
            b.doorHinge = hinge;

            // warm string lights and two lamps (nothing fluorescent down here)
            Material bulb = ClearviewArt.Mat("M_CV_Bulb", new Color(1f, 0.85f, 0.55f), null, Vector2.one, 0.5f, 0f, new Color(3f, 2.2f, 1.1f));
            for (int i = 0; i < 18; i++)
            {
                float t = i / 17f;
                Vector3 p = new Vector3(cx - w * 0.5f + 0.3f + t * (w - 0.6f), 2.55f - Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.3f, cz - d * 0.5f + 0.4f + t * 0.4f);
                ClearviewArt.Prim(PrimitiveType.Sphere, "Bulb", r, p, Vector3.one * 0.07f, bulb);
            }
            foreach (Vector3 lp in new[] { new Vector3(cx - 1.6f, 1.9f, cz - 0.6f), new Vector3(cx + 1.6f, 1.9f, cz + 0.6f) })
            {
                Light l = new GameObject("Warm lamp").AddComponent<Light>();
                l.transform.SetParent(r, false);
                l.transform.position = lp;
                l.type = LightType.Point; l.range = 5.5f; l.intensity = 1.8f; l.color = new Color(1f, 0.78f, 0.5f);
                l.shadows = LightShadows.Soft;
            }
            // what the walls say down here
            ClearviewArt.Poster(r, "HandPainted_Rule", new Vector3(cx, 1.75f, cz - d * 0.5f + 0.05f), 180f, 2.8f, 0.7f, new Color(0.95f, 0.9f, 0.78f), new Color(0.2f, 0.25f, 0.45f), "WE DON'T KEEP THINGS\nFROM EACH OTHER.");
            string[] years = { "'79", "'86", "'94", "'03", "'11", "'19" };
            for (int i = 0; i < years.Length; i++)
                ClearviewArt.Poster(r, "Polaroid " + years[i], new Vector3(cx - w * 0.5f + 0.05f, 1.35f + (i % 2) * 0.35f, cz - 1.5f + i * 0.5f), -90f, 0.25f, 0.3f, Color.white, ClearviewArt.Ink, "SEAT 31", years[i]);

            // crates, a sofa, snacks
            Transform crates = ClearviewArt.Group("Crates", r);
            Vector3 centre = new Vector3(cx, 0f, cz + 0.4f);
            Material crate = ClearviewArt.Wood;
            // a half circle in FRONT of your crate; the girl who hums sits in the middle, facing you
            string[] who = { "Triangle", "Never Blinks", "Hums When Scared", "Lent Me An Eraser", "Bus 14", "Don't Reply" };
            bool[] girl = { false, true, true, false, true, false };
            for (int i = 0; i < who.Length; i++)
            {
                float a = Mathf.Lerp(165f, 15f, i / (float)(who.Length - 1)) * Mathf.Deg2Rad;
                Vector3 p = centre + new Vector3(Mathf.Cos(a) * 1.75f, 0f, Mathf.Sin(a) * 1.3f);
                ClearviewArt.Box("Crate", crates, p + new Vector3(0f, 0.22f, -0.05f), new Vector3(0.48f, 0.44f, 0.42f), crate, true);
                GameObject f = new GameObject("Student_" + who[i]);
                f.transform.SetParent(r, false);
                f.transform.position = p;
                Vector3 look = centre - p; look.y = 0f;
                f.transform.rotation = Quaternion.LookRotation(look);
                ClearviewArt.Seated(f.transform, girl[i]);
                Transform lookT = f.transform.Find("Chibi");
                if (who[i] == "Hums When Scared") DressHummer(f.transform);
                if (who[i] == "Triangle") ChalkTriangle(f.transform, f.transform.TransformPoint(new Vector3(0f, 0.86f, 0.087f)), 0.1f);
                // their phones, glowing in their laps
                GameObject ph = ClearviewArt.Box("Phone", f.transform, new Vector3(0f, 0.8f, 0.4f), new Vector3(0.07f, 0.01f, 0.13f), bulb, false, new Vector3(-20f, 0f, 0f));
                ph.name = "Phone (glowing)";
            }
            ClearviewArt.Box("Sofa_Seat", r, new Vector3(cx + 2.3f, 0.22f, cz - 1.6f), new Vector3(0.8f, 0.44f, 1.6f), ClearviewArt.Blanket, true);
            ClearviewArt.Box("Sofa_Back", r, new Vector3(cx + 2.65f, 0.6f, cz - 1.6f), new Vector3(0.18f, 0.8f, 1.6f), ClearviewArt.Blanket, true);
            ClearviewArt.Box("LowTable", r, new Vector3(cx, 0.2f, cz + 0.4f), new Vector3(0.9f, 0.04f, 0.6f), crate, true);
            ClearviewArt.Box("Snacks", r, new Vector3(cx - 0.15f, 0.26f, cz + 0.35f), new Vector3(0.22f, 0.08f, 0.14f), ClearviewArt.Pastels[2]);
            ClearviewArt.Box("Snacks", r, new Vector3(cx + 0.2f, 0.25f, cz + 0.5f), new Vector3(0.16f, 0.06f, 0.16f), ClearviewArt.Pastels[3]);

            // your crate, facing the circle and the door
            Vector3 mine = centre + new Vector3(0f, 0f, -1.75f);
            ClearviewArt.Box("Crate (yours)", crates, mine + new Vector3(0f, 0.22f, -0.05f), new Vector3(0.48f, 0.44f, 0.42f), crate, true);
            Transform seat = new GameObject("BasementSeat").transform;
            seat.SetParent(r, false);
            seat.SetPositionAndRotation(mine, Quaternion.identity);
            b.seat = seat;

            // Voss, in the doorway (hidden until the end)
            GameObject v = Place("Fig_Teacher", r, new Vector3(cx, 0f, nz + 0.45f), 180f);
            if (PrefabUtility.IsPartOfPrefabInstance(v)) PrefabUtility.UnpackPrefabInstance(v, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            v.name = "VOSS (doorway)";
            TeacherPatrol tp = v.GetComponent<TeacherPatrol>();
            if (tp != null) Object.DestroyImmediate(tp);
            foreach (Light l in v.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            Transform fan = v.transform.Find("VisionFan");
            if (fan != null) Object.DestroyImmediate(fan.gameObject);
            foreach (Renderer rd in v.GetComponentsInChildren<Renderer>(true)) rd.sharedMaterial = rd.gameObject.name == "Eyes" ? ClearviewArt.EyeBand : ClearviewArt.Authority;
            ClearviewArt.Standing(v.transform, false, false);
            foreach (Collider c in v.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            v.SetActive(false);
            b.voss = v;
            return b;
        }

        static void DecorateClassroom(Transform room, bool inspection)
        {
            Transform d = ClearviewArt.Group("Clearview_Decor", room);
            ClearviewArt.Fluorescents(d, 0.4f, 0.4f, 9.6f, 11.6f, 2, 3, 1.1f, inspection ? 4 : 0);
            Color white = ClearviewArt.OffWhite, red = ClearviewArt.DeepRed, ink = ClearviewArt.Ink;

            // north wall (blackboard): viewer looks +Z
            if (inspection)
            {
                ClearviewArt.Poster(d, "Banner_HappyInspection", new Vector3(5f, 2.58f, 11.9f), 0f, 4.0f, 0.5f, ClearviewArt.PastelPink, red, "HAPPY INSPECTION DAY!  :)");
                ClearviewArt.Poster(d, "Banner_Motto", new Vector3(5f, 2.6f, 0.1f), 180f, 3.6f, 0.36f, white, red, "WHAT IS SEEN, IS SAFE.");
                Transform deco = ClearviewArt.Group("Inspection_Decorations", d);
                for (int i = 0; i < 3; i++) ClearviewArt.Bunting(deco, new Vector3(0.3f, 2.9f, 3f + i * 3f), new Vector3(9.7f, 2.9f, 3f + i * 3f), 0.35f, 22);
                ClearviewArt.Balloon(deco, new Vector3(3.95f, 0.15f, 10.7f), 1.7f, 0);
                ClearviewArt.Balloon(deco, new Vector3(4.1f, 0.15f, 10.75f), 1.85f, 1);
                ClearviewArt.Balloon(deco, new Vector3(6.05f, 0.15f, 10.7f), 1.75f, 2);
                ClearviewArt.Poster(d, "Poster_InspectedDesk", new Vector3(9.9f, 1.75f, 6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelMint, ink, "AN INSPECTED DESK\nIS A HAPPY DESK", "Instructor Voss\nthanks you in advance.", ClearviewArt.PastelYellow);
                ClearviewArt.Poster(d, "Sign_Instructor", new Vector3(5f, 2.24f, 11.9f), 0f, 1.1f, 0.16f, white, ink, "INSTRUCTOR  VOSS");
            }
            else
            {
                ClearviewArt.Poster(d, "Banner_Motto", new Vector3(5f, 2.6f, 11.9f), 0f, 3.6f, 0.36f, white, red, "WHAT IS SEEN, IS SAFE.");
                ClearviewArt.Poster(d, "Poster_Founding", new Vector3(9.9f, 1.75f, 6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelYellow, ink, "FOUNDED 1952", "Far from the noise\nof the city.\nClose to you.", ClearviewArt.PastelBlue);
            }
            ClearviewArt.Poster(d, "Poster_Quiet", new Vector3(9.9f, 1.75f, 4f), 90f, 0.7f, 0.9f, ClearviewArt.PastelMint, ink, "A QUIET ROOM\nIS A GOOD ROOM", "(Thank you for being quiet.)", ClearviewArt.PastelPink);
            ClearviewArt.Poster(d, "Poster_Hands", new Vector3(9.9f, 1.75f, 8f), 90f, 0.7f, 0.9f, ClearviewArt.PastelPink, ink, "HANDS WHERE\nWE CAN SEE\nTHEM  :)", null, ClearviewArt.PastelMint);
            // back wall (behind the player): viewer looks -Z
            ClearviewArt.Poster(d, "Poster_ReportAFriend", new Vector3(3.5f, 1.8f, 0.1f), 180f, 0.8f, 1.0f, ClearviewArt.PastelYellow, ink, "REPORT A\nFRIEND TODAY!", "It's what friends are for.", ClearviewArt.PastelPink);
            ClearviewArt.Poster(d, "Sign_Devices", new Vector3(8.2f, 1.9f, 0.1f), 180f, 0.9f, 0.55f, red, white, "DEVICES ARE NOT PERMITTED", "All devices are monitored.\nThis is for your safety.");
            ClearviewArt.Clock(d, new Vector3(8.6f, 2.45f, 11.9f), 0f, inspection ? 9.0f : 9.08f);
            ClearviewArt.Speaker(d, new Vector3(1.2f, 2.62f, 11.9f), 0f);
            ClearviewArt.DecoCamera(d, new Vector3(9.55f, 2.92f, 11.55f), -140f);
            ClearviewArt.DecoCamera(d, new Vector3(0.45f, 2.92f, 0.45f), 40f);
        }

        static void DecorateCorridor(Transform cor)
        {
            Transform d = ClearviewArt.Group("Clearview_Decor", cor);
            ClearviewArt.Fluorescents(d, 10f, 0f, 14f, 12f, 1, 3, 0.95f, 2, true);
            Color ink = ClearviewArt.Ink;
            ClearviewArt.Poster(d, "Sign_NoRunning", new Vector3(12f, 1.8f, 0.1f), 180f, 1.0f, 0.5f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "NO RUNNING.\nNO WALKING SLOWLY.");
            ClearviewArt.Poster(d, "Poster_Smile", new Vector3(13.9f, 1.75f, 1.6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelBlue, ink, "SMILE!", "You are on camera.\nYou are always on camera.", ClearviewArt.PastelYellow);
            ClearviewArt.Poster(d, "Plaque_Classroom_Back", new Vector3(10.1f, 1.85f, 2.35f), -90f, 0.5f, 0.22f, ClearviewArt.OffWhite, ink, "CLASSROOM 1");
            ClearviewArt.Poster(d, "Plaque_Classroom_Front", new Vector3(10.1f, 1.85f, 9.65f), -90f, 0.5f, 0.22f, ClearviewArt.OffWhite, ink, "CLASSROOM 1");
            ClearviewArt.Poster(d, "Poster_Uniform", new Vector3(10.1f, 2.35f, 6f), -90f, 1.0f, 0.55f, ClearviewArt.PastelMint, ink, "YOUR UNIFORM\nIS YOUR FRIEND", "Wear it. Be it.", ClearviewArt.PastelPink);
            ClearviewArt.DecoCamera(d, new Vector3(13.6f, 2.92f, 0.4f), -25f);
            ClearviewArt.Speaker(d, new Vector3(12f, 2.62f, 0.1f), 180f);
        }

        /// <summary>A closed door slab in a doorway (x, z = centre of the 2 m door module).</summary>
        static void ClosedDoor(Transform parent, Vector3 centre, bool alongZ, string sign, float signYaw)
        {
            GameObject g = ClearviewArt.Box("Door (closed)", parent, Vector3.zero, alongZ ? new Vector3(0.07f, 2.5f, 1.25f) : new Vector3(1.25f, 2.5f, 0.07f), ClearviewArt.DoorLeaf, true);
            g.transform.position = centre + new Vector3(0f, 1.25f, 0f);
            ClearviewArt.Box("Handle", g.transform, new Vector3(alongZ ? 0.8f : -0.4f, -0.05f, alongZ ? -0.4f : 0.8f), new Vector3(0.6f, 0.02f, 0.06f), ClearviewArt.Steel);
            if (!string.IsNullOrEmpty(sign))
            {
                Quaternion r = Quaternion.Euler(0f, signYaw, 0f);
                ClearviewArt.Poster(parent, "DoorSign", centre + new Vector3(0f, 1.75f, 0f) - r * Vector3.forward * 0.04f, signYaw, 0.55f, 0.28f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, sign);
            }
        }

        // ------------------------------------------------------------------ Chapter 2: the cafeteria
        static bool BuildLunch()
        {
            Scene s;
            GameObject level = CopyBlockout(L2Path, "LEVEL02_Lunch", out s);
            if (level == null) return false;
            Transform caf = level.transform.Find("Ch4_Cafeteria");
            Transform corridor = level.transform.Find("Corridor");
            Transform classroom = level.transform.Find("Ch1_Classroom_TheLesson");
            if (caf == null) { EditorUtility.DisplayDialog("Game", "Ch4_Cafeteria not found.", "OK"); return false; }
            caf.name = "Ch2_Cafeteria_Lunch";

            // the classroom is closed (only its walls stay, so the corridor is still a corridor)
            if (classroom != null)
            {
                List<GameObject> kill = new List<GameObject>();
                foreach (Transform c in classroom) if (c.name != "Shell") kill.Add(c.gameObject);
                foreach (GameObject g in kill) Object.DestroyImmediate(g);
            }

            ClearviewArt.Restyle(level.transform);
            ClearviewArt.DayLighting(FindSun(level));
            ClearviewArt.PostVolume(level.transform, false);

            if (classroom != null)
            {
                ClosedDoor(classroom, new Vector3(10f, 0f, 1f), true, null, 0f);
                ClosedDoor(classroom, new Vector3(10f, 0f, 11f), true, null, 0f);
            }
            if (corridor != null)
            {
                DecorateCorridor(corridor);
                Transform d = corridor.Find("Clearview_Decor");
                ClearviewArt.Poster(d, "DoorSign_InSession", new Vector3(10.1f, 1.85f, 2.6f), -90f, 0.6f, 0.3f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "IN SESSION\nDO NOT KNOCK");
                ClearviewArt.Poster(d, "Sign_Cafeteria", new Vector3(13.9f, 2.78f, 5f), 90f, 1.0f, 0.22f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "CAFETERIA");
            }

            // --- counters move forward: a narrow service lane behind them, closed at the west end
            Transform furn = caf.Find("Tables_Benches");
            foreach (Transform t in furn.GetComponentsInChildren<Transform>(true))
                if (t.name == "Cafe_Counter") t.position = new Vector3(t.position.x, 0f, 10.45f);
            GameObject extra = Place("Cafe_Counter", furn, new Vector3(15f, 0f, 10.45f), 0f);
            ClearviewArt.Restyle(extra.transform);
            Transform trays = ClearviewArt.Group("Trays", furn);
            for (int i = 0; i < 7; i++)
            {
                float x = 15f + i * 2f;
                ClearviewArt.Box("Tray", trays, new Vector3(x - 0.4f, 1.02f, 10.45f), new Vector3(0.4f, 0.03f, 0.3f), ClearviewArt.Steel);
                ClearviewArt.Box("Nutrition (beige)", trays, new Vector3(x - 0.4f, 1.06f, 10.45f), new Vector3(0.25f, 0.05f, 0.18f), ClearviewArt.BookC);
                ClearviewArt.Box("SneezeGuard", trays, new Vector3(x, 1.35f, 10.45f), new Vector3(1.9f, 0.02f, 0.5f), ClearviewArt.CamBody);
            }

            // --- STAFF door in the north wall (x 16..18)
            Transform walls = caf.Find("Shell/Walls");
            Transform old = null;
            if (walls != null)
                foreach (Transform w in walls)
                    if (w.name == "Wall_2m" && Vector3.Distance(w.position, new Vector3(16f, 0f, 12f)) < 0.1f) old = w;
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject doorWall = Place("Wall_Door_2m", walls != null ? walls : caf, new Vector3(16f, 0f, 12f), 0f);
            ClearviewArt.Restyle(doorWall.transform);

            Transform hinge = new GameObject("StaffDoor_Hinge").transform;
            hinge.SetParent(caf, false);
            hinge.position = new Vector3(16.4f, 0f, 12f);
            ClearviewArt.Box("Leaf", hinge, new Vector3(0.6f, 1.24f, 0f), new Vector3(1.19f, 2.46f, 0.06f), ClearviewArt.DoorLeaf, true);
            ClearviewArt.Box("Handle", hinge, new Vector3(1.05f, 1.05f, -0.05f), new Vector3(0.12f, 0.03f, 0.05f), ClearviewArt.Steel);
            ClearviewArt.Poster(hinge, "Sign_StaffOnly", new Vector3(17f, 1.75f, 11.97f), 0f, 0.6f, 0.3f, ClearviewArt.DeepRed, ClearviewArt.OffWhite, "STAFF ONLY");
            ClearviewArt.Box("StaffLight", caf, Vector3.zero, new Vector3(0.3f, 0.07f, 0.06f), ClearviewArt.SignGlow).transform.position = new Vector3(17f, 2.63f, 11.87f);
            GameObject use = new GameObject("StaffDoor_Use");
            use.transform.SetParent(caf, false);
            use.transform.position = new Vector3(17f, 1.25f, 11.8f);
            BoxCollider uc = use.AddComponent<BoxCollider>();
            uc.size = new Vector3(1.2f, 2.4f, 0.12f);
            uc.isTrigger = true;
            Interactable staffDoor = use.AddComponent<Interactable>();
            Interactable bottomDoor;
            Transform bottomHinge, voicesPoint;
            BuildStairwell(level.transform, caf, out bottomDoor, out bottomHinge, out voicesPoint);

            // --- an extra pillar: shade for the window table
            Transform cover = caf.Find("Pillars (cover vs camera)");
            GameObject pillar = Place("Pillar", cover != null ? cover : caf, new Vector3(27.9f, 0f, 7.4f), 0f);
            ClearviewArt.Restyle(pillar.transform);

            // --- the girl who hums: alone at the window table (26, 3)
            Transform people = caf.Find("Students (cover)");
            List<GameObject> drop = new List<GameObject>();
            foreach (Transform t in people)
                if (Mathf.Abs(t.position.x - 26f) < 1.35f && Mathf.Abs(t.position.z - 3f) < 0.9f) drop.Add(t.gameObject);
            foreach (GameObject g in drop) Object.DestroyImmediate(g);
            GameObject hummer = Place("Fig_Classmate_Seated", people, new Vector3(26.6f, 0f, 2.375f), 0f);
            ClearviewArt.Restyle(hummer.transform);
            hummer.name = "Fig_Classmate_Seated_Hummer";
            ClearviewArt.Undress(hummer.transform);
            ClearviewArt.Seated(hummer.transform, true);
            DressHummer(hummer.transform);
            HumSource hum = hummer.AddComponent<HumSource>();

            // --- bench seats you can use
            List<Interactable> seats = new List<Interactable>();
            Transform seatRoot = ClearviewArt.Group("Seats", caf);
            foreach (float x in new[] { 18f, 22f, 26f })
                foreach (float z in new[] { 3f, 6f, 9f })
                    foreach (int side in new[] { -1, 1 })
                    {
                        float bz = z + side * 0.625f;
                        foreach (float dx in new[] { -0.6f, 0.6f, 0f })
                        {
                            Vector3 p = new Vector3(x + dx, 0f, bz);
                            if (FigureNear(people, p, 0.5f)) continue;
                            GameObject so = new GameObject("Seat");
                            so.transform.SetParent(seatRoot, false);
                            so.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, side < 0 ? 0f : 180f, 0f));
                            BoxCollider c = so.AddComponent<BoxCollider>();
                            c.isTrigger = true;
                            c.center = new Vector3(0f, 0.56f, 0f);
                            c.size = new Vector3(0.5f, 0.2f, 0.34f);
                            seats.Add(so.AddComponent<Interactable>());
                            break;
                        }
                    }

            // --- the security camera: sweeps between the windows and the service lane
            TeacherVision camVision = null;
            Transform camT = Find(caf, "Watcher_SecurityCamera");
            if (camT != null)
            {
                camT.rotation = Quaternion.Euler(0f, -140f, 0f);
                SweepRotate sw = camT.GetComponent<SweepRotate>();
                if (sw != null) { sw.amplitude = 40f; sw.period = 14f; }
                camVision = camT.gameObject.AddComponent<TeacherVision>();
                camVision.eyeHeight = 2.8f;
                camVision.viewAngle = 50f;
                camVision.viewRange = 16f;
            }

            NavMeshSurface surface = MakeSurface(caf.gameObject);

            // --- the Lunch Monitor (deep red, mint apron)
            GameObject mon = Watcher("LUNCH_MONITOR", level.transform, new Vector3(21f, 0f, 1.4f), 0f, ClearviewArt.Authority, false, true);
            ClearviewArt.Box("Apron", mon.transform, new Vector3(0f, 0.88f, 0.178f), new Vector3(0.4f, 0.8f, 0.012f), ClearviewArt.Apron);
            PatrolAI monitor = mon.AddComponent<PatrolAI>();
            monitor.roamRect = new Vector4(15f, 0.8f, 29f, 9.5f);
            TeacherVision mv = mon.AddComponent<TeacherVision>();
            mv.viewAngle = 100f;
            mv.viewRange = 9f;
            mv.eyeHeight = 1.6f;
            Footsteps mf = mon.AddComponent<Footsteps>();
            mf.volume = 0.45f;

            // --- decor
            Transform dd = ClearviewArt.Group("Clearview_Decor", caf);
            ClearviewArt.Fluorescents(dd, 14.3f, 0.3f, 29.7f, 11.7f, 4, 3, 1.0f, 5);
            ClearviewArt.Poster(dd, "Banner_DaysSinceIncident", new Vector3(22f, 2.55f, 11.9f), 0f, 4.2f, 0.45f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "DAYS SINCE LAST INCIDENT:  0");
            ClearviewArt.Poster(dd, "Poster_Chew", new Vector3(25.6f, 1.75f, 11.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelPink, ClearviewArt.Ink, "CHEW 32 TIMES", "Smile between bites.", ClearviewArt.PastelMint);
            ClearviewArt.Poster(dd, "Poster_Menu", new Vector3(20f, 1.75f, 11.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "TODAY'S MENU", "NUTRITION\n(beige)\n\nDessert: cancelled\nfor your safety", ClearviewArt.PastelBlue);
            ClearviewArt.Poster(dd, "Poster_Privilege", new Vector3(14.1f, 1.8f, 8.5f), -90f, 0.8f, 0.9f, ClearviewArt.PastelMint, ClearviewArt.Ink, "LUNCH IS A\nPRIVILEGE", "Enjoy it correctly.", ClearviewArt.PastelYellow);
            ClearviewArt.Poster(dd, "Poster_Windows", new Vector3(29.9f, 1.8f, 11f), 90f, 0.7f, 0.8f, ClearviewArt.PastelBlue, ClearviewArt.Ink, "THE WINDOWS\nARE FOR LIGHT", "Not for looking.", ClearviewArt.PastelPink);
            ClearviewArt.Clock(dd, new Vector3(14.1f, 2.45f, 3f), -90f, 12.68f);
            ClearviewArt.Speaker(dd, new Vector3(14.1f, 2.62f, 10.9f), -90f);
            ClearviewArt.Speaker(dd, new Vector3(22f, 2.62f, 0.1f), 180f);

            // --- player
            PhoneController phone;
            Transform exposure;
            PlayerRig rig = MakeRig(new Vector3(12f, 0f, 5f), 90f, out phone, out exposure);
            Level02Director dir = rig.gameObject.AddComponent<Level02Director>();
            dir.rig = rig;
            dir.phone = phone;
            dir.navSurface = surface;
            dir.phoneExposurePoint = exposure;
            dir.monitor = monitor;
            dir.monitorVision = mv;
            dir.cameras = camVision != null ? new[] { camVision } : new TeacherVision[0];
            dir.hummer = hummer.transform;
            dir.hum = hum;
            dir.staffDoor = staffDoor;
            dir.staffDoorLeaf = hinge;
            dir.seats = seats.ToArray();
            dir.bottomDoor = bottomDoor;
            dir.bottomDoorHinge = bottomHinge;
            dir.voicesPoint = voicesPoint;

            EditorSceneManager.SaveScene(s);
            return true;
        }

        /// <summary>
        /// Behind the STAFF door: the stairs the school pretends aren't there. Concrete, a flickering tube,
        /// pipes, a handrail, generations of "Seat 31" years in chalk, the painted line - and at the bottom
        /// a door left ajar with warm light behind it. (The ground gets a hole so the stairs can really go down.)
        /// </summary>
        static void BuildStairwell(Transform level, Transform caf, out Interactable bottomDoor, out Transform bottomHinge, out Transform voicesPoint)
        {
            const float x0 = 15.95f, x1 = 18.05f, zTop = 13.3f, zEnd = 17.6f, run = 0.28f, rise = 0.2f;
            const int steps = 12;
            float bottom = -steps * rise;               // -2.4
            float zBottom = zTop + steps * run;         // 16.66

            // a hole in the ground under the stairwell
            Transform ground = Find(level, "Ground_60x40");
            Material gm = ClearviewArt.Ground;
            if (ground != null) Object.DestroyImmediate(ground.gameObject);
            Transform gr = ClearviewArt.Group("Ground (with stairwell hole)", level);
            GroundPiece(gr, -10f, -20f, 50f, 12.1f, gm);
            GroundPiece(gr, -10f, 12.1f, x0, 20f, gm);
            GroundPiece(gr, x1, 12.1f, 50f, 20f, gm);
            GroundPiece(gr, x0, zEnd + 0.1f, x1, 20f, gm);

            Transform w = ClearviewArt.Group("Stairwell", caf);
            Material con = ClearviewArt.Concrete, dado = ClearviewArt.Locker;
            ClearviewArt.Box("Landing", w, new Vector3(17f, -0.1f, (12.1f + zTop) * 0.5f), new Vector3(2.1f, 0.2f, zTop - 12.1f), ClearviewArt.Floor, true);
            for (int i = 0; i < steps; i++)
            {
                float top = -(i + 1) * rise;
                float z = zTop + i * run + run * 0.5f;
                ClearviewArt.Box("Step_" + (i + 1), w, new Vector3(17f, (top + bottom - 0.2f) * 0.5f, z), new Vector3(2.0f, top - (bottom - 0.2f), run), con, true);
                ClearviewArt.Box("Nosing", w, new Vector3(17f, top - 0.01f, z - run * 0.5f + 0.02f), new Vector3(2.0f, 0.025f, 0.045f), ClearviewArt.Pastels[2]);   // yellow safety edge
            }
            ClearviewArt.Box("BottomLanding", w, new Vector3(17f, bottom - 0.1f, (zBottom + zEnd) * 0.5f), new Vector3(2.1f, 0.2f, zEnd - zBottom), ClearviewArt.Floor, true);
            ClearviewArt.Box("Puddle", w, new Vector3(16.6f, bottom + 0.004f, 17.15f), new Vector3(0.7f, 0.004f, 0.45f), ClearviewArt.Steel);

            // walls: concrete with a faded blue-green band (the school's colours, even here)
            float wallH = 3f - (bottom - 0.2f);
            float wallY = (3f + bottom - 0.2f) * 0.5f;
            float wallZ = (12.1f + zEnd) * 0.5f, wallL = zEnd - 12.1f;
            ClearviewArt.Box("Wall_L", w, new Vector3(x0, wallY, wallZ), new Vector3(0.1f, wallH, wallL), con, true);
            ClearviewArt.Box("Wall_R", w, new Vector3(x1, wallY, wallZ), new Vector3(0.1f, wallH, wallL), con, true);
            ClearviewArt.Box("Band_L", w, new Vector3(x0 + 0.055f, bottom + 0.55f, (zBottom + zEnd) * 0.5f), new Vector3(0.01f, 1.1f, zEnd - zBottom), dado);
            ClearviewArt.Box("Band_R", w, new Vector3(x1 - 0.055f, bottom + 0.55f, (zBottom + zEnd) * 0.5f), new Vector3(0.01f, 1.1f, zEnd - zBottom), dado);
            ClearviewArt.Box("Ceiling", w, new Vector3(17f, 3.05f, wallZ), new Vector3(2.3f, 0.1f, wallL + 0.2f), con, true);
            // back wall with a doorway (x 16.55..17.55, up to bottom + 2.1)
            float dTop = bottom + 2.1f;
            ClearviewArt.Box("Back_L", w, new Vector3((x0 + 16.55f) * 0.5f, wallY, zEnd + 0.05f), new Vector3(16.55f - x0, wallH, 0.1f), con, true);
            ClearviewArt.Box("Back_R", w, new Vector3((17.55f + x1) * 0.5f, wallY, zEnd + 0.05f), new Vector3(x1 - 17.55f, wallH, 0.1f), con, true);
            ClearviewArt.Box("Back_Top", w, new Vector3(17.05f, (dTop + 3f) * 0.5f, zEnd + 0.05f), new Vector3(1.0f, 3f - dTop, 0.1f), con, true);
            // the door, left ajar - warm light behind it
            Transform hinge = new GameObject("Door_Ajar_Hinge").transform;
            hinge.SetParent(w, false);
            hinge.position = new Vector3(16.57f, bottom, zEnd + 0.02f);
            hinge.rotation = Quaternion.Euler(0f, -32f, 0f);
            ClearviewArt.Box("Leaf", hinge, new Vector3(0.48f, 1.04f, 0f), new Vector3(0.96f, 2.06f, 0.05f), ClearviewArt.DoorLeaf, true);
            bottomHinge = hinge;
            // you can't walk through - only push it (E)
            GameObject block = new GameObject("Doorway_Blocker");
            block.transform.SetParent(w, false);
            block.transform.position = new Vector3(17.05f, bottom + 1.05f, zEnd + 0.06f);
            block.AddComponent<BoxCollider>().size = new Vector3(1.0f, 2.1f, 0.1f);
            GameObject use = new GameObject("BottomDoor_Use");
            use.transform.SetParent(w, false);
            use.transform.position = new Vector3(17.05f, bottom + 1.05f, zEnd - 0.12f);
            BoxCollider uc = use.AddComponent<BoxCollider>();
            uc.size = new Vector3(1.0f, 2.0f, 0.12f);
            uc.isTrigger = true;
            bottomDoor = use.AddComponent<Interactable>();
            voicesPoint = new GameObject("Voices (behind the door)").transform;
            voicesPoint.SetParent(w, false);
            voicesPoint.position = new Vector3(17.05f, bottom + 1.2f, zEnd + 0.9f);
            Material warm = ClearviewArt.Mat("M_CV_WarmBeyond", new Color(1f, 0.85f, 0.6f), null, Vector2.one, 0.1f, 0f, new Color(3f, 2.1f, 1.2f));
            ClearviewArt.Box("Beyond", w, new Vector3(17.05f, bottom + 1.05f, zEnd + 0.6f), new Vector3(1.2f, 2.2f, 0.05f), warm);
            ClearviewArt.Box("Beyond_Floor", w, new Vector3(17.05f, bottom - 0.1f, zEnd + 0.35f), new Vector3(1.2f, 0.2f, 0.5f), ClearviewArt.Floor);
            Light spill = new GameObject("Warm light beyond").AddComponent<Light>();
            spill.transform.SetParent(w, false);
            spill.transform.position = new Vector3(17.05f, bottom + 1.4f, zEnd + 0.3f);
            spill.type = LightType.Point; spill.range = 4f; spill.intensity = 2.2f; spill.color = new Color(1f, 0.78f, 0.5f);

            // handrail on the right, pipes along the left ceiling
            Vector3 ra = new Vector3(x1 - 0.12f, 0.92f, zTop - 0.2f), rb = new Vector3(x1 - 0.12f, bottom + 0.92f, zBottom);
            Rod(w, "Handrail", ra, rb, 0.045f, ClearviewArt.Steel);
            for (int i = 0; i <= 3; i++)
            {
                Vector3 top = Vector3.Lerp(ra, rb, i / 3f);
                Rod(w, "RailPost", top, top - new Vector3(0f, 0.9f, 0f), 0.03f, ClearviewArt.Steel);
            }
            Rod(w, "Pipe", new Vector3(x0 + 0.18f, 2.75f, 12.15f), new Vector3(x0 + 0.18f, 2.75f, zEnd - 0.05f), 0.09f, ClearviewArt.Frame);
            Rod(w, "Pipe", new Vector3(x0 + 0.38f, 2.82f, 12.15f), new Vector3(x0 + 0.38f, 2.82f, zEnd - 0.05f), 0.06f, ClearviewArt.Steel);

            // light: one tired tube that flickers, plus a red emergency lamp
            Fluorescent tube = ClearviewArt.Fixture(w, new Vector3(17.2f, 2.985f, 14.6f), 0.9f, true, true);
            tube.buzzVolume = 0.1f;
            GameObject lamp = ClearviewArt.Box("EmergencyLamp", w, new Vector3(x0 + 0.08f, bottom + 2.3f, 17.1f), new Vector3(0.08f, 0.12f, 0.22f), ClearviewArt.SignGlow);
            Light el = lamp.AddComponent<Light>();
            el.type = LightType.Point; el.range = 3f; el.intensity = 0.8f; el.color = new Color(1f, 0.25f, 0.2f);

            // what the walls say
            ClearviewArt.Poster(w, "Painted_WeKeepThings", new Vector3(x0 + 0.05f, -0.2f, 15.2f), -90f, 2.6f, 0.8f, new Color(0.56f, 0.57f, 0.54f), new Color(0.72f, 0.12f, 0.1f), "WE KEEP THINGS\nFROM EACH OTHER.");
            ClearviewArt.Poster(w, "Chalk_Seat31", new Vector3(x1 - 0.05f, 0.9f, 14.2f), 90f, 0.9f, 0.75f, new Color(0.56f, 0.57f, 0.54f), new Color(0.95f, 0.95f, 0.9f), "SEAT 31", "'79   '86   '94\n'03   '11   '19\n\n...and you?");
            ClearviewArt.Poster(w, "Sign_NotHere", new Vector3(17.05f, bottom + 2.45f, zEnd), 0f, 1.0f, 0.22f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "THESE STAIRS DO NOT EXIST");
            ClearviewArt.Poster(w, "Chalk_Triangle", new Vector3(x1 - 0.05f, bottom + 1.4f, 16.9f), 90f, 0.35f, 0.35f, new Color(0.5f, 0.52f, 0.5f), new Color(0.95f, 0.95f, 0.9f), "/\\");
        }

        static void GroundPiece(Transform parent, float xa, float za, float xb, float zb, Material m)
        {
            ClearviewArt.Box("Ground", parent, new Vector3((xa + xb) * 0.5f, -0.155f, (za + zb) * 0.5f), new Vector3(xb - xa, 0.29f, zb - za), m, true);
        }

        static void Rod(Transform parent, string name, Vector3 a, Vector3 b, float thick, Material m)
        {
            GameObject g = ClearviewArt.Box(name, parent, Vector3.zero, Vector3.one, m);
            g.transform.position = (a + b) * 0.5f;
            g.transform.rotation = Quaternion.LookRotation(b - a);
            g.transform.localScale = new Vector3(thick, thick, Vector3.Distance(a, b));
        }

        /// <summary>
        /// The girl who hums, recognisable at a glance in every chapter: a big pink bow on her head,
        /// a small pink bow at the collar and a dusty-pink cardigan instead of the grey uniform.
        /// </summary>
        static void DressHummer(Transform fig)
        {
            Material pink = ClearviewArt.Mat("M_CV_PinkCardigan", new Color(0.86f, 0.62f, 0.67f), null, Vector2.one, 0.1f);
            foreach (Renderer r in fig.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name;
                if (n == "Body" || n == "Torso" || n == "UpperArm" || n == "Forearm") r.sharedMaterial = pink;
            }
            Transform look = fig.Find("Chibi");
            if (look == null) return;
            ClearviewArt.Bow(look, new Vector3(0f, 1.44f, -0.04f), 0.15f, ClearviewArt.Ribbon);
            ClearviewArt.Bow(look, new Vector3(0f, 1.03f, 0.096f), 0.07f, ClearviewArt.Ribbon);
        }

        static bool FigureNear(Transform people, Vector3 p, float r)
        {
            foreach (Transform t in people)
            {
                Vector3 q = t.position;
                if (new Vector2(q.x - p.x, q.z - p.z).magnitude < r) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ Chapter 3: the dormitory (new white box)
        const string W = "Wall_2m", D = "Wall_Door_2m", N = "Wall_Window_2m";

        static void BuildLightsOut()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject("LEVEL03_LightsOut").transform;
            Transform world = ClearviewArt.Group("World (NavMesh)", root);

            Place("Ground_60x40", world, Vector3.zero, 0f);
            Transform room = ClearviewArt.Group("Dorm_Room304", world);
            Transform hall = ClearviewArt.Group("Hallway", world);
            Floors(room, 0f, 0f, 4, 5, true);
            Floors(hall, 8f, -2f, 1, 7, true);
            Transform rw = ClearviewArt.Group("Walls", room);
            Run(rw, 0f, 0f, false, W, W, W, W);              // south
            Run(rw, 0f, 10f, false, W, W, W, W);             // north
            Run(rw, 0f, 0f, true, W, N, N, N, W);            // west: windows onto the yard
            Run(rw, 8f, 0f, true, W, D, W, W, W);            // east: the door to the hallway (z 2..4)
            foreach (Vector2 g in new[] { new Vector2(0f, 0f), new Vector2(8f, 0f), new Vector2(0f, 10f), new Vector2(8f, 10f) }) Place("Glue_Corner", rw, new Vector3(g.x, 0f, g.y), 0f);
            Transform hw = ClearviewArt.Group("Walls", hall);
            Run(hw, 10f, -2f, true, W, W, W, W, W, W, W);    // east
            Run(hw, 8f, -2f, false, W);                      // south end
            Run(hw, 8f, 12f, false, W);                      // north end
            Run(hw, 8f, -2f, true, W);
            Run(hw, 8f, 10f, true, W);
            foreach (Vector2 g in new[] { new Vector2(8f, -2f), new Vector2(10f, -2f), new Vector2(8f, 12f), new Vector2(10f, 12f) }) Place("Glue_Corner", hw, new Vector3(g.x, 0f, g.y), 0f);

            ClearviewArt.Restyle(root);

            // beds: (head x, z, length direction)
            Transform beds = ClearviewArt.Group("Beds", room);
            Transform playerBed = Bed(beds, 7.9f, 4.4f, -1, false, "Bed_PLAYER");
            Bed(beds, 7.9f, 8.3f, -1, true, "Bed");
            Bed(beds, 7.9f, 0.9f, -1, true, "Bed");
            Bed(beds, 0.1f, 1.3f, 1, true, "Bed");
            Bed(beds, 0.1f, 4.4f, 1, true, "Bed_NeverBlinks");
            Bed(beds, 0.1f, 7.6f, 1, true, "Bed");
            Transform lockers = ClearviewArt.Group("Lockers", room);
            for (int i = 0; i < 4; i++) ClearviewArt.Restyle(Place("Prop_Locker", lockers, new Vector3(3.25f + i * 0.5f, 0f, 9.65f), 180f).transform);

            // the dorm across the yard
            Transform yard = ClearviewArt.Group("AcrossTheYard", root);
            ClearviewArt.Box("Dorm_B", yard, new Vector3(-9.5f, 4.5f, 5f), new Vector3(3f, 9f, 18f), ClearviewArt.FarBuilding);
            ClearviewArt.Box("Wall_Yard", yard, new Vector3(-4f, 0.6f, 5f), new Vector3(0.3f, 1.2f, 18f), ClearviewArt.Concrete);
            List<Renderer> signal = new List<Renderer>();
            float[] rows = { 1.7f, 4.3f, 6.9f };
            float[] cols = { -1.5f, 1.2f, 3.9f, 6.3f, 9f, 11.6f };
            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < cols.Length; c++)
                {
                    bool isSignal = r == 1 && (c == 2 || c == 3);
                    bool lit = !isSignal && ((r * 7 + c * 3) % 5 == 0);
                    GameObject win = ClearviewArt.Box("Window", yard, new Vector3(-7.99f, rows[r], cols[c]), new Vector3(0.02f, 1.1f, 1.3f), isSignal || lit ? ClearviewArt.FarWindow : ClearviewArt.Black);
                    if (isSignal) signal.Add(win.GetComponent<Renderer>());
                }
            Light across = new GameObject("Signal Light (across)").AddComponent<Light>();
            across.transform.SetParent(yard, false);
            across.transform.position = new Vector3(-7.4f, 4.3f, 5.1f);
            across.type = LightType.Point; across.range = 8f; across.intensity = 6f; across.color = new Color(1f, 0.85f, 0.6f);
            across.enabled = false;

            // moonlight through the west windows
            Transform lights = ClearviewArt.Group("Lighting", root);
            Light moon = new GameObject("Moon (through west windows)").AddComponent<Light>();
            moon.transform.SetParent(lights, false);
            moon.type = LightType.Directional;
            moon.transform.rotation = Quaternion.Euler(28f, 100f, 0f);
            ClearviewArt.NightLighting(moon);
            ClearviewArt.PostVolume(root, true);
            Fluorescent f1 = ClearviewArt.Fixture(hall, new Vector3(9f, 2.985f, 2.6f), 0.5f, true, true);
            f1.buzzVolume = 0.09f;
            ClearviewArt.Fixture(hall, new Vector3(9f, 2.985f, 9f), 0.35f, false, true);

            Transform d = ClearviewArt.Group("Clearview_Decor", root);
            ClearviewArt.Poster(d, "Sign_LightsOut", new Vector3(9.9f, 2.3f, 3f), 90f, 0.9f, 0.24f, ClearviewArt.DeepRed, Color.white, "LIGHTS OUT  21:00", null, null, 0.7f);
            Light sg = new GameObject("Sign glow").AddComponent<Light>();
            sg.transform.SetParent(d, false);
            sg.transform.position = new Vector3(9.6f, 2.3f, 3f);
            sg.type = LightType.Point; sg.range = 2.5f; sg.intensity = 0.5f; sg.color = new Color(1f, 0.2f, 0.15f);
            ClearviewArt.Poster(d, "Plaque_Room304", new Vector3(8.1f, 1.9f, 1.55f), -90f, 0.45f, 0.2f, ClearviewArt.OffWhite, ClearviewArt.Ink, "ROOM 304", null, null, 0.45f);
            ClearviewArt.Poster(d, "Poster_Sleep", new Vector3(4f, 1.75f, 9.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelBlue, ClearviewArt.Ink, "SLEEP IS\nMANDATORY", "Dreams are optional.\nPlease dream quietly.", ClearviewArt.PastelPink, 0.22f);
            ClearviewArt.Poster(d, "Poster_Counting", new Vector3(4f, 1.75f, 0.1f), 180f, 0.8f, 0.9f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "COUNT SHEEP,\nNOT DAYS", null, ClearviewArt.PastelMint, 0.22f);
            ClearviewArt.Clock(d, new Vector3(6.2f, 2.4f, 0.1f), 180f, 23.67f);
            ClearviewArt.Speaker(d, new Vector3(2f, 2.62f, 0.1f), 180f);
            ClearviewArt.DecoCamera(d, new Vector3(7.75f, 2.92f, 9.75f), -135f);
            ClearviewArt.DecoCamera(d, new Vector3(9.8f, 2.92f, 11.6f), -170f);

            GameObject windowTarget = new GameObject("WindowTarget");
            windowTarget.transform.SetParent(root, false);
            windowTarget.transform.position = new Vector3(0f, 1.6f, 5f);
            Light nb = new GameObject("NeverBlinks phone glow").AddComponent<Light>();
            nb.transform.SetParent(beds, false);
            nb.transform.position = new Vector3(1.6f, 0.95f, 4.4f);
            nb.type = LightType.Point; nb.range = 1.8f; nb.intensity = 1.4f; nb.color = new Color(0.7f, 0.85f, 1f);
            nb.enabled = false;

            NavMeshSurface surface = MakeSurface(world.gameObject);

            // the night warden: navy, cap, flashlight, keys
            GameObject wd = Watcher("NIGHT_WARDEN", root, new Vector3(9f, 0f, -1.2f), 0f, ClearviewArt.Warden, true);
            foreach (Light l in wd.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            Light torch = new GameObject("Flashlight").AddComponent<Light>();
            torch.transform.SetParent(wd.transform, false);
            torch.transform.localPosition = new Vector3(0.32f, 0.9f, 0.32f);
            torch.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            torch.type = LightType.Spot; torch.spotAngle = 42f; torch.range = 13f; torch.intensity = 4f;
            torch.color = new Color(1f, 0.95f, 0.82f);
            torch.shadows = LightShadows.Soft;
            ClearviewArt.Box("Torch", wd.transform, new Vector3(0.32f, 0.8f, 0.18f), new Vector3(0.06f, 0.06f, 0.24f), ClearviewArt.Black, false, new Vector3(14f, 0f, 0f));
            PatrolAI warden = wd.AddComponent<PatrolAI>();
            warden.roamOnBegin = false;
            warden.scanAngle = 40f;
            warden.scanSpeed = 0.45f;
            TeacherVision wv = wd.AddComponent<TeacherVision>();
            wv.viewAngle = 70f;
            wv.viewRange = 14f;
            wv.eyeHeight = 1.6f;
            Footsteps steps = wd.AddComponent<Footsteps>();
            steps.volume = 0.75f;
            steps.maxDistance = 24f;
            steps.keysEverySteps = 5;
            steps.pitch = 0.9f;
            wd.GetComponent<NavMeshAgent>().speed = 1.05f;

            // the girl who hums, tiptoeing back from the stairwell (opening of the chapter)
            GameObject sn = Watcher("SNEAKING_STUDENT (the hummer)", root, new Vector3(9f, 0f, 11.2f), 180f, ClearviewArt.Uniform, false, true);
            foreach (Light l in sn.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            Transform snLook = sn.transform.Find("Chibi");
            if (snLook != null) ClearviewArt.Bow(snLook, new Vector3(0f, 1.845f, -0.08f), 0.11f, ClearviewArt.Ribbon);
            PatrolAI sneaker = sn.AddComponent<PatrolAI>();
            sneaker.roamOnBegin = false;
            sneaker.scanAngle = 15f;
            sn.GetComponent<NavMeshAgent>().speed = 0.9f;
            Footsteps snSteps = sn.AddComponent<Footsteps>();
            snSteps.volume = 0.22f;
            snSteps.pitch = 1.35f;
            snSteps.stepLength = 0.5f;

            // player: on the edge of the bed by the door, facing the window
            Transform bedSeat = new GameObject("BedSeat").transform;
            bedSeat.SetParent(playerBed, true);
            bedSeat.SetPositionAndRotation(new Vector3(6.15f, 0f, 4.4f), Quaternion.Euler(0f, -90f, 0f));
            PhoneController phone;
            Transform exposure;
            PlayerRig rig = MakeRig(bedSeat.position, -90f, out phone, out exposure);
            Camera cam = rig.eye.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.035f);
            Level03Director dir = rig.gameObject.AddComponent<Level03Director>();
            dir.rig = rig;
            dir.phone = phone;
            dir.navSurface = surface;
            dir.phoneExposurePoint = exposure;
            dir.warden = warden;
            dir.wardenVision = wv;
            dir.wardenSteps = steps;
            dir.sneaker = sneaker;
            dir.bedSeat = bedSeat;
            dir.windowTarget = windowTarget.transform;
            dir.acrossWindows = signal.ToArray();
            dir.acrossSignal = across;
            dir.neverBlinksGlow = nb;
            dir.bedsideStand = new Vector3(5.3f, 0f, 4.4f);
            dir.doorInside = new Vector3(6.9f, 0f, 3f);

            EditorSceneManager.SaveScene(s, L3Path);
        }

        /// <summary>A dorm bed along X. headX = x of the headboard, dir = +1 (bed runs east) or -1 (runs west).</summary>
        static Transform Bed(Transform parent, float headX, float z, int dir, bool sleeper, string name)
        {
            Transform b = ClearviewArt.Group(name, parent);
            float cx = headX + dir * 0.975f;
            ClearviewArt.Box("Frame", b, new Vector3(cx, 0.2f, z), new Vector3(1.95f, 0.4f, 0.9f), ClearviewArt.Metal, true);
            ClearviewArt.Box("Mattress", b, new Vector3(cx, 0.46f, z), new Vector3(1.9f, 0.12f, 0.86f), ClearviewArt.Mattress, true);
            ClearviewArt.Box("Headboard", b, new Vector3(headX + dir * 0.02f, 0.5f, z), new Vector3(0.04f, 1.0f, 0.9f), ClearviewArt.Metal, true);
            ClearviewArt.Box("Pillow", b, new Vector3(headX + dir * 0.25f, 0.56f, z), new Vector3(0.32f, 0.08f, 0.6f), ClearviewArt.Pillow);
            if (sleeper)
            {
                ClearviewArt.Box("Blanket", b, new Vector3(cx + dir * 0.25f, 0.6f, z), new Vector3(1.35f, 0.2f, 0.8f), ClearviewArt.Blanket, true);
                ClearviewArt.Box("Head", b, new Vector3(headX + dir * 0.3f, 0.68f, z), new Vector3(0.2f, 0.18f, 0.22f), ClearviewArt.Skin);
                ClearviewArt.Box("Hair", b, new Vector3(headX + dir * 0.23f, 0.7f, z), new Vector3(0.1f, 0.2f, 0.24f), ClearviewArt.Hair);
            }
            else
            {
                // your blanket, pushed back towards the headboard
                ClearviewArt.Box("Blanket", b, new Vector3(headX + dir * 0.7f, 0.56f, z), new Vector3(0.6f, 0.1f, 0.84f), ClearviewArt.Blanket);
            }
            return b;
        }

        static void Floors(Transform parent, float x0, float z0, int nx, int nz, bool ceiling)
        {
            Transform f = ClearviewArt.Group("Floor", parent);
            Transform c = ceiling ? ClearviewArt.Group("Ceiling", parent) : null;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    Place("Floor_2x2", f, new Vector3(x0 + i * 2f, 0f, z0 + j * 2f), 0f);
                    if (ceiling) Place("Ceiling_2x2_ShadowOnly", c, new Vector3(x0 + i * 2f, 0f, z0 + j * 2f), 0f);
                }
        }

        static void Run(Transform parent, float x, float z, bool alongZ, params string[] modules)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                float d = i * 2f;
                if (alongZ) Place(modules[i], parent, new Vector3(x, 0f, z + d), -90f);
                else Place(modules[i], parent, new Vector3(x + d, 0f, z), 0f);
            }
        }

        // ------------------------------------------------------------------ shared builders
        static GameObject Place(string prefab, Transform parent, Vector3 pos, float yaw)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + prefab + ".prefab");
            if (asset == null) { Debug.LogError("[Game] Missing blockout prefab: " + prefab); return new GameObject(prefab + " (missing)"); }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        /// <summary>A walking authority figure (from the kit's Fig_Teacher), ready for PatrolAI.</summary>
        static GameObject Watcher(string name, Transform parent, Vector3 pos, float yaw, Material body, bool cap, bool female = false)
        {
            GameObject g = Place("Fig_Teacher", parent, pos, yaw);
            if (PrefabUtility.IsPartOfPrefabInstance(g)) PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = name;
            TeacherPatrol tp = g.GetComponent<TeacherPatrol>();
            if (tp != null) Object.DestroyImmediate(tp);
            Transform fan = g.transform.Find("VisionFan");
            if (fan != null) Object.DestroyImmediate(fan.gameObject);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = r.gameObject.name == "Eyes" ? ClearviewArt.EyeBand : body;
            ClearviewArt.Standing(g.transform, cap, female);
            foreach (Light l in g.GetComponentsInChildren<Light>(true)) { l.intensity = 1.4f; l.color = new Color(1f, 0.4f, 0.35f); }
            SetLayer(g, 2);
            AddAgent(g, 1.15f);
            return g;
        }

        static void AddAgent(GameObject g, float speed)
        {
            NavMeshAgent agent = g.AddComponent<NavMeshAgent>();
            agent.speed = speed;
            agent.angularSpeed = 300f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.15f;
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.enabled = false;   // turned on after the NavMesh is built at runtime
        }

        static readonly string[] Furniture = { "Desk_Student", "Desk_Player", "Desk_Teacher", "Desk_Player_Cubby", "Chair", "Cafe_Table", "Cafe_Bench", "Cafe_Counter", "Prop_Shelf", "Prop_Locker", "Pillar", "Blackboard" };

        /// <summary>Nobody walks on desks, chairs, benches, counters or beds: their tops are "Not Walkable" in the NavMesh.</summary>
        static void NoWalkOnFurniture(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool hit = n.StartsWith("Bed") || n == "Trays" || n == "Seats";
                foreach (string f in Furniture) if (n == f || n.StartsWith(f + " (")) hit = true;
                if (!hit || t.GetComponent<NavMeshModifier>() != null) continue;
                NavMeshModifier m = t.gameObject.AddComponent<NavMeshModifier>();
                m.overrideArea = true;
                m.area = 1;   // Not Walkable
            }
        }

        static NavMeshSurface MakeSurface(GameObject g)
        {
            NoWalkOnFurniture(g.transform);
            NavMeshSurface surface = g.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~(1 << 2);
            surface.overrideVoxelSize = true;   // finer NavMesh = feet closer to the real floor
            surface.voxelSize = 0.05f;
            return surface;
        }

        static PlayerRig MakeRig(Vector3 pos, float yaw, out PhoneController phone, out Transform exposure)
        {
            GameObject rigGo = new GameObject("PLAYER");
            rigGo.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            CharacterController cc = rigGo.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;
            GameObject eye = new GameObject("Eye");
            eye.transform.SetParent(rigGo.transform, false);
            eye.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            eye.tag = "MainCamera";
            Camera cam = eye.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 120f;
            ClearviewArt.SetupCamera(cam);
            eye.AddComponent<AudioListener>();
            GameObject ex = new GameObject("PhoneExposurePoint");
            ex.transform.SetParent(rigGo.transform, false);
            ex.transform.localPosition = new Vector3(0f, 1.0f, 0.25f);
            exposure = ex.transform;

            PlayerRig rig = rigGo.AddComponent<PlayerRig>();
            rig.eye = eye.transform;
            phone = rigGo.AddComponent<PhoneController>();
            Light glow = new GameObject("PhoneGlow").AddComponent<Light>();
            glow.transform.SetParent(eye.transform, false);
            glow.transform.localPosition = new Vector3(0f, -0.35f, 0.32f);
            glow.type = LightType.Point;
            glow.range = 2.4f;
            glow.intensity = 0f;
            glow.color = new Color(0.72f, 0.86f, 1f);
            glow.shadows = LightShadows.None;
            glow.enabled = false;
            phone.glowLight = glow;
            return rig;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        // ------------------------------------------------------------------ the desk with the cubby
        class DeskParts
        {
            public Interactable cubbyZone, books, loosePanel, hiddenPhone;
        }

        static Material mWood, mInner, mDark, mBookA, mBookB, mChalk;

        /// <summary>
        /// Desk-local: pivot at the floor, centre of the desk. +Z = towards the teacher, the player sits on the -Z side.
        /// The cubby is open towards the player. Inside it, books hide a loose false back; behind that a 9 cm
        /// hidden space holds the phone. (No marks on the desk: the codes come from the lesson.)
        /// </summary>
        static DeskParts BuildCubbyDesk(Transform parent, Vector3 position)
        {
            DeskParts parts = new DeskParts();
            GameObject root = new GameObject("Desk_Player_Cubby");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.identity);
            Transform d = root.transform;

            Cube("DeskTop", d, new Vector3(0f, 0.735f, 0f), new Vector3(0.7f, 0.03f, 0.5f), mWood, true);
            Cube("Side_L", d, new Vector3(-0.33f, 0.36f, 0f), new Vector3(0.04f, 0.72f, 0.5f), mWood, true);
            Cube("Side_R", d, new Vector3(0.33f, 0.36f, 0f), new Vector3(0.04f, 0.72f, 0.5f), mWood, true);
            Cube("BackPanel", d, new Vector3(0f, 0.51f, 0.23f), new Vector3(0.62f, 0.42f, 0.04f), mInner, true);
            Cube("CubbyFloor", d, new Vector3(0f, 0.51f, -0.01f), new Vector3(0.62f, 0.02f, 0.44f), mInner, true);
            // generations of initials scratched into the top (decay beneath perfection)
            Cube("Scratch_1", d, new Vector3(-0.18f, 0.7505f, 0.08f), new Vector3(0.09f, 0.0012f, 0.006f), mInner, false, new Vector3(0f, 20f, 0f));
            Cube("Scratch_2", d, new Vector3(0.2f, 0.7505f, -0.12f), new Vector3(0.06f, 0.0012f, 0.005f), mInner, false, new Vector3(0f, -35f, 0f));
            Cube("Scratch_3", d, new Vector3(0.05f, 0.7505f, 0.15f), new Vector3(0.12f, 0.0012f, 0.004f), mInner, false, new Vector3(0f, 5f, 0f));

            GameObject zone = new GameObject("CubbyOpening");
            zone.transform.SetParent(d, false);
            zone.transform.localPosition = new Vector3(0f, 0.5f, -0.26f);
            BoxCollider zc = zone.AddComponent<BoxCollider>();
            zc.size = new Vector3(0.62f, 0.44f, 0.04f);
            zc.isTrigger = true;
            parts.cubbyZone = zone.AddComponent<Interactable>();

            // books in front of the false back. Sized (0.14 wide) so they slide 0.23 m to the left
            // and still fit inside the cubby (inner walls at x = +-0.31) without touching anything.
            GameObject books = new GameObject("Books");
            books.transform.SetParent(d, false);
            Cube("Book_1", books.transform, new Vector3(0f, 0.5375f, -0.06f), new Vector3(0.14f, 0.035f, 0.22f), mBookA, true);
            Cube("Book_2", books.transform, new Vector3(0.005f, 0.57f, -0.065f), new Vector3(0.13f, 0.03f, 0.2f), mBookB, true);
            Cube("Book_3", books.transform, new Vector3(0f, 0.60f, 0.075f), new Vector3(0.14f, 0.16f, 0.025f), mBookA, true);
            parts.books = books.AddComponent<Interactable>();

            Cube("FalseBack_L", d, new Vector3(-0.23f, 0.62f, 0.1f), new Vector3(0.16f, 0.2f, 0.015f), mInner, true);
            Cube("FalseBack_R", d, new Vector3(0.23f, 0.62f, 0.1f), new Vector3(0.16f, 0.2f, 0.015f), mInner, true);
            GameObject loose = Cube("LooseBackPanel", d, new Vector3(0f, 0.62f, 0.1f), new Vector3(0.30f, 0.2f, 0.015f), mInner, true);
            parts.loosePanel = loose.AddComponent<Interactable>();

            GameObject phone = Cube("HiddenPhone", d, new Vector3(0f, 0.6f, 0.19f), new Vector3(0.08f, 0.15f, 0.012f), mDark, true, new Vector3(10f, 0f, 0f));
            parts.hiddenPhone = phone.AddComponent<Interactable>();
            return parts;
        }

        static void ChalkTriangle(Transform parent, Vector3 center, float side)
        {
            GameObject g = new GameObject("ChalkTriangle_FrontChair");
            g.transform.SetParent(parent, false);
            g.transform.position = center;
            float h = side * 0.866f;
            Vector2 a = new Vector2(-side / 2f, -h / 3f), b = new Vector2(side / 2f, -h / 3f), top = new Vector2(0f, h * 2f / 3f);
            ChalkBar(g.transform, a, b);
            ChalkBar(g.transform, b, top);
            ChalkBar(g.transform, top, a);
        }

        static void ChalkBar(Transform parent, Vector2 from, Vector2 to)
        {
            Vector2 mid = (from + to) * 0.5f, dir = to - from;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Cube("Chalk", parent, new Vector3(mid.x, mid.y, 0f), new Vector3(dir.magnitude, 0.008f, 0.002f), mChalk, false, new Vector3(0f, 0f, ang));
        }

        static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 size, Material m, bool collider, Vector3? euler = null)
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

        static Transform Nearest(Transform root, string namePrefix, Vector3 p, float maxDist)
        {
            Transform best = null;
            float bestD = maxDist;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(namePrefix)) continue;
                Vector3 q = t.position; q.y = 0f;
                float dd = Vector3.Distance(q, new Vector3(p.x, 0f, p.z));
                if (dd < bestD) { bestD = dd; best = t; }
            }
            return best;
        }

        static void SetLayer(GameObject g, int layer)
        {
            foreach (Transform t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        static void MakeMaterials()
        {
            EnsureFolder("Assets/Game/Generated/Materials");
            mWood = ClearviewArt.Wood;
            mInner = Mat("M_DeskInner", new Color(0.36f, 0.28f, 0.21f), 0f);
            mDark = Mat("M_PhoneBody", new Color(0.06f, 0.06f, 0.07f), 0f);
            mBookA = Mat("M_BookBlue", new Color(0.2f, 0.3f, 0.5f), 0f);
            mBookB = Mat("M_BookRed", new Color(0.5f, 0.18f, 0.16f), 0f);
            mChalk = Mat("M_Chalk", new Color(0.95f, 0.95f, 0.92f), 0.2f);
        }

        static Material Mat(string name, Color c, float emission)
        {
            string path = "Assets/Game/Generated/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void SetBuildScenes()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
            foreach (string p in AllPaths) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (EditorBuildSettingsScene old in EditorBuildSettings.scenes)
            {
                if (System.Array.IndexOf(AllPaths, old.path) >= 0) continue;
                list.Add(new EditorBuildSettingsScene(old.path, false));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureFolder(string path) { ClearviewArt.EnsureFolder(path); }
    }
}
