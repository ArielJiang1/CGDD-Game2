# DEVLOG

Only append. Do not edit old sections. Two samples stay so students can see the format.

Only changes to the game itself are logged: the verb, win and loss, pressure, and what the player can feel.
Site language, diagram styling, and click-to-read-script do not go into an Interaction or a Stage Reflection.

Each gameplay change should add a section `## Interaction NNN — title` (the Agent can append this when it changes the game).
Add `## Stage Reflection NNN — title` only when you ask for one: Keep / Change / Cut, and which AI suggestions you accepted or rejected. That reflection stays on the game.

## Interaction 000 — EXAMPLE (sample, do not delete)

- Date: —
- Who asked: teacher (sample)
- Request: Tighten one tunable and see if it is still the same game.
- What changed: Sample only. The current code was not changed. A real change would touch only one `@tune` field.
- Result: The player's verb did not change. Win and loss did not change. Pressure changed, not the skeleton.

## Stage Reflection 000 — EXAMPLE (sample, do not delete)

### Keep
- A core verb you can say in one sentence, and a clear win/loss.

### Change
- To make it harder, turn an existing `@tune` first. Do not add a new system yet.

### Cut
- No shop, no deep inventory, no optimization that is off-lesson.

### AI
- Accept: tune feel with existing fields; avoid opening new scripts.
- Reject: GraphView, object pools, long hand-written scene YAML. This class is about script relationships and tunable numbers.

## Interaction 001 — White box: school blockout kit

- Date: 2026-09-28
- Who asked: Ariel & Nina
- Request: Build a white-box school from modular prefabs (classroom, corridor, cafeteria) using player metrics from The Level Design Book.
- What changed: Blockout kit (walls, doors, windows, desks, figures, a security camera, a patrolling teacher) and the Blockout_School scene. The player sits in the back corner seat by the window.
- Result: The core verb was set: watch the teacher, then use the phone when he can't see you. Classmates and pillars act as cover. There was no win or loss yet.

## Interaction 002 — Chapter 1 "The Lesson": you can win, you can lose

- Date: 2026-10-12
- Who asked: Ariel & Nina (assignment: "You can win, you can lose")
- Request: Make one level with a clear win and loss, Inside-style (no HUD hearts).
- What changed: The player is pulled to their seat, then finds a phone behind a loose panel in the desk. The passcodes come from the lecture (founding year 1952; lights-out 21:00 = 2100). The teacher walks on a NavMesh and asks questions about what he already said. A Photos clue leads to the student in front: tap their shoulder to win. If he sees the phone, the phone shakes and turns red. Three warnings means he takes the phone and you lose.
- Result: Win (contact made) and loss (phone taken) now exist. The verbs are watch, peek, act and hide. The danger meter (`LevelDirectorBase.noticeSeconds`) controls the pressure.

## Interaction 003 — Playtest feedback round (Chapter 1)

- Date: mid-October 2026
- Who asked: Ariel & Nina (playtest notes)
- Request: The hearts read as too gamey, subtitles overlapped, the entrance felt stiff, and it was unclear where to look.
- What changed: Removed the hearts; the three chances are now only spoken by the teacher. Only one subtitle shows at a time (teacher at the top, thoughts at the bottom-left). The player types their name and the teacher reads it in a roll call. The rules are told through thoughts during the roll call. The phone is lower and bigger. Being noticed shows as the phone shaking and turning red.
- Result: Win/loss unchanged. The verb is unchanged, but the player now learns the rules before the pressure starts.

## Interaction 004 — Four chapters, a story and an art pass

- Date: 2026-10-19
- Who asked: Ariel & Nina (cultural aesthetic: anti-utopian + dark humour)
- Request: Apply the Clearview Academy style and grow the game to about 20 minutes, with a beginning, an ending, chapters and win/loss.
- What changed: Ch.2 Lunch (security camera + Lunch Monitor; find the girl who hums by sound; reach the STAFF door). Ch.3 Lights Out (a night warden you track by footsteps and keys; hold Space to pretend to sleep; flash the window three times). Ch.4 Inspection Day (the 1979 photo shows Instructor Voss sat in the same seat; the final choice is to hide it or send it to everyone, giving two endings). Added a title page with Continue and Chapters, an end page after every chapter, and retry-from-chapter. Added procedural textures, fluorescent lights, posters, cameras with blinking LEDs, PA announcements and generated sound.
- Result: Each chapter keeps the same verb but adds a new watcher: a camera, a roaming monitor, a guard you can only hear. The loss is the same each time (three warnings). The win changes per chapter, and the final win is a choice between two endings.

