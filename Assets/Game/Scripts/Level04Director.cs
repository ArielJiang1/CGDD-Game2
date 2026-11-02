// @role Chapter 6 Inspection Day (Friday, the fifth day): get the phone back, the 1979 photo (Voss was Seat 31), desk inspection, final choice: hide it (THE SEAT) or send it to everyone (LIGHTS)
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
        [Tooltip("The students in the basement (they turn their heads to the door when Voss appears).")]
        public Transform[] basementStudents;
        [Tooltip("What he finds on the desks he checks, in verdict order (pencils, horse drawing, nothing, the 'help' note). Built by GameSceneBuilder.")]
        public GameObject[] verdictProps;

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
            phone.appUnlocked[PhoneController.AppCamera] = true;
            phone.appUnlocked[PhoneController.AppSearch] = true;
            phone.lockNotification = L("2 new messages\nTriangle: inspection today.", "2 条新信息\n三角：今天查课桌。");
            phone.AddHistory("Triangle", L("volume 31. now you know what this place kept.", "第 31 卷。现在你知道这地方留下了什么。"), false);
            phone.AddHistory("Never Blinks", L("day five tomorrow. can't sleep. goodnight :)", "明天就是第五天。睡不着。晚安 :)"), false);
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
            return n == 1 ? L("  One. You know how many you get.", "  一次。你知道自己有几次机会。") : L("  Two. I was sitting where you are when I got my second.", "  两次。我当年拿到第二次警告时，就坐在你那个位置。");
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
            yield return ChapterCard("CHAPTER 6", "INSPECTION DAY", "FRIDAY  -  09:00");
            rig.BackToSeat();
            if (cubbyZone != null) cubbyZone.active = true;
            if (bellClip != null) { ambient.Play(bellClip, 0.8f); yield return Wait(2.5f); }
            PA(L("Good morning! Today is Inspection Day. Instructor Voss will inspect every desk.", "早上好！今天是检查日。沃斯督导将检查每一张课桌。"), 6f, false, 0.6f);
            Authority(L("Sit straight. A short lesson first. I will be asking questions.", "坐直。先上一小段课。我会提问。"), 5f, false, 0.6f);
            Thought(L("I left it behind the panel last night.\n", "昨晚我把它藏在挡板后面了。\n") + Rule(L("Get it back", "把它拿回来")) + L(" before he gets to my desk.", "，赶在他检查到我的课桌之前。"), 6f, false, 0.6f);
            lecture = StartCoroutine(Lecture());
            StartCoroutine(Reminder(L("(Look down at the cubby. The phone is still behind the panel.)", "（低头看课桌抽屉。手机还在挡板后面。）"), () => cubbySearched, 28f));
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
            Thought(L("Still here. Sit up ", "还在。按 ") + Rule(L("[Space]", "[空格]")) + L(", ", " 坐直，") + Rule(L("HOLD RIGHT MOUSE", "按住右键")) + L(" when he isn't looking.", "看手机——趁他没在看。"), 7f, true);
            StartCoroutine(Messages());
        }

        IEnumerator Messages()
        {
            yield return Wait(2f);
            phone.Receive("Triangle", L("inspection today. he checks every desk. every single one.", "今天查课桌。他每张都查。每一张。"));
            yield return Wait(1.5f);
            phone.Receive("Triangle", L("before he gets to you: look at the last photo. the one we never told you about.", "他查到你之前：看最后一张照片。我们一直没告诉你的那张。"));
            StartCoroutine(Reminder(L("Triangle said: ", "三角说：") + Rule(L("Photos - the last photo", "相册 - 最后一张照片")) + L(".", "。"), () => phone.OldPhotoFound || inspecting || ended, 34f));
            while (!phone.OldPhotoFound && !inspecting && !ended) yield return null;
            if (ended || inspecting) yield break;
            Thought(L("1979. Back row, by the window. ", "1979 年。最后一排，靠窗。") + Rule(L("My seat.", "我的座位。")) + L("\nIn pencil: VOSS. He sat here.", "\n铅笔写着：沃斯。他坐过这里。"), 6f, true, 0.5f);
            phone.Receive("Triangle", L("he had this phone once. he gave it up. that's how he got to stay.", "这部手机他以前也有过。他交出去了。所以他才能留下来。"));
            yield return Wait(2f);
            phone.Receive("Triangle", L("he's starting. decide what you want it to be for.", "他开始了。想好你要拿它做什么。"));
            yield return Wait(2f);
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
            PA(L("Inspection will now begin. Please remain seated and grateful.", "检查现在开始。请保持坐姿，并心怀感激。"), 4.5f, false, 0.4f);
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
            // every verdict matches something on that desk (put there now, so you can see it before he gets there)
            string[] verdicts =
            {
                "Pencils. Two. Approved.",
                "A drawing of a horse. Confiscated. ...It is a good horse.",
                "Nothing. Good. Nothing is good.",
                "A note that says 'help'. Is this a joke? Confiscated. Ha. Ha.",
            };
            bool[] confiscated = { false, true, false, true };
            if (order.Count > verdicts.Length) order.RemoveRange(verdicts.Length, order.Count - verdicts.Length);
            for (int i = 0; i < order.Count; i++)
            {
                GameObject prop = verdictProps != null && i < verdictProps.Length ? verdictProps[i] : null;
                if (prop == null || order[i] == null) continue;
                Vector3 c = order[i].position;
                prop.transform.position = new Vector3(c.x + (i % 2 == 0 ? -0.06f : 0.06f), 0.752f, c.z + 0.6f);
                prop.SetActive(true);
            }
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
                yield return Wait(0.3f);
                while (!teacher.HasArrived && t < 8f && !ended) { t += Time.deltaTime; yield return null; }
                if (ended) yield break;
                Authority(Loc.T(verdicts[v % verdicts.Length]), 4.2f, true, 0.2f);
                ambient.Play(AudioSynth.Creak(), 0.25f, 1.4f);
                // a long silence after each verdict: time to think about what he just took (or didn't)
                t = 0f;
                while (t < 3.6f && !ended) { t += GameSpeed.Dt; yield return null; }
                // confiscated: he takes it with him
                if (confiscated[v % confiscated.Length] && verdictProps != null && v < verdictProps.Length && verdictProps[v] != null) verdictProps[v].SetActive(false);
                v++;
            }
            if (ended) yield break;
            yield return Choice();
        }

        IEnumerator Choice()
        {
            yield return WaitIdle();
            choosing = true;
            phone.CancelTask();
            Authority(Name + L(". Seat thirty-one. Your turn.", "。三十一号座位。轮到你了。"), 5f, true);
            Thought(L("He's coming. ", "他过来了。") + Rule(L("Decide.", "做决定。")), 4f);
            ShowQuestion(L("He's one desk away.\n\n1    Hide it again. Keep the seat's secret.\n2    Send it. To everyone.", "他只差一张课桌了。\n\n1    再藏起来。守住这个座位的秘密。\n2    发出去。发给所有人。"));
            int pick = 0;
            float tt = 0f;
            while (tt < choiceSeconds && pick == 0 && !ended)
            {
                tt += Time.deltaTime;
                questionTimer.rectTransform.anchorMax = new Vector2(1f - tt / choiceSeconds, 1f);
                questionStatus.text = L("Press 1 or 2", "按 1 或 2");
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
            yield return Wait(0.3f);
            while (!teacher.HasArrived && t < 8f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator EndingSeat()
        {
            phone.allowRaise = false;
            phone.hasPhone = false;
            rig.Lock();
            Thought(L("(You slide it back behind the panel. Like whoever sat here before you.)", "（你把它塞回挡板后面。就像之前坐在这里的每一个人。）"), 5f, true);
            yield return WalkToYou();
            yield return WaitIdle();
            ambient.Duck(6f);
            Authority(L("Lid up.", "掀开桌盖。"), 3f);
            yield return WaitIdle();
            Authority(L("Books. Approved.", "书本。合格。"), 3.5f);
            Authority("...", 3f);
            Authority(L("This panel is loose.", "这块挡板松了。"), 4f);
            yield return WaitIdle();
            Thought(L("Don't breathe.", "别呼吸。"), 3f);
            yield return WaitIdle();
            Authority(L("...It has always been loose.", "……它一直都是松的。"), 4.5f);
            Authority(L("An inspected desk is a happy desk, ", "被检查过的课桌，是快乐的课桌，") + Name + L(".", "。"), 5f);
            yield return WaitIdle();
            if (teacher != null) teacher.WalkToAndFace(teacher.lectureSpot, teacher.lectureFacingYaw);
            PA(L("Inspection complete. Zero items found. Congratulations, Clearview!", "检查完毕。未发现任何物品。恭喜明鉴！"), 6f);
            yield return WaitIdle();
            Thought(L("It's still there. For whoever sits here next.", "它还在那里。留给下一个坐在这里的人。"), 5f);
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
                sendText = L("the stairs are open. everyone. now.", "楼梯开着。所有人。现在。"),
                typeSeconds = 4f,
                onDone = i => sent = true,
            });
            Thought(Rule(L("[LEFT MOUSE]", "[左键]")) + L(" select, then ", " 选择，然后 ") + Rule(L("HOLD LEFT MOUSE", "按住左键")) + L(" to send.", " 发送。"), 99f, true);
            StartCoroutine(WalkToYou());
            yield return Wait(1.5f);
            Authority(L("...What are you doing.", "……你在干什么。"), 4f, true);
            float t = 0f;
            bool said = false;
            while (!sent)
            {
                t += Time.deltaTime;
                if (t > 6f && !said) { said = true; Authority(L("Put it down, ", "放下，") + Name + L(". Put it DOWN.", "。放下！"), 5f, true); }
                yield return null;
            }
            ClearLine();
            yield return Wait(0.6f);
            yield return LightsUp();
            yield return Wait(2.6f);   // let the chord ring out
            StudentSay("CLASSMATE", L("...Seat thirty-one.", "……三十一号。"), 2.2f, false, 0.1f);
            StudentSay("CLASSMATE", L("It's open. The stairs.", "开着。楼梯。"), 2.2f, false, 0.1f);
            yield return WaitIdle();
            Authority("...", 1.8f, false, 0.1f);
            yield return WaitIdle();
            phone.forceRaise = false;
            if (teacher != null) teacher.WalkToAndFace(teacher.boardSpot, teacher.boardFacingYaw);
            Authority(L("Page forty. Read quietly.", "第四十页。安静阅读。"), 3f, false, 0.1f);
            yield return WaitIdle();
            Thought(L("He turned to the board.\nHe isn't looking. ", "他转向了黑板。\n他没在看。") + Rule(L("He's letting us go.", "他在放我们走。")), 4f, false, 0.1f);
            yield return WaitIdle();
            ambient.Play(AudioSynth.Glitch(), 0.5f);
            PA(L("<color=#7a1d16>...N0thing happened t0day. Thank y0u.</color>", "<color=#7a1d16>……今天什么都没发生。谢谢。</color>"), 3.5f);
            yield return WaitIdle();
            GameFlow.endingChoice = 2;
            if (basementSeat != null) yield return Basement();
            yield return FinishLevel("ending_lights", 2.5f);
        }

        /// <summary>Epilogue of LIGHTS: that afternoon, at the bottom of the stairs. Then Voss in the doorway.</summary>
        IEnumerator Basement()
        {
            yield return Fade(0f, 1f, 1.6f);
            caption.text = L("THAT AFTERNOON.\n\nTHE ROOM AT THE BOTTOM OF THE STAIRS.", "那天下午。\n\n楼梯底下的房间。");
            phone.forceRaise = false;
            phone.hasPhone = true;      // down here you can use it - everyone does
            phone.allowRaise = true;
            rig.SitAt(basementSeat.position, basementSeat.eulerAngles.y);
            PrepareHeads();
            rig.BackToSeat();
            ambient.droneVolume = 0.03f;
            ambient.ventVolume = 0.02f;
            AudioSource voices = gameObject.AddComponent<AudioSource>();
            voices.clip = AudioSynth.Murmur();
            voices.loop = true;
            voices.spatialBlend = 0f;
            voices.volume = 0.35f;
            voices.Play();
            yield return Wait(2f);
            caption.text = "";
            yield return Fade(1f, 0f, 1.4f);

            StudentSay("TRIANGLE", L("Seat thirty-one! Sit. ...Told you. After the bell.", "三十一号！坐。……我说过的。下课铃之后。"), 3.2f, false, 0.15f);
            StudentSay("HUMS WHEN SCARED", L("You came. I'm not humming - did you notice?", "你来了。我没在哼歌——你发现了吗？"), 3.2f, false, 0.15f);
            Coroutine chat = StartCoroutine(BasementChat());
            StudentSay("NEVER BLINKS", L("Still can't sleep. But it's better down here.", "还是睡不着。但在这下面好多了。"), 3.2f, false, 0.15f);
            StudentSay("LENT ME AN ERASER", L("Say anything. Out loud. Nobody's writing it down.", "随便说什么。大声说。没人在记。"), 3.2f, false, 0.15f);
            yield return WaitIdle();
            Thought(L("Nobody is whispering. ", "没有人在小声说话。") + Rule(L("Nobody looks at the ceiling.", "没有人抬头看天花板。")) +
                    L("\nEveryone's phone is out. ", "\n每个人都拿着手机。") + Rule(L("Mine too: HOLD RIGHT MOUSE.", "我的也可以：按住鼠标右键。")), 5f, false, 0.15f);
            yield return WaitIdle();
            yield return Wait(6f);   // time to read the group chat

            // someone at the door
            if (vossInDoorway != null) vossInDoorway.SetActive(true);
            ambient.Play(AudioSynth.Creak(), 0.5f, 0.8f);
            if (chat != null) StopCoroutine(chat);
            TurnHeads(true);   // slowly, one after another, everyone turns to the door
            for (float k = 0f; k < 1f; k += Time.deltaTime / 1.2f) { voices.volume = Mathf.Lerp(0.35f, 0.03f, k); yield return null; }
            Thought(L("The doorway. ", "门口。") + Rule(L("Instructor Voss.", "沃斯督导。")) + L("\nEveryone stops talking.", "\n所有人都不说话了。"), 4.5f, true);
            yield return Wait(4.5f);
            Authority("...", 3f);
            yield return Wait(3.2f);
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
            Thought(L("He closed the door.\n", "他关上了门。\n") + Rule(L("From the outside.", "从外面。")), 5f, true);
            yield return Wait(1.2f);
            TurnHeads(false);   // ...and slowly, back to each other
            yield return Wait(1.6f);
            for (float k = 0f; k < 1f; k += Time.deltaTime / 2f) { voices.volume = Mathf.Lerp(0.03f, 0.4f, k); yield return null; }
            StudentSay("TRIANGLE", L("...So. Where were we?", "……所以。我们刚说到哪了？"), 3.5f);
            phone.Receive("Never Blinks", L("did he just close it for us?", "他刚刚……是帮我们把门关上了吗？"));
            yield return Wait(2f);
            phone.Receive("Triangle", L("he sat in seat 31 once. maybe he remembers.", "他以前也坐过 31 号。也许他还记得。"));
            yield return WaitIdle();
            yield return Wait(3f);
        }

        // ------------------------------------------------------------------ the basement: a group chat, heads that turn
        IEnumerator BasementChat()
        {
            string[,] chat =
            {
                { "Bus 14", "first time typing without checking who's behind me" },
                { "Don't Reply", "replying. for once." },
                { "Hums When Scared", "la la la (that's me humming. on purpose)" },
                { "Lent Me An Eraser", "welcome, 31. you still owe me an eraser btw" },
                { "Never Blinks", "i blinked. nobody wrote it down" },
                { "Triangle", "house rule down here: say it out loud or type it. both count." },
            };
            string[] zh =
            {
                "第一次打字不用先看身后有没有人",
                "回复。就这一次。",
                "啦啦啦（是我在哼歌。故意的）",
                "欢迎你，31。对了你还欠我一块橡皮",
                "我眨眼了。没人记下来",
                "这下面的规矩：说出来，或者打出来。都算。",
            };
            for (int i = 0; i < zh.Length; i++)
            {
                yield return Wait(i == 0 ? 0.5f : Random.Range(1.6f, 2.6f));
                phone.Receive(chat[i, 0], L(chat[i, 1], zh[i]));
            }
        }

        Transform[] heads;
        Quaternion[] headRest;
        Coroutine[] headTurns;

        /// <summary>Gathers each basement student's head parts under a neck pivot, so the head can turn on its own.</summary>
        void PrepareHeads()
        {
            if (basementStudents == null) return;
            heads = new Transform[basementStudents.Length];
            headRest = new Quaternion[basementStudents.Length];
            headTurns = new Coroutine[basementStudents.Length];
            for (int i = 0; i < basementStudents.Length; i++)
            {
                Transform f = basementStudents[i];
                if (f == null) continue;
                Transform look = f.Find("Chibi");
                if (look == null) continue;
                Transform pivot = new GameObject("HeadPivot").transform;
                pivot.SetParent(look, false);
                pivot.localPosition = new Vector3(0f, 1.13f, 0f);
                List<Transform> parts = new List<Transform>();
                foreach (Transform c in look) if (c != pivot && c.localPosition.y >= 1.13f) parts.Add(c);
                foreach (Transform c in parts) c.SetParent(pivot, true);
                heads[i] = pivot;
                headRest[i] = pivot.localRotation;
            }
        }

        void TurnHeads(bool toDoor)
        {
            if (heads == null) return;
            for (int i = 0; i < heads.Length; i++)
            {
                if (heads[i] == null) continue;
                Quaternion target = headRest[i];
                if (toDoor && vossInDoorway != null)
                {
                    Vector3 to = vossInDoorway.transform.position - heads[i].position; to.y = 0f;
                    Vector3 local = heads[i].parent.InverseTransformDirection(to);
                    float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -80f, 80f);
                    target = headRest[i] * Quaternion.Euler(-4f, yaw, 0f);
                }
                if (headTurns[i] != null) StopCoroutine(headTurns[i]);
                headTurns[i] = StartCoroutine(TurnHead(heads[i], target, (toDoor ? 0.25f : 0.15f) * i + Random.Range(0f, 0.2f), toDoor ? 1.6f : 1.9f));
            }
        }

        static IEnumerator TurnHead(Transform h, Quaternion to, float delay, float seconds)
        {
            yield return new WaitForSeconds(delay);
            Quaternion from = h.localRotation;
            for (float k = 0f; k < 1f; k += Time.deltaTime / seconds)
            {
                h.localRotation = Quaternion.Slerp(from, to, k * k * (3f - 2f * k));
                yield return null;
            }
            h.localRotation = to;
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
            yield return Wait(1.2f);

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
                yield return Wait(wait);
            }
            yield return Wait(0.25f);
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
                Thought(L("Same rules. If he can see it, ", "规则一样。如果他能看到手机，") + Rule(L("it shakes and turns red", "它会抖动并变红")) + L(".", "。"));
            }
            UpdateObjective();
            UpdateInteraction();
            UpdateDanger();
            UpdateCalls();
        }

        void UpdateObjective()
        {
            if (choosing) { SetObjective(L("DECIDE.\n1 = hide it again    2 = send it to everyone", "做决定。\n1 = 再藏起来    2 = 发给所有人")); return; }
            if (inspecting) { SetObjective(L("The inspection has begun.\nKeep the phone hidden. Yours is the last desk.", "检查已经开始。\n藏好手机。你的课桌是最后一张。")); return; }
            if (!gotPhone) { SetObjective(L("Get the phone back.\nLook down at the cubby (E), pull the loose panel.", "把手机拿回来。\n低头看课桌抽屉（E），拉开松动的挡板。")); return; }
            if (!phone.OldPhotoFound) { SetObjective(L("Answer his questions (1 / 2 / 3, phone down).\nCheck PHOTOS - the LAST photo.", "回答他的问题（1 / 2 / 3，先放下手机）。\n查看相册——最后一张照片。")); return; }
            SetObjective(L("Wait. He's starting the inspection.", "等着。他要开始检查了。"));
        }

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

        void UpdateDanger()
        {
            bool seen = phone.IsExposed && teacher != null && teacher.Running && vision != null && vision.CanSee(phoneExposurePoint.position, rig.transform);
            // during the inspection he is close and paying attention: he notices faster
            float secs = inspecting ? noticeSeconds * 0.7f : noticeSeconds;
            bool searching = rig.CurrentMode == PlayerRig.Mode.Inspect && inspecting && teacher != null && vision != null && vision.CanSee(rig.eye.position, rig.transform, 4f);
            float rate = seen ? 1f / Mathf.Max(0.05f, secs) : (searching ? 1f / 4f : 0f);
            if (AccumulateDanger(rate))
            {
                if (seen) Strike(L("...Is that a device, ", "……那是电子设备吗，") + Name + L("?", "？"));
                else Strike(L("Hands out of the desk. I inspect it, not you.", "手拿出来。检查课桌的是我，不是你。"));
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
            Authority(L("That's three, ", "第三次了，") + Name + L(".", "。"), 4f, true);
            yield return WalkToYou();
            yield return WaitIdle();
            Authority(L("Give it to me. ...You'll thank me. Just like I was.", "交给我。……你会感谢我的。就像我当年一样。"), 6f);
            yield return WaitIdle();
            PA(L("Inspection complete. One item found. Congratulations, Clearview!", "检查完毕。发现一件物品。恭喜明鉴！"), 5.5f);
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
                    Authority(Loc.T(teacher.lines[idx].text));
                    i++;
                    yield return WaitIdle();
                    if (i == 4 && !pa) { pa = true; PA(L("Reminder: smiling during inspection is encouraged, but not required. It is encouraged.", "提醒：检查期间鼓励微笑，但不强制。是鼓励。")); yield return WaitIdle(); }
                    float t = 0f;
                    while (t < 3f && !ended) { if (DialogueIdle) t += GameSpeed.Dt; yield return null; }
                }
                yield return null;
            }
        }

        void UpdateCalls()
        {
            if (!gotPhone || inspecting || teacher == null) return;
            if (!calling)
            {
                if (nextCall > 0f) { nextCall -= GameSpeed.Dt; if (nextCall <= 0f) StartCall(); }
                return;
            }
            callTimer -= Time.deltaTime;
            questionTimer.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(callTimer / answerSeconds), 1f);
            bool canAnswer = rig.CurrentMode == PlayerRig.Mode.Seated && !phone.IsExposed;
            if (phone.IsExposed) questionStatus.text = L("PUT THE PHONE DOWN to answer!  (release Right Mouse)", "先放下手机才能回答！（松开右键）");
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
            if (correct) Authority(L("...Correct. You listen. Like I did.", "……正确。你在听。像我当年一样。"), 4f, true);
            else if (timedOut) Strike(L("I asked you a question, ", "我在问你问题，") + Name + L(".", "。"));
            else Strike(L("Wrong. Nobody in that seat has ever been wrong.", "错了。坐那个位置的人从来没答错过。"));
        }
    }
}
