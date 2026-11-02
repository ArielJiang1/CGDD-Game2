# COVER - v2 (6 chapters, Monday to Friday, ~35 min)

## Build the scenes
After a code update Unity asks "Game scenes are out of date - rebuild?" -> **Rebuild now**.
Or menu **Game > Build All Scenes (Title, 6 Chapters, End)**. Hand edits in the game scenes are lost;
`Assets/Blockout/Scenes/Blockout_School.unity` is never changed. Open `Scenes/Title` and press Play.
Title > Chapters jumps to any chapter (for playtests). Progress + name are saved (Continue).

## Flow
Title (New Game / Continue / Chapters) -> Ch.1 -> End page -> Ch.2 -> ... -> Ch.6 -> Ending 1 or 2 + credits.
Caught (3rd warning) -> GAME OVER page -> Retry restarts the same chapter.

| Ch | Scene | Threat | Goal |
|----|-------|--------|------|
| 1 The Lesson | Level01_Lesson | teacher, questions | find the phone (1952), Photos (2100), tap the student with the chalk triangle |
| 2 Lunch | Level02_Lunch | sweeping camera + Lunch Monitor | Messages (1240, PA says it), find the girl who HUMS by sound, forward to her within reach, sneak behind the counters to the STAFF door when the camera faces the windows |
| 3 Lights Out | Level03_LightsOut | night warden (listen: footsteps + keys, flashlight) | HOLD Space = lie down/pretend to sleep; reply to Never Blinks; Contacts (0304 = the room he says); flash the screen at the window 3x |
| 4 Picture Day (Wed) | Level04_PictureDay | the class mills about; every ~45 s a CLASS PHOTO countdown (12 s): be on the YELLOW X (it moves to a new place on the risers before every photo), phone down, still, HOLD SPACE to smile - or a report; 2 prefects, the photographer; shutter click + flash (back ON every time the phone comes out) | between photos: CAMERA (1045), photograph the report board, the LIVE screen, the 1967 photo (LEFT MOUSE = shutter, A/D = flash); send them to Triangle; then one perfect final photo |
| 5 The Archive (Thu) | Level05_Archive | the librarian: the first message BUZZES and she comes (a warning) -> SILENT MODE (the little bell, phone up + M); the NOISE METER (walk fills, Ctrl/C = quiet steps, Shift = loud, still = drains); turning pages is louder than walking (faster = much louder) | the oldest book on the returns cart: RECEIVED APR 1967 -> SEARCH (0467). Search only knows words you READ: 1967 -> DAY FIVE -> Records vol. 31 among the OLD BOOKS AWAITING REPAIR (north-east, behind the rope; being seen there = a warning) -> read it page by page (A/D) |
| 6 Inspection Day (Fri) | Level06_Inspector | Instructor Voss inspects every desk | get the phone back, answer questions, find the 1979 photo (VOSS, same seat); at your desk choose 1 hide (Ending THE SEAT) or 2 send to everyone (Ending LIGHTS) |

## Language (English / 中文)
Title page: **Language** button switches English / 中文 (saved, the title reloads).
- Story lines in the directors: `L("English", "中文")` inline.
- Text stored in scenes or lists (posters, contacts, photo captions, hints, lesson lines, questions, speaker labels, prompts):
  English in the scene, Chinese in `Scripts/LocTable.cs` (looked up by `Loc.T`). New English text without an entry just stays English.
- Chinese uses a system font (PingFang SC on Mac, Microsoft YaHei on Windows).

## Phone apps (one per chapter)
Home screen is in unlock order: 1 Photos (Ch.1, 2100) - 2 Messages (Ch.2, 1240) - 3 Contacts (Ch.3, 0304) - 4 Camera (Ch.4, 1045) - 5 Search (Ch.5, 0467).
An app's passcode hint shows **???** until you reach the chapter that unlocks it.

## Reading posters (everything printed means something)
Walk up to a poster, sign or label and look at it for a moment (seated: just look at it): you READ it.
It gives a thought (a clue, or a joke about Clearview), a Search word (Ch.5), or the poster changes while you read it.
Set in the builder with `R(ClearviewArt.Poster(...), "reaction", "keyword", "altTitle", "altBody")` (Readable.cs).

## The homage: the Third Wave (Cubberley High School, Palo Alto, April 1967)
A teacher turned his class into a "movement" in five days to show how it happens: discipline, community, action
(member cards, a cupped-hand salute, three students assigned to report - about twenty reported), and on the fifth
day a rally where a leader would appear on TV; the screen stayed empty. (Later: the TV film / novel "The Wave",
the German film "Die Welle".) In COVER: five days Monday-Friday, the STRENGTH THROUGH... banners, "20 reports,
3 reporters", citizen cards with a red X, the LIVE screen with nobody on it, the Cubberley Wing (opened April 1967),
Records vol. 31 (DAY FIVE), the banned novel THE WAVE (1981).
1952 (the founding year, the first phone code): the year the US Supreme Court upheld New York's Feinberg Law
(Adler v. Board of Education - schools may investigate and fire teachers for "subversive" ties; Justice Douglas:
it "turns the school system into a spying project"), struck down in January 1967 (Keyishian). Also 1952: the
school film "Duck and Cover". Roll call in Ch.1: Irving Adler, Harry Keyishian, Ellwood Cubberley, Bert (the turtle). Clearview "kept the lesson and threw away the ending".

## Playtest
The OBJECTIVE box (top-left) always says the next step. **F8** skips the current chapter.

## Controls
WASD walk, Mouse look, E interact, Space sit up / stand up (Ch.3: HOLD Space = lie down; Ch.4: HOLD Space = smile for the class photo),
HOLD Right Mouse = phone, number keys = codes, A/D choose, Left Mouse open/select (hold = send), Q back,
1/2/3 answer (phone down first). Shift = run (loud in the library), Ctrl / C = walk quietly, M = silent mode (from Ch.5). Camera: Left Mouse = shutter, A/D = flash. Search: A/D word, HOLD Left Mouse.

## Art pass (Editor/ClearviewArt.cs)
Procedural textures (walls with water stains / mould / peeling paint, vinyl floor, ceiling tiles, laminate),
Clearview palette (clinical white, blue-green dado, dirty green furniture, deep red = authority, ironic pastels),
fluorescent grid (some flicker), posters/signs (typewriter font), cameras with blinking LEDs, PA speakers,
working wall clocks, URP post (Day / Night profiles in Generated/Clearview). All sound is synthesised (AudioSynth.cs).

## Tune (Inspector)
- PLAYER > LevelXXDirector: noticeSeconds, chances, subtitle timings, chapter-specific settings
- PLAYER > PhoneController: codes, hints, contacts, photo captions
- Teacher/Monitor/Warden: TeacherAI / PatrolAI timings, TeacherVision viewAngle & viewRange

- **Story speed (1x / 2x):** Tab in game, or the title-screen button. Speeds up dialogue, messages and scripted waits (not the timers you play against). Saved in PlayerPrefs (`cover_speed2x`); code in `Scripts/GameSpeed.cs` (use `Wait(s)` in directors, `GameSpeed.Dt` for story timers).
- **Everything mentioned is there:** the lesson's Charter, founder's portrait, bell, oak and pigeons are in the Ch.1/Ch.6 classroom and courtyard (`Courtyard`, `FounderPortrait`, `WallBell` in GameSceneBuilder; `Pigeon.cs`). Things that aren't posters can be read through `Readable.radius`.