## Interaction 005 — Guidance: an objective panel and the hum by distance

- Date: 2026-10-19
- Who asked: Ariel & Nina (stuck at "find the girl by the window" in Ch.2)
- Request: Players did not know where to go or what to do next.
- What changed: An always-on OBJECTIVE panel (top-left) states the next step and where to go. In Ch.2, the humming now fades in only within 6.5 m (`HumSource.maxDistance`). Hints play when you get close, you can press E on her, and a signal line shows while the phone is up (within `Level02Director.reachDistance`). F8 skips a chapter for playtests.
- Result: Same game, but guided. Getting stuck no longer replaces the tension.

## Interaction 006 — Stairs you walk down; random bed checks; a shorter finale

- Date: 2026-10-19
- Who asked: Ariel & Nina
- Request: The stairs behind the STAFF door felt fake. The warden was predictable. Chapter 4 had dead time.
- What changed: Ch.2 ends with the player walking down a real stairwell (painted "WE KEEP THINGS FROM EACH OTHER", chalk "SEAT 31" years) to a door with voices behind it. Ch.3 bed checks are picked from six events, and the same one never comes twice in a row. Ch.4 inspects only four desks, and the ending LIGHTS finally shows the room at the bottom of the stairs, where Voss closes the door from the outside.
- Result: Win/loss unchanged. The story now pays off the stairs from Ch.2 in the last scene.

## Interaction 007 — Pacing and feedback polish

- Date: 2026-10-20
- Who asked: Ariel & Nina
- Request: Too many warden visits in Ch.3. The PA box looked like a phone notification. There was no notification for new messages.
- What changed: The warden now waits 24–38 s between visits and fewer visits come into the room. PA lines use the normal dark subtitle style. The phone shows a real new-message notification at the bottom while it is down.
- Result: There is more room to act in Ch.3. Same verbs and same win/loss.

## Interaction 008 — The climax sounds like a climax

- Date: 2026-10-23
- Who asked: Ariel & Nina
- Request: When every phone lights up (ending LIGHTS), the sound should feel big. The crack on the phone screen went outside the glass.
- What changed: The room goes quiet and a chord swells. Each phone rings its own rising note as it lights, faster and faster, then a large open chord blooms. The phone screen now clips its contents.
- Result: The emotional peak of the game is louder than any warning. Gameplay is unchanged.

## Interaction 009 — The climax: freedom, but lonely

- Date: 2026-10-23
- Who asked: Ariel & Nina
- Request: The climax sound felt too positive. It should feel liberating but desolate, a little solemn but not heroic.
- What changed: The rising chord is now A minor (add9). The phones ring up a minor pentatonic scale, softer and slower. The final chord has open fifths and ninths with no third (neither happy nor sad), swells in instead of hitting, and fades into wind. One distant bell is answered a step lower.
- Result: Gameplay is unchanged. The ending now reads as release with a cost, which fits Voss closing the door from the outside.

## Interaction 010 — Apps unlock in chapter order, "???" hints, Chinese version

- Date: 2026-10-24
- Who asked: Ariel & Nina
- Request: Each chapter should unlock one phone app, so the apps should be listed in unlock order. A passcode hint should read "???" until the player reaches the chapter that unlocks it. Add a Chinese version that can be switched at the start.
- What changed: The home screen now lists the apps in unlock order (1 Photos, 2 Messages, 3 Contacts, 4 Camera, 5 Search), each with its number. A hint shows "Hint: ???" until the current chapter is the app's chapter or later. Camera and Search have codes ready for the coming chapters. The title page has a Language button (English / 中文, saved). Every story line, phone text, poster, lesson question and end page has a Chinese version and uses a Chinese system font.
- Result: Win/loss and player verbs are unchanged. Later apps no longer give their answers away early. Chinese players can now play and read the posters.

## Interaction 011 — No more flickering faces on the characters

