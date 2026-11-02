// @role Chapter 4 Picture Day (Wednesday): a gym full of students milling about; every so often a CLASS PHOTO countdown - get to your spot, phone down, hold still, smile. Between photos: CAMERA (1045), photograph 3 things without being reported
// @calls LevelDirectorBase, PatrolAI, TeacherVision, PhoneController, PhotoTarget, Readable
// @tune freeSeconds:float:20-90:free time between two class photos
// @tune callSeconds:float:5-40:countdown to get to your spot
// @tune shutterHearing:float:1-8:how close a prefect must be to hear the shutter (m) - 2.2 = right next to you
// @tune flashRange:float:4-20:how far a flash is seen (any direction, line of sight)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 4 "Picture Day" (the gymnasium in the Cubberley Wing, Wednesday - "day three").
    ///  - The class mills about the gym. Every minute or so: CLASS PHOTO. A countdown; everyone walks to their place on
    ///    the risers. You must be on YOUR spot (the yellow X - it MOVES before every photo) when it ends: phone down, standing
    ///    still, HOLD SPACE to smile. Missing, moving, not smiling or a phone in the picture = a warning (a report).
    ///    The photographer always finds a reason to take it again ("someone blinked").
    ///  - Only in the free time between photos can you use the phone. Two PREFECTS walk the floor and REPORT.
    ///  - CAMERA (code 10:45 -> 1045): photograph the report board, the LIVE screen with nobody on it and the April 1967
    ///    photo, then send them to Triangle. The shutter clicks (prefects close by hear it). The FLASH switches itself
    ///    back ON every time the phone comes out (A / D turns it off).
    ///  - After sending: the final photo. Get it right = WIN.
    /// </summary>
    public class PictureDayDirector : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public PatrolAI photographer;
        public TeacherVision photographerVision;
        public PatrolAI[] prefects;
        public TeacherVision[] prefectVisions;
        public Transform[] prefectPhotoSpots;
        public PatrolAI[] walkers;
        [Tooltip("All places on the risers (one more than there are walkers). Each photo, the X picks one for you.")]
        public Transform[] walkerSpots;
        public Transform spotMarker;
        public float walkerCallSpeed = 1.9f;
        public float walkerFreeSpeed = 1.25f;
        public Transform cameraSpot;
        public Transform playerSpot;
        public Light photoFlash;

        [Header("Rules")]
        public float freeSeconds = 42f;
        public float firstFreeSeconds = 55f;
        public float callSeconds = 10f;
        public float spotRadius = 0.6f;
        public float shutterHearing = 2.2f;
        public float flashRange = 15f;

        enum Phase { Intro, Free, Call, Shot, Done }
        Phase phase = Phase.Intro;
        float phaseLeft, prefectTalk, paTimer = 50f;
        int paIndex, photosShot;
        bool unlockedCam, allPhotos, taskGiven, sent, finalCall, toldFlash, toldRaise, toldCallRules;
        bool board, screen, trophy;
        Vector3 lastPos;
        float speed;
        Text countdown;
        int playerPlace = -1, calls;
        string placeText = "";

        static readonly string[] ambientPA =
        {
            "Reminder: a citizen card must be carried at all times. Including in dreams.",
            "Prefects are thanked for their service. Students are thanked for being observed.",
            "If you see something, say something. If you see nothing, say that too.",
            "Please do not blink during your photograph. Blinking will be photographed again.",
        };
        static readonly string[] retakes =
        {
            "...Someone blinked. Again in a minute. Free time - don't wander far.",
            "...Someone wasn't smiling with their whole face. Again in a minute.",
            "...A shadow. On a chin. Unacceptable. Once more, in a minute.",
            "...Lovely. Lovely. Except one of you. Again in a minute.",
        };

        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "PHOTOGRAPHER";
            authorityPitch = 1.05f;
            phone.hasPhone = true;
            phone.allowRaise = false;
            phone.phoneClock = "10:16";
            phone.flashOnEveryRaise = true;
            phone.appUnlocked[PhoneController.AppPhotos] = true;
            phone.appUnlocked[PhoneController.AppMessages] = true;
            phone.appUnlocked[PhoneController.AppContacts] = true;
            phone.lockNotification = L("No new messages.", "没有新信息。");
            phone.AddHistory("Never Blinks", L("three lights answered last night. three.", "昨晚有三盏灯回应了。三盏。"), false);
            phone.AddHistory("Triangle", L("wednesday is picture day. bring the phone. keep it low.", "周三是拍照日。带上手机。放低一点。"), false);
            phone.onMessageArrived = () => { if (!phone.IsExposed) PhoneBuzz(); };
            phone.onShutter = OnShutter;
            if (photoFlash != null) photoFlash.enabled = false;
            BuildCountdown();
        }

        protected override void Start()
        {
            base.Start();
            if (photographer != null) photographer.Begin();
            if (prefects != null) foreach (PatrolAI p in prefects) if (p != null) p.Begin();
            if (walkers != null) foreach (PatrolAI w in walkers) if (w != null) w.Begin();
            lastPos = rig.transform.position;
            rig.Lock();
            StartCoroutine(Opening());
        }

        IEnumerator Opening()
        {
            yield return ChapterCard("CHAPTER 4", "PICTURE DAY", "WEDNESDAY  -  10:15");
            PA(L("Good morning, Clearview. Today is Picture Day. Classes will be photographed from ten forty-five. Ten. Forty-five.", "早上好，明鉴学院。今天是拍照日。各班从十点四十五分开始拍照。十。四十五。"), 7f);
            PA(L("Your photograph will be printed on your new Citizen Card. When you hear the countdown, go to your place. Smile.", "你的照片将印在新的公民卡上。听到倒计时，请回到你的位置。微笑。"), 7f);
            yield return Wait(1f);
            rig.AllowWalk();
            Thought(L("Wednesday. Monday it was rules. Yesterday we all stood up and said the motto together. Twice.\nToday: photos for our new ", "星期三。星期一讲规矩。昨天我们全体起立，一起念校训。念了两遍。\n今天：给新的") + Rule(L("citizen cards", "公民卡")) + L(".", "拍照。"), 8f);
            Thought(Rule(L("Red armbands", "红袖章")) + L(" = prefects. They don't take phones. They ", " = 学生督察。他们不收手机。他们") + Rule(L("report", "举报")) + L(".\nWhen the countdown starts, I have to be ", "。\n倒计时一开始，我就得回到") + Rule(L("on my spot", "我的位置")) + L(".", "上。"), 8f);
            phone.Receive("Triangle", L("this phone has a camera. the code is when they photograph us.", "这部手机有相机。密码是他们给我们拍照的时间。"));
            phone.Receive("Triangle", L("three things: the board on the east wall. the 'live' screen. the old photo in the trophy case.", "三样东西：东墙的布告板。那块“直播”屏幕。奖杯柜里的老照片。"));
            phone.Receive("Triangle", L("only between photos. and the flash turns itself back on every time. turn it OFF.", "只能在两次拍照之间拍。还有，闪光灯每次都会自己重新打开。记得关掉。"));
            yield return WaitIdle();
            phase = Phase.Free;
            phaseLeft = firstFreeSeconds;
            StartCoroutine(Reminder(L("My phone buzzed. ", "手机震了。") + Rule(L("HOLD RIGHT MOUSE", "按住鼠标右键")) + L(" - where no armband is looking.", "——在没有红袖章看着的地方。"), () => phone.RaisedOnce || ended, 24f));
        }

        // ------------------------------------------------------------------ main loop
        protected override void LevelUpdate()
        {
            Vector3 p = rig.transform.position;
            Vector3 dp = p - lastPos; dp.y = 0f;
            speed = Mathf.Lerp(speed, dp.magnitude / Mathf.Max(0.0001f, Time.deltaTime), 0.3f);
            lastPos = p;

            PlayerRig.Mode m = rig.CurrentMode;
            phone.allowRaise = phone.hasPhone && m == PlayerRig.Mode.Walk && phase != Phase.Shot && phase != Phase.Done;
            rig.freezeMovement = phone.IsExposed;
            if (phone.IsExposed && !toldRaise)
            {
                toldRaise = true;
                Thought(L("While I look at the phone, ", "看手机的时候，") + Rule(L("I stand still", "我站着不动")) + L(". Anyone looking can see it.", "。谁看过来都能看见。"), 6f);
            }
            if (!toldFlash && phone.Screen == PhoneController.PhoneScreen.Camera && phone.IsUp)
            {
                toldFlash = true;
                Thought(L("The camera. Aim with the middle of the screen, ", "相机。用屏幕中间对准，") + Rule(L("LEFT MOUSE = shutter", "鼠标左键 = 快门")) + L(".\n", "。\n") + Rule(L("The flash is ON", "闪光灯是开着的")) + L(" - A / D turns it off. Every time.", "——按 A / D 关掉。每次都要。"), 8f, true);
            }
            UpdatePhase();
            UpdateStory();
            UpdateObjective();
            UpdateDanger();
            UpdateAmbient();
            SetPrompt(PromptNow());
        }

        bool OnSpot()
        {
            if (playerSpot == null) return true;
            Vector3 a = rig.transform.position, b = playerSpot.position;
            if (a.y < b.y - 0.15f) return false;
            a.y = b.y = 0f;
            return Vector3.Distance(a, b) < spotRadius;
        }

        string PromptNow()
        {
            if (rig.CurrentMode != PlayerRig.Mode.Walk) return null;
            if (phase == Phase.Call && OnSpot() && !phone.IsExposed)
                return GameInput.SpaceHeld() ? L("SMILING  :)   (keep holding SPACE, don't move)", "微笑中  :)  （按住空格别松，别动）") : L("On your spot. HOLD SPACE to smile.", "到位了。按住空格微笑。");
            if (phone.IsExposed && phone.Screen == PhoneController.PhoneScreen.Camera)
            {
                PhotoTarget t = phone.Aimed;
                if (t != null && !t.taken) return L("IN FRAME  -  LEFT MOUSE: take the photo", "已入框  -  鼠标左键：拍照");
                return null;
            }
            return RaycastInteract();
        }

        // ------------------------------------------------------------------ the class-photo cycle
        void UpdatePhase()
        {
            if (phase == Phase.Intro || phase == Phase.Done) { countdown.text = ""; return; }
            if (phase == Phase.Shot) return;
            phaseLeft -= phase == Phase.Free && allPhotos ? GameSpeed.Dt : Time.deltaTime;   // only pure waiting speeds up (2x)
            if (phase == Phase.Free)
            {
                countdown.text = L("next class photo in ", "下次合照：") + Mathf.CeilToInt(Mathf.Max(0f, phaseLeft)) + L(" s", " 秒");
                countdown.color = phaseLeft < 10f ? new Color(1f, 0.6f, 0.4f) : new Color(1f, 0.85f, 0.55f, 0.6f);
                countdown.fontSize = phaseLeft < 10f ? 40 : 28;
                if (phaseLeft <= 0f) StartCall();
                return;
            }
            // call: count down
            countdown.text = L("CLASS PHOTO  ", "班级合照  ") + Mathf.CeilToInt(Mathf.Max(0f, phaseLeft));
            countdown.fontSize = 52;
            countdown.color = Mathf.Repeat(phaseLeft, 0.5f) > 0.25f ? new Color(1f, 0.3f, 0.22f) : new Color(1f, 0.75f, 0.6f);
            if (phaseLeft <= 0f) StartCoroutine(TakeClassPhoto());
        }

        void StartCall()
        {
            phase = Phase.Call;
            phaseLeft = callSeconds;
            finalCall = sent;
            ambient.Play(AudioSynth.Chime(), 0.4f);
            PA(finalCall ? L("Final class photograph. Take your places. Make it perfect.", "最后一次合照。各就各位。务必完美。")
                         : L("Class photograph! Take your places. Quickly.", "拍合照了！各就各位。快。"), 4f, true);
            if (finalCall) Thought(L("The last one. The X is back where it started: ", "最后一张。X 回到了最开始的地方：") + Rule(L("back row, by the window.", "最后一排，靠窗。")), 4f);
            // the photographer moves the X: a new place for you every photo (the first one: back row, window end)
            if (walkerSpots != null && walkerSpots.Length > 0)
            {
                int n = walkerSpots.Length;
                // the first photo and the FINAL one: back row, window end (where Triangle says "it's always that spot")
                bool fixedSpot = calls == 0 || finalCall;
                int pick = fixedSpot ? n - 1 : Random.Range(0, n);
                if (!fixedSpot && pick == playerPlace) pick = (pick + 1 + Random.Range(0, n - 1)) % n;
                playerPlace = pick;
                Vector3 p = walkerSpots[pick].position;
                if (playerSpot != null) playerSpot.position = p;
                if (spotMarker != null) spotMarker.position = p;
                placeText = PlaceText(p);
                ambient.Play(AudioSynth.Ping(), 0.3f, 1.3f);
                // everyone else takes the other places
                int w = 0;
                for (int i = 0; i < n && walkers != null && w < walkers.Length; i++)
                {
                    if (i == pick) continue;
                    PatrolAI wk = walkers[w++];
                    if (wk == null) continue;
                    UnityEngine.AI.NavMeshAgent ag = wk.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (ag != null) ag.speed = walkerCallSpeed;
                    wk.RunRoute(new[] { walkerSpots[i].position }, new[] { 999f }, new[] { 180f }, null);
                }
            }
            calls++;
            if (!toldCallRules)
            {
                toldCallRules = true;
                Thought(L("Everyone is walking to the risers. ", "所有人都在往台阶上走。") + Rule(L("My spot = the YELLOW X.", "我的位置 = 那个黄色的 X。")) +
                        L("\nPhone DOWN. Stand still. ", "\n放下手机。站着别动。") + Rule(L("HOLD SPACE to smile.", "按住空格微笑。")), 7f);
            }
            else if (!finalCall) Thought(L("The X has moved: ", "X 换地方了：") + Rule(placeText), 4f);
            if (prefects != null)
                for (int i = 0; i < prefects.Length; i++)
                    if (prefects[i] != null && prefectPhotoSpots != null && i < prefectPhotoSpots.Length)
                        prefects[i].RunRoute(new[] { prefectPhotoSpots[i].position }, new[] { 999f }, new[] { prefectPhotoSpots[i].eulerAngles.y }, null);
            if (photographer != null && cameraSpot != null)
                photographer.RunRoute(new[] { cameraSpot.position }, new[] { 999f }, new[] { 0f }, null);
        }

        IEnumerator TakeClassPhoto()
        {
            phase = Phase.Shot;
            countdown.text = "";
            Authority(L("Three... two... one... say 'Clearview!'", "三……二……一……说“明鉴！”"), 2.6f, true);
            yield return Wait(2.2f);
            StudentSay("EVERYONE", L("CLEARVIEW!", "明鉴！"), 1.4f, true);
            yield return Wait(0.35f);
            // the moment the shutter goes: judge
            bool onSpot = OnSpot(), phoneDown = !phone.IsExposed, smiling = GameInput.SpaceHeld(), still = speed < 0.35f;
            ambient.Play(AudioSynth.Click(), 0.9f, 0.8f);
            if (photoFlash != null) photoFlash.enabled = true;
            fade.color = Color.white;
            yield return Wait(0.12f);
            if (photoFlash != null) photoFlash.enabled = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f) { fade.color = new Color(1f, 1f, 1f, 1f - t); yield return null; }
            fade.color = new Color(0f, 0f, 0f, 0f);
            photosShot++;
            bool ok = onSpot && phoneDown && smiling && still;
            if (!onSpot) Strike(L("Seat 31 is missing from the photograph. A prefect has been informed.", "31 号座的同学不在照片里。已通知学生督察。"), true);
            else if (!phoneDown) Strike(L("Is that a PHONE in my photograph?", "我的照片里是不是有一部手机？"));
            else if (!still) Strike(L("You moved. Seat 31 is a blur. Report filed.", "你动了。照片里的 31 号座糊成了一片。举报已登记。"), true);
            else if (!smiling) Strike(L("Seat 31. That was not a smile. Report filed.", "31 号座。那不叫微笑。举报已登记。"), true);
            if (ended) yield break;
            yield return Wait(1.2f);
            if (ok && finalCall)
            {
                phase = Phase.Done;
                yield return FinalPhoto();
                yield break;
            }
            if (ok) Authority(Loc.T(retakes[(photosShot - 1) % retakes.Length]), 5f);
            else Authority(L("Again in a minute. All of you. Thank Seat 31.", "一分钟后重拍。所有人。谢谢 31 号座。"), 4.5f);
            // free time: everyone wanders off again
            if (walkers != null)
                foreach (PatrolAI w in walkers)
                {
                    if (w == null) continue;
                    UnityEngine.AI.NavMeshAgent ag = w.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (ag != null) ag.speed = walkerFreeSpeed;
                    w.ResumeRoam();
                }
            if (prefects != null) foreach (PatrolAI p in prefects) if (p != null) p.ResumeRoam();
            phase = Phase.Free;
            phaseLeft = freeSeconds;
            StartCoroutine(PhotographerFreeTime());
        }

        /// <summary>"middle row, door end" - where the X is, in words.</summary>
        string PlaceText(Vector3 p)
        {
            string row = p.y > 0.5f ? L("BACK row", "最后一排") : (p.y > 0.3f ? L("MIDDLE row", "中间一排") : L("FRONT row", "第一排"));
            string side = p.x < 5.2f ? L("by the WINDOWS", "靠窗那头") : (p.x > 10.8f ? L("the far (EAST) end", "靠东墙那头") : L("in the MIDDLE", "正中间"));
            return row + L(", ", "，") + side;
        }

        /// <summary>Between photos the photographer fiddles with his lights (and turns his back on the hall now and then).</summary>
        IEnumerator PhotographerFreeTime()
        {
            if (photographer == null || cameraSpot == null) yield break;
            Vector3 home = cameraSpot.position;
            yield return Wait(Random.Range(4f, 9f));
            while (phase == Phase.Free && !ended)
            {
                Vector3 lamp = home + new Vector3(Random.value < 0.5f ? -2.6f : 2.6f, 0f, 1.1f);
                photographer.RunRoute(new[] { lamp, home }, new[] { 5f, Random.Range(6f, 10f) }, new[] { 0f, 180f }, null);
                while (!photographer.RouteDone && phase == Phase.Free && !ended) yield return null;
            }
        }

        IEnumerator FinalPhoto()
        {
            rig.Lock();
            phone.allowRaise = false;
            SetPrompt(null);
            SetObjective("");
            Authority(L("...Perfect. Nobody move. That one is for the cards.", "……完美。谁都别动。这张就印在卡上。"), 4f, true);
            Thought(L("In the photo, I'm in the back row, by the window.\nSame as every year. Same as every face before mine.", "照片里，我在最后一排，靠窗。\n和每一年一样。和我之前的每一张脸一样。"), 6f);
            PA(L("Thank you, Clearview. Your cards will be ready tomorrow. Some cards will have a red X. Please do not ask what it means.", "谢谢你，明鉴。你们的卡明天就能领取。有些卡上会有一个红色的 X。请不要问那是什么意思。"), 7f);
            yield return WaitIdle();
            yield return FinishLevel("photo", 2f);
        }

        // ------------------------------------------------------------------ story
        void UpdateStory()
        {
            if (!unlockedCam && phone.IsUnlocked(PhoneController.AppCamera))
            {
                unlockedCam = true;
                Thought(L("10:45. It opened. ", "10:45。打开了。") + Rule(L("Three photos", "三张照片")) + L(" - between the class photos, never during.", "——要在两次合照之间拍，别在倒计时里拍。"), 6f);
            }
            if (!allPhotos && board && screen && trophy)
            {
                allPhotos = true;
                phone.Receive("Triangle", L("that's all three. send them to me. Messages.", "三张都齐了。发给我。用“信息”。"));
            }
            if (allPhotos && !taskGiven && phone.UnreadCount == 0)
            {
                taskGiven = true;
                phone.StartTask(new PhoneTask
                {
                    forward = true,
                    header = "Forward to...",
                    options = new[] { "Lent Me An Eraser", "Triangle", "Hums When Scared", "Never Blinks" },
                    correct = 1,
                    sendText = L("[3 photos]", "[3 张照片]"),
                    onDone = i => StartCoroutine(Sent()),
                    typeSeconds = 3f,
                });
            }
        }

        IEnumerator Sent()
        {
            sent = true;
            ambient.Play(AudioSynth.Confirm(), 0.4f);
            yield return Wait(1.5f);
            phone.Receive("Triangle", L("got them. look at the board again: 20 reports. 3 reporters. who wrote the other 17?", "收到了。再看看那块板：20 份举报。3 个举报员。剩下那 17 份是谁写的？"));
            phone.Receive("Triangle", L("tomorrow after school: the library. it's in the cubberley wing too. same month as that photo.", "明天放学后：图书馆。它也在库伯利楼。和那张照片同一个月。"));
            phone.Receive("Triangle", L("now one perfect photo, and they let us go. back row, by the window. it's always that spot.", "现在拍一张完美的合照，他们就放我们走。最后一排，靠窗。一直都是那个位置。"));
            if (phase == Phase.Free) phaseLeft = Mathf.Min(phaseLeft, 15f);
        }

        void UpdateObjective()
        {
            if (phase == Phase.Done || phase == Phase.Intro) return;
            if (phase == Phase.Call || phase == Phase.Shot)
            {
                SetObjective(L("CLASS PHOTO! Before the countdown ends:\nstand on the YELLOW X - ", "班级合照！倒计时结束前：\n站到黄色的 X 上——") + placeText + L(".\nPhone DOWN. Don't move. HOLD SPACE to smile.", "。\n放下手机。别动。按住空格微笑。"));
                return;
            }
            string next = "\n" + L("(Free time until the next class photo - see the timer.)", "（自由活动，直到下次合照——看上方的计时。）");
            if (sent) { SetObjective(L("Wait for the final class photo.\nStay near the risers: the X will move again.", "等最后一次合照。\n待在台阶附近：X 还会换位置。") + next); return; }
            if (allPhotos) { SetObjective(L("Send the 3 photos to Triangle.\nMessages -> A / D pick Triangle -> Left Mouse, then HOLD Left Mouse.", "把 3 张照片发给三角。\n信息 -> A / D 选三角 -> 鼠标左键，再按住左键。") + next); return; }
            if (unlockedCam)
            {
                string Box(bool b) { return b ? "[x] " : "[  ] "; }
                SetObjective(L("Photograph 3 things (Camera, LEFT MOUSE = shutter):\n", "拍下 3 样东西（相机，鼠标左键 = 快门）：\n")
                    + Box(board) + L("the report board (EAST wall)\n", "举报布告板（东墙）\n")
                    + Box(screen) + L("the LIVE screen (WEST wall, by the door)\n", "“直播”屏幕（西墙，门边）\n")
                    + Box(trophy) + L("the 1967 photo (trophy case, SOUTH wall)\n", "1967 年的照片（奖杯柜，南墙）\n")
                    + L("Flash OFF every time (A / D). No prefect close when you click.", "每次都要关闪光灯（A / D）。按快门时别让督察在附近。") + next);
                return;
            }
            if (phone.RaisedOnce) SetObjective(L("Unlock CAMERA.\nCode = when your class is photographed (10:45 -> 4 digits).", "解锁“相机”。\n密码 = 你们班拍照的时间（10:45 → 四位数）。") + next);
            else SetObjective(L("Read Triangle's messages: HOLD RIGHT MOUSE.\nNobody may see the phone - prefects (red armbands), the photographer.", "读三角的信息：按住鼠标右键。\n不能让人看到手机——学生督察（红袖章）、摄影师。") + next);
        }

        // ------------------------------------------------------------------ camera
        void OnShutter(PhotoTarget t, bool flash)
        {
            if (ended || phase == Phase.Done) return;
            float d;
            PatrolAI near = NearestPrefect(out d);
            bool heard = near != null && d < shutterHearing;
            bool seen = flash && FlashSeen();
            if (t != null)
            {
                if (t.id == "board" && !board) { board = true; Thought(L("Twenty reports in three days. Only three of us were asked to report.\nThe other seventeen... ", "三天，二十份举报。只有三个人是被指派去举报的。\n剩下那十七份……") + Rule(L("volunteered", "是自愿的")) + L(".", "。"), 6.5f); }
                if (t.id == "screen" && !screen) { screen = true; Thought(L("LIVE. The Founder will speak to us at 10:45.\nThe screen shows ", "直播。创办人将在 10:45 对我们讲话。\n屏幕上") + Rule(L("nothing", "什么都没有")) + L(". Just snow.", "。只有雪花。"), 6.5f); }
                if (t.id == "trophy" && !trophy) { trophy = true; Thought(L("April 1967. The Cubberley Wing opens.\nEveryone in the photo holds the same hand up, ", "1967 年 4 月。库伯利楼启用。\n照片里每个人都举着同一只手，") + Rule(L("cupped, at the shoulder", "弯成杯状，举在肩旁")) + L(".", "。"), 7f); }
            }
            if (seen)
            {
                Strike(L("A flash has been observed in the gymnasium. Only the official photographer may flash. Report filed.", "体育馆内发现闪光。只有官方摄影师可以使用闪光灯。举报已登记。"), true);
                return;
            }
            if (heard)
            {
                StudentSay("PREFECT", L("...Was that a shutter?", "……刚才是快门声吗？"), 2.5f, true);
                Strike(L("A shutter has been reported by a prefect. Thank you, prefect.", "一名学生督察举报了一次快门声。谢谢你，督察。"), true);
                return;
            }
            if (flash) Thought(L("The flash went off. Nobody turned. ...Lucky.\n", "闪光灯亮了。没人回头。……走运。\n") + Rule(L("A / D: flash OFF - every time I take the phone out.", "A / D：关闪光灯——每次拿出手机都要关。")), 5f);
        }

        PatrolAI NearestPrefect(out float dist)
        {
            dist = 999f;
            PatrolAI best = null;
            if (prefects == null) return null;
            foreach (PatrolAI p in prefects)
            {
                if (p == null) continue;
                float d = Vector3.Distance(p.transform.position, rig.transform.position);
                if (d < dist) { dist = d; best = p; }
            }
            return best;
        }

        bool FlashSeen()
        {
            Vector3 p = rig.eye.position;
            if (photographerVision != null && LineOfSight(photographerVision, p)) return true;
            if (prefectVisions != null) foreach (TeacherVision v in prefectVisions) if (v != null && LineOfSight(v, p)) return true;
            return false;
        }

        bool LineOfSight(TeacherVision v, Vector3 p)
        {
            Vector3 e = v.EyePosition;
            Vector3 to = p - e;
            float d = to.magnitude;
            if (d > flashRange) return false;
            foreach (RaycastHit h in Physics.RaycastAll(e, to / d, d - 0.05f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(v.transform) || h.transform.IsChildOf(rig.transform)) continue;
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ watchers
        void UpdateDanger()
        {
            bool byPrefect = false, byPhotographer = false;
            if (phone.IsExposed)
            {
                Vector3 p = phoneExposurePoint.position;
                byPhotographer = photographerVision != null && photographerVision.CanSee(p, rig.transform);
                if (!byPhotographer && prefectVisions != null)
                    foreach (TeacherVision v in prefectVisions) if (v != null && v.CanSee(p, rig.transform)) byPrefect = true;
            }
            if (AccumulateDanger(byPrefect || byPhotographer ? 1f / Mathf.Max(0.05f, noticeSeconds) : 0f))
            {
                if (byPhotographer) Strike(L("No phones in my pictures. ...I'm telling the office.", "我的照片里不许有手机。……我要告诉办公室。"));
                else
                {
                    StudentSay("PREFECT", L("Device. I saw it. I'm writing it down.", "设备。我看见了。我记下来了。"), 2.5f, true);
                    Strike(L("A device has been reported in the gymnasium. Thank you, prefect.", "体育馆内有人举报了一台设备。谢谢你，督察。"), true);
                }
            }
        }

        void UpdateAmbient()
        {
            paTimer -= GameSpeed.Dt;
            if (paTimer <= 0f && DialogueIdle && phase == Phase.Free)
            {
                paTimer = Random.Range(50f, 70f);
                PA(Loc.T(ambientPA[paIndex % ambientPA.Length]), 6f);
                paIndex++;
            }
            if (prefectTalk > 0f) prefectTalk -= Time.deltaTime;
            if (prefectTalk <= 0f && DialogueIdle && !phone.IsExposed && phase == Phase.Free)
            {
                float d;
                PatrolAI p = NearestPrefect(out d);
                if (p != null && d < 2.2f)
                {
                    prefectTalk = 28f;
                    string[] lines = { "Card. ...Oh, you don't have it yet. I'll remember you.", "Smile. It's for your card.", "Strength through discipline." };
                    StudentSay("PREFECT", Loc.T(lines[Random.Range(0, lines.Length)]), 3.5f);
                }
            }
        }

        void BuildCountdown()
        {
            Canvas c = UIFactory.MakeCanvas("PhotoCountdown", 41);
            c.transform.SetParent(transform, false);
            countdown = UIFactory.AddText(UIFactory.Rect("Countdown", c.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -275f), new Vector2(900f, 70f)), "", 46, Color.white, TextAnchor.MiddleCenter);
            countdown.fontStyle = FontStyle.Bold;
            UIFactory.AddShadow(countdown, new Color(0f, 0f, 0f, 0.85f), 3f);
        }

        protected override IEnumerator OnCaught()
        {
            phase = Phase.Done;
            countdown.text = "";
            phone.allowRaise = false;
            phone.CancelTask();
            rig.Lock();
            PA(L("Report number twenty-one: Seat 31. A prefect will collect the student.", "第二十一份举报：31 号座。一名督察将前来带走该生。"), 5f, true);
            float d;
            PatrolAI p = NearestPrefect(out d);
            if (p != null)
            {
                Vector3 to = rig.transform.position - p.transform.position; to.y = 0f;
                p.Approach(rig.transform.position - to.normalized * 0.9f, rig.transform);
                float t = 0f;
                yield return Wait(0.4f);
                while (!p.HasArrived && t < 7f) { t += Time.deltaTime; yield return null; }
            }
            StudentSay("PREFECT", L("Come with me. You'll get your photo taken in the office.", "跟我走。你的照片去办公室拍。"), 4f);
            yield return WaitIdle();
            yield return Caught("reported");
        }
    }
}
