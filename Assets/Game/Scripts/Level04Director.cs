// @role Chapter 4 Inspection Day: get the phone back, the 1979 photo (Voss was Seat 31), desk inspection, final choice: hide it (THE SEAT) or send it to everyone (LIGHTS)
// @calls LevelDirectorBase, TeacherAI, TeacherVision, PhoneController, PlayerRig, Interactable, GameFlow
// @tune answerSeconds:float:4-20:time to answer his questions
// @tune inspectionFallbackSeconds:float:60-300:the inspection starts anyway this long after you take the phone
// @tune choiceSeconds:float:5-30:time to decide when he is one desk away
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 4 "Inspection Day" - the finale. Back in the same classroom, same seat.
    ///  1. The phone is still behind the loose panel in your desk. Get it back while he lectures.
    ///  2. He asks questions, like on the first day (only about what he has said).
    ///  3. Triangle: "look at the last photo." 1979, same seat - VOSS. The teacher sat here.
    ///  4. The inspection begins: desk by desk, front to back. Yours is last.
    ///  5. One desk away, you decide:
    ///       1 = hide it again   -> ENDING "THE SEAT"   (the secret survives, and so does the school)
    ///       2 = send it to all  -> ENDING "LIGHTS"     (every hidden phone in the room lights up)
    ///  Third warning: "...Just like I was." (LOSE, retry from this chapter)
    /// </summary>
    public class Level04Director : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public TeacherAI teacher;
        public TeacherVision vision;
        public Transform seatPoint;
        public Interactable cubbyZone, loosePanel, hiddenPhone;
        public Transform takePhoneStandPoint;
        public AudioClip bellClip;
        [Header("Ending LIGHTS: the room behind the stairs")]
        public Transform basementSeat;
        public Transform basementDoorHinge;
        public GameObject vossInDoorway;

        [Header("Questions")]
        public float answerSeconds = 10f;
        public Vector2 callInterval = new Vector2(24f, 36f);
        [Tooltip("If the old photo is never found, the inspection starts anyway this long after taking the phone.")]
        public float inspectionFallbackSeconds = 100f;
        public float choiceSeconds = 14f;

        bool calling, cubbySearched, gotPhone, inspecting, toldDanger, choosing;
        float nextCall = -1f, callTimer, phoneTime;
        int linesSaid = -1;
        LessonQuestion question;
        readonly List<int> asked = new List<int>();
        readonly List<Transform> classmates = new List<Transform>();
        Coroutine lecture;

        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "INSTRUCTOR VOSS";
            authorityPitch = 0.76f;
            if (teacher != null)
            {
                teacher.lines = LessonDefaults.InspectionLines();
                teacher.questions = LessonDefaults.InspectionQuestions();
            }
            phone.hasPhone = false;
            phone.allowRaise = false;
            phone.phoneClock = "09:00";
            phone.showOldPhoto = true;
            phone.appUnlocked[PhoneController.AppPhotos] = true;
            phone.appUnlocked[PhoneController.AppMessages] = true;
            phone.appUnlocked[PhoneController.AppContacts] = true;
            phone.lockNotification = "2 new messages\nTriangle: inspection today.";
            phone.AddHistory("Triangle", "the stairs are open tonight. you did good at lunch.", false);
            phone.AddHistory("Never Blinks", "they saw. goodnight :)", false);
            phone.onMessageArrived = () => { if (!phone.IsExposed) PhoneBuzz(); };
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.name.StartsWith("Fig_Classmate_Seated")) classmates.Add(t);
            SetupDesk();
        }

        protected override void Start()
        {
            base.Start();
            if (seatPoint != null) rig.SitAt(seatPoint.position, seatPoint.eulerAngles.y);
            if (teacher != null) teacher.Begin();
            StartCoroutine(Opening());
        }

        protected override string StrikeSuffix(int n, bool pa)
        {
            if (pa) return base.StrikeSuffix(n, true);
            return n == 1 ? "  One. You know how many you get." : "  Two. I was sitting where you are when I got my second.";
        }

        protected override bool ReminderBlocked() { return calling || choosing; }

        void SetupDesk()
        {
            if (cubbyZone != null)
            {
                cubbyZone.prompt = "Search your desk";
                cubbyZone.active = false;
                cubbyZone.onUse = () => { rig.EnterInspect(); SetZone(false); cubbySearched = true; };
            }
            if (loosePanel != null) { loosePanel.prompt = "Pull the loose panel"; loosePanel.onUse = () => StartCoroutine(PullPanel()); }
            if (hiddenPhone != null) { hiddenPhone.prompt = "Take the phone back"; hiddenPhone.active = false; hiddenPhone.onUse = TakePhone; }
        }

        void SetZone(bool on)
        {
            if (cubbyZone == null) return;
            foreach (Collider c in cubbyZone.GetComponents<Collider>()) c.enabled = on;
        }

        // ------------------------------------------------------------------ story
        IEnumerator Opening()
        {
            rig.Lock();
            yield return ChapterCard("CHAPTER 4", "INSPECTION DAY", "FRIDAY  -  09:00");
            rig.BackToSeat();
            if (cubbyZone != null) cubbyZone.active = true;
            if (bellClip != null) { ambient.Play(bellClip, 0.8f); yield return new WaitForSeconds(2.5f); }
            PA("Good morning! Today is Inspection Day. Instructor Voss will inspect every desk.", 6f, false, 0.6f);
            Authority("Sit straight. A short lesson first. I will be asking questions.", 5f, false, 0.6f);
            Thought("I left it behind the panel last night.\n" + Rule("Get it back") + " before he gets to my desk.", 6f, false, 0.6f);
            lecture = StartCoroutine(Lecture());
            StartCoroutine(Reminder("(Look down at the cubby. The phone is still behind the panel.)", () => cubbySearched, 28f));
        }

        IEnumerator PullPanel()
        {
            loosePanel.active = false;
            Transform t = loosePanel.transform;
            foreach (Collider c in t.GetComponentsInChildren<Collider>()) c.enabled = false;
            Vector3 a = t.localPosition, b = a + new Vector3(0f, 0f, -0.14f), c2 = b + new Vector3(0.16f, 0f, 0f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f) { t.localPosition = Vector3.Lerp(a, b, k * k * (3f - 2f * k)); yield return null; }
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f) { t.localPosition = Vector3.Lerp(b, c2, k * k * (3f - 2f * k)); yield return null; }
            t.localPosition = c2;
            if (hiddenPhone != null) hiddenPhone.active = true;
        }

        void TakePhone()
        {
            hiddenPhone.active = false;
            hiddenPhone.gameObject.SetActive(false);
            phone.hasPhone = true;
            gotPhone = true;
            nextCall = Random.Range(callInterval.x, callInterval.y) * 0.5f;
            Thought("Still here. Sit up " + Rule("[Space]") + ", " + Rule("HOLD RIGHT MOUSE") + " when he isn't looking.", 7f, true);
            StartCoroutine(Messages());
        }

        IEnumerator Messages()
        {
            yield return new WaitForSeconds(2f);
            phone.Receive("Triangle", "inspection today. he checks every desk. every single one.");
            yield return new WaitForSeconds(1.5f);
            phone.Receive("Triangle", "before he gets to you: look at the last photo. the one we never told you about.");
            StartCoroutine(Reminder("Triangle said: " + Rule("Photos - the last photo") + ".", () => phone.OldPhotoFound || inspecting || ended, 34f));
            while (!phone.OldPhotoFound && !inspecting && !ended) yield return null;
            if (ended || inspecting) yield break;
            Thought("1979. Back row, by the window. " + Rule("My seat.") + "\nIn pencil: VOSS. He sat here.", 6f, true, 0.5f);
            phone.Receive("Triangle", "he had this phone once. he gave it up. that's how he got to stay.");
            yield return new WaitForSeconds(2f);
            phone.Receive("Triangle", "he's starting. decide what you want it to be for.");
            yield return new WaitForSeconds(2f);
            StartInspection();
        }

        void StartInspection()
        {
            if (inspecting || ended) return;
            inspecting = true;
            if (calling) { calling = false; questionBox.SetActive(false); }
            if (lecture != null) StopCoroutine(lecture);
            StartCoroutine(Inspection());
        }

        // ------------------------------------------------------------------ the inspection
        IEnumerator Inspection()
        {
            yield return WaitIdle();
            PA("Inspection will now begin. Please remain seated and grateful.", 4.5f, false, 0.4f);
            UnityEngine.AI.NavMeshAgent ag = teacher.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (ag != null) ag.speed = 1.9f;

            // only a few desks on the way to yours: one at the front, one in the middle, then the two next to you
            List<Transform> all = new List<Transform>(classmates);
            all.RemoveAll(x => x == null);
            all.Sort((a, b) => b.position.z.CompareTo(a.position.z));
            List<Transform> near = new List<Transform>(all);
            near.Sort((a, b) => Vector3.Distance(b.position, rig.transform.position).CompareTo(Vector3.Distance(a.position, rig.transform.position)));
            List<Transform> order = new List<Transform>();
            if (all.Count > 0) order.Add(all[0]);
            if (all.Count > 2 && !order.Contains(all[all.Count / 2])) order.Add(all[all.Count / 2]);
            for (int k = Mathf.Max(0, near.Count - 2); k < near.Count; k++) if (!order.Contains(near[k])) order.Add(near[k]);
            string[] verdicts =
            {
                "Pencils. Two. Approved.",
                "A drawing of a horse. Confiscated. ...It is a good horse.",
                "Nothing. Good. Nothing is good.",
                "A note that says 'help'. Is this a joke? Confiscated. Ha. Ha.",
                "Approved.",
                "Gum. Under the desk. Since 1986. Approved, historically.",
                "An eraser. Whose eraser is this? ...Approved.",
                "Approved. Thank you for your cooperation.",
            };
            int v = 0;
            for (int i = 0; i < order.Count && !ended; i++)
            {
                Transform s = order[i];
                if (s == null) continue;
                // he stands in the nearest aisle, never on or between the desks
                float ax = 0.65f;
                foreach (float a in new[] { 0.65f, 3.5f, 6.5f, 9.3f }) if (Mathf.Abs(a - s.position.x) < Mathf.Abs(ax - s.position.x)) ax = a;
                Vector3 stand = new Vector3(ax, 0f, s.position.z);
                teacher.Approach(stand, s);
                float t = 0f;
                yield return new WaitForSeconds(0.3f);
                while (!teacher.HasArrived && t < 8f && !ended) { t += Time.deltaTime; yield return null; }
                if (ended) yield break;
                Authority(verdicts[v % verdicts.Length], 3f, true, 0.2f);
                v++;
                ambient.Play(AudioSynth.Creak(), 0.25f, 1.4f);
                t = 0f;
                while (t < 1.4f && !ended) { t += Time.deltaTime; yield return null; }
            }
            if (ended) yield break;
            yield return Choice();
        }

        IEnumerator Choice()
        {
            yield return WaitIdle();
            choosing = true;
            phone.CancelTask();
            Authority(Name + ". Seat thirty-one. Your turn.", 5f, true);
            Thought("He's coming. " + Rule("Decide."), 4f);
            ShowQuestion("He's one desk away.\n\n1    Hide it again. Keep the seat's secret.\n2    Send it. To everyone.");
            int pick = 0;
            float tt = 0f;
            while (tt < choiceSeconds && pick == 0 && !ended)
            {
                tt += Time.deltaTime;
                questionTimer.rectTransform.anchorMax = new Vector2(1f - tt / choiceSeconds, 1f);
                questionStatus.text = "Press 1 or 2";
                int d = GameInput.DigitDown();
                if (d == 1 || d == 2) pick = d;
                yield return null;
            }
            questionBox.SetActive(false);
            choosing = false;
            SetObjective("");
            if (ended) yield break;
            ended = true;   // no more warnings from here on: this is your decision
            if (pick == 2) yield return EndingLights();
            else yield return EndingSeat();
        }

        IEnumerator WalkToYou()
        {
            if (teacher == null || takePhoneStandPoint == null) yield break;
            teacher.Approach(takePhoneStandPoint.position, rig.transform);
            float t = 0f;
            yield return new WaitForSeconds(0.3f);
            while (!teacher.HasArrived && t < 8f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator EndingSeat()
        {
            phone.allowRaise = false;
            phone.hasPhone = false;
            rig.Lock();
            Thought("(You slide it back behind the panel. Like whoever sat here before you.)", 5f, true);
            yield return WalkToYou();
            yield return WaitIdle();
            ambient.Duck(6f);
            Authority("Lid up.", 3f);
            yield return WaitIdle();
            Authority("Books. Approved.", 3.5f);
            Authority("...", 3f);
            Authority("This panel is loose.", 4f);
            yield return WaitIdle();
            Thought("Don't breathe.", 3f);
            yield return WaitIdle();
            Authority("...It has always been loose.", 4.5f);
            Authority("An inspected desk is a happy desk, " + Name + ".", 5f);
            yield return WaitIdle();
            if (teacher != null) teacher.WalkToAndFace(teacher.lectureSpot, teacher.lectureFacingYaw);
            PA("Inspection complete. Zero items found. Congratulations, Clearview!", 6f);
            yield return WaitIdle();
            Thought("It's still there. For whoever sits here next.", 5f);
            yield return WaitIdle();
            GameFlow.endingChoice = 1;
            yield return FinishLevel("ending_seat", 2.5f);
        }

        IEnumerator EndingLights()
        {
            rig.Lock();
            phone.forceRaise = true;
            phone.ForceOpen(PhoneController.AppMessages);
            bool sent = false;
            phone.StartTask(new PhoneTask
            {
                forward = true,
                header = "Forward",
                options = new[] { "EVERYONE (all contacts)" },
                sendText = "the stairs are open. everyone. now.",
                typeSeconds = 4f,
                onDone = i => sent = true,
            });
            Thought(Rule("[LEFT MOUSE]") + " select, then " + Rule("HOLD LEFT MOUSE") + " to send.", 99f, true);
            StartCoroutine(WalkToYou());
            yield return new WaitForSeconds(1.5f);
            Authority("...What are you doing.", 4f, true);
            float t = 0f;
            bool said = false;
            while (!sent)
            {
                t += Time.deltaTime;
                if (t > 6f && !said) { said = true; Authority("Put it down, " + Name + ". Put it DOWN.", 5f, true); }
                yield return null;
            }
            ClearLine();
            yield return new WaitForSeconds(1.2f);
            yield return LightsUp();
            yield return new WaitForSeconds(4f);   // let the chord ring out
            StudentSay("CLASSMATE", "...Seat thirty-one.", 3.5f);
            StudentSay("CLASSMATE", "It's open. The stairs.", 3.5f);
            yield return WaitIdle();
            Authority("...", 3f);
            yield return WaitIdle();
            phone.forceRaise = false;
            if (teacher != null) teacher.WalkToAndFace(teacher.boardSpot, teacher.boardFacingYaw);
            Authority("Page forty. Read quietly.", 4.5f);
            yield return WaitIdle();
            Thought("He turned to the board.\nHe isn't looking. " + Rule("He's letting us go."), 6f);
            yield return WaitIdle();
            ambient.Play(AudioSynth.Glitch(), 0.5f);
            PA("<color=#7a1d16>...N0thing happened t0day. Thank y0u.</color>", 5f);
            yield return WaitIdle();
            GameFlow.endingChoice = 2;
            if (basementSeat != null) yield return Basement();
            yield return FinishLevel("ending_lights", 2.5f);
        }

        /// <summary>Epilogue of LIGHTS: that afternoon, at the bottom of the stairs. Then Voss in the doorway.</summary>
        IEnumerator Basement()
        {
            yield return Fade(0f, 1f, 1.6f);
            caption.text = "THAT AFTERNOON.\n\nTHE ROOM AT THE BOTTOM OF THE STAIRS.";
            phone.forceRaise = false;
            phone.hasPhone = false;
            rig.SitAt(basementSeat.position, basementSeat.eulerAngles.y);
            rig.BackToSeat();
            ambient.droneVolume = 0.03f;
            ambient.ventVolume = 0.02f;
            AudioSource voices = gameObject.AddComponent<AudioSource>();
            voices.clip = AudioSynth.Murmur();
            voices.loop = true;
            voices.spatialBlend = 0f;
            voices.volume = 0.35f;
            voices.Play();
            yield return new WaitForSeconds(3f);
            caption.text = "";
            yield return Fade(1f, 0f, 1.8f);

            StudentSay("TRIANGLE", "Seat thirty-one! Sit. ...Told you. After the bell.", 4.5f);
            StudentSay("HUMS WHEN SCARED", "You came. I'm not humming - did you notice?", 4.5f);
            StudentSay("NEVER BLINKS", "Still can't sleep. But it's better down here.", 4.5f);
            StudentSay("LENT ME AN ERASER", "Say anything. Out loud. Nobody's writing it down.", 4.5f);
            yield return WaitIdle();
            Thought("Nobody is whispering. " + Rule("Nobody looks at the ceiling."), 4.5f);
            yield return WaitIdle();

            // someone at the door
            if (vossInDoorway != null) vossInDoorway.SetActive(true);
            ambient.Play(AudioSynth.Creak(), 0.5f, 0.8f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 1.2f) { voices.volume = Mathf.Lerp(0.35f, 0.03f, k); yield return null; }
            Thought("The doorway. " + Rule("Instructor Voss.") + "\nEveryone stops talking.", 4.5f, true);
            yield return new WaitForSeconds(4.5f);
            Authority("...", 3f);
            yield return new WaitForSeconds(3.2f);
            // he steps back onto the landing and closes the door - from the outside
            Vector3 v0 = vossInDoorway != null ? vossInDoorway.transform.position : Vector3.zero;
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.8f)
            {
                if (vossInDoorway != null) vossInDoorway.transform.position = v0 + vossInDoorway.transform.forward * -0.8f * k;
                yield return null;
            }
            if (basementDoorHinge != null)
            {
                Quaternion a = basementDoorHinge.rotation, b = Quaternion.Euler(0f, 0f, 0f);
                for (float k = 0f; k < 1f; k += Time.deltaTime / 1.4f)
                {
                    basementDoorHinge.rotation = Quaternion.Slerp(a, b, k * k * (3f - 2f * k));
                    if (k > 0.5f && vossInDoorway != null) vossInDoorway.SetActive(false);
                    yield return null;
                }
                basementDoorHinge.rotation = b;
            }
            if (vossInDoorway != null) vossInDoorway.SetActive(false);
            ambient.Play(AudioSynth.Click(), 0.6f, 0.6f);
            Thought("He closed the door.\n" + Rule("From the outside."), 5f, true);
            yield return new WaitForSeconds(2.5f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2f) { voices.volume = Mathf.Lerp(0.03f, 0.4f, k); yield return null; }
            StudentSay("TRIANGLE", "...So. Where were we?", 3.5f);
            yield return WaitIdle();
        }

        /// <summary>
        /// The climax: every hidden phone in the room lights up, nearest first, faster and faster. A wide chord
        /// rises underneath; each phone rings its own note going up a major scale; when the last one is lit,
        /// a big open chord blooms. (Everything else goes quiet so this is the loudest moment in the game.)
        /// </summary>
        IEnumerator LightsUp()
        {
            List<Transform> order = new List<Transform>(classmates);
            order.RemoveAll(x => x == null);
            order.Sort((a, b) => Vector3.Distance(a.position, rig.transform.position).CompareTo(Vector3.Distance(b.position, rig.transform.position)));

            ambient.droneVolume = 0f;
            ambient.ventVolume = 0f;
            AudioSource swell = OneShot2D(AudioSynth.Swell(), 0.6f, 1f);
            yield return new WaitForSeconds(1.2f);

            float[] scale = { 1f, 1.2f, 1.3333f, 1.5f, 1.8f, 2f, 2.4f, 2.6667f, 3f, 3.6f, 4f };   // minor pentatonic, going up (free, but lonely)
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            for (int i = 0; i < order.Count; i++)
            {
                Transform s = order[i];
                GameObject g = new GameObject("PhoneGlow");
                g.transform.position = s.position + new Vector3(0f, 0.85f, 0.25f);
                Light l = g.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.62f, 0.85f, 1f);
                l.range = 1.8f;
                l.intensity = 0f;
                StartCoroutine(GlowIn(l, 2.4f));
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(g.transform, false);
                quad.transform.localScale = new Vector3(0.07f, 0.13f, 1f);
                quad.transform.localRotation = Quaternion.Euler(60f, 180f, 0f);
                Renderer r = quad.GetComponent<Renderer>();
                if (sh != null) r.material = new Material(sh) { color = new Color(0.75f, 0.92f, 1f) };

                float pitch = scale[Mathf.Min(i, scale.Length - 1)] * 0.5f * (i >= scale.Length ? 1f + (i - scale.Length + 1) * 0.06f : 1f);
                OneShot2D(AudioSynth.Ping(), Mathf.Lerp(0.22f, 0.4f, i / (float)Mathf.Max(1, order.Count)), pitch);
                ambient.Play(AudioSynth.Vibrate(), 0.15f, Random.Range(0.9f, 1.15f));
                // starts slow, then the whole room catches
                float wait = Mathf.Lerp(0.6f, 0.13f, Mathf.Clamp01(i / 7f));
                yield return new WaitForSeconds(wait);
            }
            yield return new WaitForSeconds(0.25f);
            OneShot2D(AudioSynth.Bloom(), 0.9f, 1f);
            if (swell != null) StartCoroutine(FadeOut(swell, 2.5f));
            dangerFx.level = 0f;
        }

        IEnumerator GlowIn(Light l, float to)
        {
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.35f) { if (l != null) l.intensity = Mathf.Lerp(0f, to * 1.6f, k); yield return null; }
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.6f) { if (l != null) l.intensity = Mathf.Lerp(to * 1.6f, to, k); yield return null; }
        }

        AudioSource OneShot2D(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return null;
            GameObject g = new GameObject("Climax audio");
            g.transform.SetParent(transform, false);
            AudioSource a = g.AddComponent<AudioSource>();
            a.clip = clip;
            a.volume = volume;
            a.pitch = pitch;
            a.spatialBlend = 0f;
            a.priority = 0;
            a.Play();
            Destroy(g, clip.length / Mathf.Max(0.1f, pitch) + 0.5f);
            return a;
        }

        IEnumerator FadeOut(AudioSource a, float seconds)
        {
            float v0 = a != null ? a.volume : 0f;
            for (float k = 0f; k < 1f && a != null; k += Time.deltaTime / seconds) { a.volume = v0 * (1f - k); yield return null; }
        }

        // ------------------------------------------------------------------ main loop
        protected override void LevelUpdate()
        {
            phone.allowRaise = rig.CurrentMode == PlayerRig.Mode.Seated && phone.hasPhone && !choosing;
            if (gotPhone)
            {
                phoneTime += Time.deltaTime;
                if (!inspecting && phoneTime > inspectionFallbackSeconds) StartInspection();
            }
            if (phone.hasPhone && phone.RaisedOnce && !toldDanger)
            {
                toldDanger = true;
                Thought("Same rules. If he can see it, " + Rule("it shakes and turns red") + ".");
            }
            UpdateObjective();
            UpdateInteraction();
            UpdateDanger();
            UpdateCalls();
        }

        void UpdateObjective()
        {
            if (choosing) { SetObjective("DECIDE.\n1 = hide it again    2 = send it to everyone"); return; }
            if (inspecting) { SetObjective("The inspection has begun.\nKeep the phone hidden. Yours is the last desk."); return; }
            if (!gotPhone) { SetObjective("Get the phone back.\nLook down at the cubby (E), pull the loose panel."); return; }
            if (!phone.OldPhotoFound) { SetObjective("Answer his questions (1 / 2 / 3, phone down).\nCheck PHOTOS - the LAST photo."); return; }
            SetObjective("Wait. He's starting the inspection.");
        }

        void UpdateInteraction()
        {
            string p = null;
            PlayerRig.Mode m = rig.CurrentMode;
            if (m == PlayerRig.Mode.Seated || m == PlayerRig.Mode.Inspect)
            {
                if (m == PlayerRig.Mode.Inspect && GameInput.SpaceDown()) { rig.BackToSeat(); SetZone(true); SetPrompt(null); return; }
                p = RaycastInteract();
                if (p == null && m == PlayerRig.Mode.Inspect) p = "[Space]  Sit back up";
            }
            SetPrompt(p);
        }

        void UpdateDanger()
        {
            bool seen = phone.IsExposed && teacher != null && teacher.Running && vision != null && vision.CanSee(phoneExposurePoint.position, rig.transform);
            // during the inspection he is close and paying attention: he notices faster
            float secs = inspecting ? noticeSeconds * 0.7f : noticeSeconds;
            bool searching = rig.CurrentMode == PlayerRig.Mode.Inspect && inspecting && teacher != null && vision != null && vision.CanSee(rig.eye.position, rig.transform, 4f);
            float rate = seen ? 1f / Mathf.Max(0.05f, secs) : (searching ? 1f / 4f : 0f);
            if (AccumulateDanger(rate))
            {
                if (seen) Strike("...Is that a device, " + Name + "?");
                else Strike("Hands out of the desk. I inspect it, not you.");
            }
        }

        protected override IEnumerator OnCaught()
        {
            calling = false;
            questionBox.SetActive(false);
            phone.allowRaise = false;
            phone.CancelTask();
            rig.Lock();
            if (lecture != null) StopCoroutine(lecture);
            Authority("That's three, " + Name + ".", 4f, true);
            yield return WalkToYou();
            yield return WaitIdle();
            Authority("Give it to me. ...You'll thank me. Just like I was.", 6f);
            yield return WaitIdle();
            PA("Inspection complete. One item found. Congratulations, Clearview!", 5.5f);
            yield return WaitIdle();
            yield return Caught("inspected");
        }

        // ------------------------------------------------------------------ lecture & questions
        IEnumerator Lecture()
        {
            int i = 0;
            bool pa = false;
            while (!ended && !inspecting)
            {
                if (!calling && DialogueIdle && teacher != null && teacher.lines.Count > 0)
                {
                    int idx = i % teacher.lines.Count;
                    if (idx > linesSaid) linesSaid = idx;
                    Authority(teacher.lines[idx].text);
                    i++;
                    yield return WaitIdle();
                    if (i == 4 && !pa) { pa = true; PA("Reminder: smiling during inspection is encouraged, but not required. It is encouraged."); yield return WaitIdle(); }
                    float t = 0f;
                    while (t < 3f && !ended) { if (DialogueIdle) t += Time.deltaTime; yield return null; }
                }
                yield return null;
            }
        }

        void UpdateCalls()
        {
            if (!gotPhone || inspecting || teacher == null) return;
            if (!calling)
            {
                if (nextCall > 0f) { nextCall -= Time.deltaTime; if (nextCall <= 0f) StartCall(); }
                return;
            }
            callTimer -= Time.deltaTime;
            questionTimer.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(callTimer / answerSeconds), 1f);
            bool canAnswer = rig.CurrentMode == PlayerRig.Mode.Seated && !phone.IsExposed;
            if (phone.IsExposed) questionStatus.text = "PUT THE PHONE DOWN to answer!  (release Right Mouse)";
            else if (rig.CurrentMode != PlayerRig.Mode.Seated) questionStatus.text = "SIT UP to answer!  [Space]";
            else questionStatus.text = "Press 1, 2 or 3";
            if (canAnswer)
            {
                int d = GameInput.DigitDown();
                if (d >= 1 && d <= 3) { EndCall(d - 1 == question.correct, false); return; }
            }
            if (callTimer <= 0f) EndCall(false, true);
        }

        void StartCall()
        {
            question = PickQuestion();
            if (question == null) { nextCall = 10f; return; }
            calling = true;
            callTimer = answerSeconds;
            teacher.CallOn(rig.transform);
            Authority(Name + ".  " + question.question, answerSeconds + 1f, true);
            string q = question.question + "\n";
            for (int i = 0; i < question.options.Length; i++) q += "\n" + (i + 1) + "    " + question.options[i];
            ShowQuestion(q);
        }

        LessonQuestion PickQuestion()
        {
            List<int> pool = new List<int>();
            for (int i = 0; i < teacher.questions.Count; i++)
                if (teacher.questions[i].aboutLine <= linesSaid && !asked.Contains(i)) pool.Add(i);
            if (pool.Count == 0)
            {
                asked.Clear();
                for (int i = 0; i < teacher.questions.Count; i++) if (teacher.questions[i].aboutLine <= linesSaid) pool.Add(i);
            }
            if (pool.Count == 0) return null;
            int pick = pool[Random.Range(0, pool.Count)];
            foreach (int i in pool) if (teacher.questions[i].aboutLine >= 0 && Random.value < 0.7f) { pick = i; break; }
            asked.Add(pick);
            return teacher.questions[pick];
        }

        void EndCall(bool correct, bool timedOut)
        {
            calling = false;
            questionBox.SetActive(false);
            teacher.EndCall();
            nextCall = Random.Range(callInterval.x, callInterval.y);
            if (correct) Authority("...Correct. You listen. Like I did.", 4f, true);
            else if (timedOut) Strike("I asked you a question, " + Name + ".");
            else Strike("Wrong. Nobody in that seat has ever been wrong.");
        }
    }
}
