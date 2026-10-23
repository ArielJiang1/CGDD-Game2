// @role Chapter 1 The Lesson: walked to your seat, roll call, find the phone in the desk, codes from the lecture, tap the student with the chalk triangle
// @calls LevelDirectorBase, TeacherAI, TeacherVision, PhoneController, PlayerRig, Interactable
// @tune answerSeconds:float:4-20:time to answer when the teacher calls on you
// @tune firstCallAtBootSecond:float:0-5:the first question comes this many seconds into the phone start-up
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 1 "The Lesson".
    ///  1. Hallway: the teacher calls you in. Walking through the door you are pulled to your seat while the
    ///     class turns to stare. No control during the walk.
    ///  2. ROLL CALL: the teacher reads names (yours too - answer with 1). Between names, the player's
    ///     thoughts quietly explain the rules (devices monitored, random questions, three warnings, the desk).
    ///  3. Desk cubby -> books -> loose back panel -> the old phone.
    ///  4. Codes come from numbers the teacher says: founding year 1952 (phone), lights-out 21:00 -> 2100 (Photos).
    ///  5. A photo shows the sign of the student in front of you -> tap their shoulder -> WIN.
    ///  Only ONE subtitle is on screen at a time: teacher (top) OR thought (bottom-left).
    ///  Danger: while he can see the phone, the phone shakes and turns red. Full = a warning.
    ///  Three warnings, never shown as UI - the teacher says so. Third: he takes the phone (LOSE).
    /// </summary>
    public class Level01Director : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public TeacherAI teacher;
        public TeacherVision vision;
        public Transform seatPoint;
        public Interactable cubbyZone, books, loosePanel, hiddenPhone, frontStudent;
        [Tooltip("The chair of the student in front - they turn around together with it.")]
        public Transform frontChair;
        public Transform takePhoneStandPoint;
        [Tooltip("The school bell at the very start.")]
        public AudioClip bellClip;

        [Header("Classroom doors (x min, z min, x max, z max) - entering one pulls you to your seat")]
        public Vector4 backDoorZone = new Vector4(8.6f, 0f, 9.95f, 2.2f);
        public Vector4 frontDoorZone = new Vector4(8.6f, 9.8f, 9.95f, 12f);
        public float sideAisleX = 9.3f;
        public float backAisleZ = 1.0f;

        [Header("Questions")]
        public float answerSeconds = 10f;
        public Vector2 callInterval = new Vector2(35f, 60f);
        [Tooltip("The first question always comes this many seconds into the first phone start-up.")]
        public float firstCallAtBootSecond = 3f;

        [Header("Roll call (the player's name goes after these)")]
        public string[] namesBefore = { "Emily Carter", "Daniel Brooks", "Sophie Turner" };
        public string[] namesAfter = { "Lucas Reed" };

        float nextCall = -1f;
        bool seated, calling, rollCall, bootCallDone, cubbySearched, toldAboutDanger, staring;
        LessonQuestion question;
        float callTimer;
        int linesSaid = -1;
        readonly List<int> asked = new List<int>();
        readonly List<Transform> classmates = new List<Transform>();
        readonly List<Quaternion> classmateRest = new List<Quaternion>();
        Coroutine lecture;

        // ------------------------------------------------------------------ setup
        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "TEACHER";
            authorityPitch = 0.8f;
            if (phone != null) { phone.hasPhone = false; phone.allowRaise = false; phone.phoneClock = "09:05"; }
            SetupDesk();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!t.name.StartsWith("Fig_Classmate_Seated")) continue;
                classmates.Add(t);
                classmateRest.Add(t.rotation);
            }
        }

        protected override void Start()
        {
            base.Start();
            if (teacher != null) teacher.Begin();
            StartCoroutine(Opening());
        }

        void SetupDesk()
        {
            if (cubbyZone != null)
            {
                cubbyZone.prompt = "Search your desk";
                cubbyZone.active = false;   // unlocked after the roll call
                cubbyZone.onUse = () => { rig.EnterInspect(); SetZone(false); cubbySearched = true; };
            }
            if (books != null) { books.prompt = "Move the books aside"; books.onUse = () => StartCoroutine(MoveBooks()); }
            if (loosePanel != null) { loosePanel.prompt = "The back panel is loose - pull it out"; loosePanel.active = false; loosePanel.onUse = () => StartCoroutine(PullPanel()); }
            if (hiddenPhone != null) { hiddenPhone.prompt = "Take the phone"; hiddenPhone.active = false; hiddenPhone.onUse = TakePhone; }
            if (frontStudent != null) { frontStudent.prompt = "Tap their shoulder twice"; frontStudent.active = false; frontStudent.onUse = () => StartCoroutine(StudentTurns()); }
        }

        void SetZone(bool on)
        {
            if (cubbyZone == null) return;
            foreach (Collider c in cubbyZone.GetComponents<Collider>()) c.enabled = on;
        }

        protected override bool ReminderBlocked() { return calling || rollCall; }

        // ------------------------------------------------------------------ main loop
        protected override void LevelAlways() { UpdateStare(); }

        protected override void LevelUpdate()
        {
            if (rig.CurrentMode == PlayerRig.Mode.Walk && EnteredClassroom(rig.transform.position)) BeginPull();

            phone.allowRaise = rig.CurrentMode == PlayerRig.Mode.Seated && phone.hasPhone;
            if (phone.hasPhone && phone.RaisedOnce && !toldAboutDanger)
            {
                toldAboutDanger = true;
                Thought("If he can see it, " + Rule("the phone shakes and turns red") + ".\nKeep it out of his sight.");
            }

            UpdateObjective();
            UpdateInteraction();
            UpdateDanger();
            UpdateCalls();
        }

        void UpdateObjective()
        {
            if (rig.CurrentMode == PlayerRig.Mode.Walk) { SetObjective("Walk into the classroom.\n(W + mouse)"); return; }
            if (!seated) { SetObjective(""); return; }
            if (rollCall) { SetObjective("Roll call.\nPress 1 when YOUR name is called."); return; }
            if (!phone.hasPhone) { SetObjective("Search your desk.\nLook down at the cubby and press E."); return; }
            if (!phone.IsUnlocked(PhoneController.AppPhotos)) { SetObjective("Sit up, HOLD RIGHT MOUSE.\nPhone code = the year the school was founded. Then PHOTOS = lights out (24-hour clock)."); return; }
            if (!phone.ClueFound) { SetObjective("Look through PHOTOS (A / D).\nOne of them shows a mark you can find in this room."); return; }
            SetObjective("Tap the shoulder of the student\nwith the chalk triangle (E).");
        }

        bool EnteredClassroom(Vector3 p) { return In(p, backDoorZone) || In(p, frontDoorZone); }
        static bool In(Vector3 p, Vector4 r) { return p.x >= r.x && p.x <= r.z && p.z >= r.y && p.z <= r.w; }

        // ------------------------------------------------------------------ opening & walk in
        IEnumerator Opening()
        {
            // quick start: he calls you in almost immediately; you can walk once he has spoken
            rig.Lock();
            caption.text = "MONDAY.  FIRST PERIOD.";
            if (bellClip != null) ambient.Play(bellClip, 0.85f);
            StartCoroutine(Fade(1f, 0f, 1.2f));
            yield return new WaitForSeconds(bellClip != null ? 2.6f : 0.5f);
            Authority("(from inside the classroom)  ...The bell has rung. Whoever is standing at the door. In. Now.", 4.5f, true, 0.3f);
            yield return new WaitForSeconds(1.2f);
            caption.text = "";
            if (rig.CurrentMode == PlayerRig.Mode.Locked && !seated) rig.AllowWalk();
            Thought("Late. Everyone is going to stare.\nW + mouse: walk into the classroom", 30f, false, 0.3f);
        }

        void BeginPull()
        {
            Vector3 p = rig.transform.position;
            Vector3 s = seatPoint.position;
            Vector3[] path =
            {
                new Vector3(sideAisleX, p.y, Mathf.Clamp(p.z, backAisleZ, 11f)),
                new Vector3(sideAisleX, p.y, backAisleZ),
                new Vector3(s.x + 0.8f, p.y, backAisleZ),
                new Vector3(s.x, p.y, backAisleZ + 0.15f),
                s,
            };
            rig.pullLookYaw = seatPoint.eulerAngles.y;
            rig.PullToSeat(path, s, seatPoint.eulerAngles.y, OnSeated);
            staring = true;
            if (teacher != null) teacher.CallOn(rig.transform);
            // short gaps here - these lines play while you are being walked to your seat
            Authority("You're late, " + Name + ".", 2.8f, true, 0.3f);
            Authority("Everyone has been waiting for you.", 3f, false, 0.3f);
            Authority("Sit.", 2f, false, 0.5f);
        }

        void OnSeated()
        {
            seated = true;
            StartCoroutine(RollCall());
        }

        // ------------------------------------------------------------------ roll call (rules told through thoughts)
        IEnumerator RollCall()
        {
            rollCall = true;
            yield return WaitIdle();
            yield return new WaitForSeconds(0.6f);
            staring = false;

            Authority("Roll call. Answer when you hear your name.", 5f);
            yield return RollName(namesBefore.Length > 0 ? namesBefore[0] : null);
            Thought(Rule("Every device in this school is monitored.") + "\nIf he sees a phone, he takes it.", 8.5f);
            yield return RollName(namesBefore.Length > 1 ? namesBefore[1] : null);
            Thought("He calls on people at random.\n" + Rule("Listen to him") + " - he only asks about what he has said.", 8.5f);
            yield return RollName(namesBefore.Length > 2 ? namesBefore[2] : null);
            Thought("The older kids say he gives " + Rule("three warnings") + ".\nAfter the third, he takes whatever you're hiding.", 9f);
            yield return WaitIdle();

            // the player's own name
            bool answered = false;
            for (int attempt = 0; attempt < 2 && !answered && !ended; attempt++)
            {
                Authority(attempt == 0 ? Name + "." : Name + ". I said your name.", 99f, true);
                ShowQuestion("Your name was called.\n\n1    \"Here.\"");
                float t = 0f;
                while (t < answerSeconds && !answered)
                {
                    t += Time.deltaTime;
                    questionTimer.rectTransform.anchorMax = new Vector2(1f - t / answerSeconds, 1f);
                    questionStatus.text = phone.IsExposed ? "Put the phone down first!" : "Press 1";
                    if (!phone.IsExposed && GameInput.DigitDown() == 1) answered = true;
                    yield return null;
                }
                questionBox.SetActive(false);
                ClearLine();
            }
            if (answered) { StudentSay(Name, "Here.", 2.5f); Authority("...Late, and slow. Noted.", 4.5f); }
            else Authority("...Present, I suppose. Barely.", 4.5f);

            Thought("And this seat... they say whoever sits here\n" + Rule("finds something in the desk") + ".", 8.5f);
            foreach (string n in namesAfter) yield return RollName(n);
            Authority("Good. Today: the Founding Charter. Listen carefully. I will be asking questions.", 7f);
            yield return WaitIdle();

            rollCall = false;
            if (cubbyZone != null) cubbyZone.active = true;
            if (teacher != null) teacher.EndCall();
            lecture = StartCoroutine(Lecture());
            StartCoroutine(Reminder("(Something about this desk feels off... look down at the cubby.)", () => cubbySearched, 30f));
        }

        IEnumerator RollName(string n)
        {
            if (string.IsNullOrEmpty(n)) yield break;
            Authority(n + ".", 3.5f);
            StudentSay(n, "Here.", 3f);
            yield return null;
        }

        // ------------------------------------------------------------------ the classmates stare while you walk in
        void UpdateStare()
        {
            for (int i = 0; i < classmates.Count; i++)
            {
                Transform t = classmates[i];
                if (t == null) continue;
                if (frontStudent != null && t == frontStudent.transform && ended && strikes < chances) continue;  // turning to you (win)
                if (staring)
                {
                    Vector3 d = rig.transform.position - t.position; d.y = 0f;
                    if (d.sqrMagnitude < 0.01f) continue;
                    // a small twist only - a seated body can't spin round without going through the chair and desk
                    Quaternion limited = Quaternion.RotateTowards(classmateRest[i], Quaternion.LookRotation(d), 32f);
                    t.rotation = Quaternion.RotateTowards(t.rotation, limited, 60f * Time.deltaTime);
                }
                else t.rotation = Quaternion.RotateTowards(t.rotation, classmateRest[i], 40f * Time.deltaTime);
            }
        }

        // ------------------------------------------------------------------ interaction
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

        IEnumerator MoveBooks()
        {
            // slid to the left side of the cubby - they stay where you can see them
            books.active = false;
            Transform t = books.transform;
            Vector3 from = t.localPosition, to = from + new Vector3(-0.23f, 0f, 0f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.6f) { t.localPosition = Vector3.Lerp(from, to, k * k * (3f - 2f * k)); yield return null; }
            t.localPosition = to;
            if (loosePanel != null) loosePanel.active = true;
        }

        IEnumerator PullPanel()
        {
            // pulled towards you, then set aside to the right - still in view
            loosePanel.active = false;
            Transform t = loosePanel.transform;
            foreach (Collider c in t.GetComponentsInChildren<Collider>()) c.enabled = false;
            Vector3 a = t.localPosition, b = a + new Vector3(0f, 0f, -0.14f), c2 = b + new Vector3(0.16f, 0f, 0f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f) { t.localPosition = Vector3.Lerp(a, b, k * k * (3f - 2f * k)); yield return null; }
            for (float k = 0f; k < 1f; k += Time.deltaTime / 0.45f) { t.localPosition = Vector3.Lerp(b, c2, k * k * (3f - 2f * k)); yield return null; }
            t.localPosition = c2;
            if (hiddenPhone != null) hiddenPhone.active = true;
            Thought("There's something hidden behind it.", 4f, true);
        }

        void TakePhone()
        {
            hiddenPhone.active = false;
            hiddenPhone.gameObject.SetActive(false);
            phone.hasPhone = true;
            nextCall = Random.Range(callInterval.x, callInterval.y);
            Thought("(An old phone. A sticky note on the back: \"it opens the year it all began.\")", 8f, true);
            StartCoroutine(Reminder("Sit up [Space], then " + Rule("HOLD RIGHT MOUSE") + " to take out the phone.", () => phone.RaisedOnce || ended, 20f));
        }

        IEnumerator StudentTurns()
        {
            ended = true;
            frontStudent.active = false;
            phone.allowRaise = false;
            rig.Lock();
            if (lecture != null) StopCoroutine(lecture);
            Thought("(You tap their shoulder. Twice.)", 2.5f, true);
            yield return new WaitForSeconds(2f);
            // they turn round to face you, chair and all (the chair spins with them, so nothing clips)
            Transform s = frontStudent.transform;
            Quaternion r0 = s.rotation;
            Quaternion c0 = frontChair != null ? frontChair.rotation : Quaternion.identity;
            for (float k = 0f; k <= 1f; k += Time.deltaTime / 1.1f)
            {
                float e = Mathf.Clamp01(k * k * (3f - 2f * k));
                Quaternion turn = Quaternion.Euler(0f, -180f * e, 0f);   // both turn the same way (towards the window side)
                s.rotation = r0 * turn;
                if (frontChair != null) frontChair.rotation = c0 * turn;
                yield return null;
            }
            s.rotation = r0 * Quaternion.Euler(0f, 180f, 0f);
            if (frontChair != null) frontChair.rotation = c0 * Quaternion.Euler(0f, 180f, 0f);
            yield return new WaitForSeconds(0.8f);
            StudentSay("STUDENT (whispering)", "...Took you long enough. Keep it hidden.", 4.5f);
            StudentSay("STUDENT (whispering)", "After the bell - the stairwell. I'll show you where we really go.", 5.5f);
            yield return new WaitForSeconds(1f);
            yield return WaitIdle();
            yield return FinishLevel("contact", 1.2f);
        }

        // ------------------------------------------------------------------ danger (stealth)
        void UpdateDanger()
        {
            bool seen = phone.IsExposed && teacher != null && teacher.Running && vision != null && vision.CanSee(phoneExposurePoint.position, rig.transform);
            if (AccumulateDanger(seen ? 1f / Mathf.Max(0.05f, noticeSeconds) : 0f))
                Strike("...What was that? Eyes up here, " + Name + ".");
            if (phone.ClueFound && frontStudent != null && !frontStudent.active && !ended)
            {
                frontStudent.active = true;
                Thought("A chalk triangle... " + Rule("the chair in front of me has one") + ".", 7f);
            }
        }

        protected override IEnumerator OnCaught()
        {
            calling = false;
            questionBox.SetActive(false);
            phone.allowRaise = false;
            rig.Lock();
            if (lecture != null) StopCoroutine(lecture);
            Authority("That's three, " + Name + ". Give it to me.", 6f, true);
            if (teacher != null && takePhoneStandPoint != null) teacher.Approach(takePhoneStandPoint.position, rig.transform);
            staring = true;
            float t = 0f;
            yield return new WaitForSeconds(0.5f);
            while (teacher != null && !teacher.HasArrived && t < 8f) { t += Time.deltaTime; yield return null; }
            yield return WaitIdle();
            Thought("(He takes the phone. Everyone saw.)", 4f, true);
            yield return WaitIdle();
            yield return Caught("taken");
        }

        // ------------------------------------------------------------------ lecture & questions
        IEnumerator Lecture()
        {
            int i = 0;
            bool pa1 = false, pa2 = false;
            while (!ended)
            {
                if (!calling && DialogueIdle && teacher != null && teacher.lines.Count > 0)
                {
                    int idx = i % teacher.lines.Count;
                    if (idx > linesSaid) linesSaid = idx;
                    Authority(teacher.lines[idx].text);
                    i++;
                    yield return WaitIdle();
                    // the school interrupts, politely (dark humour)
                    if (i == 5 && !pa1) { pa1 = true; PA("This is a reminder that you are happy. Thank you."); yield return WaitIdle(); }
                    if (i == 10 && !pa2) { pa2 = true; PA("Reminder: the pigeons in the courtyard are not your friends."); yield return WaitIdle(); }
                    // a pause between lines - this is where the player's thoughts get their turn
                    float t = 0f;
                    while (t < 3f && !ended) { if (DialogueIdle) t += Time.deltaTime; yield return null; }
                }
                yield return null;
            }
        }

        void UpdateCalls()
        {
            if (!seated || rollCall || !phone.hasPhone || teacher == null) return;
            if (!calling)
            {
                if (!bootCallDone && phone.BootSecondsElapsed >= firstCallAtBootSecond) { bootCallDone = true; StartCall(); return; }
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
            if (correct) Authority("...Correct. Hm. Keep listening.", 4f, true);
            else if (timedOut) Strike("I asked you a question, " + Name + ".");
            else Strike("Wrong. Are you even listening?");
        }
    }
}
