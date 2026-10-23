// @role Chapter 3 Lights Out: listen for the warden, hold Space to pretend to sleep, Contacts code from his room call, flash the screen at the window 3 times
// @calls LevelDirectorBase, PatrolAI, TeacherVision, Footsteps, PhoneController, PlayerRig
// @tune lightNoticeSeconds:float:0.5-4:seconds his flashlight needs on the phone screen
// @tune awakeNoticeSeconds:float:1-6:seconds he needs to see you sitting up during a bed check
using System.Collections;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 3 "Lights Out" (dormitory, Room 304, night).
    ///  - Watch becomes LISTEN: it is almost dark. You hear the Warden's footsteps and keys in the hall.
    ///  - HOLD [SPACE] = lie down and pretend to sleep. If he comes in and you are sitting up, he notices (slowly).
    ///  - The phone screen is the brightest thing in the room: if his flashlight catches it, he notices fast.
    ///  - Never Blinks (across the room) can't sleep. Reply to her. Contacts' code is the room number (0304),
    ///    which he says every night. The oldest contact: "Seat 31 - '79".
    ///  - Triangle: flash your screen at the window three times so the dorm across the yard knows. They answer. WIN.
    /// </summary>
    public class Level03Director : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public PatrolAI warden;
        [Tooltip("A student sneaking back from the stairwell at the start (the girl who hums).")]
        public PatrolAI sneaker;
        public TeacherVision wardenVision;
        public Footsteps wardenSteps;
        public Transform bedSeat;
        public Transform windowTarget;
        public Renderer[] acrossWindows;
        public Light acrossSignal;
        public Light neverBlinksGlow;

        [Header("Warden route (corridor centre line x = 9)")]
        public Vector3 corridorSouth = new Vector3(9f, 0f, -1.2f);
        public Vector3 corridorNorth = new Vector3(9f, 0f, 11.2f);
        public Vector3 doorOutside = new Vector3(9f, 0f, 3f);
        public Vector3 doorInside = new Vector3(6.9f, 0f, 3f);
        public Vector3 aisleSouth = new Vector3(4f, 0f, 3f);
        public Vector3 aisleMiddle = new Vector3(4f, 0f, 6.2f);
        public Vector3 aisleNorth = new Vector3(4f, 0f, 8.8f);
        public Vector3 bedsideStand = new Vector3(5.4f, 0f, 5.6f);
        [Tooltip("Inside the dorm room (x min, z min, x max, z max).")]
        public Vector4 roomZone = new Vector4(0.1f, 0.1f, 7.95f, 9.95f);

        [Header("Rules")]
        [Tooltip("Seconds until he notices the phone's light in his flashlight.")]
        public float lightNoticeSeconds = 1.6f;
        [Tooltip("Seconds until he notices you are sitting up during a bed check.")]
        public float awakeNoticeSeconds = 3.2f;

        bool firstCheckDone, replied, contactsAsked, firstContactTold, signalAsked, answered, wardenWasInside, toldComing;
        int flashes;
        string lastSeenReason = "";
        float wardenWait;
        Coroutine wardenLoop;

        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "WARDEN";
            authorityPitch = 0.68f;
            phone.hasPhone = true;
            phone.allowRaise = false;
            phone.phoneClock = "23:41";
            phone.appUnlocked[PhoneController.AppPhotos] = true;
            phone.appUnlocked[PhoneController.AppMessages] = true;
            phone.lockNotification = "No new messages.";
            phone.AddHistory("Triangle", "the stairs are open tonight. you did good at lunch.", false);
            phone.AddHistory("Hums When Scared", "thank you. I mean it.", false);
            phone.onMessageArrived = () => { if (!phone.IsExposed) PhoneBuzz(); };
            ambient.droneVolume = 0.11f;
            ambient.ventVolume = 0.05f;
            ambient.SetExtraLoop(AudioSynth.Breathing(), 0.07f);
            if (neverBlinksGlow != null) neverBlinksGlow.enabled = false;
            GameObject rl = new GameObject("RoomLight (warden switches it on)");
            rl.transform.position = new Vector3(4f, 2.8f, 5f);
            roomLight = rl.AddComponent<Light>();
            roomLight.type = LightType.Point;
            roomLight.range = 10f;
            roomLight.intensity = 0f;
            roomLight.color = new Color(0.92f, 0.98f, 1f);
            if (acrossSignal != null) acrossSignal.enabled = false;
        }

        protected override void Start()
        {
            base.Start();
            if (bedSeat != null) rig.SitAt(bedSeat.position, bedSeat.eulerAngles.y);
            if (warden != null) warden.Begin();
            StartCoroutine(Script());
        }

        protected override string StrikeSuffix(int n, bool pa)
        {
            return n == 1 ? "  That's once. Three times, and it's mine." : "  Twice. Next time, it's mine.";
        }

        // ------------------------------------------------------------------ story
        IEnumerator Script()
        {
            rig.Lock();
            yield return ChapterCard("CHAPTER 3", "LIGHTS OUT", "MONDAY  -  23:40");
            rig.BackToSeat();
            yield return SneakBack();
            Authority("Room three-oh-four. Three. Zero. Four.", 5f);
            Authority("Lights out was at twenty-one hundred. Bed check every fifteen minutes.", 6.5f);
            Authority("Sleep.", 2.5f);
            yield return WaitIdle();
            if (warden != null) warden.RunRoute(new[] { corridorNorth }, new[] { 6f }, new[] { float.NaN }, null);
            Thought("Dark. I can hear his keys down the hall.", 5f);
            Thought(Rule("Listen for his footsteps.") + " When he comes in,\n" + Rule("HOLD [SPACE]") + " to lie down and pretend to sleep.", 9f);
            Thought("In the dark, the phone is the brightest thing in the room.\n" + Rule("Only when he's gone."), 8f);
            yield return WaitIdle();
            yield return new WaitForSeconds(4f);

            // the first bed check is scripted, so everyone learns to lie down
            if (!toldComing) { toldComing = true; Thought("Footsteps. He's coming back. " + Rule("[SPACE]"), 4f, true); }
            yield return RoomCheck();
            firstCheckDone = true;
            wardenLoop = StartCoroutine(WardenLoop());

            yield return new WaitForSeconds(3f);
            NeverBlinksTypes();
            phone.Receive("Never Blinks", "are you awake");
            yield return new WaitForSeconds(2.5f);
            phone.Receive("Never Blinks", "it's me. across from you. i can't sleep. i count his footsteps.");
            Thought("A message. Across the room - Never Blinks.\n" + Rule("Only when he's gone."), 6f);
            while (phone.UnreadCount > 0 && !ended) yield return null;
            phone.StartTask(new PhoneTask
            {
                header = "Reply",
                toLabel = "To: Never Blinks",
                options = new[] { "I'm here.", "Go to sleep." },
                onDone = i => { replied = true; StartCoroutine(AfterReply(i)); },
                typeSeconds = 2.5f,
            });
            while (!replied && !ended) yield return null;
            while (!phone.IsUnlocked(PhoneController.AppContacts) && !ended) yield return null;
            while (!phone.FirstContactSeen && !ended) yield return null;
            if (ended) yield break;
            firstContactTold = true;
            Thought("Seat 31. Nineteen seventy-nine.\nSomeone had this phone almost " + Rule("fifty years ago") + ".", 7f);
            yield return new WaitForSeconds(3f);
            NeverBlinksTypes();
            phone.Receive("Never Blinks", "see it? the oldest one. nobody knows who.");
            yield return new WaitForSeconds(7f);
            phone.Receive("Triangle", "one more thing tonight.");
            phone.Receive("Triangle", "the dorm across the yard watches our window at midnight. flash your screen at it three times.");
            while (phone.UnreadCount > 0 && !ended) yield return null;
            signalAsked = true;
            Thought(Rule("Phone up, face the window, [LEFT MOUSE] to flash.") + "\nThree times. Only when he's far away.", 9f);
        }

        /// <summary>Opening: someone tiptoes back from the stairwell, stops at your door, and whispers.</summary>
        IEnumerator SneakBack()
        {
            if (sneaker == null) yield break;
            sneaker.Begin();
            bool done = false;
            sneaker.RunRoute(new[] { doorOutside }, new[] { 0f }, new[] { -90f }, () => done = true);
            Thought("Footsteps. Light ones - not his.", 3.5f);
            float t = 0f;
            while (!done && t < 14f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.6f);
            StudentSay("(whispering)", "...psst. Seat thirty-one. It's real - down there. The stairs.", 4.5f);
            StudentSay("(whispering)", "Everyone was talking. Out loud. Come next time.", 4f);
            yield return WaitIdle();
            Thought("The girl who hums. Back from the stairs.\nShe's smiling. " + Rule("People really go down there."), 5.5f);
            done = false;
            sneaker.RunRoute(new[] { corridorNorth }, new[] { 0f }, new[] { float.NaN }, () => done = true);
            t = 0f;
            while (!done && t < 12f) { t += Time.deltaTime; yield return null; }
            sneaker.gameObject.SetActive(false);
            yield return WaitIdle();
            if (wardenSteps != null) wardenSteps.PlayAt(AudioSynth.Keys(), 0.8f);
        }

        IEnumerator AfterReply(int choice)
        {
            yield return new WaitForSeconds(2f);
            NeverBlinksTypes();
            phone.Receive("Never Blinks", choice == 0 ? "...thanks. nobody says that here." : "...you sound like him.");
            yield return new WaitForSeconds(2.5f);
            phone.Receive("Never Blinks", "triangle says: the oldest name in Contacts. its code is this room. he says it every night.");
            contactsAsked = true;
            while (phone.UnreadCount > 0 && !ended) yield return null;
            Thought("This room... " + Rule("Three. Zero. Four.") + "\nContacts.", 6f);
        }

        void NeverBlinksTypes()
        {
            if (neverBlinksGlow != null) StartCoroutine(Glow(neverBlinksGlow, 2.2f));
        }

        IEnumerator Glow(Light l, float seconds)
        {
            l.enabled = true;
            yield return new WaitForSeconds(seconds);
            l.enabled = false;
        }

        IEnumerator Answer()
        {
            answered = true;
            yield return new WaitForSeconds(2.5f);
            for (int i = 0; i < 3; i++)
            {
                SetAcross(true);
                yield return new WaitForSeconds(0.35f);
                SetAcross(false);
                yield return new WaitForSeconds(0.45f);
            }
            Thought("Three lights across the yard.\n" + Rule("Someone answered."), 5f, true);
            yield return new WaitForSeconds(2f);
            NeverBlinksTypes();
            phone.Receive("Never Blinks", "they saw. goodnight :)");
            yield return new WaitForSeconds(4.5f);
            ended = true;
            phone.allowRaise = false;
            yield return Fade(0f, 1f, 2.5f);
            ambient.Play(AudioSynth.Chime(), 0.4f);
            caption.text = "06:00\n\nGOOD MORNING, CLEARVIEW. YOU SLEPT WELL.\nTHANK YOU FOR SLEEPING.";
            yield return new WaitForSeconds(4.5f);
            GameFlow.EndLevel(LessonResult.Win, "signal");
        }

        void SetAcross(bool on)
        {
            if (acrossSignal != null) acrossSignal.enabled = on;
            if (acrossWindows != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                foreach (Renderer r in acrossWindows)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_EmissionColor", on ? new Color(3f, 2.6f, 1.6f) : new Color(0.25f, 0.2f, 0.1f));
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        // ------------------------------------------------------------------ warden
        IEnumerator RoomCheck()
        {
            if (warden == null) yield break;
            bool done = false;
            warden.RunRoute(
                new[] { doorOutside, doorInside, aisleSouth, aisleMiddle, aisleNorth, aisleMiddle, doorInside, doorOutside, corridorSouth },
                new[] { 1.2f, 0.3f, 0.8f, 2.6f, 1.6f, 0.4f, 0.3f, 0.5f, 0f },
                new[] { -90f, float.NaN, 0f, 90f, 0f, float.NaN, float.NaN, float.NaN, float.NaN },
                () => done = true);
            while (!done && !ended) yield return null;
        }

        IEnumerator Peek()
        {
            if (warden == null) yield break;
            bool done = false;
            Vector3 end = Random.value < 0.5f ? corridorNorth : corridorSouth;
            warden.RunRoute(new[] { doorOutside, end }, new[] { 4f, 0f }, new[] { -90f, float.NaN }, () => done = true);
            while (!done && !ended) yield return null;
        }

        IEnumerator PassBy()
        {
            if (warden == null) yield break;
            bool done = false;
            Vector3 end = warden.transform.position.z > 5f ? corridorSouth : corridorNorth;
            warden.RunRoute(new[] { end }, new[] { 0f }, new[] { float.NaN }, () => done = true);
            while (!done && !ended) yield return null;
        }

        // ---- random bed-check events: every visit is a little different
        static readonly Vector3[] sleeperSpots =
        {
            new Vector3(2.6f, 0f, 1.3f), new Vector3(2.6f, 0f, 7.6f), new Vector3(5.4f, 0f, 8.3f), new Vector3(5.4f, 0f, 0.9f), new Vector3(2.6f, 0f, 4.4f),
        };
        static readonly string[] enterLines =
        {
            "Bed check.", "Three-oh-four. Count.", "...", "Nobody move.", "Eyes closed. All of them.", "I can hear you breathing. Good. Keep doing that.",
        };
        static readonly string[] sleeperLines =
        {
            "Still breathing. Good.", "Bed number two. Asleep. Correct.", "Who taught you to snore like that.", "...Sleep is mandatory.",
        };
        static readonly string[] mumbles = { "...don't take it...", "...mum?...", "...thirty-two times...", "...I'm smiling, I'm smiling..." };
        Light roomLight;
        int lastEvent = -1;

        IEnumerator Route(Vector3[] pts, float[] waits, float[] yaws)
        {
            if (warden == null) yield break;
            bool done = false;
            warden.RunRoute(pts, waits, yaws, () => done = true);
            while (!done && !ended) yield return null;
        }

        IEnumerator GoTo(Vector3 p, float wait, float yaw)
        {
            yield return Route(new[] { p }, new[] { wait }, new[] { yaw });
        }

        void WardenSays(string line, float seconds = 3.5f)
        {
            if (!ended && DialogueIdle) Authority(line, seconds);
        }

        /// <summary>He stops at a sleeping student and shines the light on them. Sometimes they talk in their sleep first.</summary>
        IEnumerator CheckSleeper()
        {
            Vector3 spot = sleeperSpots[Random.Range(0, sleeperSpots.Length)];
            if (Random.value < 0.5f) StudentSay("SLEEPING STUDENT", mumbles[Random.Range(0, mumbles.Length)], 3.5f);
            yield return Route(new[] { doorOutside, doorInside, new Vector3(4f, 0f, spot.z) }, new[] { 0.8f, 0.2f, 0.2f }, new[] { -90f, float.NaN, float.NaN });
            yield return GoTo(spot, 3.2f, spot.x < 4f ? -90f : 90f);
            WardenSays(sleeperLines[Random.Range(0, sleeperLines.Length)]);
            yield return Route(new[] { doorInside, doorOutside, Random.value < 0.5f ? corridorSouth : corridorNorth }, new[] { 0.3f, 0.4f, 0f }, new[] { float.NaN, float.NaN, float.NaN });
        }

        /// <summary>He walks right up to your bed and just stands there.</summary>
        IEnumerator StandOverYou()
        {
            yield return Route(new[] { doorOutside, doorInside }, new[] { 1.0f, 0.4f }, new[] { -90f, float.NaN });
            yield return GoTo(bedsideStand, Random.Range(4f, 6.5f), 90f);
            WardenSays(Random.value < 0.5f ? "...I know when someone is pretending." : "...", 3.5f);
            yield return Route(new[] { doorInside, doorOutside, corridorSouth }, new[] { 0.3f, 0.3f, 0f }, new[] { float.NaN, float.NaN, float.NaN });
        }

        /// <summary>He fills the doorway and doesn't move. Keys. Silence. Then leaves.</summary>
        IEnumerator DoorwaySilence()
        {
            yield return GoTo(doorOutside, 0.5f, -90f);
            yield return GoTo(doorInside, Random.Range(6f, 9f), -90f);
            if (wardenSteps != null) wardenSteps.PlayAt(AudioSynth.Keys(), 0.6f);
            WardenSays(Random.value < 0.5f ? "Sweet dreams, three-oh-four." : "Good. Nobody is awake.", 3.5f);
            yield return Route(new[] { doorOutside, Random.value < 0.5f ? corridorNorth : corridorSouth }, new[] { 0.3f, 0f }, new[] { float.NaN, float.NaN });
        }

        /// <summary>Footsteps come right to the door... stop... and go away again.</summary>
        IEnumerator FakeOut()
        {
            yield return GoTo(doorOutside, Random.Range(1.2f, 2.5f), 0f);
            yield return GoTo(Random.value < 0.5f ? corridorNorth : corridorSouth, 0f, float.NaN);
        }

        /// <summary>He counts heads out loud, bed by bed. Yours is somewhere in the count.</summary>
        IEnumerator CountHeads()
        {
            yield return Route(new[] { doorOutside, doorInside, aisleSouth }, new[] { 0.6f, 0.2f, 0.4f }, new[] { -90f, float.NaN, 0f });
            string[] n = { "One.", "Two.", "Three.", "Four.", "Five.", "...Six." };
            Vector3[] stops = { new Vector3(4f, 0f, 1.3f), new Vector3(4f, 0f, 0.9f), new Vector3(4f, 0f, 4.4f), new Vector3(4.6f, 0f, 4.4f), new Vector3(4f, 0f, 7.6f), new Vector3(4f, 0f, 8.3f) };
            float[] yaws = { -90f, 90f, -90f, 90f, -90f, 90f };
            for (int i = 0; i < stops.Length && !ended; i++)
            {
                yield return GoTo(stops[i], i == 3 ? 2.2f : 0.9f, yaws[i]);
                Authority(n[i], 1.6f, true, 0.1f);
            }
            yield return Route(new[] { aisleMiddle, doorInside, doorOutside, corridorSouth }, new[] { 0.2f, 0.2f, 0.3f, 0f }, new[] { float.NaN, float.NaN, float.NaN, float.NaN });
        }

        /// <summary>He flicks the room light on. Everyone is lit. Then off again.</summary>
        IEnumerator LightsOn()
        {
            yield return GoTo(doorOutside, 0.4f, -90f);
            yield return GoTo(doorInside, 0.1f, -90f);
            ambient.Play(AudioSynth.Click(), 0.8f, 0.7f);
            if (roomLight != null) roomLight.intensity = 2.2f;
            Authority("Lights.", 2f, true);
            float t = 0f;
            while (t < Random.Range(2.5f, 4f) && !ended) { t += Time.deltaTime; yield return null; }
            ambient.Play(AudioSynth.Click(), 0.8f, 0.7f);
            if (roomLight != null) roomLight.intensity = 0f;
            yield return Route(new[] { doorOutside, corridorNorth }, new[] { 0.6f, 0f }, new[] { float.NaN, float.NaN });
        }

        IEnumerator RandomCheck()
        {
            // never the same event twice in a row
            int e;
            do { e = Random.Range(0, 6); } while (e == lastEvent);
            lastEvent = e;
            if (DialogueIdle && e != 3) Authority(enterLines[Random.Range(0, enterLines.Length)], 3f);
            switch (e)
            {
                case 0: yield return RoomCheck(); break;
                case 1: yield return CheckSleeper(); break;
                case 2: yield return StandOverYou(); break;
                case 3: yield return FakeOut(); break;
                case 4: yield return CountHeads(); break;
                default: yield return Random.value < 0.5f ? LightsOn() : DoorwaySilence(); break;
            }
        }

        IEnumerator WardenLoop()
        {
            while (!ended)
            {
                // long quiet gaps: time to use the phone (he is still somewhere in the hall)
                wardenWait = Random.Range(24f, 38f);
                while (wardenWait > 0f && !ended) { wardenWait -= Time.deltaTime; yield return null; }
                if (ended) yield break;
                float r = Random.value;
                if (r < 0.35f) yield return PassBy();
                else if (r < 0.6f) yield return Peek();
                else
                {
                    yield return RandomCheck();
                    // after he has been in the room, he stays away a bit longer
                    float rest = Random.Range(10f, 16f);
                    while (rest > 0f && !ended) { rest -= Time.deltaTime; yield return null; }
                }
            }
        }

        // ------------------------------------------------------------------ main loop
        protected override void LevelUpdate()
        {
            // lie down / sit up
            bool canLie = !phone.IsExposed;
            if (rig.CurrentMode == PlayerRig.Mode.Seated && GameInput.SpaceHeld() && canLie) rig.LieDown();
            else if (rig.CurrentMode == PlayerRig.Mode.Lying && !GameInput.SpaceHeld()) rig.SitUp();
            SetLids(rig.CurrentMode == PlayerRig.Mode.Lying ? 0.72f : 0f);
            phone.allowRaise = rig.CurrentMode == PlayerRig.Mode.Seated;

            // the door creaks when he comes in
            bool inside = warden != null && In(warden.transform.position, roomZone);
            if (inside && !wardenWasInside && wardenSteps != null) wardenSteps.PlayAt(AudioSynth.Creak(), 0.7f);
            wardenWasInside = inside;

            UpdateObjective();
            UpdateSignal();
            UpdateDanger(inside);
            UpdatePrompt();
        }

        void UpdateObjective()
        {
            if (!firstCheckDone) { SetObjective("Listen for his footsteps.\nWhen he comes in: HOLD SPACE to lie down."); return; }
            if (!replied) { SetObjective("Reply to Never Blinks (Messages).\nOnly when he's gone. HOLD SPACE when he comes."); return; }
            if (!phone.IsUnlocked(PhoneController.AppContacts)) { SetObjective("Unlock CONTACTS.\nCode = this room's number (he says it every night)."); return; }
            if (!phone.FirstContactSeen) { SetObjective("Open Contacts and look at\nthe OLDEST contact."); return; }
            if (!signalAsked) { SetObjective("Wait for messages.\nLie down (HOLD SPACE) whenever he comes."); return; }
            if (flashes < 3) { SetObjective("Face the WINDOW, phone up, LEFT MOUSE to flash (" + flashes + "/3).\nOnly when he is far away."); return; }
            SetObjective("");
        }

        bool FacingWindow()
        {
            if (windowTarget == null || rig.eye == null) return false;
            Vector3 to = windowTarget.position - rig.eye.position; to.y = 0f;
            Vector3 f = rig.eye.forward; f.y = 0f;
            return Vector3.Angle(f, to) < 28f;
        }

        void UpdateSignal()
        {
            if (!signalAsked || answered || flashes >= 3) return;
            if (phone.IsUp && phone.ScreenOn && FacingWindow() && GameInput.LeftDown())
            {
                flashes++;
                phone.glowBoost = 1f;
                ambient.Play(AudioSynth.Click(), 0.5f);
                if (warden != null && wardenVision != null && wardenVision.CanSee(phoneExposurePoint.position, rig.transform, 16f))
                {
                    danger = Mathf.Min(0.99f, danger + 0.45f);
                    lastSeenReason = "light";
                }
                if (flashes >= 3) StartCoroutine(Answer());
            }
        }

        void UpdatePrompt()
        {
            string p = null;
            if (signalAsked && !answered && flashes < 3 && phone.IsUp && phone.ScreenOn)
                p = FacingWindow() ? "[Left Mouse]  Flash the screen at the window  (" + flashes + "/3)" : "Face the window";
            else if (!phone.IsExposed && rig.CurrentMode == PlayerRig.Mode.Seated && !firstCheckDone) p = "HOLD [Space]  Lie down";
            SetPrompt(p);
        }

        void UpdateDanger(bool wardenInside)
        {
            float rate = 0f;
            if (warden != null && warden.Running && wardenVision != null)
            {
                if (phone.IsExposed && wardenVision.CanSee(phoneExposurePoint.position, rig.transform, 14f))
                {
                    rate = 1f / Mathf.Max(0.05f, lightNoticeSeconds);
                    lastSeenReason = "light";
                }
                else if (wardenInside && rig.CurrentMode != PlayerRig.Mode.Lying && wardenVision.CanSee(rig.eye.position, rig.transform, 8f))
                {
                    rate = 1f / Mathf.Max(0.05f, awakeNoticeSeconds);
                    lastSeenReason = "awake";
                }
            }
            if (AccumulateDanger(rate))
            {
                if (lastSeenReason == "light") Strike("...A light. Lights out means lights out.");
                else Strike("Why are you sitting up? Lie down.");
            }
        }

        static bool In(Vector3 p, Vector4 r) { return p.x >= r.x && p.x <= r.z && p.z >= r.y && p.z <= r.w; }

        protected override IEnumerator OnCaught()
        {
            if (wardenLoop != null) StopCoroutine(wardenLoop);
            phone.allowRaise = false;
            phone.CancelTask();
            rig.Lock();
            SetLids(0f);
            Authority("Hand it over.", 4f, true);
            if (warden != null)
            {
                warden.Approach(bedsideStand, rig.transform);
                float t = 0f;
                yield return new WaitForSeconds(0.4f);
                while (!warden.HasArrived && t < 9f) { t += Time.deltaTime; yield return null; }
            }
            yield return WaitIdle();
            Authority("Sweet dreams.", 3f);
            yield return WaitIdle();
            yield return Caught("confiscated");
        }
    }
}