- Date: 2026-10-24
- Who asked: Ariel & Nina
- Request: Two faces of a character model should not lie exactly on top of each other, because that flickers (z-fighting).
- What changed: After a student or adult is dressed, the scene builder checks every pair of box parts. If two faces lie in the same plane and face the same way (for example the hair top and the back hair, or a part flush with the body block), the smaller part grows by 3 mm on that axis. Scene version 15.
- Result: Gameplay is unchanged. Characters no longer flicker when the camera moves.

## Interaction 012 — Collar redesign, Chinese names, faster roll call

- Date: 2026-10-25
- Who asked: Ariel & Nina
- Request: The collar still overlapped the clothes and flickered. Character names should be Chinese in the Chinese version. Roll call in Ch.1 should be tighter.
- What changed: The collar is now a skin neck, a collar band that sits 5 mm inside the torso, and two tilted collar points. The anti-flicker check now also counts the white-box body. Roll-call names and VOSS have Chinese names. Each name + "Here" takes about 3.5 s instead of 6.5 s, and the gaps between lines are shorter.
- Result: Gameplay is unchanged. Ch.1 gets to the lesson faster.

## Interaction 013 — Two new chapters, readable posters, the Third Wave homage

- Date: 2026-10-25
- Who asked: Ariel & Nina
- Request: Add chapters so every phone app gets used, one app per chapter. Every text in the game should mean something (a clue or an easter egg), and posters should respond when the player reads them. Base the background on a real event in education as an homage.
- What changed: The game now runs Monday to Friday over six chapters, after the Third Wave experiment (Palo Alto, April 1967). New Ch.4 Picture Day (Camera, 1045): photograph the report board, the empty LIVE screen and the 1967 photo while prefects report you, the shutter can be heard and the flash can be seen; then stand in the class photo. New Ch.5 The Archive (Search, 0467): the librarian's noise meter; Search only knows words you have read; 1967 → DAY FIVE → drawer 31 tells what happened in 1967. Inspection Day is now Ch.6. Every poster in every chapter can be read (walk up and look); it gives a thought, a clue, a Search word, or changes as you read it.
- Result: Two new wins and new verbs (photograph, move quietly, search with words you read, read the walls). The loss is still three warnings. The two endings are unchanged.

## Interaction 014 — Picture Day as a rhythm, silent mode and old books, the 1952 homage

- Date: 2026-10-25
- Who asked: Ariel & Nina
- Request: Ch.4 scenery flickered (overlapping surfaces), and everyone sat still while only the player moved. Make the class walk about, call a class photo every so often (get to your place, smile), and allow the phone only in the gaps. The flash should switch back on every time the phone comes out. In Ch.5, a message should give you away first, then you learn about silent mode; then footsteps; the target book should sit with the old books awaiting repair, which students may not touch; turning its pages should be noisy, and noisier when fast. Also give the roll-call names a historical meaning.
- What changed: An anti-flicker pass now runs over every chapter, so no two boxes share a face. Ch.4: fifteen students roam the gym. Every 40–55 s a 20 s countdown starts and everyone walks to the risers. The player must be on the "31" spot, phone down, standing still and holding SPACE to smile, or it is a report (a warning); the photographer always finds a reason to retake. The camera flash resets to ON on every raise. Ch.5: the first message buzzes and the librarian comes (a warning). Only then is the phone's little bell explained: silent mode, M. Then the noise meter: walk, Ctrl/C to walk quietly, Shift to run. Records vol. 31 sits among the old books awaiting repair: open it and turn the pages with A/D; each page rustles louder than a step, and fast turns are much louder; being seen there is a warning. The roll call is now Irving Adler, Harry Keyishian, Ellwood Cubberley and Bert (Feinberg Law 1952 / Keyishian 1967 / Cubberley / "Duck and Cover" 1952). Searching "1952" finds the court-ruling record.
- Result: Ch.4 adds *be where you're told, on time* and *smile*. Ch.5 adds *silence the phone*, *sneak* and *read slowly*. Loss is still three warnings.

## Interaction 015 — Clearer girls and boys, a moving X, faster countdown, Chinese text pass

