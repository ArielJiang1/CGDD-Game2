// @role Chapter 2 Lunch: sweeping camera + Lunch Monitor, follow the humming, forward the message within reach, sneak to the STAFF door and down the stairs
// @calls LevelDirectorBase, PatrolAI, TeacherVision, HumSource, PhoneController, PlayerRig, Interactable
// @tune reachDistance:float:2-8:how close you must be to the girl for the message to send
using System.Collections;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 2 "Lunch" (the cafeteria).
    ///  - New threat: a sweeping security camera + a roaming Lunch Monitor. Danger comes from WHERE you stand.
    ///    Pillars block the camera; people and pillars block the Monitor. The hallway is the only unwatched place.
    ///  - Phone: unlock again (1952), then Messages - its code is announced by the PA: lunch at 12:40 -> 1240.
    ///  - Triangle's job: find the girl who hums when she's scared (listen for the humming), get close
    ///    (this phone only reaches phones nearby) and forward "the stairs are open tonight".
    ///  - Then slip behind the counters to the STAFF door while the camera looks at the windows. WIN.
    ///  - Being seen with the phone, or seen behind the counters: a warning. Three: the phone is taken.
    /// </summary>
    public class Level02Director : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public PatrolAI monitor;
        public TeacherVision monitorVision;
        public TeacherVision[] cameras;
        public Transform hummer;
        public HumSource hum;
        public Interactable staffDoor;
        public Transform staffDoorLeaf;
        [Tooltip("Bench seats you can sit on (position = seat, forward = facing the table).")]
        public Interactable[] seats;
        [Header("The stairwell behind the STAFF door")]
        public Interactable bottomDoor;
        public Transform bottomDoorHinge;
        public Transform voicesPoint;
        public Vector3 throughDoorPoint = new Vector3(17f, 0f, 12.65f);

        [Header("Zones")]
        [Tooltip("Behind the counters (x min, z min, x max, z max). Being seen here = a warning.")]
        public Vector4 kitchenZone = new Vector4(16f, 10.88f, 29.95f, 11.95f);
        public Vector3 kitchenReturnPoint = new Vector3(28.6f, 0f, 9.7f);
        public float kitchenReturnYaw = 180f;
        [Tooltip("Inside the dining hall (for the Monitor's first comment).")]
        public Vector4 diningZone = new Vector4(14.2f, 0.1f, 29.9f, 10.8f);
        [Tooltip("This phone only reaches phones this close.")]
        public float reachDistance = 4f;

        bool gotJob, taskGiven, delivered, enteredDining, toldKitchen, toldRaise, winning, heardHum, toldInReach, listened;
        float taskTime;
        bool throughDoor, saidPaint, saidChalk, saidVoices;
        AudioSource voices;
        Light hummerGlow;
        float kitchenSeen, kitchenGrace, paTimer = 40f, monitorTalk;
        Interactable sitting;

        static readonly string[] ambientPA =
        {
            "Reminder: smiling is part of your uniform.",
            "Today's dessert has been cancelled for your safety. Enjoy your safety.",
            "A student has been happy for fourteen consecutive days. Congratulations to everyone.",
            "Please do not look out of the windows. The windows are for light.",
            "Lunch ends in five minutes. Please finish being nourished.",
        };
        int paIndex;

        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "LUNCH MONITOR";
            authorityPitch = 0.95f;
            phone.hasPhone = true;
            phone.allowRaise = false;
            phone.phoneClock = "12:41";
            phone.appUnlocked[PhoneController.AppPhotos] = true;
            phone.lockNotification = L("No new messages.", "没有新信息。");
            phone.onMessageArrived = () => { if (!phone.IsExposed) PhoneBuzz(); };
            if (staffDoor != null) { staffDoor.prompt = "Slip through the STAFF door"; staffDoor.onUse = UseDoor; }
            if (bottomDoor != null) { bottomDoor.prompt = "Push the door open"; bottomDoor.onUse = PushBottomDoor; }
            if (hummer != null)
            {
                Interactable it = hummer.GetComponent<Interactable>();
                if (it == null) it = hummer.gameObject.AddComponent<Interactable>();
                it.prompt = "Listen to her";
                it.onUse = ListenToHer;
                // her own phone, glowing under the table - this phone can only reach phones close by
                GameObject g = new GameObject("HerPhoneGlow");
                g.transform.SetParent(hummer, false);
                g.transform.localPosition = new Vector3(0f, 0.75f, 0.35f);
                hummerGlow = g.AddComponent<Light>();
                hummerGlow.type = LightType.Point;
                hummerGlow.color = new Color(0.65f, 0.85f, 1f);
                hummerGlow.range = 1.2f;
                hummerGlow.intensity = 0f;
            }
            if (seats != null)
                foreach (Interactable s in seats)
                {
                    if (s == null) continue;
                    Interactable seat = s;
                    seat.prompt = "Sit down";
                    seat.onUse = () => SitOn(seat);
                }
        }

        protected override void Start()
        {
            base.Start();
            if (monitor != null) monitor.Begin();
            rig.Lock();
            StartCoroutine(Opening());
        }

        IEnumerator Opening()
        {
            yield return ChapterCard("CHAPTER 2", "LUNCH", "MONDAY  -  12:40");
            phone.Receive("Triangle", L("check Messages. the code is when we eat.", "看信息。密码是我们吃饭的时间。"));
            PA(L("Good afternoon, Clearview. Lunch begins at twelve-forty. Twelve. Forty.", "下午好，明鉴学院。午餐将于十二点四十分开始。十二。四十。"), 6.5f);
            PA(L("Please chew each bite thirty-two times. Thank you for eating correctly.", "请将每一口咀嚼三十二次。感谢您正确地进食。"), 6f);
            yield return new WaitForSeconds(1.5f);
            rig.AllowWalk();
            SetObjective(L("Read Triangle's message.\nStay in the HALLWAY (safe) and HOLD RIGHT MOUSE.", "读三角发来的信息。\n待在走廊（安全）里，按住鼠标右键。"));
            Thought(L("Triangle said: after the bell, the stairwell.\nBut the only stairs are ", "三角说：下课后，楼梯间。\n可唯一的楼梯在") + Rule(L("behind the kitchen", "厨房后面")) + L(".", "。"), 8f);
            Thought(Rule(L("One camera. One Lunch Monitor.", "一台摄像头。一个午餐监管员。")) + L("\nThe hallway is the only place nobody watches.", "\n只有走廊没人盯着。"), 8f);
            yield return WaitIdle();
            StartCoroutine(Reminder(L("My phone buzzed. ", "手机震了。") + Rule(L("HOLD RIGHT MOUSE", "按住鼠标右键")) + L(" - here in the hallway, where it's safe.", "——就在走廊，这里安全。"), () => phone.RaisedOnce || ended, 22f));
        }

        // ------------------------------------------------------------------ main loop
        protected override void LevelUpdate()
        {
            if (throughDoor)
            {
                // behind the STAFF door nobody is watching: no camera, no monitor, no phone needed
                rig.freezeMovement = false;
                phone.allowRaise = false;
                UpdateStairs();
                SetPrompt(rig.CurrentMode == PlayerRig.Mode.Walk ? RaycastInteract() : null);
                return;
            }
            PlayerRig.Mode m = rig.CurrentMode;
            phone.allowRaise = phone.hasPhone && (m == PlayerRig.Mode.Walk || m == PlayerRig.Mode.Seated);
            rig.freezeMovement = phone.IsExposed;
            if (phone.IsExposed && m == PlayerRig.Mode.Walk && !toldRaise)
            {
                toldRaise = true;
                Thought(L("While I look at the phone, ", "看手机的时候，") + Rule(L("I stand still", "我站着不动")) + L(". Anyone looking can see it.", "。谁看过来都能看见。"), 7f);
            }

            UpdateObjective();
            UpdateHumHints();
            UpdateInteraction();
            UpdateStory();
            UpdateDanger();
            UpdateKitchen();
            UpdateAmbientLines();
        }

        void UpdateInteraction()
        {
            string p = null;
            if (rig.CurrentMode == PlayerRig.Mode.Seated)
            {
                if (GameInput.SpaceDown() && !phone.IsExposed) { StandUp(); SetPrompt(null); return; }
                p = phone.IsExposed ? null : L("[Space]  Stand up", "[空格]  站起来");
            }
            else if (rig.CurrentMode == PlayerRig.Mode.Walk) p = RaycastInteract();
            if (phone.IsExposed && taskGiven && !delivered && phone.OpenApp == PhoneController.AppMessages && phone.UnreadCount == 0)
                p = InReach() ? L("SIGNAL: STRONG  -  A / D pick \"Hums When Scared\", Left Mouse, then HOLD Left Mouse", "信号：强  -  A / D 选\"害怕时会哼歌\"，点鼠标左键，再按住左键发送")
                              : L("SIGNAL: NONE  -  lower the phone and walk closer to the humming girl", "信号：无  -  放下手机，走近那个哼歌的女孩");
            SetPrompt(p);
        }

        void SitOn(Interactable seat)
        {
            if (seat == null) return;
            sitting = seat;
            rig.SitAt(seat.transform.position, seat.transform.eulerAngles.y);
            foreach (Interactable s in seats) if (s != null) s.active = false;
        }

        void StandUp()
        {
            if (sitting == null) return;
            Vector3 back = sitting.transform.position - sitting.transform.forward * 0.62f;
            rig.StandUpAt(back);
            sitting = null;
            foreach (Interactable s in seats) if (s != null) s.active = true;
        }

        // ------------------------------------------------------------------ story
        void UpdateStory()
        {
            if (!gotJob && phone.IsUnlocked(PhoneController.AppMessages))
            {
                gotJob = true;
                phone.Receive("Triangle", L("you made it. don't look for me. they watch who I talk to.", "你做到了。别来找我。他们盯着我跟谁说话。"));
                phone.Receive("Triangle", L("a job first. someone at the window tables hums when she's scared. listen for her.", "先办件事。窗边那桌有个女孩，害怕时会哼歌。听声音找她。"));
                phone.Receive("Triangle", L("tell her: the stairs are open tonight. this phone only reaches phones close by.", "告诉她：今晚楼梯开着。这部手机只能发给附近的手机。"));
            }
            if (gotJob && !taskGiven && phone.UnreadCount == 0)
            {
                taskGiven = true;
                phone.StartTask(new PhoneTask
                {
                    forward = true,
                    header = "Forward to...",
                    options = new[] { "Mom (not mine)", "Don't Reply", "Lent Me An Eraser", "Hums When Scared", "Bus 14" },
                    correct = 3,
                    sendText = L("the stairs are open tonight.", "今晚楼梯开着。"),
                    check = i => InReach() ? null : L("No signal. Get closer to her.", "没有信号。再靠近她一点。"),
                    onDone = i => StartCoroutine(Delivered()),
                    typeSeconds = 3f,
                });
                Thought(Rule(L("Find the one who hums", "找到那个哼歌的人")) + L(" - pink ribbon, window table, far right corner.\nPut the phone DOWN to walk. Pillars block the camera.", "——粉色蝴蝶结，靠窗那桌，最右边的角落。\n放下手机才能走。柱子能挡住摄像头。"), 9f);
            }
            if (!enteredDining && In(rig.transform.position, diningZone))
            {
                enteredDining = true;
                if (monitor != null && Vector3.Distance(monitor.transform.position, rig.transform.position) < 9f)
                    Authority(L("Find a seat. Eat. Smile.", "找位子。吃饭。微笑。"), 4f);
            }
        }

        bool InReach()
        {
            if (hummer == null) return true;
            Vector3 a = rig.transform.position, b = hummer.position;
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b) <= reachDistance;
        }

        /// <summary>The goal box top-left always says what to do next, and where.</summary>
        void UpdateObjective()
        {
            if (winning) return;
            if (delivered) { SetObjective(L("Go to the STAFF door (north wall, behind the counters).\nEnter the lane at the FAR RIGHT end of the counters. Move when the corner camera faces the windows.", "去员工通道门（北墙，柜台后面）。\n从柜台最右端进入后面的过道。等角落的摄像头转向窗户时再走。")); return; }
            if (taskGiven)
            {
                if (InReach()) SetObjective(L("You're close enough.\nHOLD RIGHT MOUSE -> Messages -> send to \"Hums When Scared\".", "已经够近了。\n按住右键 -> 信息 -> 发给“害怕时会哼歌”。"));
                else SetObjective(L("Find the girl who HUMS (pink ribbon).\nShe sits alone at the window table in the FAR RIGHT corner. Follow the sound.", "找到那个哼歌的女孩（粉色蝴蝶结）。\n她一个人坐在最右边角落的靠窗那桌。循着声音走。"));
                return;
            }
            if (gotJob) { SetObjective(L("Keep the phone up and read Triangle's messages.", "别放下手机，读完三角的信息。")); return; }
            if (phone.RaisedOnce) SetObjective(L("Unlock MESSAGES.\nCode = when lunch begins (the PA said it: 12:40 -> 4 digits).", "解锁\"信息\"。\n密码 = 午餐开始的时间（广播说过：12:40 → 四位数）。"));
        }

        void UpdateHumHints()
        {
            if (hummer == null) return;
            float d = Vector3.Distance(new Vector3(rig.transform.position.x, 0f, rig.transform.position.z), new Vector3(hummer.position.x, 0f, hummer.position.z));
            if (hummerGlow != null) hummerGlow.intensity = taskGiven && !delivered ? 0.6f + 0.4f * Mathf.Sin(Time.time * 3f) : 0f;
            // she rocks slightly while she hums
            if (hum != null && hum.Humming) hummer.localRotation = Quaternion.Euler(0f, hummer.localEulerAngles.y, Mathf.Sin(Time.time * 2.2f) * 2.5f);
            if (!taskGiven || delivered) return;
            taskTime += Time.deltaTime;
            if (!heardHum && d < 6f && DialogueIdle)
            {
                heardHum = true;
                Thought(L("That humming... the school anthem, off-key.\n", "那个哼歌声……是校歌，跑调了。\n") + Rule(L("It's coming from the window table.", "是从靠窗那桌传来的。")) + L(" Pink ribbon.", "粉色蝴蝶结。"), 6f);
            }
            if (!toldInReach && InReach())
            {
                toldInReach = true;
                ambient.Play(AudioSynth.Confirm(), 0.3f, 1.5f);
                Thought(Rule(L("Close enough - signal!", "够近了——发信号！")) + L("\nHOLD RIGHT MOUSE and send it to \"Hums When Scared\".", "\n按住右键，发给“害怕时会哼歌”。"), 7f, true);
            }
            if (taskTime > 50f && !InReach() && DialogueIdle)
            {
                taskTime = 0f;
                Thought(L("Listen. ", "听。") + Rule(L("The humming", "哼歌声")) + L(" - far right corner, by the south windows.", "——最右边的角落，南边窗户旁。"), 6f);
            }
        }

        void ListenToHer()
        {
            if (delivered) { Thought(L("She mouths: thank you.", "她用口型说：谢谢。"), 3.5f, true); return; }
            if (!taskGiven) { Thought(L("She's humming the school anthem. Off-key, eyes on her tray.\nShe looks scared.", "她在哼校歌。跑调了，眼睛盯着餐盘。\n她看起来很害怕。"), 5f, true); return; }
            listened = true;
            Thought(L("It's her. I can't talk to her - ", "就是她。我不能跟她说话——") + Rule(L("they watch who talks", "他们盯着谁在说话")) + L(".\nHOLD RIGHT MOUSE and send the message from here.", "。\n按住鼠标右键，就在这里把信息发出去。"), 7f, true);
        }

        IEnumerator Delivered()
        {
            delivered = true;
            ambient.Play(AudioSynth.Confirm(), 0.4f);
            yield return new WaitForSeconds(1.5f);
            if (hum != null) hum.StopHumming();
            phone.Receive("Hums When Scared", L("...ok. ok. thank you.", "……好。好。谢谢你。"));
            phone.Receive("Hums When Scared", L("tell Triangle I'll be there.", "告诉三角形，我会去的。"));
            yield return new WaitForSeconds(5f);
            phone.Receive("Triangle", L("good. now the STAFF door behind the counters.", "很好。现在去柜台后面的员工通道门。"));
            phone.Receive("Triangle", L("the corner camera looks at the windows every few seconds. that's your moment.", "角落的摄像头每隔几秒就会转向窗户。那就是你的机会。"));
            Thought(Rule(L("Behind the counters, to the STAFF door.", "绕到柜台后面，去员工通道门。")) + L("\nGo when the camera faces the windows.", "\n等摄像头转向窗户再走。"), 9f);
        }

        void UseDoor()
        {
            if (!delivered) { Thought(L("Not yet. Triangle said: the job first.", "还不行。三角说了：先办事。"), 4f, true); return; }
            if (!winning) StartCoroutine(ThroughStaffDoor());
        }

        /// <summary>The STAFF door opens; you step through, it shuts behind you, and the stairs are yours to walk down.</summary>
        IEnumerator ThroughStaffDoor()
        {
            winning = true;
            rig.Lock();
            SetPrompt(null);
            phone.allowRaise = false;
            ambient.Play(AudioSynth.Creak(), 0.6f);
            Quaternion r0 = staffDoorLeaf != null ? staffDoorLeaf.localRotation : Quaternion.identity;
            Quaternion r1 = r0 * Quaternion.Euler(0f, -100f, 0f);
            if (staffDoorLeaf != null)
                for (float k = 0f; k < 1f; k += Time.deltaTime / 0.8f) { staffDoorLeaf.localRotation = Quaternion.Slerp(r0, r1, k); yield return null; }
            yield return Fade(0f, 1f, 0.35f);
            rig.Teleport(throughDoorPoint, 0f);
            throughDoor = true;
            danger = 0f;
            if (staffDoorLeaf != null) staffDoorLeaf.localRotation = r0;   // it shuts behind you
            ambient.Play(AudioSynth.Click(), 0.7f, 0.6f);
            ambient.droneVolume *= 0.5f;
            if (voicesPoint != null)
            {
                voices = voicesPoint.gameObject.AddComponent<AudioSource>();
                voices.clip = AudioSynth.Murmur();
                voices.loop = true;
                voices.spatialBlend = 1f;
                voices.rolloffMode = AudioRolloffMode.Linear;
                voices.minDistance = 1.5f;
                voices.maxDistance = 7f;
                voices.volume = 0.55f;
                voices.dopplerLevel = 0f;
                voices.Play();
            }
            yield return Fade(1f, 0f, 0.6f);
            rig.AllowWalk();
            SetObjective(L("Go down the stairs.\nAt the bottom: the door (E).", "走下楼梯。\n最底下有一扇门（E）。"));
            Thought(L("The door clicks shut behind me. ", "门在身后咔哒一声关上了。") + Rule(L("No camera here.", "这里没有摄像头。")) + L("\nStairs. Going down.", "\n楼梯。往下走。"), 5f, true);
        }

        void UpdateStairs()
        {
            float z = rig.transform.position.z;
            if (!saidPaint && z > 13.5f)
            {
                saidPaint = true;
                Thought(L("Someone painted on the wall, a long time ago:\n", "很久以前，有人在墙上刷了一行字：\n") + Rule(L("WE KEEP THINGS FROM EACH OTHER.", "我们对彼此有所隐瞒。")), 5.5f);
            }
            if (!saidChalk && z > 14.6f)
            {
                saidChalk = true;
                Thought(L("And in chalk: ", "还有粉笔字：") + Rule(L("SEAT 31", "31 号座位")) + L(". '79, '86, '94...\nYear after year. ...And me?", "。'79、'86、'94……\n一年又一年。……那我呢？"), 5.5f);
            }
            if (!saidVoices && z > 16.1f)
            {
                saidVoices = true;
                Thought(L("Voices. Behind that door.\n", "门后有人声。\n") + Rule(L("Nobody is whispering.", "没有人在小声说话。")), 5f, true);
            }
        }

        void PushBottomDoor()
        {
            if (ended) return;
            StartCoroutine(PushDoorRoutine());
        }

        IEnumerator PushDoorRoutine()
        {
            ended = true;
            rig.Lock();
            SetPrompt(null);
            SetObjective("");
            ambient.Play(AudioSynth.Creak(), 0.7f, 0.9f);
            if (bottomDoorHinge != null)
            {
                Quaternion a = bottomDoorHinge.rotation, b = a * Quaternion.Euler(0f, -55f, 0f);
                for (float k = 0f; k < 1f; k += Time.deltaTime / 1.1f)
                {
                    bottomDoorHinge.rotation = Quaternion.Slerp(a, b, k * k * (3f - 2f * k));
                    if (voices != null) voices.volume = Mathf.Lerp(0.55f, 1f, k);
                    yield return null;
                }
            }
            Thought(L("Warm light. A lot of people, talking out loud.\nSomeone says my seat number - like a name.", "暖黄的灯光。很多人，在大声说话。\n有人叫出我的座位号——像在叫一个名字。"), 5f, true);
            yield return new WaitForSeconds(3.5f);
            yield return FinishLevel("stairs", 2.2f);
        }

        // ------------------------------------------------------------------ watchers
        bool AnyCameraSees(Vector3 p)
        {
            if (cameras == null) return false;
            foreach (TeacherVision c in cameras) if (c != null && c.CanSee(p, rig.transform)) return true;
            return false;
        }

        void UpdateDanger()
        {
            bool byMonitor = false, byCamera = false;
            if (phone.IsExposed)
            {
                Vector3 p = phoneExposurePoint.position;
                byMonitor = monitorVision != null && monitor != null && monitor.Running && monitorVision.CanSee(p, rig.transform);
                byCamera = !byMonitor && AnyCameraSees(p);
            }
            if (AccumulateDanger(byMonitor || byCamera ? 1f / Mathf.Max(0.05f, noticeSeconds) : 0f))
            {
                if (byMonitor) Strike(L("...Was that a phone?", "……那是手机吗？"));
                else Strike(L("A device has been observed in the dining hall.", "食堂内发现一台电子设备。"), true);
            }
        }

        void UpdateKitchen()
        {
            if (kitchenGrace > 0f) kitchenGrace -= Time.deltaTime;
            bool inside = In(rig.transform.position, kitchenZone);
            if (inside && !toldKitchen)
            {
                toldKitchen = true;
                Thought(L("Behind the counters. ", "到柜台后面了。") + Rule(L("Keep moving", "别停")) + L(" - before it turns back.", "——趁它还没转回来。"), 5f, true);
            }
            bool seen = false;
            if (inside && kitchenGrace <= 0f)
            {
                Vector3 head = rig.eye.position;
                seen = AnyCameraSees(head) || (monitorVision != null && monitor != null && monitor.Running && monitorVision.CanSee(head, rig.transform));
            }
            kitchenSeen = seen ? kitchenSeen + Time.deltaTime : Mathf.Max(0f, kitchenSeen - Time.deltaTime);
            if (kitchenSeen > 0.7f)
            {
                kitchenSeen = 0f;
                kitchenGrace = 2.5f;
                Strike(L("Students are reminded that the kitchen is not a destination.", "提醒各位同学：厨房不是目的地。"), true);
                if (!ended) rig.Teleport(kitchenReturnPoint, kitchenReturnYaw);
            }
        }

        void UpdateAmbientLines()
        {
            paTimer -= Time.deltaTime;
            if (paTimer <= 0f && DialogueIdle)
            {
                paTimer = Random.Range(55f, 75f);
                PA(Loc.T(ambientPA[paIndex % ambientPA.Length]), 6.5f);
                paIndex++;
            }
            if (monitorTalk > 0f) monitorTalk -= Time.deltaTime;
            if (monitor != null && monitorTalk <= 0f && DialogueIdle && !phone.IsExposed)
            {
                if (Vector3.Distance(monitor.transform.position, rig.transform.position) < 2.2f && In(rig.transform.position, diningZone))
                {
                    monitorTalk = 30f;
                    string[] lines = { "Chew. Thirty-two times.", "Why aren't you eating?", "Eyes forward. Thank you." };
                    Authority(Loc.T(lines[Random.Range(0, lines.Length)]), 4f);
                }
            }
        }

        static bool In(Vector3 p, Vector4 r) { return p.x >= r.x && p.x <= r.z && p.z >= r.y && p.z <= r.w; }

        protected override IEnumerator OnCaught()
        {
            phone.allowRaise = false;
            phone.CancelTask();
            rig.Lock();
            Authority(L("Device. Now.", "设备。交出来。"), 4f, true);
            if (monitor != null)
            {
                Vector3 to = rig.transform.position - monitor.transform.position; to.y = 0f;
                Vector3 stand = rig.transform.position - to.normalized * 0.9f;
                monitor.Approach(stand, rig.transform);
                float t = 0f;
                yield return new WaitForSeconds(0.4f);
                while (!monitor.HasArrived && t < 7f) { t += Time.deltaTime; yield return null; }
            }
            yield return WaitIdle();
            PA(L("A device has been retired. Thank you for your cooperation.", "一台设备已被回收。感谢您的配合。"), 5f);
            yield return WaitIdle();
            yield return Caught("confiscated");
        }
    }
}
