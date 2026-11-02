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
        // Roll-call names are an homage: Irving Adler (the teacher fired under New York's Feinberg Law - Adler v. Board
        // of Education, 1952), Harry Keyishian (whose case struck that law down in January 1967), Ellwood Cubberley
        // (the school reformer the Cubberley Wing / Cubberley High School is named after) and Bert, the turtle from
        // the 1952 school film "Duck and Cover".
        public string[] namesBefore = { "Irving Adler", "Harry Keyishian", "Ellwood Cubberley" };
        public string[] namesAfter = { "Bert Turtle" };

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
                cubbyZone.prompt = L("Search your desk", "翻找课桌");
                cubbyZone.active = false;   // unlocked after the roll call
                cubbyZone.onUse = () => { rig.EnterInspect(); SetZone(false); cubbySearched = true; };
            }
            if (books != null) { books.prompt = L("Move the books aside", "把书挪到一边"); books.onUse = () => StartCoroutine(MoveBooks()); }
            if (loosePanel != null) { loosePanel.prompt = L("The back panel is loose - pull it out", "背板松了——把它拉出来"); loosePanel.active = false; loosePanel.onUse = () => StartCoroutine(PullPanel()); }
            if (hiddenPhone != null) { hiddenPhone.prompt = L("Take the phone", "拿起手机"); hiddenPhone.active = false; hiddenPhone.onUse = TakePhone; }
            if (frontStudent != null) { frontStudent.prompt = L("Tap their shoulder twice", "轻拍两下前桌的肩膀"); frontStudent.active = false; frontStudent.onUse = () => StartCoroutine(StudentTurns()); }
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
                Thought(L("If he can see it, ", "只要他看得见，") + Rule(L("the phone shakes and turns red", "手机就会抖动、变红")) + L(".\nKeep it out of his sight.", "。\n别让他看见。"));
            }

            UpdateObjective();
            UpdateInteraction();
            UpdateDanger();
            UpdateCalls();
        }

        void UpdateObjective()
        {
            if (rig.CurrentMode == PlayerRig.Mode.Walk) { SetObjective(L("Walk into the classroom.\n(W + mouse)", "走进教室。\n（W + 鼠标）")); return; }
            if (!seated) { SetObjective(""); return; }
            if (rollCall) { SetObjective(L("Roll call.\nPress 1 when YOUR name is called.", "点名。\n叫到你的名字时按 1。")); return; }
            if (!phone.hasPhone) { SetObjective(L("Search your desk.\nLook down at the cubby and press E.", "翻找课桌。\n低头看向桌肚，按 E。")); return; }
            if (!phone.IsUnlocked(PhoneController.AppPhotos)) { SetObjective(L("Sit up, HOLD RIGHT MOUSE.\nPhone code = the year the school was founded. Then PHOTOS = lights out (24-hour clock).", "坐直，按住鼠标右键。\n手机密码 = 学校建校的年份。相册密码 = 熄灯时间（24 小时制）。")); return; }
            if (!phone.ClueFound) { SetObjective(L("Look through PHOTOS (A / D).\nOne of them shows a mark you can find in this room.", "翻看相册（A / D）。\n其中一张照片上的记号，就在这间教室里。")); return; }
            SetObjective(L("Tap the shoulder of the student\nwith the chalk triangle (E).", "轻拍椅背上有粉笔三角的\n那位同学（E）。"));
        }

        bool EnteredClassroom(Vector3 p) { return In(p, backDoorZone) || In(p, frontDoorZone); }
        static bool In(Vector3 p, Vector4 r) { return p.x >= r.x && p.x <= r.z && p.z >= r.y && p.z <= r.w; }

        // ------------------------------------------------------------------ opening & walk in
        IEnumerator Opening()
        {
            // quick start: he calls you in almost immediately; you can walk once he has spoken
            rig.Lock();
            caption.text = L("MONDAY.  FIRST PERIOD.", "星期一。第一节课。");
            if (bellClip != null) ambient.Play(bellClip, 0.85f);
            StartCoroutine(Fade(1f, 0f, 1.2f));
            yield return Wait(bellClip != null ? 2.6f : 0.5f);
            Authority(L("(from inside the classroom)  ...The bell has rung. Whoever is standing at the door. In. Now.", "（教室里传来）……铃已经响了。站在门口的那个。进来。现在。"), 4.5f, true, 0.3f);
            yield return Wait(1.2f);
            caption.text = "";
            if (rig.CurrentMode == PlayerRig.Mode.Locked && !seated) rig.AllowWalk();
            Thought(L("Late. Everyone is going to stare.\nW + mouse: walk into the classroom", "迟到了。所有人都会盯着我看。\nW + 鼠标：走进教室"), 30f, false, 0.3f);
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
            Authority(L("You're late, " + Name + ".", Name + "，你迟到了。"), 2.8f, true, 0.3f);
            Authority(L("Everyone has been waiting for you.", "所有人都在等你。"), 3f, false, 0.3f);
            Authority(L("Sit.", "坐下。"), 2f, false, 0.5f);
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
            staring = false;

            Authority(L("Roll call. Answer when you hear your name.", "点名。听到名字就答到。"), 2.2f, false, 0.15f);
            yield return RollName(namesBefore.Length > 0 ? namesBefore[0] : null);
            Thought(Rule(L("Every device in this school is monitored.", "这所学校的每台设备都被监控着。")) + L("\nIf he sees a phone, he takes it.", "\n只要他看见手机，就会收走。"), 4f, false, 0.15f);
            yield return RollName(namesBefore.Length > 1 ? namesBefore[1] : null);
            Thought(L("He calls on people at random.\n", "他会随机点人回答问题。\n") + Rule(L("Listen to him", "认真听他讲")) + L(" - he only asks about what he has said.", "——他只问他讲过的内容。"), 4f, false, 0.15f);
            yield return RollName(namesBefore.Length > 2 ? namesBefore[2] : null);
            Thought(L("The older kids say he gives ", "学长学姐说他会给") + Rule(L("three warnings", "三次警告")) + L(".\nAfter the third, he takes whatever you're hiding.", "。\n第三次之后，你藏的东西就归他了。"), 4.5f, false, 0.15f);
            yield return WaitIdle();

            // the player's own name
            bool answered = false;
            for (int attempt = 0; attempt < 2 && !answered && !ended; attempt++)
            {
                Authority(attempt == 0 ? Name + L(".", "。") : L(Name + ". I said your name.", Name + "。我叫你的名字了。"), 99f, true);
                ShowQuestion(L("Your name was called.\n\n1    \"Here.\"", "叫到你的名字了。\n\n1    \"到。\""));
                float t = 0f;
                while (t < answerSeconds && !answered)
                {
                    t += Time.deltaTime;
                    questionTimer.rectTransform.anchorMax = new Vector2(1f - t / answerSeconds, 1f);
                    questionStatus.text = phone.IsExposed ? L("Put the phone down first!", "先把手机放下！") : L("Press 1", "按 1");
                    if (!phone.IsExposed && GameInput.DigitDown() == 1) answered = true;
                    yield return null;
                }
                questionBox.SetActive(false);
                ClearLine();
            }
            if (answered) { StudentSay(Name, L("Here.", "到。"), 1f, false, 0.15f); Authority(L("...Late, and slow. Noted.", "……迟到，还反应慢。记下了。"), 2.2f, false, 0.2f); }
            else Authority(L("...Present, I suppose. Barely.", "……算你在吧。勉强。"), 2.2f, false, 0.2f);
            // the desk can be searched right away (the rest of the roll call goes on around you)
            rollCall = false;
            if (cubbyZone != null) cubbyZone.active = true;
            StartCoroutine(Reminder(L("(Something about this desk feels off... look down at the cubby.)", "（这张课桌有点不对劲……低头看看桌肚。）"), () => cubbySearched, 30f));

            Thought(L("And this seat... they say whoever sits here\n", "还有这个座位……据说坐在这里的人\n") + Rule(L("finds something in the desk", "都会在课桌里找到点什么")) + L(".", "。"), 4.5f, false, 0.15f);
            foreach (string n in namesAfter) yield return RollName(n);
            Authority(L("Good. Today: the Founding Charter. Listen carefully. I will be asking questions.", "很好。今天讲《建校章程》。认真听，我会提问。"), 3.8f, false, 0.3f);
            yield return WaitIdle();

            if (teacher != null) teacher.EndCall();
            lecture = StartCoroutine(Lecture());
        }

        IEnumerator RollName(string n)
        {
            if (string.IsNullOrEmpty(n)) yield break;
            string shown = Loc.T(n);
            Authority(shown + L(".", "。"), 1f, false, 0.1f);
            StudentSay(shown, L("Here.", "到。"), 0.8f, false, 0.15f);
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
                if (p == null && m == PlayerRig.Mode.Inspect) p = L("[Space]  Sit back up", "[空格]  坐直");
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
            Thought(L("There's something hidden behind it.", "后面藏着东西。"), 4f, true);
        }

        void TakePhone()
        {
            hiddenPhone.active = false;
            hiddenPhone.gameObject.SetActive(false);
            phone.hasPhone = true;
            nextCall = Random.Range(callInterval.x, callInterval.y);
            Thought(L("(An old phone. A sticky note on the back: \"it opens the year it all began.\")", "（一部旧手机。背面贴着一张便利贴：\"密码是一切开始的那一年。\"）"), 8f, true);
            StartCoroutine(Reminder(L("Sit up [Space], then ", "先坐直 [空格]，再") + Rule(L("HOLD RIGHT MOUSE", "按住鼠标右键")) + L(" to take out the phone.", "拿出手机。"), () => phone.RaisedOnce || ended, 20f));
        }

        IEnumerator StudentTurns()
        {
            ended = true;
            frontStudent.active = false;
            phone.allowRaise = false;
            rig.Lock();
            if (lecture != null) StopCoroutine(lecture);
            Thought(L("(You tap their shoulder. Twice.)", "（你拍了拍前桌的肩膀。两下。）"), 1.8f, true);
            yield return Wait(0.8f);
            // they turn round to face you, chair and all (the chair spins with them, so nothing clips)
            Transform s = frontStudent.transform;
            Quaternion r0 = s.rotation;
            Quaternion c0 = frontChair != null ? frontChair.rotation : Quaternion.identity;
            for (float k = 0f; k <= 1f; k += GameSpeed.Dt / 0.8f)
            {
                float e = Mathf.Clamp01(k * k * (3f - 2f * k));
                Quaternion turn = Quaternion.Euler(0f, -180f * e, 0f);   // both turn the same way (towards the window side)
                s.rotation = r0 * turn;
                if (frontChair != null) frontChair.rotation = c0 * turn;
                yield return null;
            }
            s.rotation = r0 * Quaternion.Euler(0f, 180f, 0f);
            if (frontChair != null) frontChair.rotation = c0 * Quaternion.Euler(0f, 180f, 0f);
            yield return Wait(0.3f);
            StudentSay(L("STUDENT (whispering)", "同学（小声）"), L("...Took you long enough. Keep it hidden.", "……你可真慢。藏好它。"), 2.8f, false, 0.15f);
            StudentSay(L("STUDENT (whispering)", "同学（小声）"), L("After the bell - the stairwell. I'll show you where we really go.", "下课后——楼梯间。我带你去我们真正去的地方。"), 3.8f, false, 0.15f);
            yield return Wait(0.3f);
            yield return WaitIdle();
            yield return FinishLevel("contact", 1.2f);
        }

        // ------------------------------------------------------------------ danger (stealth)
        void UpdateDanger()
        {
            bool seen = phone.IsExposed && teacher != null && teacher.Running && vision != null && vision.CanSee(phoneExposurePoint.position, rig.transform);
            if (AccumulateDanger(seen ? 1f / Mathf.Max(0.05f, noticeSeconds) : 0f))
                Strike(L("...What was that? Eyes up here, " + Name + ".", "……那是什么？" + Name + "，看这边。"));
            if (phone.ClueFound && frontStudent != null && !frontStudent.active && !ended)
            {
                frontStudent.active = true;
                Thought(L("A chalk triangle... ", "粉笔三角……") + Rule(L("the chair in front of me has one", "前桌的椅背上就有一个")) + L(".", "。"), 7f);
            }
        }

        protected override IEnumerator OnCaught()
        {
            calling = false;
            questionBox.SetActive(false);
            phone.allowRaise = false;
            rig.Lock();
            if (lecture != null) StopCoroutine(lecture);
            Authority(L("That's three, " + Name + ". Give it to me.", "第三次了，" + Name + "。交出来。"), 6f, true);
            if (teacher != null && takePhoneStandPoint != null) teacher.Approach(takePhoneStandPoint.position, rig.transform);
            staring = true;
            float t = 0f;
            yield return Wait(0.5f);
            while (teacher != null && !teacher.HasArrived && t < 8f) { t += Time.deltaTime; yield return null; }
            yield return WaitIdle();
            Thought(L("(He takes the phone. Everyone saw.)", "（他拿走了手机。所有人都看见了。）"), 4f, true);
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
                    string line = Loc.T(teacher.lines[idx].text);
                    // as long as it takes to read, no longer
                    Authority(line, Mathf.Clamp(1.4f + line.Length * (Loc.Chinese ? 0.1f : 0.042f), 2.4f, 5f), false, 0.25f);
                    i++;
                    yield return WaitIdle();
                    // the school interrupts, politely (dark humour)
                    if (i == 5 && !pa1) { pa1 = true; PA(L("This is a reminder that you are happy. Thank you.", "温馨提示：您很快乐。谢谢。"), 3.2f, false, 0.25f); yield return WaitIdle(); }
                    if (i == 10 && !pa2) { pa2 = true; PA(L("Reminder: the pigeons in the courtyard are not your friends.", "提醒：庭院里的鸽子不是您的朋友。"), 3.4f, false, 0.25f); yield return WaitIdle(); }
                    // a short pause between lines - this is where the player's thoughts get their turn
                    float t = 0f;
                    while (t < 1.1f && !ended) { if (DialogueIdle) t += GameSpeed.Dt; yield return null; }
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
                if (nextCall > 0f) { nextCall -= GameSpeed.Dt; if (nextCall <= 0f) StartCall(); }
                return;
            }

            callTimer -= Time.deltaTime;
            questionTimer.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(callTimer / answerSeconds), 1f);
            bool canAnswer = rig.CurrentMode == PlayerRig.Mode.Seated && !phone.IsExposed;
            if (phone.IsExposed) questionStatus.text = L("PUT THE PHONE DOWN to answer!  (release Right Mouse)", "先放下手机再回答！（松开鼠标右键）");
            else if (rig.CurrentMode != PlayerRig.Mode.Seated) questionStatus.text = L("SIT UP to answer!  [Space]", "坐直才能回答！[空格]");
            else questionStatus.text = L("Press 1, 2 or 3", "按 1、2 或 3");

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
            Authority(Name + L(".  ", "。") + Loc.T(question.question), answerSeconds + 1f, true);
            string q = Loc.T(question.question) + "\n";
            for (int i = 0; i < question.options.Length; i++) q += "\n" + (i + 1) + "    " + Loc.T(question.options[i]);
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
            if (correct) Authority(L("...Correct. Hm. Keep listening.", "……正确。哼。继续听。"), 4f, true);
            else if (timedOut) Strike(L("I asked you a question, " + Name + ".", Name + "，我在问你问题。"));
            else Strike(L("Wrong. Are you even listening?", "错了。你到底有没有在听？"));
        }
    }
}