- Date: 2026-10-26
- Who asked: Ariel & Nina
- Request: Make girl and boy models clearer; girls should not look like they have a beard. In Ch.4 the X on the floor should move between photos, and the class-photo countdown should be faster (the gaps between photos stay the same). Fix Chinese grammar, and keep Chinese text inside its boxes (wrap).
- What changed: The mouth was a dark bar under the nose, which read as a moustache. Girls now have rose lips, blush and lashes; boys have a thin skin-toned mouth. Girls' side hair hangs behind the ears instead of along the jaw. Standing students (Ch.4) are now schoolgirls (long hair or ponytail, bow, skirt, white socks) or schoolboys (school tie, grey trousers). Ch.4: the yellow X (with a small flag) moves to a random place on the risers before every photo, and the objective says where it is. The countdown is 12 s (was 20), and the students walk faster during it. Chinese: names unified (三角, 午餐监督员, 304 寝室), awkward lines rewritten, and the old "drawer 31" reference corrected to vol. 31. Subtitle and thought boxes grow with their text; phone texts shrink to fit.
- Result: Ch.4 gets tighter: you have to find a new spot each time, in less time. Win/loss unchanged.

## Interaction 016 — No "moustache" on the girls, prefects need to be close

- Date: 2026-10-26
- Who asked: Ariel & Nina
- Request: Girls still had something above the lips that looked like a moustache. In Ch.4 a photo almost always set off a prefect warning; a prefect should have to stand close.
- What changed: Girls' nose is now small, skin-coloured and casts no shadow (its shadow above the lip read as a moustache). A prefect now hears the shutter only within 2.2 m (was 4.5 m), and prefects see 6 m in a 90° cone (was 8 m, 100°).
- Result: Ch.4 is easier. Photograph when no armband is right next to you. Win/loss unchanged.

## Interaction 017 — Real eyes, the final photo spot, an unreliable silent switch

- Date: 2026-10-26
- Who asked: Ariel & Nina
- Request: The final class photo should use the spot the text names (back row, by the window). No dark rectangle on girls' faces. The spot should be only an X, no flag. In Ch.5 page turns should fill the meter more, a second librarian should patrol, and silent mode should fail at random, without the player knowing when.
- What changed: The white-box eye band, a dark bar that sat low on the standing figures and read as a moustache, is gone. Every figure now has two eyes with a glint; girls also get lashes, blush, rose lips and a small nose. Ch.4: the first and the final photo always use back row, window end, and the flag is removed. Ch.5: a page turn now adds 0.16 to the meter (was 0.09), and ×2.6 if fast. An assistant librarian walks the aisles all the time. Each time the phone comes out, silent mode has a 30% chance of quietly switching off; only the grey bell shows it. The school's alerts arrive every 28–45 s and buzz if it has failed.
- Result: Ch.5 is harder (check the bell every time, two watchers). Ch.4 ending matches the text. Win/loss unchanged.

## Interaction 018 — Dazed students

- Date: 2026-10-26
- Who asked: Ariel & Nina
- Request: Remove the eye highlights, make the black eyes bigger and taller than they are wide, and make the students look more dazed.
- What changed: Eyes are now plain black, tall narrow rectangles (no glint). Students, seated and the standing schoolchildren, have eyes set wider apart, raised short brows and a tiny mouth. Adults keep normal faces.
- Result: Look only. Gameplay is unchanged.

## Interaction 019 — 2x story speed, faster roll call, full library research, a courtyard for the lesson

- Date: 2026-11-02
- Who asked: Ariel & Nina
- Request: Make the roll call much faster. Add a 2x speed for all dialogue, messages and waiting (for example the lights-out waits). Get the player into the lights-out chapter's first step sooner. Check that no wall posters overlap. Everything the dialogue or text mentions (pigeons, the left-handed moustache) must be in the world, or the text goes. In the library, every word must be searched before the chapter ends, with progress in the objective.
- What changed: Tab (or the title-screen button "Story speed") switches 1x / 2x and is remembered. 2x speeds up line timing, the typewriter, message reading and typing, scripted waits, lesson pacing, PA timers, the warden's rest between visits (Ch.3), and the Ch.4 free time after your photos are sent. Timers you play against (answer timer, photo countdown, noise) stay real-time. Roll call: each name is now 1.0 s and each "Here" 0.8 s, with gaps of 0.1–0.15 s. Ch.3 opens with one instruction line (hold SPACE when he comes in), and the first message comes after about 3 s. Ch.5: all 8 search words must be searched; the objective shows "SEARCHED n / 8" with a checklist, and unknown words show as ???. The classroom now has the Founding Charter (four rules), the founder's portrait with a pencil moustache whose curls are drawn left-handed, and the wall bell. Outside the west windows there is a courtyard with the founder's oak and pigeons on the ledges, the grass and a branch. All of these can be read by looking at them. Ch.6 adds "SINCE 1953" to the banner and a "LAST YEAR: 412 ITEMS FOUND" poster. Each desk Voss inspects now shows what he names: pencils, a horse drawing, nothing, a 'help' note. He takes away what he confiscates. Unused verdicts (gum, eraser) were removed. Posters checked: no overlaps. The two classroom hits from the checker were posters in different chapters, and the corridor sign is on a closed door. ScenesVersion is now 24.
- Result: Same win and loss conditions in every chapter except Ch.5, which now ends only after all 8 words are searched and the record has been read. 2x is optional.

