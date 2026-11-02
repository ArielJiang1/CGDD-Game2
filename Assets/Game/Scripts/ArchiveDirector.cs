// @role Chapter 5 The Archive (Thursday): a buzz gives you away -> SILENT MODE (the little bell, M); the noise meter (footsteps, Ctrl = quiet); the oldest book's stamp (SEARCH = 0467); search words you READ; the old books awaiting repair - turning the pages is louder than walking
// @calls LevelDirectorBase, PatrolAI, TeacherVision, PhoneController, Readable, Interactable, AudioSynth, Pigeon
// @tune walkDoubling:float:0.5-6:walking without a break doubles the noise rate every this many seconds
// @tune walkMaxFactor:float:1-10:cap on that build-up
// @tune pigeonNear:float:1-5:closer than this to the window when she comes to shoo the pigeons = a warning (m)
// @tune noiseWalk:float:0-0.3:meter per second while walking
// @tune noiseSneak:float:0-0.1:meter per second while walking quietly (Ctrl / C)
// @tune noiseRun:float:0.1-1:meter per second while running (Shift)
// @tune noiseDecay:float:0.05-0.5:meter drain per second while standing still
// @tune flipNoise:float:0.02-0.4:meter per page turned (slow)
// @tune silentFailChance:float:0-1:chance the silent switch quietly fails each time the phone comes out
// @tune fastFlipFactor:float:1-5:a page turned within fastFlipSeconds of the last one is this much louder
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PhoneGame
{
    /// <summary>
    /// Chapter 5 "The Archive" (the library in the Cubberley Wing, Thursday after school - "day four").
    ///  1. The first message BUZZES out loud. The librarian comes looking: a warning. Only now does anyone mention the
    ///     little bell in the corner of the phone screen: SILENT MODE (phone up, M).
    ///     The old switch is unreliable: each time the phone comes out it may switch itself off (only the bell shows it),
    ///     and the school's alerts arrive every half minute or so.
    ///  2. Then the NOISE METER (top right): walking fills it, running (Shift) fills it fast, walking quietly
    ///     (Ctrl / C, half speed) barely; standing still drains it; closer to her = louder. Full = "Shhh." = a warning.
    ///     A message arriving while the phone is NOT silent = a loud spike.
    ///  3. SEARCH, code = the stamp in the oldest book on the returns cart: RECEIVED APR 1967 -> 0467.
    ///     Search only knows words you have READ: 1967 -> DAY FIVE -> "Records, vol. 31", shelved with the OLD BOOKS
    ///     AWAITING REPAIR (north-east, behind the rope). Students may not touch them.
    ///  4. Open it and turn the pages (A / D). Every page RUSTLES - louder than footsteps, and much louder if you turn
    ///     them fast. Being seen there, or with the book, = a warning (and you're sent out). Read to the last page. WIN.
    /// </summary>
    public class ArchiveDirector : LevelDirectorBase
    {
        [Header("Scene references (filled by the scene builder)")]
        public PatrolAI librarian;
        public TeacherVision librarianVision;
        [Tooltip("A second librarian who walks the aisles the whole time.")]
        public PatrolAI assistant;
        public TeacherVision assistantVision;
        public Transform deskSpot;
        public Interactable returnsCart;
        public Interactable oldBook;

        [Header("Zones")]
        [Tooltip("The old books awaiting repair, behind the rope (x min, z min, x max, z max). Students may not be seen here.")]
        public Vector4 restrictedZone = new Vector4(9.8f, 9.7f, 14f, 12f);
        public Vector3 restrictedReturn = new Vector3(8f, 0f, 8.6f);

        [Header("Noise meter")]
        public float noiseWalk = 0.08f;
        public float noiseSneak = 0.012f;
        public float noiseRun = 0.45f;
        public float noiseDecay = 0.12f;
        public float nearLibrarian = 6f;
        public float buzzNoise = 0.6f;

        [Header("Turning pages")]
        public float flipNoise = 0.16f;
        public float fastFlipSeconds = 0.7f;
        public float fastFlipFactor = 2.6f;
        [Header("Pigeons (west window ledges)")]
        [Tooltip("Inside the windows: where the librarian stands to shoo the pigeons.")]
        public Transform pigeonSpot;
        [Tooltip("Seconds between the flock getting noisy.")]
        public Vector2 pigeonEvery = new Vector2(40f, 60f);
        [Tooltip("Closer than this to the window when she comes = a warning.")]
        public float pigeonNear = 2.6f;
        [Tooltip("Walking without a break: the noise rate doubles every this many seconds (capped).")]
        public float walkDoubling = 2f;
        public float walkMaxFactor = 5f;
        [Header("Silent mode (unreliable)")]
        [Range(0f, 1f)] public float silentFailChance = 0.3f;
        public Vector2 alertEvery = new Vector2(28f, 45f);

        bool firstBuzz, silentOn, meterOn, stampRead, unlocked, foundRecord, finished, toldRaise, toldSearchWords, toldSneak, toldFlip, toldMeter;
        bool bookOpen, toldSilentFail, bookRead, toldResearch;
        /// <summary>Every word that can be found in the library. The chapter ends only when ALL have been searched.</summary>
        static readonly string[] AllWords = { "CLEARVIEW", "1952", "1967", "DAY FIVE", "STRENGTH", "THE WAVE", "VOSS", "RED X" };
        readonly System.Collections.Generic.HashSet<string> searched = new System.Collections.Generic.HashSet<string>();
        bool AllSearched { get { return searched.Count >= AllWords.Length; } }

        /// <summary>"Searched 3 / 8: [x] CLEARVIEW  [x] 1967  [ ] ??? ..." - unknown words stay ??? until you've read them.</summary>
        string Progress()
        {
            string o = L("SEARCHED ", "已搜索 ") + searched.Count + " / " + AllWords.Length + "\n";
            int col = 0;
            foreach (string w in AllWords)
            {
                bool known = phone.searchWords.Contains(w);
                o += (searched.Contains(w) ? "[x] " : "[  ] ") + (known ? Loc.T(w) : "???");
                o += (++col % 2 == 0) ? "\n" : "    ";
            }
            return o.TrimEnd();
        }
        int page;
        float lastFlip = -9f, pageDwell;
        float noise, noiseGrace, zoneSeen, zoneGrace, routeTimer = 18f, speed, walkStreak, pigeonTimer = 30f;
        bool pigeonBusy, toldPigeons;
        Vector3 lastPos;
        Image meterFill;
        Text meterLabel;
        GameObject meterRoot, bookRoot;
        Text bookText, bookPage, bookHelp;

        protected override void Awake()
        {
            base.Awake();
            authorityLabel = "LIBRARIAN";
            authorityPitch = 1.15f;
            phone.hasPhone = true;
            phone.allowRaise = false;
            phone.phoneClock = "15:31";
            phone.showOldPhoto = false;
            phone.appUnlocked[PhoneController.AppPhotos] = true;
            phone.appUnlocked[PhoneController.AppMessages] = true;
            phone.appUnlocked[PhoneController.AppContacts] = true;
            phone.appUnlocked[PhoneController.AppCamera] = true;
            phone.silentAvailable = false;
            phone.silent = false;
            phone.lockNotification = L("No new messages.", "没有新信息。");
            phone.AddHistory("Triangle", L("got them. look at the board again: 20 reports. 3 reporters.", "收到了。再看看那块板：20 份举报。3 个举报员。"), false);
            phone.AddHistory("Hums When Scared", L("my card has no X. I checked twice.", "我的卡上没有 X。我检查了两遍。"), false);
            phone.onMessageArrived = OnMessage;
            phone.onSilentChanged = OnSilent;
            phone.searchWords.Clear();
            phone.AddSearchWord("CLEARVIEW");
            phone.AddSearchWord("1952");
            phone.searchFunc = SearchResult;
            phone.onSearched = OnSearched;
            if (returnsCart != null) { returnsCart.prompt = "Read the stamp in the oldest book"; returnsCart.onUse = ReadStamp; }
            if (oldBook != null) { oldBook.prompt = "Open the old book (Records, vol. 31)"; oldBook.onUse = OpenBook; }
            BuildMeter();
            BuildBook();
        }

        protected override void Start()
        {
            base.Start();
            if (librarian != null) librarian.Begin();
            if (assistant != null) assistant.Begin();
            lastPos = rig.transform.position;
            rig.Lock();
            StartCoroutine(Opening());
        }

        IEnumerator Opening()
        {
            yield return ChapterCard("CHAPTER 5", "THE ARCHIVE", "THURSDAY  -  15:30");
            Authority(L("Shoes quiet. Voices off. I hear everything in here.", "鞋子放轻。声音关掉。这里的一切我都听得见。"), 4.5f);
            rig.AllowWalk();
            Thought(L("Thursday, after school. The library is in the Cubberley Wing -\nthe name on the trophy case. ", "星期四，放学后。图书馆在库伯利楼——\n就是奖杯柜上那个名字。") + Rule(L("Same month as that photo.", "和那张照片同一个月。")), 7f);
            yield return WaitIdle();
            yield return Wait(2.5f);
            // the first message: it buzzes. Out loud. In a library.
            phone.Receive("Triangle", L("you're in? good. put your phone on silent first - she hears everything.", "进去了？很好。先把手机调成静音——她什么都听得见。"));
        }

        // ------------------------------------------------------------------ the buzz, silent mode
        void OnMessage()
        {
            if (phone.silent) return;
            ambient.Play(AudioSynth.Vibrate(), 0.95f);
            if (!firstBuzz) { firstBuzz = true; StartCoroutine(FirstBuzz()); return; }
            if (!meterOn) return;
            noise = Mathf.Clamp01(noise + buzzNoise);
            if (phone.silentJustFailed && !toldSilentFail)
            {
                toldSilentFail = true;
                Thought(L("It BUZZED. I put it on silent... didn't I?\nThe little bell is grey again. ", "它震了！我明明调成静音了……不是吗？\n小铃铛又变回灰色了。") + Rule(L("The switch is broken - check the bell EVERY time I take it out.", "开关坏了——每次拿出手机都要看一眼小铃铛。")), 8f, true);
            }
            else Thought(L("It buzzed again. ", "又震了。") + Rule(L("Silent mode: phone up, M. Check the bell.", "静音模式：拿起手机，按 M。看一眼小铃铛。")), 4.5f, true);
            phone.silentJustFailed = false;
        }

        /// <summary>The school's own alerts arrive now and then. If the switch has failed, they buzz.</summary>
        IEnumerator Alerts()
        {
            string[] en =
            {
                "reminder: silence is for everyone.",
                "your library visit is being enjoyed.",
                "a book you have not borrowed is overdue.",
                "please do not read alone.",
                "thank you for being quiet. we can tell.",
            };
            string[] zh =
            {
                "提醒：安静是为了每一个人。",
                "您的图书馆之行正在被享受。",
                "一本您没有借过的书已逾期。",
                "请不要独自阅读。",
                "感谢您保持安静。我们听得出来。",
            };
            int i = 0;
            while (!ended && !finished)
            {
                float t = Random.Range(alertEvery.x, alertEvery.y);
                while (t > 0f && !ended && !finished) { t -= Time.deltaTime; yield return null; }
                if (ended || finished) yield break;
                phone.Receive("CLEARVIEW ALERTS", L(en[i % en.Length], zh[i % zh.Length]));
                i++;
            }
        }

        IEnumerator FirstBuzz()
        {
            Authority(L("...Whose phone was that?", "……那是谁的手机？"), 3f, true);
            Thought(L("It buzzed. Out loud. ", "它震了。很响。") + Rule(L("Phone DOWN. Don't move.", "放下手机。别动。")), 4f);
            if (librarian != null)
            {
                Vector3 to = rig.transform.position - librarian.transform.position; to.y = 0f;
                Vector3 stop = rig.transform.position - to.normalized * Mathf.Min(2.4f, to.magnitude);
                librarian.Approach(stop, rig.transform);
                float t = 0f;
                yield return Wait(0.4f);
                while (!librarian.HasArrived && t < 8f) { t += Time.deltaTime; yield return null; }
            }
            yield return Wait(1.2f);
            Strike(L("In MY library. Silence is for everyone.", "在我的图书馆里。安静是为了每一个人。"));
            yield return WaitIdle();
            Authority(L("I'll be listening.", "我会一直听着。"), 3f);
            if (librarian != null && deskSpot != null) librarian.RunRoute(new[] { deskSpot.position }, new[] { 0.2f }, new[] { 0f }, null);
            routeTimer = 25f;
            yield return WaitIdle();
            phone.silentAvailable = true;
            Thought(L("Silent... this phone has a ", "静音……这部手机有") + Rule(L("little bell", "一个小铃铛")) + L(" in the corner of the screen. I never needed it before.\n", "，就在屏幕角落。以前从来没用过。\n") + Rule(L("Phone up, press M.", "拿起手机，按 M。")), 8f);
        }

        void OnSilent(bool on)
        {
            if (!on || silentOn) return;
            silentOn = true;
            meterOn = true;
            meterRoot.SetActive(true);
            ambient.Play(AudioSynth.Confirm(), 0.25f, 1.4f);
            phone.silentFailChance = silentFailChance;
            StartCoroutine(Alerts());
            Thought(L("Silent. ...Now my feet. She hears footsteps too.\n", "静音了。……现在是脚步。她也听得见脚步声。\n") + Rule(L("HOLD CTRL (or C) to walk quietly.", "按住 Ctrl（或 C）轻轻走。")), 7f, true);
            StartCoroutine(TriangleAfterSilent());
        }

        IEnumerator TriangleAfterSilent()
        {
            yield return Wait(4f);
            phone.Receive("Triangle", L("better. search opens with the stamp in the oldest book. month and year.", "好多了。“搜索”的密码是最旧那本书里的印章。月份加年份。"));
            phone.Receive("Triangle", L("the oldest book is never on a shelf. it's on the returns cart. nobody ever returns it.", "最旧的书从来不在书架上。它在还书车上。从来没人还它。"));
            phone.Receive("Triangle", L("and stay off the windows. the pigeons get loud, she comes to shoo them, and whoever's standing there gets the blame.", "还有，别待在窗边。鸽子一吵，她就会过去赶，站在那儿的人会一起挨骂。"));
            phone.Receive("Triangle", L("then search what happened in '67.", "然后搜一搜 67 年发生了什么。"));
        }

        // ------------------------------------------------------------------ main loop
        protected override void LevelUpdate()
        {
            if (finished) return;
            Vector3 p = rig.transform.position;
            Vector3 dp = p - lastPos; dp.y = 0f;
            speed = dp.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            lastPos = p;

            PlayerRig.Mode m = rig.CurrentMode;
            phone.allowRaise = phone.hasPhone && m == PlayerRig.Mode.Walk && !bookOpen;
            rig.freezeMovement = phone.IsExposed || bookOpen;
            if (phone.IsExposed && !toldRaise && silentOn)
            {
                toldRaise = true;
                Thought(L("While I look at the phone, ", "看手机的时候，") + Rule(L("I stand still", "我站着不动")) + L(". Shelves are tall here. Good.", "。这里的书架很高。很好。"), 6f);
            }
            if (!unlocked && phone.IsUnlocked(PhoneController.AppSearch))
            {
                unlocked = true;
                Thought(L("0467. It opened: the school's catalogue. ", "0467。打开了：学校的图书目录。") + Rule(L("Search 1967.", "搜索 1967。")), 5f);
            }
            if (!toldSearchWords && phone.Screen == PhoneController.PhoneScreen.Search && phone.IsUp)
            {
                toldSearchWords = true;
                Thought(L("It only searches words I've ", "它只能搜我") + Rule(L("read", "读到过")) + L(" somewhere.\nPosters, labels, the yearbook... ", "的词。\n海报、标签、年鉴……") + Rule(L("Walk up to them and look.", "走过去看。")), 7f, true);
            }
            if (bookOpen) UpdateBook();
            UpdateNoise();
            UpdateObjective();
            UpdateDanger();
            UpdateRestricted();
            UpdateLibrarianRounds();
            UpdatePigeons();
            SetPrompt(m == PlayerRig.Mode.Walk && !bookOpen ? RaycastInteract() : null);
        }

        void UpdateObjective()
        {
            if (finished) return;
            if (bookRead && !AllSearched) { SetObjective(L("Finish the research: SEARCH every word in the library.\nUnknown words (???): read more posters, labels, the yearbook.\n", "完成调查：把图书馆里每一个词都搜一遍。\n未知的词（???）：去读更多海报、标签、年鉴。\n") + Progress()); return; }
            if (bookOpen) { SetObjective(L("Turn the pages: A / D. SLOWLY - every page rustles.\nRead to the last page. Q: close the book.\nIf she comes round the shelf: close it.", "翻页：A / D。慢慢翻——每一页都会沙沙响。\n读到最后一页。Q：合上书。\n她要是绕过书架过来了：合上它。")); return; }
            if (foundRecord) { SetObjective(L("Records, vol. 31: the OLD BOOKS AWAITING REPAIR\n(NORTH-EAST corner, behind the rope). Go when nobody looks. Open it (E).\n", "记录，第 31 卷：在“待修旧书”里\n（东北角，绳子后面）。趁没人看时过去。打开它（E）。\n") + Progress()); return; }
            if (unlocked)
            {
                SetObjective(L("SEARCH every word you can find - start with 1967.\nSearch only knows words you have READ: walk up to posters and labels.\n", "把能找到的词都搜一遍——先搜 1967。\n搜索只认识你读到过的词：走近海报和标签去读。\n") + Progress());
                return;
            }
            if (stampRead) { SetObjective(L("Unlock SEARCH.\nCode = the stamp: APR 1967 -> month + year (4 digits).", "解锁“搜索”。\n密码 = 印章：1967 年 4 月 → 月份 + 年份（四位数）。")); return; }
            if (silentOn) { SetObjective(L("Find the OLDEST book: on the RETURNS CART (west wall, by the door).\nRead the stamp (E). Walk quietly: HOLD CTRL / C. Watch the meter.", "找到最旧的那本书：在还书车上（西墙，门边）。\n读印章（E）。轻轻走：按住 Ctrl / C。注意噪音表。")); return; }
            if (phone.silentAvailable) { SetObjective(L("Put the phone on SILENT.\nHOLD RIGHT MOUSE (behind a shelf), then press M. The little bell turns red.", "把手机调成静音。\n按住鼠标右键（躲到书架后面），再按 M。小铃铛会变红。")); return; }
            SetObjective(L("Don't move. Phone down.", "别动。放下手机。"));
        }

        void ReadStamp()
        {
            if (stampRead) { Thought(L("RECEIVED: APR 1967. The stamp hasn't changed.", "收到：1967 年 4 月。印章还是那个印章。"), 3.5f, true); return; }
            stampRead = true;
            phone.AddSearchWord("1967");
            ambient.Play(AudioSynth.Rustle(), 0.3f);
            noise = Mathf.Clamp01(noise + flipNoise);
            Thought(L("The oldest book in the library. Inside the cover, a stamp:\n", "图书馆里最旧的书。封面内侧有一个印章：\n") + Rule(L("RECEIVED: APR 1967", "收到：1967 年 4 月")) + L(" - Clearview Library, Cubberley Wing.", " - 明鉴图书馆，库伯利楼。"), 7f, true);
            Thought(L("April = 04. 1967 = 67. ", "四月 = 04。1967 = 67。") + Rule(L("0467.", "0467。")) + L("\n(Search word noted: 1967)", "\n（记下搜索词：1967）"), 6f);
        }

        // ------------------------------------------------------------------ search
        string SearchResult(string w)
        {
            switch (w)
            {
                case "1967":
                    return L("1 record found.\n\n'DAY FIVE'  -  April 1967\nStatus: RESTRICTED\n\n(Ask a librarian.)\n(Do not ask a librarian.)",
                             "找到 1 条记录。\n\n“第五天”  -  1967 年 4 月\n状态：限制查阅\n\n（请咨询图书管理员。）\n（不要咨询图书管理员。）");
                case "DAY FIVE":
                    return L("RECORDS, VOL. 31  (copied by hand)\n\nShelved with: OLD BOOKS AWAITING REPAIR\nnorth-east corner, behind the rope.\nDO NOT HANDLE.\n\nLast opened: 1980, by H. VOSS.",
                             "记录，第 31 卷（手抄本）\n\n存放于：待修旧书\n东北角，绳子后面。\n请勿触碰。\n\n上次翻阅：1980 年，H. 沃斯。");
                case "STRENGTH":
                    return L("School motto, adopted April 1967:\n\nStrength through discipline.\nStrength through community.\nStrength through action.\n\n(There was a fourth line. It has been removed.)",
                             "校训，1967 年 4 月采用：\n\n纪律带来力量。\n集体带来力量。\n行动带来力量。\n\n（原本还有第四句。已被删除。）");
                case "THE WAVE":
                    return L("THE WAVE (1981)\nA teacher turns his class into a movement in five days - to show them how easily it happens.\n\nCopies owned: 0\nStatus: never purchased",
                             "《浪潮》（1981）\n一位老师在五天里把他的班级变成一场运动——为了让他们看清这种事有多容易发生。\n\n馆藏：0 册\n状态：从未购入");
                case "VOSS":
                    return L("VOSS, H.\nSeat 31, class of 1980.\nReporter of the Year, 1980.\nReturned one (1) device to the school.\n\nNow: Instructor.",
                             "沃斯，H.\n31 号座，1980 届。\n1980 年度最佳举报员。\n向学校交还设备一（1）台。\n\n现任：督导。");
                case "CLEARVIEW":
                    return L("4,812 results.\n\nThey all say: what is seen, is safe.", "4,812 条结果。\n\n它们都在说：被看见的，就是安全的。");
                case "1952":
                    return L("1 record (damaged).\n\n'March 1952. A court has ruled that a school may watch its teachers.\nThe Founder is delighted. Clearview opens\nthat same spring.'\n\n(The rest of the page has been reorganised.)",
                             "1 条记录（已损毁）。\n\n“1952 年 3 月。法院裁定：学校可以监视它的老师。\n创办人非常满意。明鉴学院\n于同年春天开学。”\n\n（本页其余部分已被重新整理。）");
                case "RED X":
                    return L("RED X: see REPORTER.\nREPORTER: see RED X.", "红色 X：参见“举报员”。\n举报员：参见“红色 X”。");
            }
            return null;
        }

        void OnSearched(string w)
        {
            searched.Add(w);
            if (bookRead && AllSearched && !finished) { StartCoroutine(Finish()); return; }
            if (w == "1967" && phone.AddSearchWord("DAY FIVE"))
                Thought(L("A record called ", "一条叫") + Rule(L("DAY FIVE", "“第五天”")) + L(". Restricted.\n(Search word noted: DAY FIVE)", "的记录。限制查阅。\n（记下搜索词：第五天）"), 6f, true);
            if (w == "DAY FIVE" && !foundRecord)
            {
                foundRecord = true;
                Thought(L("Volume 31. ", "第 31 卷。") + Rule(L("Of course it's 31.", "当然是 31。")) + L("\nWith the old books waiting for repair. The ones we're not allowed to touch.", "\n和那些等着修的旧书放在一起。我们不许碰的那些。"), 7f, true);
                phone.Receive("Triangle", L("the repair shelf? go between her rounds. and turn the pages SLOWLY. old paper is loud.", "待修书架？趁她巡视的间隙去。翻页要慢。旧纸很响。"));
            }
        }

        // ------------------------------------------------------------------ the old book
        string[] Pages()
        {
            return new[]
            {
                L("CLEARVIEW LIBRARY\nRECORDS, VOL. 31\n\n(water damaged -\nawaiting repair -\ndo not handle)", "明鉴图书馆\n记录，第 31 卷\n\n（水渍受损——\n等待修复——\n请勿触碰）"),
                L("Lost and found, 1966:\n1 umbrella. 3 gloves (left).\n1 diary - burned, unread,\nfor the owner's safety.", "失物招领，1966 年：\n雨伞 1 把。手套 3 只（左手）。\n日记 1 本——为了失主的安全，\n未读即焚。"),
                L("Visitors, April 1967:\n'Two observers from the\nCubberley Wing committee.\nThey took notes. Many notes.'", "访客记录，1967 年 4 月：\n“库伯利楼委员会的\n两位观察员。\n他们做了笔记。很多笔记。”"),
                L("[ this page has been\ntorn out ]", "［ 这一页\n被撕掉了 ］"),
                L("DAY FIVE\n(copied by hand from a newspaper, April 1967)\n\nA high school in California. A history teacher wants to show his class how it happens - how people go along.", "第五天\n（1967 年 4 月，从报纸上手抄）\n\n加州的一所高中。一位历史老师想让他的学生看清它是怎么发生的——人们是怎么跟着走的。"),
                L("Day one: discipline.\nDay two: community.\nDay three: action - member cards, a salute, and three students assigned to report the others.\n\nBy the end of it, about twenty were reporting.", "第一天：纪律。\n第二天：集体。\n第三天：行动——会员卡、一种敬礼，还有三个被指派去举报别人的学生。\n\n到最后，大约有二十个人在举报。"),
                L("Day five: a rally. They are told a leader will speak to them on the screen.\n\nThe screen shows nothing.\n\nIt was a lesson. And then it ended.", "第五天：一场集会。他们被告知，屏幕上会有一位领袖对他们讲话。\n\n屏幕上什么都没有。\n\n那是一堂课。然后，它结束了。"),
                L("(in pencil, under the typing)\n\n\"Clearview opened its Cubberley Wing that same month.\nThey kept the lesson.\nThey threw away the ending.\"", "（打字稿下面，用铅笔写着）\n\n“明鉴学院在同一个月启用了库伯利楼。\n他们留下了那堂课。\n扔掉了结局。”"),
                L("(a newer hand)\n\n\"I read this in 1980.\nI gave the phone back anyway.\n\n- H.V.\"", "（另一种更新的字迹）\n\n“我在 1980 年读过这个。\n我还是把手机交了回去。\n\n——H.V.”"),
            };
        }

        void OpenBook()
        {
            if (!foundRecord) { Thought(L("OLD BOOKS - AWAITING REPAIR. Spines falling apart.\nI don't even know which one I'd want.", "待修旧书。书脊都散了。\n我都不知道自己要找哪一本。"), 4.5f, true); return; }
            if (bookOpen || finished) return;
            bookOpen = true;
            page = 0;
            pageDwell = 0f;
            bookRoot.SetActive(true);
            ShowPage();
            ambient.Play(AudioSynth.Creak(), 0.25f, 1.7f);
            noise = Mathf.Clamp01(noise + flipNoise * Near());
            if (!toldFlip)
            {
                toldFlip = true;
                Thought(Rule(L("Every page rustles.", "每一页都会沙沙响。")) + L(" Louder than footsteps.\nOne page at a time. Slowly.", "比脚步声还响。\n一页一页来。慢慢翻。"), 6f, true);
            }
        }

        void CloseBook()
        {
            bookOpen = false;
            bookRoot.SetActive(false);
        }

        void UpdateBook()
        {
            string[] pages = Pages();
            if (GameInput.BackDown()) { CloseBook(); return; }
            int dir = GameInput.RightKeyDown() ? 1 : (GameInput.LeftKeyDown() ? -1 : 0);
            if (dir != 0)
            {
                int np = Mathf.Clamp(page + dir, 0, pages.Length - 1);
                if (np != page)
                {
                    bool fast = Time.time - lastFlip < fastFlipSeconds;
                    lastFlip = Time.time;
                    page = np;
                    pageDwell = 0f;
                    ShowPage();
                    ambient.Play(AudioSynth.Rustle(), fast ? 0.75f : 0.45f, Random.Range(0.9f, 1.1f));
                    noise = Mathf.Clamp01(noise + flipNoise * (fast ? fastFlipFactor : 1f) * Near());
                    noiseGrace = 0f;
                    if (fast && DialogueIdle) Thought(L("Too fast - the paper crackles.", "太快了——纸页哗哗作响。"), 2.5f);
                }
            }
            pageDwell += Time.deltaTime;
            if (page == pages.Length - 1 && pageDwell > 2.5f && !finished && !bookRead)
            {
                bookRead = true;
                if (AllSearched) StartCoroutine(Finish());
                else if (!toldResearch)
                {
                    toldResearch = true;
                    CloseBook();
                    Thought(L("That's the record. But I only know half of what this place hides.\n", "这就是那份记录。可这地方藏着的东西，我只知道一半。\n") + Rule(L("Search every word in the library", "把图书馆里每一个词都搜一遍")) + L(" - then I'll know what I'm walking into tomorrow.", "——明天我才知道自己要面对什么。"), 8f, true);
                }
            }
        }

        void ShowPage()
        {
            string[] pages = Pages();
            bookText.text = pages[page];
            UIFactory.FitFont(bookText, pages[page], 16);
            bookPage.text = (page + 1) + " / " + pages.Length;
            bookHelp.text = L("A / D: turn the page (slowly)     Q: close", "A / D：翻页（慢慢来）     Q：合上");
        }

        IEnumerator Finish()
        {
            finished = true;
            yield return Wait(0.5f);
            CloseBook();
            rig.Lock();
            phone.allowRaise = false;
            SetPrompt(null);
            SetObjective("");
            meterRoot.SetActive(false);
            ambient.Duck(12f);
            Thought(L("Five days. Meant to end with the truth.\nClearview kept the first four.", "五天。本该以真相结束。\n明鉴学院留下了前四天。"), 6f, true);
            yield return WaitIdle();
            phone.Receive("Triangle", L("now you know. the screen is always empty. there's no leader. there's just the school.", "现在你知道了。屏幕永远是空的。没有领袖。只有这所学校。"));
            phone.Receive("Triangle", L("tomorrow is friday. day five. inspection. he'll check every desk.", "明天是星期五。第五天。检查日。他会查每一张课桌。"));
            Thought(L("Tomorrow is the fifth day.", "明天就是第五天。"), 4f);
            yield return WaitIdle();
            yield return FinishLevel("record", 2.2f);
        }

        void BuildBook()
        {
            Canvas c = UIFactory.MakeCanvas("OldBook", 39);
            c.transform.SetParent(transform, false);
            RectTransform box = UIFactory.Centered("Book", c.transform, -260f, 40f, 760f, 520f);
            UIFactory.AddImage(box, new Color(0.25f, 0.17f, 0.1f, 0.96f));
            RectTransform paper = UIFactory.Rect("Paper", box, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30f, -30f));
            UIFactory.AddImage(paper, new Color(0.86f, 0.8f, 0.66f));
            UIFactory.AddImage(UIFactory.Rect("Stain", paper, new Vector2(0.62f, 0.05f), new Vector2(0.98f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.6f, 0.5f, 0.32f, 0.25f));
            bookText = UIFactory.AddText(UIFactory.Rect("Text", paper, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(-70f, -110f)), "", 27, new Color(0.18f, 0.13f, 0.09f), TextAnchor.MiddleCenter);
            bookPage = UIFactory.AddText(UIFactory.Rect("Page", paper, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(0f, 26f)), "", 18, new Color(0.35f, 0.27f, 0.18f), TextAnchor.MiddleCenter);
            bookHelp = UIFactory.AddText(UIFactory.Rect("Help", paper, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(0f, 26f)), "", 17, new Color(0.45f, 0.3f, 0.2f), TextAnchor.MiddleCenter);
            bookRoot = box.gameObject;
            bookRoot.SetActive(false);
        }

        // ------------------------------------------------------------------ noise meter
        void BuildMeter()
        {
            Canvas c = UIFactory.MakeCanvas("NoiseMeter", 41);
            c.transform.SetParent(transform, false);
            RectTransform box = UIFactory.Rect("Meter", c.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -170f), new Vector2(300f, 70f));
            UIFactory.AddImage(box, new Color(0.05f, 0.055f, 0.06f, 0.9f));
            meterLabel = UIFactory.AddText(UIFactory.TopLeft("Label", box, 16f, 8f, 270f, 22f), L("QUIET, PLEASE", "请保持安静"), 16, new Color(1f, 0.8f, 0.45f), TextAnchor.UpperLeft);
            meterLabel.fontStyle = FontStyle.Bold;
            RectTransform bar = UIFactory.TopLeft("Bar", box, 16f, 38f, 268f, 16f);
            UIFactory.AddImage(bar, new Color(1f, 1f, 1f, 0.12f));
            meterFill = UIFactory.AddImage(UIFactory.Rect("Fill", bar, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero), new Color(0.6f, 0.85f, 0.6f));
            meterRoot = box.gameObject;
            meterRoot.SetActive(false);
        }

        float Near()
        {
            float d = 999f;
            if (librarian != null) d = Vector3.Distance(librarian.transform.position, rig.transform.position);
            if (assistant != null) d = Mathf.Min(d, Vector3.Distance(assistant.transform.position, rig.transform.position));
            return 1f + Mathf.Clamp01((nearLibrarian - d) / nearLibrarian) * 1.5f;
        }

        bool Sees(Vector3 p)
        {
            if (librarianVision != null && librarian != null && librarian.Running && librarianVision.CanSee(p, rig.transform)) return true;
            if (assistantVision != null && assistant != null && assistant.Running && assistantVision.CanSee(p, rig.transform)) return true;
            return false;
        }

        void UpdateNoise()
        {
            if (!meterOn) return;
            if (noiseGrace > 0f) noiseGrace -= Time.deltaTime;
            bool moving = speed > 0.3f;
            // the longer you walk without stopping, the faster the meter climbs (exponential); quiet steps don't count
            if (moving && !GameInput.SneakHeld()) walkStreak += Time.deltaTime;
            else walkStreak = Mathf.Max(0f, walkStreak - Time.deltaTime * 3f);
            float rate;
            if (!moving) rate = -noiseDecay;
            else if (GameInput.SneakHeld()) rate = noiseSneak;
            else if (speed > 3.4f) rate = noiseRun;
            else rate = noiseWalk;
            if (rate > 0f) rate *= Near() * Mathf.Min(walkMaxFactor, Mathf.Pow(2f, walkStreak / Mathf.Max(0.2f, walkDoubling)));
            if (noiseGrace > 0f && rate > 0f) rate = 0f;
            if (bookOpen && rate < 0f) rate *= 0.6f;   // holding an old book open isn't quite silence
            noise = Mathf.Clamp01(noise + rate * Time.deltaTime);
            meterFill.rectTransform.anchorMax = new Vector2(noise, 1f);
            meterFill.color = Color.Lerp(new Color(0.6f, 0.85f, 0.6f), new Color(0.95f, 0.25f, 0.18f), noise);
            meterLabel.text = noise > 0.75f ? L("QUIET, PLEASE  -  SHHH...", "请保持安静  -  嘘……") : L("QUIET, PLEASE", "请保持安静");
            if (moving && !GameInput.SneakHeld() && !toldSneak && noise > 0.25f)
            {
                toldSneak = true;
                Thought(Rule(L("The meter", "噪音表")) + L(" (top right) fills when I walk. ", "（右上角）一走路就涨。") + Rule(L("HOLD CTRL / C", "按住 Ctrl / C")) + L(" = quiet steps.\nStanding still lets it drain. Running is the worst.", " = 轻步走。\n站着不动它就会降。跑步最糟。"), 7f, true);
            }
            if (!toldMeter && noise > 0.6f)
            {
                toldMeter = true;
                Thought(L("Closer to her = louder. ", "离她越近 = 越响。") + Rule(L("Stand still for a moment.", "先站着别动一会儿。")), 5f, true);
            }
            if (noise >= 1f)
            {
                noise = 0.35f;
                noiseGrace = 3f;
                Strike(bookOpen ? L("Who is turning pages back there? ...SHHH.", "谁在后面翻书？……嘘！") : L("SHHH. ...Libraries are for silence. Silence is for everyone.", "嘘。……图书馆是用来安静的。安静是为了每一个人。"));
            }
        }

        // ------------------------------------------------------------------ watchers
        void UpdateDanger()
        {
            bool seen = false;
            if (phone.IsExposed) seen = Sees(phoneExposurePoint.position);
            if (AccumulateDanger(seen ? 1f / Mathf.Max(0.05f, noticeSeconds) : 0f))
                Strike(L("A device. In my library. Devices are not books.", "一台设备。在我的图书馆里。设备不是书。"));
        }

        void UpdateRestricted()
        {
            if (zoneGrace > 0f) zoneGrace -= Time.deltaTime;
            bool inside = In(rig.transform.position, restrictedZone);
            bool seen = inside && zoneGrace <= 0f && Sees(rig.eye.position);
            zoneSeen = seen ? zoneSeen + Time.deltaTime * (bookOpen ? 2f : 1f) : Mathf.Max(0f, zoneSeen - Time.deltaTime);
            if (zoneSeen > 0.6f)
            {
                zoneSeen = 0f;
                zoneGrace = 2.5f;
                bool withBook = bookOpen;
                CloseBook();
                Strike(withBook ? L("Students do NOT touch the old books. They are waiting for repair.", "学生不许碰旧书。它们在等着修。") : L("That corner is for books awaiting repair. Not for students.", "那个角落是给待修的书的。不是给学生的。"));
                if (!ended) rig.Teleport(restrictedReturn, 180f);
            }
        }

        /// <summary>She sits at the desk, then walks a round of the aisles, then comes back. Her rounds pass the rope.</summary>
        void UpdateLibrarianRounds()
        {
            if (librarian == null || deskSpot == null || !librarian.RouteDone || !firstBuzz || !silentOn) return;
            routeTimer -= Time.deltaTime;
            if (routeTimer > 0f) return;
            routeTimer = Random.Range(22f, 30f);
            bool east = Random.value < 0.5f;
            Vector3[] pts = east
                ? new[] { new Vector3(9.4f, 0f, 3.2f), new Vector3(9.4f, 0f, 9.2f), new Vector3(11.8f, 0f, 9.2f), new Vector3(7f, 0f, 8.4f), deskSpot.position }
                : new[] { new Vector3(4.6f, 0f, 3.2f), new Vector3(4.6f, 0f, 9.2f), new Vector3(7f, 0f, 9.4f), new Vector3(9.4f, 0f, 9.2f), deskSpot.position };
            float[] waits = { 1.5f, 2.5f, 3f, 1.5f, 0.2f };
            float[] yaws = { float.NaN, 90f, 0f, float.NaN, 0f };
            librarian.RunRoute(pts, waits, yaws, null);
        }

        // ------------------------------------------------------------------ the pigeons: noisy, and she hates them
        void UpdatePigeons()
        {
            if (pigeonBusy || !silentOn || librarian == null || pigeonSpot == null || deskSpot == null || ended || finished || Pigeon.All.Count == 0) return;
            pigeonTimer -= Time.deltaTime;
            if (pigeonTimer <= 0f) StartCoroutine(PigeonEvent());
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        IEnumerator PigeonEvent()
        {
            pigeonBusy = true;
            foreach (Pigeon pg in Pigeon.All) pg.Agitate(16f);
            yield return Wait(2f);   // the flapping first
            if (ended || finished) { pigeonBusy = false; yield break; }
            Authority(L("...Those birds. Again.", "……又是那些鸟。"), 2.5f);
            if (!toldPigeons)
            {
                toldPigeons = true;
                Thought(L("The pigeons on the window ledge. ", "窗台上的鸽子。") + Rule(L("She's coming to the windows - stay away from them.", "她要过来窗边了——离窗户远点。")), 5f);
            }
            bool done = false;
            librarian.RunRoute(new[] { pigeonSpot.position, deskSpot.position }, new[] { 4.5f, 0.2f }, new[] { pigeonSpot.eulerAngles.y, 0f }, () => done = true);
            float t = 0f;
            while (!ended && t < 14f && Flat(librarian.transform.position - pigeonSpot.position) > 0.6f) { t += Time.deltaTime; yield return null; }
            if (!ended)
            {
                ambient.Play(AudioSynth.Click(), 0.45f, 0.55f);   // she raps on the glass
                Authority(L("Shoo. SHOO. ...Filthy, noisy things.", "去。去！……又脏又吵的东西。"), 3f, true);
                foreach (Pigeon pg in Pigeon.All) pg.Calm();
                float stay = 0f;
                while (!ended && stay < 4f)
                {
                    stay += Time.deltaTime;
                    if (Flat(rig.transform.position - pigeonSpot.position) < pigeonNear)
                    {
                        Strike(L("And you. Away from the window. Nobody stands about with the birds.", "还有你。离窗户远点。谁也不许跟那些鸟待在一起。"));
                        break;
                    }
                    yield return null;
                }
            }
            t = 0f;
            while (!done && !ended && t < 15f) { t += Time.deltaTime; yield return null; }
            routeTimer = Random.Range(10f, 18f);
            pigeonTimer = Random.Range(pigeonEvery.x, pigeonEvery.y);
            pigeonBusy = false;
        }

        static bool In(Vector3 p, Vector4 r) { return p.x >= r.x && p.x <= r.z && p.z >= r.y && p.z <= r.w; }

        protected override IEnumerator OnCaught()
        {
            phone.allowRaise = false;
            CloseBook();
            rig.Lock();
            meterRoot.SetActive(false);
            Authority(L("That's three. The device is overdue. I'll take it.", "第三次了。这台设备逾期了。我收走了。"), 4f, true);
            if (librarian != null)
            {
                Vector3 to = rig.transform.position - librarian.transform.position; to.y = 0f;
                librarian.Approach(rig.transform.position - to.normalized * 0.9f, rig.transform);
                float t = 0f;
                yield return Wait(0.4f);
                while (!librarian.HasArrived && t < 7f) { t += Time.deltaTime; yield return null; }
            }
            Authority(L("Returned. ...Shhh.", "已归还。……嘘。"), 3.5f);
            yield return WaitIdle();
            yield return Caught("shhh");
        }

        protected override void OnRead(Readable r)
        {
            base.OnRead(r);
            if (r.keyword == "STRENGTH" || r.keyword == "THE WAVE" || r.keyword == "VOSS" || r.keyword == "RED X") ambient.Play(AudioSynth.Ping(), 0.25f, 1.2f);
        }
    }
}
