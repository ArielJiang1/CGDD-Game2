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
    ///  Level04_PictureDay.unity - Ch.4, a new white box: the gymnasium (Cubberley Wing), class photo risers, prefects
    ///  Level05_Archive.unity    - Ch.5, a new white box: the library (Cubberley Wing), tall shelves, the restricted archive
    ///  Level06_Inspector.unity  - Ch.6, the Ch.1 classroom again, decorated for Inspection Day
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
        const string L4Path = SceneDir + "/Level04_PictureDay.unity";
        const string L5Path = SceneDir + "/Level05_Archive.unity";
        const string L6Path = SceneDir + "/Level06_Inspector.unity";
        const string OldL4Path = SceneDir + "/Level04_Inspector.unity";   // before the new chapters (renamed to Ch.6)
        const string EndPath = SceneDir + "/End.unity";
        const string BlockoutPath = "Assets/Blockout/Scenes/Blockout_School.unity";
        const string PrefabDir = "Assets/Blockout/Prefabs/";

        static readonly string[] AllPaths = { TitlePath, L1Path, L2Path, L3Path, L4Path, L5Path, L6Path, EndPath };

        /// <summary>
        /// Bump this whenever the scene-building code or serialized defaults change. Unity compares it on load
        /// (GameSceneAutoRebuild) and offers to rebuild out-of-date scenes, so nobody plays an old build by accident.
        /// </summary>
        public const int ScenesVersion = 27;

        [MenuItem("Game/Build All Scenes (Title, 6 Chapters, End)", false, 1)]
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
                EditorUtility.DisplayProgressBar("Game", "Chapter 4 - Picture Day", 0.76f);
                BuildPictureDay();
                EditorUtility.DisplayProgressBar("Game", "Chapter 5 - The Archive", 0.82f);
                BuildArchive();
                EditorUtility.DisplayProgressBar("Game", "Chapter 6 - Inspection Day", 0.9f);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OldL4Path) != null) AssetDatabase.DeleteAsset(OldL4Path);
                if (!BuildClassroom(L6Path, true)) return;
                SetBuildScenes();
                GameSceneAutoRebuild.MarkBuilt();
            }
            finally { EditorUtility.ClearProgressBar(); }

            EditorSceneManager.OpenScene(TitlePath, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Game",
                "Done.\n\nTitle, 6 chapters and End are in " + SceneDir + " and in Build Profiles.\n\n" +
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
            GameObject level = CopyBlockout(path, inspection ? "LEVEL06_InspectionDay" : "LEVEL01_TheLesson", out s);
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
                R(ClosedDoor(corridor, new Vector3(14f, 0f, 5f), true, "CAFETERIA\nOPENS 12:40", 90f), "12:40. Lunch is at 12:40. Every day. Exactly.\nThey say times here like passwords.");
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
                ClearviewArt.VossBeard(t.transform);   // Voss: the only full beard in the school
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
            Courtyard(level.transform, inspection);
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
                dir.basementStudents = b.students.ToArray();
                dir.verdictProps = VerdictProps(classroom);
            }

            Backdrop(level.transform, -10.5f, -3.5f, 14.2f, 15.5f, inspection ? 61 : 11, false);
            AddEggs(level.transform);
            ClearviewArt.NoCoplanarFaces(level.transform);   // nothing flush, nothing flickering
            EditorSceneManager.SaveScene(s);
            return true;
        }

        class Basement { public Transform seat, doorHinge; public GameObject voss; public List<Transform> students = new List<Transform>(); }

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
                b.students.Add(f.transform);
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
            ClearviewArt.VossBeard(v.transform);
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
                R(ClearviewArt.Poster(d, "Banner_HappyInspection", new Vector3(5f, 2.58f, 11.9f), 0f, 4.0f, 0.5f, ClearviewArt.PastelPink, red, "HAPPY INSPECTION DAY!  SINCE 1953  :)"),
                  "The balloons say HAPPY. The bunting says HAPPY.\nNobody in the room says anything.");
                R(ClearviewArt.Poster(d, "Banner_Motto", new Vector3(5f, 2.6f, 0.1f), 180f, 3.6f, 0.36f, white, red, "WHAT IS SEEN, IS SAFE."),
                  "Friday. Day five. In 1967 the fifth day was the one with the empty screen.");
                Transform deco = ClearviewArt.Group("Inspection_Decorations", d);
                for (int i = 0; i < 3; i++) ClearviewArt.Bunting(deco, new Vector3(0.3f, 2.9f, 3f + i * 3f), new Vector3(9.7f, 2.9f, 3f + i * 3f), 0.35f, 22);
                ClearviewArt.Balloon(deco, new Vector3(3.95f, 0.15f, 10.7f), 1.7f, 0);
                ClearviewArt.Balloon(deco, new Vector3(4.1f, 0.15f, 10.75f), 1.85f, 1);
                ClearviewArt.Balloon(deco, new Vector3(6.05f, 0.15f, 10.7f), 1.75f, 2);
                R(ClearviewArt.Poster(d, "Poster_InspectedDesk", new Vector3(9.9f, 1.75f, 6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelMint, ink, "AN INSPECTED DESK\nIS A HAPPY DESK", "Instructor Voss\nthanks you in advance.", ClearviewArt.PastelYellow),
                  "\"Thanks you in advance.\" In advance. For something we haven't done yet.");
                R(ClearviewArt.Poster(d, "Poster_412", new Vector3(5.2f, 1.75f, 0.1f), 180f, 0.9f, 0.8f, ClearviewArt.PastelYellow, ink, "LAST YEAR:\n412 ITEMS FOUND", "All of them are safe now.\nThank you for keeping\nnothing from us.", ClearviewArt.PastelMint),
                  "Four hundred and twelve things people kept. All \"safe\" now.\nI wonder what number mine would be.");
                R(ClearviewArt.Poster(d, "Sign_Instructor", new Vector3(5f, 2.24f, 11.9f), 0f, 1.1f, 0.16f, white, ink, "INSTRUCTOR  VOSS"),
                  "VOSS. Reporter of the Year, 1980. Seat 31 before that.\nHe knows exactly where to look.");
            }
            else
            {
                R(ClearviewArt.Poster(d, "Banner_Motto", new Vector3(5f, 2.6f, 11.9f), 0f, 3.6f, 0.36f, white, red, "WHAT IS SEEN, IS SAFE."),
                  "Rule one. Somebody scratched two words into the paint underneath:\nBY WHOM?");
                R(ClearviewArt.Poster(d, "Poster_Founding", new Vector3(9.9f, 1.75f, 6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelYellow, ink, "FOUNDED 1952", "Far from the noise\nof the city.\nClose to you.", ClearviewArt.PastelBlue),
                  "1952. The year it all began.\nIf I ever find a phone in this school, that's the first number I'd try.");
            }
            R(ClearviewArt.Poster(d, "Poster_Quiet", new Vector3(9.9f, 1.75f, 4f), 90f, 0.7f, 0.9f, ClearviewArt.PastelMint, ink, "A QUIET ROOM\nIS A GOOD ROOM", "(Thank you for being quiet.)", ClearviewArt.PastelPink),
              "Small print at the bottom: \"Silence is not the same as hiding.\nPlease do not confuse them.\"");
            R(ClearviewArt.Poster(d, "Poster_Hands", new Vector3(9.9f, 1.75f, 8f), 90f, 0.7f, 0.9f, ClearviewArt.PastelPink, ink, "HANDS WHERE\nWE CAN SEE\nTHEM  :)", null, ClearviewArt.PastelMint),
              "Hands where they can see them. So the phone goes where they can't:\nunder the desk.");
            // back wall (behind the player): viewer looks -Z
            R(ClearviewArt.Poster(d, "Poster_ReportAFriend", new Vector3(3.5f, 1.8f, 0.1f), 180f, 0.8f, 1.0f, ClearviewArt.PastelYellow, ink, "REPORT A\nFRIEND TODAY!", "It's what friends are for.", ClearviewArt.PastelPink),
              inspection ? "Under it, a tally for this week. Twenty marks.\nThree of them in red." : "...The small print changed while I was reading it.",
              null, null, inspection ? null : "It's what friends are for.\n\n(Thank you for reading.\nYour interest has been noted.)");
            R(ClearviewArt.Poster(d, "Sign_Devices", new Vector3(8.2f, 1.9f, 0.1f), 180f, 0.9f, 0.55f, red, white, "DEVICES ARE NOT PERMITTED", "All devices are monitored.\nThis is for your safety."),
              "\"All devices are monitored.\" Even the ones nobody knows about?\nEspecially those, probably.");
            ClearviewArt.Clock(d, new Vector3(8.6f, 2.45f, 11.9f), 0f, inspection ? 9.0f : 9.08f);
            // the lesson's Founding Charter, the founder's portrait (with the pencil moustache) and the bell: all on the front wall
            R(ClearviewArt.Poster(d, "Poster_Charter", new Vector3(8.45f, 1.45f, 11.9f), 0f, 1.2f, 1.25f, white, ink, "THE FOUNDING\nCHARTER",
                "1. What is seen, is safe.\n2. No voice leaves these walls.\n3. Lights out at 21:00. Exactly.\n4. We do not keep things from each other.", red),
              "Four rules. Somebody underlined rule four.\nTwice. In red.");
            FounderPortrait(d, new Vector3(1.9f, 1.6f, 11.9f), 0f, inspection);
            WallBell(d, new Vector3(0.6f, 2.4f, 11.9f), 0f);
            ClearviewArt.Speaker(d, new Vector3(1.2f, 2.62f, 11.9f), 0f);
            ClearviewArt.DecoCamera(d, new Vector3(9.55f, 2.92f, 11.55f), -140f);
            ClearviewArt.DecoCamera(d, new Vector3(0.45f, 2.92f, 0.45f), 40f);
        }

        /// <summary>
        /// The founder's portrait (1952). Someone gave him a pencil moustache; the teacher says whoever did it is left-handed.
        /// Built from flat boxes on the wall: frame, painted background, bust, and the graphite moustache curling to one side.
        /// </summary>
        static void FounderPortrait(Transform parent, Vector3 wallPoint, float lookYaw, bool inspection)
        {
            Transform p = new GameObject("Portrait_Founder").transform;
            p.SetParent(parent, false);
            p.SetPositionAndRotation(wallPoint, Quaternion.Euler(0f, lookYaw, 0f));   // local -Z = towards the viewer
            Material gilt = ClearviewArt.Mat("M_CV_Gilt", new Color(0.62f, 0.5f, 0.26f), null, Vector2.one, 0.6f, 0.5f);
            Material bg = ClearviewArt.Mat("M_CV_PortraitBg", new Color(0.33f, 0.38f, 0.34f), null, Vector2.one, 0.2f);
            Material suit = ClearviewArt.Mat("M_CV_Suit", new Color(0.14f, 0.14f, 0.17f), null, Vector2.one, 0.2f);
            Material shirt = ClearviewArt.Mat("M_CV_Shirt", ClearviewArt.OffWhite, null, Vector2.one, 0.2f);
            Material graphite = ClearviewArt.Mat("M_CV_Graphite", new Color(0.2f, 0.2f, 0.22f), null, Vector2.one, 0.5f, 0.3f);
            ClearviewArt.Box("Frame", p, new Vector3(0f, 0f, -0.025f), new Vector3(0.78f, 0.98f, 0.05f), gilt);
            ClearviewArt.Box("Canvas", p, new Vector3(0f, 0f, -0.055f), new Vector3(0.64f, 0.84f, 0.012f), bg);
            ClearviewArt.Box("Suit", p, new Vector3(0f, -0.27f, -0.064f), new Vector3(0.5f, 0.3f, 0.008f), suit);
            ClearviewArt.Box("Shirt", p, new Vector3(0f, -0.17f, -0.07f), new Vector3(0.12f, 0.1f, 0.004f), shirt);
            ClearviewArt.Box("Tie", p, new Vector3(0f, -0.22f, -0.074f), new Vector3(0.035f, 0.14f, 0.003f), ClearviewArt.Authority);
            ClearviewArt.Box("Neck", p, new Vector3(0f, -0.1f, -0.066f), new Vector3(0.09f, 0.08f, 0.004f), ClearviewArt.Skin);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Head", p, new Vector3(0f, 0.04f, -0.075f), new Vector3(0.22f, 0.28f, 0.03f), ClearviewArt.Skin);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Hair", p, new Vector3(0f, 0.15f, -0.079f), new Vector3(0.23f, 0.1f, 0.03f), ClearviewArt.Hair);
            for (int i = -1; i <= 1; i += 2)
                ClearviewArt.Box("Eye", p, new Vector3(i * 0.045f, 0.06f, -0.0925f), new Vector3(0.026f, 0.012f, 0.003f), ClearviewArt.Black);
            ClearviewArt.Box("Mouth", p, new Vector3(0f, -0.045f, -0.0915f), new Vector3(0.055f, 0.008f, 0.003f), ClearviewArt.Authority);
            // the pencil moustache: both curls hook the same way (drawn by a left hand pulling the pencil)
            Transform m = ClearviewArt.Group("Moustache (pencil)", p);
            ClearviewArt.Box("Stroke_L", m, new Vector3(-0.034f, -0.014f, -0.0935f), new Vector3(0.068f, 0.01f, 0.003f), graphite, false, new Vector3(0f, 0f, -14f));
            ClearviewArt.Box("Stroke_R", m, new Vector3(0.034f, -0.014f, -0.0935f), new Vector3(0.068f, 0.01f, 0.003f), graphite, false, new Vector3(0f, 0f, 14f));
            ClearviewArt.Box("Curl_L", m, new Vector3(-0.074f, 0.002f, -0.0935f), new Vector3(0.028f, 0.008f, 0.003f), graphite, false, new Vector3(0f, 0f, 62f));
            ClearviewArt.Box("Curl_R", m, new Vector3(0.07f, -0.026f, -0.0935f), new Vector3(0.028f, 0.008f, 0.003f), graphite, false, new Vector3(0f, 0f, 62f));
            ClearviewArt.Poster(parent, "Plate_Founder", wallPoint + Vector3.down * 0.56f, lookYaw, 0.56f, 0.11f, new Color(0.62f, 0.5f, 0.26f), ClearviewArt.Ink, "OUR FOUNDER  -  1952");
            Readable r = ReadPoint(p, inspection
                ? "The founder. Somebody tried to rub out the pencil moustache for Inspection Day.\nYou can still see it. Pencil never really comes off."
                : "The founder, 1952. Somebody gave him a moustache in pencil.\nBoth curls hook to the left. Drawn by a left hand.", 0.5f, 5f);
            r.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        }

        /// <summary>The school bell on the classroom wall ("Seven classes. Seven bells.").</summary>
        static void WallBell(Transform parent, Vector3 wallPoint, float lookYaw)
        {
            Transform b = new GameObject("Wall Bell").transform;
            b.SetParent(parent, false);
            b.SetPositionAndRotation(wallPoint, Quaternion.Euler(0f, lookYaw, 0f));
            Material brass = ClearviewArt.Mat("M_CV_Brass", new Color(0.72f, 0.56f, 0.24f), null, Vector2.one, 0.75f, 0.8f);
            ClearviewArt.Box("Mount", b, new Vector3(0f, 0f, -0.02f), new Vector3(0.1f, 0.1f, 0.04f), ClearviewArt.CamBody);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Gong", b, new Vector3(0f, 0f, -0.06f), new Vector3(0.24f, 0.24f, 0.09f), brass);
            ClearviewArt.Box("Striker_Arm", b, new Vector3(0f, -0.15f, -0.07f), new Vector3(0.012f, 0.08f, 0.012f), ClearviewArt.Black);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Striker", b, new Vector3(0f, -0.115f, -0.088f), new Vector3(0.03f, 0.03f, 0.03f), ClearviewArt.Black);
            ReadPoint(b, "The bell. Seven a day. Seven classes, every single day.\nThe loudest thing in the school, and nobody else is allowed to be.", 0.25f, 4f)
                .transform.localPosition = new Vector3(0f, 0f, -0.12f);
        }

        /// <summary>A Readable on something that isn't a poster (a tree, a bell, a portrait). Faces the way the viewer looks.</summary>
        static Readable ReadPoint(Transform parent, string reaction, float radius, float range)
        {
            GameObject g = new GameObject("ReadPoint");
            g.transform.SetParent(parent, false);
            Readable r = g.AddComponent<Readable>();
            r.reaction = reaction;
            r.radius = radius;
            r.range = range;
            return r;
        }

        /// <summary>A box stretched between two points (branches).</summary>
        static GameObject Limb(Transform parent, Vector3 a, Vector3 b, float thick, Material m)
        {
            GameObject g = ClearviewArt.Box("Branch", parent, Vector3.zero, Vector3.one, m);
            g.transform.position = (a + b) * 0.5f;
            g.transform.rotation = Quaternion.LookRotation(b - a);
            g.transform.localScale = new Vector3(thick, thick, Vector3.Distance(a, b));
            return g;
        }

        static Pigeon MakePigeon(Transform parent, Vector3 feet, float yaw, float hop, Vector3 axes)
        {
            Material grey = ClearviewArt.Mat("M_CV_Pigeon", new Color(0.56f, 0.58f, 0.62f), null, Vector2.one, 0.25f);
            Material dark = ClearviewArt.Mat("M_CV_PigeonNeck", new Color(0.3f, 0.36f, 0.38f), null, Vector2.one, 0.6f, 0.3f);
            Material pink = ClearviewArt.Mat("M_CV_PigeonFeet", new Color(0.78f, 0.42f, 0.42f), null, Vector2.one, 0.2f);
            Transform p = new GameObject("Pigeon").transform;
            p.SetParent(parent, false);
            p.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
            ClearviewArt.Prim(PrimitiveType.Sphere, "Body", p, new Vector3(0f, 0.085f, 0f), new Vector3(0.12f, 0.11f, 0.2f), grey);
            ClearviewArt.Box("Wing_L", p, new Vector3(-0.055f, 0.1f, -0.02f), new Vector3(0.02f, 0.06f, 0.14f), dark, false, new Vector3(-8f, 0f, 0f));
            ClearviewArt.Box("Wing_R", p, new Vector3(0.055f, 0.1f, -0.02f), new Vector3(0.02f, 0.06f, 0.14f), dark, false, new Vector3(-8f, 0f, 0f));
            ClearviewArt.Box("Tail", p, new Vector3(0f, 0.1f, -0.13f), new Vector3(0.07f, 0.018f, 0.1f), dark, false, new Vector3(-15f, 0f, 0f));
            for (int i = -1; i <= 1; i += 2)
                ClearviewArt.Box("Leg", p, new Vector3(i * 0.022f, 0.02f, 0.01f), new Vector3(0.01f, 0.04f, 0.01f), pink);
            Transform head = new GameObject("Head").transform;
            head.SetParent(p, false);
            head.localPosition = new Vector3(0f, 0.15f, 0.075f);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Neck", head, new Vector3(0f, -0.02f, 0f), new Vector3(0.07f, 0.08f, 0.07f), dark);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.025f, 0.02f), new Vector3(0.06f, 0.06f, 0.065f), grey);
            ClearviewArt.Box("Beak", head, new Vector3(0f, 0.02f, 0.06f), new Vector3(0.014f, 0.012f, 0.03f), pink);
            for (int i = -1; i <= 1; i += 2)
                ClearviewArt.Box("Eye", head, new Vector3(i * 0.029f, 0.035f, 0.032f), new Vector3(0.004f, 0.01f, 0.01f), ClearviewArt.Black);
            Pigeon pg = p.gameObject.AddComponent<Pigeon>();
            pg.head = head;
            pg.hopRadius = hop;
            pg.hopAxes = axes;
            return pg;
        }

        /// <summary>
        /// The courtyard outside the classroom's west windows (x &lt; 0): grass, the oak the founder planted on the first
        /// day, pigeons on the window ledges, on the grass and on a low branch. Two school wings close it in north and south.
        /// The lesson and the PA talk about all of it, so it has to be there when you look out of the window.
        /// </summary>
        static void Courtyard(Transform level, bool inspection)
        {
            Transform c = ClearviewArt.Group("Courtyard (outside the west windows)", level);
            Material grass = ClearviewArt.Mat("M_CV_Grass", new Color(0.42f, 0.54f, 0.32f), null, Vector2.one, 0.1f);
            Material bark = ClearviewArt.Mat("M_CV_Bark", new Color(0.33f, 0.25f, 0.18f), null, Vector2.one, 0.1f);
            Material leafA = ClearviewArt.Mat("M_CV_OakLeaves", new Color(0.26f, 0.4f, 0.21f), null, Vector2.one, 0.1f);
            Material leafB = ClearviewArt.Mat("M_CV_OakLeaves2", new Color(0.34f, 0.48f, 0.25f), null, Vector2.one, 0.1f);
            Material hedge = ClearviewArt.Mat("M_CV_Hedge", new Color(0.22f, 0.33f, 0.19f), null, Vector2.one, 0.1f);
            Material wing = ClearviewArt.Mat("M_CV_SchoolWing", new Color(0.8f, 0.78f, 0.72f), null, Vector2.one, 0.2f);
            Material pane = ClearviewArt.Mat("M_CV_WingWindow", new Color(0.36f, 0.42f, 0.46f), null, Vector2.one, 0.8f);

            ClearviewArt.Box("Grass", c, new Vector3(-5.25f, -0.03f, 6f), new Vector3(9.9f, 0.1f, 17.6f), grass, true);   // top y 0.02
            ClearviewArt.Box("Path", c, new Vector3(-2.3f, 0f, 6f), new Vector3(3.8f, 0.06f, 0.9f), ClearviewArt.Concrete);
            ClearviewArt.Box("Hedge_W", c, new Vector3(-9.7f, 0.6f, 6f), new Vector3(0.8f, 1.2f, 17.6f), hedge, true);
            foreach (float z in new[] { 15.1f, -3.1f })
            {
                ClearviewArt.Box(z > 0f ? "Wing_North" : "Wing_South", c, new Vector3(-5.2f, 3f, z), new Vector3(10.4f, 6f, 0.4f), wing, true);
                float face = z > 0f ? z - 0.21f : z + 0.21f;
                for (int row = 0; row < 2; row++)
                    for (float x = -9f; x <= -1f; x += 2f)
                        ClearviewArt.Box("Window", c, new Vector3(x, 1.7f + row * 2.5f, face), new Vector3(1.1f, 1.2f, 0.02f), pane);
            }
            // window ledges under the classroom windows (pigeons sit here)
            foreach (float z in new[] { 3f, 5f, 7f, 9f })
                ClearviewArt.Box("Ledge", c, new Vector3(-0.2f, 0.955f, z), new Vector3(0.2f, 0.05f, 1.3f), ClearviewArt.Concrete);

            // the founder's oak
            Transform oak = ClearviewArt.Group("Oak (planted by the founder, 1952)", c);
            Vector3 o = new Vector3(-5.5f, 0f, 7f);
            ClearviewArt.Prim(PrimitiveType.Cylinder, "Trunk", oak, o + new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 1.5f, 0.6f), bark);
            ClearviewArt.Prim(PrimitiveType.Cylinder, "Root flare", oak, o + new Vector3(0f, 0.12f, 0f), new Vector3(0.95f, 0.1f, 0.95f), bark);
            Vector3 lowTip = o + new Vector3(2.0f, 2.35f, -0.7f);
            Limb(oak, o + new Vector3(0f, 2.0f, 0f), lowTip, 0.16f, bark);
            Limb(oak, o + new Vector3(0f, 2.6f, 0f), o + new Vector3(-1.6f, 3.6f, 0.9f), 0.2f, bark);
            Limb(oak, o + new Vector3(0f, 2.8f, 0f), o + new Vector3(0.8f, 4.0f, 1.4f), 0.18f, bark);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Canopy", oak, o + new Vector3(0f, 4.7f, 0f), new Vector3(4.2f, 3.2f, 4.2f), leafA);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Canopy", oak, o + new Vector3(1.3f, 4.1f, -0.6f), new Vector3(2.6f, 2.2f, 2.6f), leafB);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Canopy", oak, o + new Vector3(-1.2f, 4.3f, 0.9f), new Vector3(3.0f, 2.4f, 3.0f), leafB);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Canopy", oak, o + new Vector3(0.3f, 5.7f, 0.6f), new Vector3(2.6f, 2.0f, 2.6f), leafA);
            ClearviewArt.Prim(PrimitiveType.Sphere, "Canopy", oak, o + new Vector3(-0.4f, 4.0f, -1.3f), new Vector3(2.4f, 2.0f, 2.4f), leafA);
            Readable read = ReadPoint(oak, inspection
                ? "Inspection Day. The oak doesn't care.\nThe founder planted it on the very first day. It has never been inspected."
                : "The oak in the courtyard. The founder planted it on the very first day.\nThe only thing here older than the rules.", 2.2f, 12f);
            read.transform.position = o + new Vector3(0f, 4.2f, 0f);
            read.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            // the pigeons: on the ledges (seen from your seat), on the grass, on the low branch
            Transform birds = ClearviewArt.Group("Pigeons (not monitored... yet)", c);
            MakePigeon(birds, new Vector3(-0.22f, 0.98f, 4.75f), 20f, 0.35f, new Vector3(0f, 0f, 1f));
            MakePigeon(birds, new Vector3(-0.2f, 0.98f, 5.3f), 170f, 0f, Vector3.zero).starer = true;   // "one of them is looking right at me"
            MakePigeon(birds, new Vector3(-0.22f, 0.98f, 7.2f), 100f, 0.35f, new Vector3(0f, 0f, 1f));
            MakePigeon(birds, new Vector3(-3.4f, 0.03f, 5.3f), 60f, 0.8f, new Vector3(1f, 0f, 1f));
            MakePigeon(birds, new Vector3(-3.0f, 0.03f, 6.8f), -120f, 0.8f, new Vector3(1f, 0f, 1f));
            MakePigeon(birds, new Vector3(-7.0f, 0.03f, 5.0f), 10f, 0.8f, new Vector3(1f, 0f, 1f));
            MakePigeon(birds, lowTip + new Vector3(-0.25f, 0.08f, 0.09f), 90f, 0f, Vector3.zero);
            MakePigeon(birds, lowTip + new Vector3(-0.6f, 0.05f, 0.21f), -80f, 0f, Vector3.zero);
            Readable pr = ReadPoint(birds, inspection
                ? "Pigeons on the ledge. Nobody inspects them.\nNot yet."
                : "Pigeons on the window ledge. The courtyard ones.\nNot monitored. ...Yet. One of them is looking right at me.", 0.6f, 7f);
            pr.transform.position = new Vector3(-0.25f, 1.12f, 5.05f);
            pr.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }

        /// <summary>
        /// What you see out of the windows, in every chapter: grass instead of the bare white-box ground, a school wall,
        /// trees close by, other buildings further out (lit windows at night), so the world never ends at the glass.
        /// (x0, z0)-(x1, z1) = the playable buildings; nothing is placed inside that rectangle. No colliders anywhere.
        /// </summary>
        static void Backdrop(Transform root, float x0, float z0, float x1, float z1, int seed, bool night)
        {
            Transform g = ClearviewArt.Group("Backdrop (outside the windows)", root);
            System.Random rng = new System.Random(seed);
            System.Func<float, float, float> rnd = (a, b) => a + (float)rng.NextDouble() * (b - a);
            Material grass = ClearviewArt.Mat("M_CV_Lawn", new Color(0.4f, 0.52f, 0.31f), null, Vector2.one, 0.1f);
            Material bark = ClearviewArt.Mat("M_CV_Bark", new Color(0.33f, 0.25f, 0.18f), null, Vector2.one, 0.1f);
            Material[] leaves =
            {
                ClearviewArt.Mat("M_CV_OakLeaves", new Color(0.26f, 0.4f, 0.21f), null, Vector2.one, 0.1f),
                ClearviewArt.Mat("M_CV_OakLeaves2", new Color(0.34f, 0.48f, 0.25f), null, Vector2.one, 0.1f),
                ClearviewArt.Mat("M_CV_Conifer", new Color(0.17f, 0.3f, 0.2f), null, Vector2.one, 0.1f),
            };
            Material[] facades = night
                ? new[] { ClearviewArt.FarBuilding }
                : new[]
                {
                    ClearviewArt.Mat("M_CV_SchoolWing", new Color(0.8f, 0.78f, 0.72f), null, Vector2.one, 0.2f),
                    ClearviewArt.Mat("M_CV_BrickFar", new Color(0.62f, 0.4f, 0.33f), null, Vector2.one, 0.15f),
                    ClearviewArt.Mat("M_CV_GreyBlock", new Color(0.62f, 0.64f, 0.66f), null, Vector2.one, 0.2f),
                };
            Material pane = night ? ClearviewArt.Black : ClearviewArt.Mat("M_CV_WingWindow", new Color(0.36f, 0.42f, 0.46f), null, Vector2.one, 0.8f);

            // grass: a frame around the buildings (1.2 m clear of the walls) out to the horizon
            const float far = 160f;
            float gx0 = x0 - 1.2f, gz0 = z0 - 1.2f, gx1 = x1 + 1.2f, gz1 = z1 + 1.2f;
            ClearviewArt.Box("Lawn", g, new Vector3((-far + gx0) * 0.5f, -0.038f, 0f), new Vector3(gx0 + far, 0.1f, far * 2f), grass);
            ClearviewArt.Box("Lawn", g, new Vector3((gx1 + far) * 0.5f, -0.038f, 0f), new Vector3(far - gx1, 0.1f, far * 2f), grass);
            ClearviewArt.Box("Lawn", g, new Vector3((gx0 + gx1) * 0.5f, -0.038f, (-far + gz0) * 0.5f), new Vector3(gx1 - gx0, 0.1f, gz0 + far), grass);
            ClearviewArt.Box("Lawn", g, new Vector3((gx0 + gx1) * 0.5f, -0.038f, (gz1 + far) * 0.5f), new Vector3(gx1 - gx0, 0.1f, far - gz1), grass);

            // the school wall, 16 m out
            float m = 16f, wx0 = x0 - m, wz0 = z0 - m, wx1 = x1 + m, wz1 = z1 + m;
            foreach (Vector4 w in new[] { new Vector4(wx0, wz0, wx1, wz0), new Vector4(wx0, wz1, wx1, wz1), new Vector4(wx0, wz0, wx0, wz1), new Vector4(wx1, wz0, wx1, wz1) })
            {
                bool alongX = Mathf.Abs(w.y - w.w) < 0.01f;
                Vector3 c = new Vector3((w.x + w.z) * 0.5f, 1.1f, (w.y + w.w) * 0.5f);
                ClearviewArt.Box("SchoolWall", g, c, alongX ? new Vector3(w.z - w.x + 0.4f, 2.2f, 0.4f) : new Vector3(0.4f, 2.2f, w.w - w.y), ClearviewArt.Concrete);
            }

            // trees: 4-14 m out from the walls
            int trees = 0;
            for (int tries = 0; tries < 400 && trees < 46; tries++)
            {
                float px = rnd(x0 - 15f, x1 + 15f), pz = rnd(z0 - 15f, z1 + 15f);
                float dx = Mathf.Max(x0 - px, 0f, px - x1), dz = Mathf.Max(z0 - pz, 0f, pz - z1);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < 4.5f || d > 14f) continue;
                Vector3 p = new Vector3(px, 0f, pz);
                Transform t = ClearviewArt.Group("Tree", g);
                float h = rnd(2.2f, 3.6f);
                if (rng.NextDouble() < 0.3)
                {
                    // a tall, dark poplar / conifer
                    ClearviewArt.Prim(PrimitiveType.Cylinder, "Trunk", t, p + new Vector3(0f, 1f, 0f), new Vector3(0.3f, 1f, 0.3f), bark);
                    float w = rnd(1.4f, 2.2f);
                    ClearviewArt.Prim(PrimitiveType.Capsule, "Crown", t, p + new Vector3(0f, 1.5f + h, 0f), new Vector3(w, h * 1.2f, w), leaves[2]);
                }
                else
                {
                    ClearviewArt.Prim(PrimitiveType.Cylinder, "Trunk", t, p + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.42f, h * 0.5f, 0.42f), bark);
                    float r = rnd(2.6f, 4.2f);
                    Material lf = leaves[rng.Next(2)];
                    ClearviewArt.Prim(PrimitiveType.Sphere, "Crown", t, p + new Vector3(0f, h + r * 0.35f, 0f), new Vector3(r, r * 0.8f, r), lf);
                    ClearviewArt.Prim(PrimitiveType.Sphere, "Crown", t, p + new Vector3(r * 0.3f, h + r * 0.2f, rnd(-0.4f, 0.4f)), new Vector3(r * 0.7f, r * 0.6f, r * 0.7f), leaves[rng.Next(2)]);
                    ClearviewArt.Prim(PrimitiveType.Sphere, "Crown", t, p + new Vector3(-r * 0.28f, h + r * 0.45f, rnd(-0.4f, 0.4f)), new Vector3(r * 0.65f, r * 0.55f, r * 0.65f), lf);
                }
                trees++;
            }

            // other buildings, beyond the school wall (they block the horizon)
            List<Vector3> placed = new List<Vector3>();
            Vector3 centre = new Vector3((x0 + x1) * 0.5f, 0f, (z0 + z1) * 0.5f);
            for (int tries = 0; tries < 200 && placed.Count < 12; tries++)
            {
                float px = rnd(x0 - 36f, x1 + 36f), pz = rnd(z0 - 36f, z1 + 36f);
                float dx = Mathf.Max(x0 - px, 0f, px - x1), dz = Mathf.Max(z0 - pz, 0f, pz - z1);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < 24f || d > 36f) continue;
                Vector3 p = new Vector3(px, 0f, pz);
                bool clash = false;
                foreach (Vector3 q in placed) if (Vector3.Distance(p, q) < 16f) clash = true;
                if (clash) continue;
                placed.Add(p);
                float bw = rnd(9f, 18f), bd = rnd(7f, 12f), bh = rnd(7f, 15f);
                Vector3 toC = centre - p; toC.y = 0f;
                // face the school: the long side towards the centre
                float yaw = Mathf.Round(Mathf.Atan2(toC.x, toC.z) * Mathf.Rad2Deg / 90f) * 90f;
                Transform b = ClearviewArt.Group("Building", g);
                b.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
                Material fm = facades[rng.Next(facades.Length)];
                ClearviewArt.Box("Block", b, new Vector3(0f, bh * 0.5f, 0f), new Vector3(bw, bh, bd), fm);
                ClearviewArt.Box("Roof", b, new Vector3(0f, bh + 0.15f, 0f), new Vector3(bw + 0.3f, 0.3f, bd + 0.3f), ClearviewArt.Concrete);
                int cols = Mathf.Max(2, Mathf.FloorToInt(bw / 2.4f)), rows = Mathf.Max(2, Mathf.FloorToInt(bh / 3f));
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        float wx = -bw * 0.5f + (c + 0.5f) * bw / cols, wy = 1.6f + r * (bh - 1.5f) / rows;
                        bool lit = night && rng.NextDouble() < 0.18;
                        ClearviewArt.Box("Window", b, new Vector3(wx, wy, bd * 0.5f + 0.01f), new Vector3(1.2f, 1.3f, 0.03f), lit ? ClearviewArt.FarWindow : pane);
                    }
            }
        }

        /// <summary>Concrete ledges under west-wall windows (x = 0) with pigeons on them.</summary>
        static void LedgePigeons(Transform parent, float[] ledgeZ, float[] pigeonZ)
        {
            Transform g = ClearviewArt.Group("Window ledges + pigeons", parent);
            foreach (float z in ledgeZ)
                ClearviewArt.Box("Ledge", g, new Vector3(-0.2f, 0.955f, z), new Vector3(0.2f, 0.05f, 1.3f), ClearviewArt.Concrete);
            for (int i = 0; i < pigeonZ.Length; i++)
                MakePigeon(g, new Vector3(-0.21f, 0.98f, pigeonZ[i]), i % 2 == 0 ? 20f : 160f, 0.3f, new Vector3(0f, 0f, 1f));
        }

        /// <summary>
        /// Posters that change when you come close (PosterEgg): a too-perfect smile, a glitch, an inedible lunch,
        /// a mirror (your own face, in uniform), a red X, and Voss. Matched by poster name in every built scene.
        /// </summary>
        static void AddEggs(Transform root)
        {
            Dictionary<string, PosterEgg.Kind> eggs = PosterEgg.Table;
            foreach (Canvas c in root.GetComponentsInChildren<Canvas>(true))
            {
                PosterEgg.Kind k;
                if (!eggs.TryGetValue(c.name, out k) || c.GetComponent<PosterEgg>() != null) continue;
                c.gameObject.AddComponent<PosterEgg>().kind = k;
            }
        }

        /// <summary>
        /// Inspection Day: what Voss finds on the desks he checks (pencils, a horse drawing, nothing, a note that says 'help').
        /// Inactive until the director puts them on the right desks - his verdicts always match something you can see.
        /// </summary>
        static GameObject[] VerdictProps(Transform room)
        {
            Transform g = ClearviewArt.Group("Inspection_DeskFinds", room);
            Material yellow = ClearviewArt.Mat("M_CV_Pencil", new Color(0.95f, 0.75f, 0.18f), null, Vector2.one, 0.4f);
            Material eraser = ClearviewArt.Mat("M_CV_PencilEraser", new Color(0.92f, 0.55f, 0.58f), null, Vector2.one, 0.2f);
            Material paper = ClearviewArt.Mat("M_CV_Paper", ClearviewArt.OffWhite, null, Vector2.one, 0.1f);
            Material crayon = ClearviewArt.Mat("M_CV_Crayon", new Color(0.45f, 0.3f, 0.18f), null, Vector2.one, 0.1f);

            Transform pencils = ClearviewArt.Group("Find_Pencils", g);
            for (int i = 0; i < 2; i++)
            {
                Transform pc = ClearviewArt.Group("Pencil", pencils);
                pc.localPosition = new Vector3(-0.03f + i * 0.06f, 0.004f, 0f);
                pc.localRotation = Quaternion.Euler(0f, i == 0 ? 8f : -5f, 0f);
                ClearviewArt.Box("Wood", pc, Vector3.zero, new Vector3(0.008f, 0.008f, 0.16f), yellow);
                ClearviewArt.Box("Eraser", pc, new Vector3(0f, 0f, -0.088f), new Vector3(0.0085f, 0.0085f, 0.016f), eraser);
            }

            Transform horse = ClearviewArt.Group("Find_HorseDrawing", g);
            horse.localRotation = Quaternion.Euler(0f, 12f, 0f);
            ClearviewArt.Box("Paper", horse, new Vector3(0f, 0.0015f, 0f), new Vector3(0.21f, 0.003f, 0.15f), paper);
            Vector3 hy = new Vector3(0f, 0.0035f, 0f);
            ClearviewArt.Box("Body", horse, hy + new Vector3(0f, 0f, 0f), new Vector3(0.07f, 0.0015f, 0.03f), crayon);
            ClearviewArt.Box("Neck", horse, hy + new Vector3(0.042f, 0f, 0.022f), new Vector3(0.012f, 0.0015f, 0.035f), crayon, false, new Vector3(0f, 35f, 0f));
            ClearviewArt.Box("Head", horse, hy + new Vector3(0.062f, 0f, 0.04f), new Vector3(0.028f, 0.0015f, 0.013f), crayon);
            for (int i = 0; i < 4; i++)
                ClearviewArt.Box("Leg", horse, hy + new Vector3(-0.028f + i * 0.019f, 0f, -0.03f), new Vector3(0.006f, 0.0015f, 0.03f), crayon);
            ClearviewArt.Box("Tail", horse, hy + new Vector3(-0.045f, 0f, -0.004f), new Vector3(0.022f, 0.0015f, 0.006f), crayon, false, new Vector3(0f, -30f, 0f));

            Transform note = ClearviewArt.Group("Find_HelpNote", g);
            ClearviewArt.Box("Paper", note, new Vector3(0f, 0.045f, 0f), new Vector3(0.12f, 0.09f, 0.003f), paper, false, new Vector3(12f, 0f, 0f));
            ClearviewArt.Box("Prop", note, new Vector3(0f, 0.03f, 0.03f), new Vector3(0.1f, 0.06f, 0.003f), paper, false, new Vector3(-35f, 0f, 0f));   // folded back leg: it stands
            Canvas t = ClearviewArt.Poster(note, "Note", Vector3.zero, 0f, 0.1f, 0.06f, ClearviewArt.OffWhite, ClearviewArt.Ink, "help");
            t.transform.localPosition = new Vector3(0f, 0.045f, -0.0035f);
            t.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            GameObject[] props = { pencils.gameObject, horse.gameObject, null, note.gameObject };
            foreach (GameObject p in props) if (p != null) p.SetActive(false);
            return props;
        }

        static void DecorateCorridor(Transform cor)
        {
            Transform d = ClearviewArt.Group("Clearview_Decor", cor);
            ClearviewArt.Fluorescents(d, 10f, 0f, 14f, 12f, 1, 3, 0.95f, 2, true);
            Color ink = ClearviewArt.Ink;
            R(ClearviewArt.Poster(d, "Sign_NoRunning", new Vector3(12f, 1.8f, 0.1f), 180f, 1.0f, 0.5f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "NO RUNNING.\nNO WALKING SLOWLY."),
              "Between running and walking slowly there is exactly one allowed speed.\nTheirs.");
            R(ClearviewArt.Poster(d, "Poster_Smile", new Vector3(13.9f, 1.75f, 1.6f), 90f, 0.7f, 0.9f, ClearviewArt.PastelBlue, ink, "SMILE!", "You are on camera.\nYou are always on camera.", ClearviewArt.PastelYellow),
              "Always on camera. ...But a camera can only look one way at a time.");
            ClearviewArt.Poster(d, "Plaque_Classroom_Back", new Vector3(10.1f, 1.85f, 2.35f), -90f, 0.5f, 0.22f, ClearviewArt.OffWhite, ink, "CLASSROOM 1");
            ClearviewArt.Poster(d, "Plaque_Classroom_Front", new Vector3(10.1f, 1.85f, 9.65f), -90f, 0.5f, 0.22f, ClearviewArt.OffWhite, ink, "CLASSROOM 1");
            R(ClearviewArt.Poster(d, "Poster_Uniform", new Vector3(10.1f, 2.35f, 6f), -90f, 1.0f, 0.55f, ClearviewArt.PastelMint, ink, "YOUR UNIFORM\nIS YOUR FRIEND", "Wear it. Be it.", ClearviewArt.PastelPink),
              "Someone crossed out \"Be it\" in pencil and wrote: \"Hide in it.\"");
            ClearviewArt.DecoCamera(d, new Vector3(13.6f, 2.92f, 0.4f), -25f);
            ClearviewArt.Speaker(d, new Vector3(12f, 2.62f, 0.1f), 180f);
        }

        /// <summary>A closed door slab in a doorway (x, z = centre of the 2 m door module).</summary>
        static Canvas ClosedDoor(Transform parent, Vector3 centre, bool alongZ, string sign, float signYaw)
        {
            GameObject g = ClearviewArt.Box("Door (closed)", parent, Vector3.zero, alongZ ? new Vector3(0.07f, 2.5f, 1.25f) : new Vector3(1.25f, 2.5f, 0.07f), ClearviewArt.DoorLeaf, true);
            g.transform.position = centre + new Vector3(0f, 1.25f, 0f);
            ClearviewArt.Box("Handle", g.transform, new Vector3(alongZ ? 0.8f : -0.4f, -0.05f, alongZ ? -0.4f : 0.8f), new Vector3(0.6f, 0.02f, 0.06f), ClearviewArt.Steel);
            if (!string.IsNullOrEmpty(sign))
            {
                Quaternion r = Quaternion.Euler(0f, signYaw, 0f);
                return ClearviewArt.Poster(parent, "DoorSign", centre + new Vector3(0f, 1.75f, 0f) - r * Vector3.forward * 0.04f, signYaw, 0.55f, 0.28f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, sign);
            }
            return null;
        }

        /// <summary>
        /// Makes a poster readable: walk up to it and look, and the player reads it (a thought, a Search word, or the
        /// poster changing). Every printed thing in the school should mean something: a clue, or a joke about Clearview.
        /// </summary>
        static Readable R(Canvas c, string reaction, string keyword = null, string altTitle = null, string altBody = null)
        {
            if (c == null) return null;
            Readable r = c.gameObject.AddComponent<Readable>();
            r.reaction = reaction;
            r.keyword = keyword;
            r.altTitle = altTitle;
            r.altBody = altBody;
            return r;
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
                // the sign hangs ON the back door (the CLASSROOM 1 plaque beside the door stays clear)
                R(ClosedDoor(classroom, new Vector3(10f, 0f, 1f), true, "IN SESSION\nDO NOT KNOCK", -90f), "In session. During lunch. ...Who is in there?");
                ClosedDoor(classroom, new Vector3(10f, 0f, 11f), true, null, 0f);
            }
            if (corridor != null)
            {
                DecorateCorridor(corridor);
                Transform d = corridor.Find("Clearview_Decor");
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
            R(ClearviewArt.Poster(hinge, "Sign_StaffOnly", new Vector3(17f, 1.75f, 11.97f), 0f, 0.6f, 0.3f, ClearviewArt.DeepRed, ClearviewArt.OffWhite, "STAFF ONLY"),
              "STAFF ONLY. The handle is worn shiny.\nMore hands than there are staff.");
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
            R(ClearviewArt.Poster(dd, "Banner_DaysSinceIncident", new Vector3(22f, 2.55f, 11.9f), 0f, 4.2f, 0.45f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "DAYS SINCE LAST INCIDENT:  0"),
              "The 0 isn't printed. It's painted. Over something else. Many times.");
            R(ClearviewArt.Poster(dd, "Poster_Chew", new Vector3(25.6f, 1.75f, 11.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelPink, ClearviewArt.Ink, "CHEW 32 TIMES", "Smile between bites.", ClearviewArt.PastelMint),
              "Thirty-two. Somewhere by the windows a girl is counting under her breath.\nNo - humming.");
            R(ClearviewArt.Poster(dd, "Poster_Menu", new Vector3(20f, 1.75f, 11.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "TODAY'S MENU", "NUTRITION\n(beige)\n\nDessert: cancelled\nfor your safety", ClearviewArt.PastelBlue),
              "At the very bottom, three times: SERVED AT 12:40. 12:40. 12:40.");
            R(ClearviewArt.Poster(dd, "Poster_Privilege", new Vector3(14.1f, 1.8f, 8.5f), -90f, 0.8f, 0.9f, ClearviewArt.PastelMint, ClearviewArt.Ink, "LUNCH IS A\nPRIVILEGE", "Enjoy it correctly.", ClearviewArt.PastelYellow),
              "\"Enjoy it correctly.\" There are diagrams on the back.\nI don't turn it over.");
            R(ClearviewArt.Poster(dd, "Poster_Windows", new Vector3(29.9f, 1.8f, 11f), 90f, 0.7f, 0.8f, ClearviewArt.PastelBlue, ClearviewArt.Ink, "THE WINDOWS\nARE FOR LIGHT", "Not for looking.", ClearviewArt.PastelPink),
              "Not for looking. ...The camera in the corner keeps looking at them, though.\nEvery few seconds it turns its back on the counters.");
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

            Backdrop(level.transform, -0.5f, -0.5f, 30.5f, 18f, 22, false);
            AddEggs(level.transform);
            ClearviewArt.NoCoplanarFaces(level.transform);
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
            R(ClearviewArt.Poster(w, "Sign_NotHere", new Vector3(17.05f, bottom + 2.45f, zEnd), 0f, 1.0f, 0.22f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "THESE STAIRS DO NOT EXIST"),
              "In pencil, underneath: \"then who keeps walking down them?\"");
            R(ClearviewArt.Poster(w, "Chalk_Triangle", new Vector3(x1 - 0.05f, bottom + 1.4f, 16.9f), 90f, 0.35f, 0.35f, new Color(0.5f, 0.52f, 0.5f), new Color(0.95f, 0.95f, 0.9f), "/\\"),
              "A chalk triangle. The same as on the chair in front of mine.");
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
            R(ClearviewArt.Poster(d, "Sign_LightsOut", new Vector3(9.9f, 2.3f, 3f), 90f, 0.9f, 0.24f, ClearviewArt.DeepRed, Color.white, "LIGHTS OUT  21:00", null, null, 0.7f),
              "21:00. Twenty-one hundred. The Photos code - they made us learn it by heart.");
            Light sg = new GameObject("Sign glow").AddComponent<Light>();
            sg.transform.SetParent(d, false);
            sg.transform.position = new Vector3(9.6f, 2.3f, 3f);
            sg.type = LightType.Point; sg.range = 2.5f; sg.intensity = 0.5f; sg.color = new Color(1f, 0.2f, 0.15f);
            R(ClearviewArt.Poster(d, "Plaque_Room304", new Vector3(8.1f, 1.9f, 1.55f), -90f, 0.45f, 0.2f, ClearviewArt.OffWhite, ClearviewArt.Ink, "ROOM 304", null, null, 0.45f),
              "Room 304. Three-oh-four. The room number, the way he counts it.");
            R(ClearviewArt.Poster(d, "Poster_Sleep", new Vector3(4f, 1.75f, 9.9f), 0f, 0.8f, 0.9f, ClearviewArt.PastelBlue, ClearviewArt.Ink, "SLEEP IS\nMANDATORY", "Dreams are optional.\nPlease dream quietly.", ClearviewArt.PastelPink, 0.22f),
              "\"Please dream quietly.\" Someone in this room talks in their sleep.\nMostly numbers.");
            R(ClearviewArt.Poster(d, "Poster_Counting", new Vector3(4f, 1.75f, 0.1f), 180f, 0.8f, 0.9f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "COUNT SHEEP,\nNOT DAYS", null, ClearviewArt.PastelMint, 0.22f),
              "Someone counted days anyway: tiny marks all along the edge.\nHundreds of them. Different pens.");
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
            GameObject sn = Watcher("SNEAKING_STUDENT (the hummer)", root, new Vector3(9f, 0f, 11.2f), 180f, ClearviewArt.Uniform, false, true, true);
            foreach (Light l in sn.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            Transform snLook = sn.transform.Find("Chibi");
            if (snLook != null) ClearviewArt.Bow(snLook, new Vector3(0f, 1.845f, -0.08f), 0.11f, ClearviewArt.Ribbon);
            PatrolAI sneaker = sn.AddComponent<PatrolAI>();
            sneaker.roamOnBegin = false;
            sneaker.scanAngle = 15f;
            sn.GetComponent<NavMeshAgent>().speed = 1.4f;
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

            Backdrop(root, -11.5f, -4.5f, 10.5f, 14.5f, 33, true);
            AddEggs(root);
            ClearviewArt.NoCoplanarFaces(root);
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

        // ------------------------------------------------------------------ Chapter 4: Picture Day (new white box)
        /// <summary>
        /// The gymnasium in the Cubberley Wing (x 0..16, z 0..12). Class-photo risers along the north wall (three tiers,
        /// the back row standing), the photographer's tripod in the middle, two prefects walking the floor.
        /// Things to photograph: the report board (east wall), the "LIVE" screen (west wall), the 1967 photo in the
        /// trophy case (south wall). Your spot: back row, window end, a "31" sticker on the wall behind it.
        /// </summary>
        static void BuildPictureDay()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject("LEVEL04_PictureDay").transform;
            Transform world = ClearviewArt.Group("World (NavMesh)", root);
            Place("Ground_60x40", world, Vector3.zero, 0f);
            Transform hall = ClearviewArt.Group("Gymnasium_CubberleyWing", world);
            Floors(hall, 0f, 0f, 8, 6, true);
            Transform hw = ClearviewArt.Group("Walls", hall);
            Run(hw, 0f, 0f, false, W, W, W, D, W, W, W, W);        // south: the door (x 6..8)
            Run(hw, 0f, 12f, false, W, W, W, W, W, W, W, W);       // north: behind the risers
            Run(hw, 0f, 0f, true, W, W, N, N, N, W);               // west: windows (z 4..10)
            Run(hw, 16f, 0f, true, W, W, W, W, W, W);              // east
            foreach (Vector2 g in new[] { new Vector2(0f, 0f), new Vector2(16f, 0f), new Vector2(0f, 12f), new Vector2(16f, 12f) }) Place("Glue_Corner", hw, new Vector3(g.x, 0f, g.y), 0f);
            ClearviewArt.Restyle(root);
            ClosedDoor(hall, new Vector3(7f, 0f, 0f), false, null, 0f);

            Light sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(42f, 80f, 0f);
            ClearviewArt.DayLighting(sun);
            ClearviewArt.PostVolume(root, false);

            // --- the risers: three tiers. Nobody sits on them until the countdown: then everyone stands in their place
            Transform risers = ClearviewArt.Group("Risers (class photo)", hall);
            float[] tierZ = { 9.0f, 9.8f, 10.6f };
            float[] tierH = { 0.2f, 0.4f, 0.6f };
            for (int t = 0; t < 3; t++)
                ClearviewArt.Box("Tier_" + (t + 1), risers, new Vector3(8f, tierH[t] * 0.5f, tierZ[t]), new Vector3(12f, tierH[t], 0.8f), ClearviewArt.Wood, true);
            ClearviewArt.Box("Tier_Back", risers, new Vector3(8f, 0.3f, 11.5f), new Vector3(11.98f, 0.594f, 1.0f), ClearviewArt.Wood, true);
            // places: 5 per row (the back row's window end is yours)
            List<Transform> places = new List<Transform>();
            Transform placeRoot = ClearviewArt.Group("Photo places", hall);
            float[] xs = { 4.3f, 6.2f, 8.1f, 10.0f, 11.9f };
            for (int t = 0; t < 3; t++)
                for (int i = 0; i < xs.Length; i++)
                {
                    Transform pl = new GameObject("Place_" + (t + 1) + "_" + (i + 1)).transform;
                    pl.SetParent(placeRoot, false);
                    pl.SetPositionAndRotation(new Vector3(xs[i] + (t == 1 ? 0.45f : 0f), tierH[t], tierZ[t]), Quaternion.Euler(0f, 180f, 0f));
                    places.Add(pl);
                }
            int walkerCount = places.Count;   // 15 students; the 16th place (back row, window end) starts as yours
            Transform first = new GameObject("Place_3_0 (window end)").transform;
            first.SetParent(placeRoot, false);
            first.SetPositionAndRotation(new Vector3(2.9f, tierH[2], tierZ[2]), Quaternion.Euler(0f, 180f, 0f));
            places.Add(first);
            // your spot: marked with a yellow X - the photographer moves it before every photo
            Transform spot = new GameObject("PlayerSpot").transform;
            spot.SetParent(hall, false);
            spot.position = first.position;
            Transform marker = ClearviewArt.Group("SpotMarker (yellow X)", hall);
            marker.position = first.position;
            Material tape = ClearviewArt.Mat("M_CV_TapeX", new Color(1f, 0.85f, 0.2f), null, Vector2.one, 0.3f, 0f, new Color(0.9f, 0.7f, 0.1f));
            ClearviewArt.Box("Tape_X_1", marker, new Vector3(0f, 0.002f, 0f), new Vector3(0.55f, 0.003f, 0.07f), tape, false, new Vector3(0f, 45f, 0f));
            ClearviewArt.Box("Tape_X_2", marker, new Vector3(0f, 0.0055f, 0f), new Vector3(0.55f, 0.003f, 0.07f), tape, false, new Vector3(0f, -45f, 0f));
            // the prefects stand at both ends during the photo, watching the rows
            Transform[] prefectSpots = new Transform[2];
            Vector3[] psp = { new Vector3(1.4f, 0f, 9.4f), new Vector3(14.6f, 0f, 9.4f) };
            float[] pyaw = { 90f, -90f };
            for (int i = 0; i < 2; i++)
            {
                prefectSpots[i] = new GameObject("PrefectPhotoSpot_" + i).transform;
                prefectSpots[i].SetParent(placeRoot, false);
                prefectSpots[i].SetPositionAndRotation(psp[i], Quaternion.Euler(0f, pyaw[i], 0f));
            }
            Transform d = ClearviewArt.Group("Clearview_Decor", root);
            R(ClearviewArt.Poster(d, "Sticker_31", new Vector3(3.0f, 1.75f, 11.9f), 0f, 0.3f, 0.3f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "31"),
              "A sticker on the wall: 31. The yellow X on the risers moves before every photo.\nThe 31 doesn't.");

            // --- the photographer's corner: tripod, two umbrella lights, the big flash
            Transform rig0 = ClearviewArt.Group("Photographer", hall);
            Vector3 cam = new Vector3(8f, 0f, 5.2f);
            foreach (float a in new[] { 0f, 120f, 240f })
            {
                Quaternion q = Quaternion.Euler(0f, a, 0f);
                Rod(rig0, "TripodLeg", cam + new Vector3(0f, 1.3f, 0f), cam + q * new Vector3(0f, 0f, 0.35f), 0.025f, ClearviewArt.Black);
            }
            ClearviewArt.Box("Camera_Body", rig0, cam + new Vector3(0f, 1.4f, 0f), new Vector3(0.28f, 0.22f, 0.3f), ClearviewArt.Black, true);
            ClearviewArt.Box("Camera_Lens", rig0, cam + new Vector3(0f, 1.4f, 0.22f), new Vector3(0.14f, 0.14f, 0.16f), ClearviewArt.CamBody);
            Material umb = ClearviewArt.Mat("M_CV_Umbrella", new Color(0.95f, 0.95f, 0.93f), null, Vector2.one, 0.2f, 0f, new Color(0.6f, 0.6f, 0.58f));
            foreach (float x in new[] { 5.2f, 10.8f })
            {
                Vector3 b = new Vector3(x, 0f, 6.0f);
                Rod(rig0, "LightStand", b, b + new Vector3(0f, 2.1f, 0f), 0.03f, ClearviewArt.Steel);
                ClearviewArt.Prim(PrimitiveType.Cylinder, "Umbrella", rig0, b + new Vector3(0f, 2.2f, 0f), new Vector3(0.9f, 0.01f, 0.9f), umb, new Vector3(-60f, 0f, 0f));
                Light l = new GameObject("Umbrella light").AddComponent<Light>();
                l.transform.SetParent(rig0, false);
                l.transform.position = b + new Vector3(0f, 2.1f, 0.3f);
                l.type = LightType.Point; l.range = 6f; l.intensity = 1.1f; l.color = new Color(1f, 0.97f, 0.92f);
            }
            Light flash = new GameObject("PhotoFlash").AddComponent<Light>();
            flash.transform.SetParent(rig0, false);
            flash.transform.position = cam + new Vector3(0f, 2.4f, 0.4f);
            flash.type = LightType.Point; flash.range = 16f; flash.intensity = 8f; flash.color = Color.white;
            flash.enabled = false;
            Transform camSpot = new GameObject("CameraSpot (photographer)").transform;
            camSpot.SetParent(hall, false);
            camSpot.position = new Vector3(8f, 0f, 4.55f);

            // --- 1. the report board (east wall)
            Canvas board = ClearviewArt.Poster(d, "Board_Reports", new Vector3(15.9f, 1.7f, 6.5f), 90f, 1.4f, 1.0f, new Color(0.72f, 0.58f, 0.4f), ClearviewArt.Ink,
                "THIS WEEK", "REPORTS FILED: 20\nREPORTERS ASSIGNED: 3\n\nThank you for volunteering.", ClearviewArt.DeepRed);
            R(board, "THIS WEEK. Reports filed: 20. Reporters assigned: 3.\n\"Thank you for volunteering.\"");
            Target(board.gameObject, "board", "The report board: 20 reports, 3 reporters", 10f);
            // --- 2. the LIVE screen (west wall, by the door)
            Transform tv = ClearviewArt.Group("LiveScreen", hall);
            ClearviewArt.Box("Cart", tv, new Vector3(0.55f, 0.45f, 2.6f), new Vector3(0.5f, 0.9f, 0.9f), ClearviewArt.Metal, true);
            ClearviewArt.Box("TV", tv, new Vector3(0.6f, 1.2f, 2.6f), new Vector3(0.42f, 0.56f, 0.72f), ClearviewArt.Black, true);
            Material snow = ClearviewArt.Mat("M_CV_TVSnow", new Color(0.5f, 0.5f, 0.52f), null, Vector2.one, 0.6f, 0f, new Color(1.2f, 1.25f, 1.3f));
            GameObject scr = ClearviewArt.Box("Screen (snow)", tv, new Vector3(0.82f, 1.22f, 2.6f), new Vector3(0.02f, 0.42f, 0.6f), snow);
            TVStatic st = scr.AddComponent<TVStatic>();
            Light tvGlow = new GameObject("Screen glow").AddComponent<Light>();
            tvGlow.transform.SetParent(tv, false);
            tvGlow.transform.position = new Vector3(1.2f, 1.25f, 2.6f);
            tvGlow.type = LightType.Point; tvGlow.range = 2.5f; tvGlow.intensity = 0.7f; tvGlow.color = new Color(0.8f, 0.85f, 1f);
            st.glow = tvGlow;
            Target(scr, "screen", "The LIVE screen. Nobody on it.", 9f);
            R(ClearviewArt.Poster(d, "Sign_LiveAddress", new Vector3(0.1f, 2.05f, 2.6f), -90f, 0.9f, 0.32f, ClearviewArt.DeepRed, ClearviewArt.OffWhite, "FOUNDER'S ADDRESS\nLIVE  10:45"),
              "LIVE at 10:45: the Founder will speak to us.\nThe screen under it is only snow.");
            // --- 3. the trophy case and the 1967 photo (south wall)
            Transform tc = ClearviewArt.Group("TrophyCase", hall);
            ClearviewArt.Box("Case", tc, new Vector3(12.5f, 1.0f, 0.3f), new Vector3(1.8f, 2.0f, 0.45f), ClearviewArt.Wood, true);
            ClearviewArt.Box("Case_Inside", tc, new Vector3(12.5f, 1.15f, 0.532f), new Vector3(1.6f, 1.5f, 0.01f), ClearviewArt.Board);
            Material gold = ClearviewArt.Mat("M_CV_Trophy", new Color(0.86f, 0.7f, 0.3f), null, Vector2.one, 0.8f, 0.8f);
            for (int i = 0; i < 4; i++)
                ClearviewArt.Prim(PrimitiveType.Cylinder, "Trophy", tc, new Vector3(11.85f + i * 0.43f, 0.55f, 0.6f), new Vector3(0.12f, 0.12f + (i % 2) * 0.05f, 0.12f), gold);
            Canvas oldPhoto = ClearviewArt.Poster(tc, "Photo_1967", new Vector3(12.5f, 1.45f, 0.545f), 180f, 0.8f, 0.6f, new Color(0.78f, 0.68f, 0.52f), new Color(0.25f, 0.18f, 0.12f),
                "CUBBERLEY WING\nAPRIL 1967", "\"Strength through discipline.\"");
            R(oldPhoto, "CUBBERLEY WING, APRIL 1967. Everyone in the photo raises the same hand,\ncupped, at the shoulder. A salute nobody has taught us. Yet.");
            Target(oldPhoto.gameObject, "trophy", "The 1967 photo: the Cubberley Wing opens", 8f);

            // --- the walls talk
            R(ClearviewArt.Poster(d, "Banner_PictureDay", new Vector3(8f, 2.7f, 11.9f), 0f, 5f, 0.45f, ClearviewArt.PastelYellow, ClearviewArt.DeepRed, "PICTURE DAY  -  10:45 SHARP"),
              "10:45. When they photograph us.\nTen-forty-five, written like a time and read like a code.");
            R(ClearviewArt.Poster(d, "Banner_Discipline", new Vector3(15.9f, 2.55f, 3f), 90f, 3f, 0.38f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "STRENGTH THROUGH DISCIPLINE"),
              "Monday's banner said WHAT IS SEEN, IS SAFE.\nSomeone printed a new one overnight.");
            R(ClearviewArt.Poster(d, "Banner_Community", new Vector3(0.1f, 2.55f, 11f), -90f, 1.8f, 0.5f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "STRENGTH THROUGH\nCOMMUNITY"),
              "...ACTION. It said COMMUNITY a second ago.\nDidn't it?", null, "STRENGTH THROUGH\nACTION");
            R(ClearviewArt.Poster(d, "Poster_WholeFace", new Vector3(0.1f, 1.7f, 1.2f), -90f, 0.6f, 0.8f, ClearviewArt.PastelPink, ClearviewArt.Ink, "SMILE WITH\nYOUR WHOLE\nFACE", "Students who do not smile\nwill be photographed again.", ClearviewArt.PastelMint),
              "Small print: \"...and again. And again.\"");
            R(ClearviewArt.Poster(d, "Poster_CitizenCard", new Vector3(15.9f, 1.7f, 9.3f), 90f, 0.8f, 1.0f, ClearviewArt.PastelMint, ClearviewArt.Ink, "YOUR PHOTO GOES ON\nYOUR CITIZEN CARD", "Carry it. Show it. Be it.", ClearviewArt.PastelYellow),
              "Citizen cards. On Monday we were students.\nSome cards will have a red X. Nobody says what it means.");
            R(ClearviewArt.Poster(d, "Sign_Gymnasium", new Vector3(7f, 2.75f, 0.1f), 180f, 1.8f, 0.24f, ClearviewArt.OffWhite, ClearviewArt.Ink, "GYMNASIUM  -  CUBBERLEY WING"),
              "Cubberley Wing. The same name is on the trophy case.\nNobody here knows who Cubberley was. Nobody asks.");
            ClearviewArt.Fluorescents(d, 0.3f, 0.3f, 15.7f, 11.7f, 4, 3, 1.0f, 6);
            ClearviewArt.Clock(d, new Vector3(15.9f, 2.45f, 11.2f), 90f, 10.25f);
            ClearviewArt.Speaker(d, new Vector3(4f, 2.62f, 0.1f), 180f);
            ClearviewArt.Speaker(d, new Vector3(15.9f, 2.62f, 8.1f), 90f);
            ClearviewArt.DecoCamera(d, new Vector3(15.6f, 2.92f, 0.4f), -40f);

            NavMeshSurface surface = MakeSurface(world.gameObject);

            // --- the photographer and the prefects
            GameObject ph = Watcher("PHOTOGRAPHER", root, camSpot.position, 0f, ClearviewArt.Mat("M_CV_PhotographerVest", new Color(0.3f, 0.32f, 0.3f), null, Vector2.one, 0.2f), false, false);
            foreach (Light l in ph.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            PatrolAI photographer = ph.AddComponent<PatrolAI>();
            photographer.roamOnBegin = false;
            photographer.scanAngle = 25f;
            photographer.scanSpeed = 0.35f;
            TeacherVision pv = ph.AddComponent<TeacherVision>();
            pv.viewAngle = 70f; pv.viewRange = 11f; pv.eyeHeight = 1.6f;
            ph.AddComponent<Footsteps>().volume = 0.35f;

            Material band = ClearviewArt.Mat("M_CV_Armband", new Color(0.78f, 0.1f, 0.08f), null, Vector2.one, 0.3f);
            List<PatrolAI> prefects = new List<PatrolAI>();
            List<TeacherVision> pvis = new List<TeacherVision>();
            Vector3[] starts = { new Vector3(3f, 0f, 4f), new Vector3(13f, 0f, 6f) };
            Vector4[] rects = { new Vector4(1.2f, 1.2f, 9f, 8f), new Vector4(7f, 1.2f, 15f, 8f) };
            for (int i = 0; i < 2; i++)
            {
                GameObject pf = Watcher("PREFECT_" + (i == 0 ? "A" : "B"), root, starts[i], i == 0 ? 90f : -90f, ClearviewArt.Uniform, false, i == 1, true);
                foreach (Light l in pf.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
                ClearviewArt.Box("Armband", pf.transform, new Vector3(-0.26f, 1.22f, 0f), new Vector3(0.085f, 0.08f, 0.135f), band);
                ClearviewArt.Box("Clipboard", pf.transform, new Vector3(0.24f, 0.95f, 0.12f), new Vector3(0.03f, 0.3f, 0.22f), ClearviewArt.Wood);
                PatrolAI pa = pf.AddComponent<PatrolAI>();
                pa.roamRect = rects[i];
                pa.waitTime = new Vector2(2.5f, 5f);
                TeacherVision v = pf.AddComponent<TeacherVision>();
                v.viewAngle = 90f; v.viewRange = 6f; v.eyeHeight = 1.6f;
                Footsteps fs = pf.AddComponent<Footsteps>();
                fs.volume = 0.4f; fs.pitch = 1.1f;
                pf.GetComponent<NavMeshAgent>().speed = 1.0f;
                prefects.Add(pa);
                pvis.Add(v);
            }

            // --- the class: milling about the gym until the countdown
            Transform people = ClearviewArt.Group("Class (walking about)", root);
            List<PatrolAI> walkers = new List<PatrolAI>();
            for (int i = 0; i < walkerCount; i++)
            {
                float a = i * 2.39f;
                Vector3 start = new Vector3(2.5f + (i * 3.7f) % 11.5f, 0f, 1.6f + (i * 2.3f) % 6f);
                GameObject w = Watcher("Student_" + (i + 1), people, start, a * 57f, ClearviewArt.Uniform, false, i % 2 == 1, true);
                foreach (Light l in w.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
                w.transform.localScale = new Vector3(0.95f, 0.93f, 0.95f);
                PatrolAI pa = w.AddComponent<PatrolAI>();
                pa.roamRect = new Vector4(1.2f, 1.2f, 14.8f, 8.0f);
                pa.waitTime = new Vector2(1.5f, 6f);
                pa.scanAngle = 35f;
                pa.scanSpeed = 0.5f;
                NavMeshAgent ag = w.GetComponent<NavMeshAgent>();
                ag.speed = 1.25f;
                ag.radius = 0.25f;
                ag.avoidancePriority = 30 + i;
                Footsteps fs = w.AddComponent<Footsteps>();
                fs.volume = 0.12f; fs.pitch = 1.1f + (i % 3) * 0.05f;
                walkers.Add(pa);
            }

            // --- player: just inside the door
            PhoneController phone;
            Transform exposure;
            PlayerRig rigP = MakeRig(new Vector3(7f, 0f, 1.1f), 0f, out phone, out exposure);
            PictureDayDirector dir = rigP.gameObject.AddComponent<PictureDayDirector>();
            dir.rig = rigP;
            dir.phone = phone;
            dir.navSurface = surface;
            dir.callSeconds = 10f;   // the class-photo countdown (the time between photos is unchanged)
            dir.phoneExposurePoint = exposure;
            dir.photographer = photographer;
            dir.photographerVision = pv;
            dir.prefects = prefects.ToArray();
            dir.prefectVisions = pvis.ToArray();
            dir.cameraSpot = camSpot;
            dir.playerSpot = spot;
            dir.walkers = walkers.ToArray();
            dir.walkerSpots = places.ToArray();
            dir.spotMarker = marker;
            dir.prefectPhotoSpots = prefectSpots;
            dir.photoFlash = flash;
            LedgePigeons(hall, new[] { 5f, 7f, 9f }, new[] { 5.2f, 8.7f });
            Backdrop(root, -0.5f, -0.5f, 16.5f, 12.5f, 44, false);
            AddEggs(root);
            ClearviewArt.NoCoplanarFaces(root);
            EditorSceneManager.SaveScene(s, L4Path);
        }

        static void Target(GameObject g, string id, string label, float range)
        {
            PhotoTarget t = g.AddComponent<PhotoTarget>();
            t.id = id;
            t.label = label;
            t.range = range;
        }

        /// <summary>A standing student for the back row (the kit's standing body in the grey uniform). Not a walker.</summary>
        static GameObject StandingStudent(Transform parent, Vector3 pos, float yaw, bool girl, bool prefect)
        {
            GameObject g = Place("Fig_Teacher", parent, pos, yaw);
            if (PrefabUtility.IsPartOfPrefabInstance(g)) PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            g.name = prefect ? "Student_Standing (prefect)" : "Student_Standing";
            TeacherPatrol tp = g.GetComponent<TeacherPatrol>();
            if (tp != null) Object.DestroyImmediate(tp);
            Transform fan = g.transform.Find("VisionFan");
            if (fan != null) Object.DestroyImmediate(fan.gameObject);
            foreach (Light l in g.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = r.gameObject.name == "Eyes" ? ClearviewArt.EyeBand : ClearviewArt.Uniform;
            g.transform.localScale = new Vector3(0.95f, 0.93f, 0.95f);   // a little smaller than the adults
            ClearviewArt.Standing(g.transform, false, girl, true);
            if (prefect) ClearviewArt.Box("Armband", g.transform, new Vector3(-0.26f, 1.22f, 0f), new Vector3(0.085f, 0.08f, 0.135f), ClearviewArt.Mat("M_CV_Armband", new Color(0.78f, 0.1f, 0.08f), null, Vector2.one, 0.3f));
            return g;
        }

        // ------------------------------------------------------------------ Chapter 5: The Archive (new white box)
        /// <summary>
        /// The library in the Cubberley Wing (x 0..14, z 0..12). Tall shelves in four rows along Z (cover), a wide
        /// middle aisle the librarian watches from her desk, the returns cart by the door (the oldest book), and the
        /// restricted corner in the north-east behind a rope: the OLD BOOKS AWAITING REPAIR, among them Records, vol. 31.
        /// </summary>
        static void BuildArchive()
        {
            Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform root = new GameObject("LEVEL05_Archive").transform;
            Transform world = ClearviewArt.Group("World (NavMesh)", root);
            Place("Ground_60x40", world, Vector3.zero, 0f);
            Transform lib = ClearviewArt.Group("Library_CubberleyWing", world);
            Floors(lib, 0f, 0f, 7, 6, true);
            Transform lw = ClearviewArt.Group("Walls", lib);
            Run(lw, 0f, 0f, false, W, D, W, W, W, W, W);            // south: the door (x 2..4)
            Run(lw, 0f, 12f, false, W, W, W, W, W, W, W);           // north
            Run(lw, 0f, 0f, true, W, N, N, N, N, W);                // west: windows (z 2..10)
            Run(lw, 14f, 0f, true, W, W, W, W, W, W);               // east
            foreach (Vector2 g in new[] { new Vector2(0f, 0f), new Vector2(14f, 0f), new Vector2(0f, 12f), new Vector2(14f, 12f) }) Place("Glue_Corner", lw, new Vector3(g.x, 0f, g.y), 0f);
            ClearviewArt.Restyle(root);
            ClosedDoor(lib, new Vector3(3f, 0f, 0f), false, null, 0f);

            Light sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(30f, 100f, 0f);   // late afternoon through the west windows
            ClearviewArt.DayLighting(sun);
            sun.color = new Color(1f, 0.86f, 0.68f);
            ClearviewArt.PostVolume(root, false);

            // --- tall shelves (2.1 m: you can't be seen over them)
            Transform shelves = ClearviewArt.Group("Shelves (cover)", lib);
            Material[] books = { ClearviewArt.BookA, ClearviewArt.BookB, ClearviewArt.BookC };
            foreach (float x in new[] { 2.2f, 4.0f, 10.0f, 11.8f })
            {
                Transform sh = ClearviewArt.Group("Shelf", shelves);
                ClearviewArt.Box("Frame", sh, new Vector3(x, 1.05f, 6f), new Vector3(0.5f, 2.1f, 5.2f), ClearviewArt.Wood, true);
                for (int row = 0; row < 5; row++)
                    for (int side = -1; side <= 1; side += 2)
                        for (int k = 0; k < 6; k++)
                        {
                            float z = 3.75f + k * 0.85f;
                            int v = Mathf.Abs((int)(x * 10f) + row * 7 + k * 3 + side) % 3;
                            ClearviewArt.Box("Books", sh, new Vector3(x + side * 0.255f, 0.28f + row * 0.4f, z), new Vector3(0.012f, 0.26f, 0.7f), books[v]);
                        }
            }
            // shelf-end labels (south ends, z 3.4)
            Transform d = ClearviewArt.Group("Clearview_Decor", root);
            R(ClearviewArt.Poster(d, "Label_History", new Vector3(2.2f, 1.95f, 3.4f), 0f, 0.48f, 0.18f, ClearviewArt.OffWhite, ClearviewArt.Ink, "HISTORY (SELECTED)"),
              "The history section is one shelf long.\nHalf of it is the Charter.");
            R(ClearviewArt.Poster(d, "Label_SelfHelp", new Vector3(4.0f, 1.95f, 3.4f), 0f, 0.48f, 0.18f, ClearviewArt.OffWhite, ClearviewArt.Ink, "SELF-HELP"),
              "\"How To Be Seen.\" \"Being Seen: A Workbook.\" \"Seen And Safe.\"\nThe whole shelf is the same book.");
            R(ClearviewArt.Poster(d, "Label_Fiction", new Vector3(10.0f, 1.95f, 3.4f), 0f, 0.48f, 0.18f, ClearviewArt.OffWhite, ClearviewArt.Ink, "FICTION (APPROVED)"),
              "Every approved novel ends with someone being grateful.");
            R(ClearviewArt.Poster(d, "Label_Reference", new Vector3(11.8f, 1.95f, 3.4f), 0f, 0.48f, 0.18f, ClearviewArt.OffWhite, ClearviewArt.Ink, "REFERENCE"),
              "The dictionary jumps from HIDE to HIDING PLACE.\nThe page in between has been cut out.");

            // --- the librarian's desk (south, facing up the middle aisle)
            Transform desk = ClearviewArt.Group("LibrarianDesk", lib);
            ClearviewArt.Box("Desk", desk, new Vector3(7f, 0.38f, 2.45f), new Vector3(2.2f, 0.76f, 0.7f), ClearviewArt.Wood, true);
            ClearviewArt.Box("Stamp", desk, new Vector3(6.6f, 0.8f, 2.4f), new Vector3(0.08f, 0.08f, 0.08f), ClearviewArt.Black);
            ClearviewArt.Box("Lamp", desk, new Vector3(7.7f, 0.95f, 2.5f), new Vector3(0.12f, 0.35f, 0.12f), ClearviewArt.Steel);
            Transform deskSpot = new GameObject("DeskSpot (librarian)").transform;
            deskSpot.SetParent(lib, false);
            deskSpot.position = new Vector3(7f, 0f, 1.5f);   // behind the desk, clear of it (she never stands on it)

            // --- the returns cart and the oldest book
            Transform cart = ClearviewArt.Group("ReturnsCart", lib);
            ClearviewArt.Box("Cart", cart, new Vector3(0.75f, 0.42f, 1.4f), new Vector3(0.5f, 0.84f, 0.9f), ClearviewArt.Metal, true);
            for (int i = 0; i < 4; i++)
                ClearviewArt.Box("Book", cart, new Vector3(0.75f, 0.9f, 1.05f + i * 0.2f), new Vector3(0.28f, 0.1f + (i % 2) * 0.04f, 0.16f), books[i % 3]);
            GameObject oldest = ClearviewArt.Box("OldestBook (never returned)", cart, new Vector3(0.75f, 0.92f, 1.82f), new Vector3(0.3f, 0.14f, 0.2f), ClearviewArt.Mat("M_CV_OldBook", new Color(0.36f, 0.27f, 0.18f), null, Vector2.one, 0.1f), true);
            oldest.GetComponent<Collider>().isTrigger = true;
            GameObject cartUse = new GameObject("Cart_Use");
            cartUse.transform.SetParent(cart, false);
            cartUse.transform.position = new Vector3(0.75f, 1.0f, 1.4f);
            BoxCollider cuc = cartUse.AddComponent<BoxCollider>();
            cuc.size = new Vector3(0.6f, 0.4f, 1.0f);
            cuc.isTrigger = true;
            Interactable cartIt = cartUse.AddComponent<Interactable>();
            R(ClearviewArt.Poster(d, "Sign_Returns", new Vector3(0.1f, 1.6f, 0.9f), -90f, 0.5f, 0.3f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "RETURNS", "All books are returned\neventually."),
              "\"All books are returned eventually.\" One of them never has been.");

            // --- the restricted archive (north-east, behind the rope)
            Transform arc = ClearviewArt.Group("Archive (restricted)", lib);
            Material rope = ClearviewArt.Mat("M_CV_Rope", new Color(0.55f, 0.08f, 0.07f), null, Vector2.one, 0.3f);
            foreach (Vector3 p in new[] { new Vector3(9.8f, 0f, 9.7f), new Vector3(9.8f, 0f, 11.9f), new Vector3(13.9f, 0f, 9.7f), new Vector3(11.85f, 0f, 9.7f) })
                Rod(arc, "RopePost", p, p + new Vector3(0f, 0.95f, 0f), 0.05f, ClearviewArt.Steel);
            Rod(arc, "Rope", new Vector3(9.8f, 0.85f, 9.7f), new Vector3(13.9f, 0.85f, 9.7f), 0.025f, rope);
            Rod(arc, "Rope", new Vector3(9.8f, 0.85f, 9.7f), new Vector3(9.8f, 0.85f, 11.9f), 0.025f, rope);
            // the repair table: old books waiting (for decades) to be glued back together
            Material oldLeather = ClearviewArt.Mat("M_CV_OldLeather", new Color(0.33f, 0.2f, 0.13f), null, Vector2.one, 0.15f);
            Material oldCloth = ClearviewArt.Mat("M_CV_OldCloth", new Color(0.3f, 0.32f, 0.26f), null, Vector2.one, 0.1f);
            ClearviewArt.Box("RepairTable", arc, new Vector3(12.3f, 0.37f, 11.3f), new Vector3(1.9f, 0.74f, 0.8f), ClearviewArt.Wood, true);
            for (int k = 0; k < 7; k++)
            {
                Vector3 bp = new Vector3(11.55f + (k % 4) * 0.36f, 0.77f + (k / 4) * 0.075f, 11.45f - (k % 2) * 0.12f);
                ClearviewArt.Box("OldBook", arc, bp, new Vector3(0.24f, 0.07f, 0.32f), k % 2 == 0 ? oldLeather : oldCloth, false, new Vector3(0f, k * 13f - 30f, 0f));
            }
            ClearviewArt.Prim(PrimitiveType.Cylinder, "GluePot", arc, new Vector3(13.05f, 0.8f, 11.05f), new Vector3(0.1f, 0.05f, 0.1f), ClearviewArt.Steel);
            ClearviewArt.Box("Brush", arc, new Vector3(12.95f, 0.75f, 10.95f), new Vector3(0.02f, 0.015f, 0.2f), ClearviewArt.Wood, false, new Vector3(0f, 25f, 0f));
            // a sagging shelf of more of them against the east wall
            ClearviewArt.Box("RepairShelf", arc, new Vector3(13.65f, 0.6f, 10.6f), new Vector3(0.45f, 1.2f, 1.4f), ClearviewArt.Wood, true);
            for (int k = 0; k < 6; k++)
                ClearviewArt.Box("OldBook (leaning)", arc, new Vector3(13.4f, 1.32f, 10.05f + k * 0.21f), new Vector3(0.05f, 0.24f, 0.17f), k % 2 == 0 ? oldCloth : oldLeather, false, new Vector3(k * 4f - 10f, 0f, 0f));
            // the one: Records, vol. 31 (it isn't a repair job - it's hidden among them)
            GameObject vol31 = ClearviewArt.Box("Records_Vol31", arc, new Vector3(11.75f, 0.785f, 11.12f), new Vector3(0.26f, 0.05f, 0.34f), ClearviewArt.Mat("M_CV_Vol31", new Color(0.45f, 0.1f, 0.08f), null, Vector2.one, 0.2f), true, new Vector3(0f, 8f, 0f));
            Interactable vol31It = vol31.AddComponent<Interactable>();
            R(ClearviewArt.Poster(d, "Sign_Repair", new Vector3(12.4f, 2.1f, 11.9f), 0f, 1.7f, 0.5f, ClearviewArt.DeepRed, ClearviewArt.OffWhite, "OLD BOOKS  -  AWAITING REPAIR", "DO NOT HANDLE.  (This means you.)"),
              "Awaiting repair since... there's no date.\nThe rope only comes up to my knees. Rules here work because we stop.");

            // --- the walls talk (Search words come from these)
            R(ClearviewArt.Poster(d, "Sign_Quiet", new Vector3(7f, 2.45f, 11.9f), 0f, 2.2f, 0.5f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "QUIET, PLEASE"),
              "Underneath, smaller: \"The meter is always listening.\"");
            R(ClearviewArt.Poster(d, "Poster_Catalogue", new Vector3(13.9f, 1.8f, 2.2f), 90f, 0.9f, 0.9f, ClearviewArt.PastelBlue, ClearviewArt.Ink, "OUR CATALOGUE BEGINS\nIN APRIL 1967", "Earlier records were reorganised\nfor your safety.", ClearviewArt.PastelYellow),
              "April 1967. The same month as the photo in the gym.\nEverything before it was \"reorganised\".", "1967");
            R(ClearviewArt.Poster(d, "Banner_Strength", new Vector3(0.1f, 2.4f, 11f), -90f, 1.7f, 0.75f, ClearviewArt.OffWhite, ClearviewArt.DeepRed, "STRENGTH THROUGH DISCIPLINE\nSTRENGTH THROUGH COMMUNITY\nSTRENGTH THROUGH ACTION"),
              "All three lines now, as if the motto had always been there.", "STRENGTH");
            R(ClearviewArt.Poster(d, "Poster_BannedBooks", new Vector3(13.9f, 1.7f, 5.4f), 90f, 0.8f, 1.0f, ClearviewArt.PastelPink, ClearviewArt.Ink, "BANNED BOOKS WEEK\n- CANCELLED -", "This year's list:\nTHE WAVE (1981)\n...and this poster.", ClearviewArt.PastelMint),
              "THE WAVE. A novel about a classroom that turns into a movement.\nThe only title they forgot to black out.", "THE WAVE");
            R(ClearviewArt.Poster(d, "Display_Yearbook1980", new Vector3(13.9f, 1.6f, 8.3f), 90f, 0.7f, 0.9f, new Color(0.62f, 0.52f, 0.38f), new Color(0.2f, 0.15f, 0.1f), "CLEARVIEW\nYEARBOOK 1980", "Reporter of the Year:\nH. VOSS  (Seat 31)", ClearviewArt.DeepRed),
              "H. VOSS. Seat 31. Reporter of the Year, 1980 -\nthe year the first owner of this phone \"gave it back\".", "VOSS");
            R(ClearviewArt.Poster(d, "Poster_CardsReady", new Vector3(1.5f, 1.7f, 11.9f), 0f, 0.8f, 1.0f, ClearviewArt.PastelYellow, ClearviewArt.Ink, "CITIZEN CARDS\nARE READY", "If your card has a RED X,\nplease report to the office.\n(You will know what to do.)", ClearviewArt.PastelBlue),
              "A red X means you report.\nHums When Scared checked hers twice.", "RED X");
            ClearviewArt.Fluorescents(d, 0.3f, 0.3f, 13.7f, 11.7f, 3, 3, 0.85f, 4);
            ClearviewArt.Clock(d, new Vector3(13.9f, 2.45f, 10.6f), 90f, 15.5f);
            ClearviewArt.Speaker(d, new Vector3(7f, 2.62f, 0.1f), 180f);

            NavMeshSurface surface = MakeSurface(world.gameObject);

            // --- the librarian (mint cardigan, glasses-chain of keys)
            GameObject li = Watcher("LIBRARIAN", root, deskSpot.position, 0f, ClearviewArt.Mat("M_CV_LibrarianCardigan", new Color(0.45f, 0.58f, 0.52f), null, Vector2.one, 0.1f), false, true);
            foreach (Light l in li.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            PatrolAI librarian = li.AddComponent<PatrolAI>();
            librarian.roamOnBegin = false;
            librarian.scanAngle = 30f;
            librarian.scanSpeed = 0.4f;
            TeacherVision lv = li.AddComponent<TeacherVision>();
            lv.viewAngle = 80f; lv.viewRange = 11f; lv.eyeHeight = 1.6f;
            Footsteps lf = li.AddComponent<Footsteps>();
            lf.volume = 0.3f; lf.pitch = 1.2f; lf.keysEverySteps = 7;
            li.GetComponent<NavMeshAgent>().speed = 0.95f;
            // --- the assistant librarian: walks the aisles the whole time (never past the rope)
            GameObject asl = Watcher("ASSISTANT_LIBRARIAN", root, new Vector3(10.9f, 0f, 5f), 0f, ClearviewArt.Mat("M_CV_AssistantVest", new Color(0.52f, 0.47f, 0.36f), null, Vector2.one, 0.1f), false, false);
            foreach (Light l in asl.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);
            PatrolAI assistant = asl.AddComponent<PatrolAI>();
            assistant.roamRect = new Vector4(0.8f, 2.8f, 13.4f, 9.3f);
            assistant.waitTime = new Vector2(2f, 4.5f);
            assistant.scanAngle = 45f;
            assistant.scanSpeed = 0.5f;
            TeacherVision av = asl.AddComponent<TeacherVision>();
            av.viewAngle = 80f; av.viewRange = 8f; av.eyeHeight = 1.6f;
            Footsteps af = asl.AddComponent<Footsteps>();
            af.volume = 0.32f; af.pitch = 0.95f;
            asl.GetComponent<NavMeshAgent>().speed = 0.85f;

            // --- player: just inside the door
            PhoneController phone;
            Transform exposure;
            PlayerRig rigA = MakeRig(new Vector3(3f, 0f, 0.9f), 0f, out phone, out exposure);
            ArchiveDirector dir = rigA.gameObject.AddComponent<ArchiveDirector>();
            dir.rig = rigA;
            dir.phone = phone;
            dir.navSurface = surface;
            dir.phoneExposurePoint = exposure;
            dir.librarian = librarian;
            dir.librarianVision = lv;
            dir.assistant = assistant;
            dir.assistantVision = av;
            dir.deskSpot = deskSpot;
            dir.returnsCart = cartIt;
            dir.oldBook = vol31It;
            // pigeons on the west window ledges: they get noisy, and she comes to the window to shoo them
            LedgePigeons(lib, new[] { 3f, 5f, 7f, 9f }, new[] { 4.8f, 5.3f, 7.1f, 8.8f });
            Transform pigeonSpot = new GameObject("PigeonWindowSpot (librarian shoos them)").transform;
            pigeonSpot.SetParent(lib, false);
            pigeonSpot.SetPositionAndRotation(new Vector3(0.75f, 0f, 6.5f), Quaternion.Euler(0f, -90f, 0f));
            dir.pigeonSpot = pigeonSpot;
            Backdrop(root, -0.5f, -0.5f, 14.5f, 12.5f, 55, false);
            AddEggs(root);
            ClearviewArt.NoCoplanarFaces(root);
            EditorSceneManager.SaveScene(s, L5Path);
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
        static GameObject Watcher(string name, Transform parent, Vector3 pos, float yaw, Material body, bool cap, bool female = false, bool young = false)
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
            ClearviewArt.Standing(g.transform, cap, female, young);
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

        static readonly string[] Furniture = { "LibrarianDesk", "ReturnsCart", "Shelf", "Crates", "LowTable", "Sofa_Seat", "Sofa_Back", "TrophyCase", "Desk_Student", "Desk_Player", "Desk_Teacher", "Desk_Player_Cubby", "Chair", "Cafe_Table", "Cafe_Bench", "Cafe_Counter", "Prop_Shelf", "Prop_Locker", "Pillar", "Blackboard" };

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
            ClearviewArt.NeutralBody(rigGo.transform);           // only mirrors see it
            cam.cullingMask &= ~(1 << PosterEgg.MirrorLayer);
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
