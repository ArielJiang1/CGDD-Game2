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