## Interaction 020 — Fix: Chinese font atlas overflow on chapter load

- Date: 2026-11-02
- Who asked: Ariel & Nina
- Request: Starting a chapter showed "Failed to update dynamic font (PingFang SC) texture; all the needed characters do not fit onto a single texture (max size 4096)".
- What changed: Wall posters use world-space canvases laid out at 1000 units per metre. They rendered their letters at 3x that size, so a banner title became a glyph of several hundred pixels. With the larger Chinese text added (Charter, posters), the shared Chinese font texture filled up. Posters now render text at 0.5 texture px per unit (500 px per metre). This is set at runtime in `TypewriterFont`, so existing scenes are fixed without a rebuild, and new ones get it from `UIFactory.MakeWorldCanvas`.
- Result: Look only (poster text slightly softer up close). Gameplay unchanged.

## Interaction 021 — Posters that change, pigeons with a job, Voss's beard, a basement that turns its head

- Date: 2026-11-02
- Who asked: Ariel & Nina
- Request: Posters should have an easter egg when you walk up to them: smile posters show an uncanny smile, glitch-related ones glitch, lunch posters show an inedible lunch, uniform or photo posters slowly become a mirror (with a neutral player model), red-X posters become a red X, and Voss posters become Voss. Give Voss a full beard as the only one in the school. Ch.3 should get to the phone faster. In Ch.1, the desk search should open right after the player answers roll call. The Ch.4 photo countdown goes from 10 s to 7 s. The pigeons should carry through the game and matter later: in the library the librarian goes to the window to shoo them, warns you if you are close, with set-up beforehand and soft coo sounds. The library walking noise should climb exponentially. Ch.6 sending the message should move faster afterwards, while Voss's verdicts get longer pauses. Boys' side fringe should connect to the top hair. In the basement, everyone slowly turns to look at Voss and turns back after he closes the door, and the phone stays usable with everyone chatting on it.
- What changed: New `PosterEgg` (built per poster name by `AddEggs`):
  - Smile: Poster_Smile, Poster_WholeFace. A symmetric toothy smile with blank eyes that follow you.
  - Glitch: Sign_Devices, Sign_LiveAddress, Banner_DaysSinceIncident, Sign_NotHere. Colour ghosts, jitter, scrambled letters and a soft glitch sound.
  - Lunch: Poster_Menu, Poster_Privilege. A grey tray, grey food, a hair in the soup and a fly.
  - Mirror: Poster_Uniform, Poster_CitizenCard, Photo_1967. A render-texture mirror of the player.
  - Red X: Poster_CardsReady. The text turns red, then a red X is drawn.
  - Voss: Poster_InspectedDesk, Display_Yearbook1980. His bearded portrait, with eyes that follow you.

  Other changes:
  - Player model: a neutral body (straight bob, uniform, trousers) on layer 30. Only mirrors render it; the player's camera skips it.
  - Voss: a full beard on him in the classroom and in the basement doorway.
  - Hair: boys' fringe now overlaps the top hair, so there is no gap.
  - Ch.1: the desk cubby unlocks right after "Here".
  - Ch.3: the opening authority line is merged, the girl walks off without being waited for, the first bed check is shorter, and message gaps are shorter.
  - Ch.4: callSeconds is 7.
  - Pigeons: synthesized coo and wing-flap sounds, soft (3D, max 9 m), on ledges in the courtyard, gym and library.
  - Ch.2: a new PA line, "Do not feed the pigeons. Pigeons are loud. Loud things are noticed."
  - Ch.5 pigeons: Triangle warns "stay off the windows". Every 40–60 s the flock gets noisy and the librarian walks to the window to shoo them. Within 2.6 m of the window spot during her 4 s there = a warning.
  - Ch.5 noise: walking without a break doubles the noise rate every 2 s, up to 5x. Quiet steps don't count.
  - Ch.6: each verdict now shows 4.2 s plus a 3.6 s silence. Lines after the send are about 35% shorter.
  - Basement: head parts are re-parented to a neck pivot so heads turn one after another towards the door and back. The phone stays usable and a group chat arrives.

  ScenesVersion is now 25.
- Result: Ch.5 is harder (exponential noise, pigeon events). Ch.4's photo call is tighter. The other changes are atmosphere and pacing. Win/loss conditions are unchanged.

## Interaction 022 — Staring pigeon, faster lesson, landscapes outside every window

- Date: 2026-11-02
- Who asked: Ariel & Nina
- Request: The pigeon must actually stare when the text says it does. The Ch.1 lecture and the chalk-triangle exchange were still too slow at 1x. The librarian should not walk onto her desk. The smile poster should have no teeth, only big eyes, a big mouth and dimples. Fix the overlapping door signs in Ch.2 and check every sign in the game. Posters should change more slowly, after the player has read them. At 2x speed the Ch.3 warden should walk faster too. The windows should show trees and buildings, not an empty plane.
- What changed:
  - Pigeon: one ledge pigeon (`starer`) turns and stares at you within 12 m.
  - Ch.1: lecture lines now last 2.4–5 s depending on length (was 7.5 s, plus a 4.4 s pause). The pause between lines is 1.1 s plus 0.25 s. The shoulder-tap scene is about half as long.
  - Library: the desk, returns cart, shelves and crates are Not Walkable in the NavMesh, and the librarian's desk spot moved to z 1.5.
  - Smile egg: no teeth. Big eyes, a thick arc mouth, dimples and blush.
  - Ch.2: "IN SESSION" now hangs on the back door itself, clear of the CLASSROOM 1 plaque. All scene signs were rechecked and there are no other overlaps.
  - Poster eggs: a poster only starts to change after you have looked at it for a reading time (2.5–7 s by text length), then changes over 5–7 s.
  - Ch.3: at 2x the warden's walking speed and pauses scale too (`PatrolAI.timeScale`).
  - New `Backdrop` in every scene: lawn, a school wall 16 m out, about 46 trees 4.5–14 m out, and about 12 buildings 24–36 m out (lit windows at night). Nothing is placed inside the playable rectangle, and nothing has colliders.
  - ScenesVersion is now 26.
- Result: Pacing and look only. Win/loss unchanged.

- Follow-up (same day): poster eggs a little faster - reading time 1.5-4.5 s (was 2.5-7), change 3 s (mirror / Voss 4.5 s, glitch 0.8 s).
- Follow-up: poster eggs now start counting only while the crosshair is ON the poster (from up to 3-5 m), then keep changing while you look its way. Reading time 0.8-2.2 s, change 1.8 s (mirror / Voss 2.5 s, glitch 0.5 s).
- Follow-up: trigger back to the original (within 2-3.6 m and within 38 degrees of the view centre; not crosshair-only). The faster timings stay (reading 0.8-2.2 s, change 1.8 s / 2.5 s / 0.5 s).
- Follow-up: poster eggs faster again at 1x - reading 0.4-1.2 s, change 1 s (mirror / Voss 1.4 s, glitch 0.3 s); 2x story speed doubles both. More glitch posters, mostly the ones that insist on a date: Poster_Catalogue (1967), Poster_BannedBooks (1981), Poster_Founding (1952), Sign_LightsOut (21:00), Banner_PictureDay (10:45), Poster_Counting, Sign_Repair. The table now lives in PosterEgg.Table and is also applied at runtime (PosterEgg.InstallAll), so no rebuild is needed.
- Follow-up: Ch.4 class-photo countdown back to 10 s (callSeconds). ScenesVersion 27.
