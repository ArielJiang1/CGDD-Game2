# COVER - v1 (4 chapters, ~20 min)

## Build the scenes
After a code update Unity asks "Game scenes are out of date - rebuild?" -> **Rebuild now**.
Or menu **Game > Build All Scenes (Title, 4 Chapters, End)**. Hand edits in the game scenes are lost;
`Assets/Blockout/Scenes/Blockout_School.unity` is never changed. Open `Scenes/Title` and press Play.
Title > Chapters jumps to any chapter (for playtests). Progress + name are saved (Continue).

## Flow
Title (New Game / Continue / Chapters) -> Ch.1 -> End page -> Ch.2 -> ... -> Ch.4 -> Ending 1 or 2 + credits.
Caught (3rd warning) -> GAME OVER page -> Retry restarts the same chapter.

| Ch | Scene | Threat | Goal |
|----|-------|--------|------|
| 1 The Lesson | Level01_Lesson | teacher, questions | find the phone (1952), Photos (2100), tap the student with the chalk triangle |
| 2 Lunch | Level02_Lunch | sweeping camera + Lunch Monitor | Messages (1240, PA says it), find the girl who HUMS by sound, forward to her within reach, sneak behind the counters to the STAFF door when the camera faces the windows |
| 3 Lights Out | Level03_LightsOut | night warden (listen: footsteps + keys, flashlight) | HOLD Space = lie down/pretend to sleep; reply to Never Blinks; Contacts (0304 = the room he says); flash the screen at the window 3x |
| 4 Inspection Day | Level04_Inspector | Instructor Voss inspects every desk | get the phone back, answer questions, find the 1979 photo (VOSS, same seat); at your desk choose 1 hide (Ending THE SEAT) or 2 send to everyone (Ending LIGHTS) |

## Playtest
The OBJECTIVE box (top-left) always says the next step. **F8** skips the current chapter.

## Controls
WASD walk, Mouse look, E interact, Space sit up / stand up (Ch.3: HOLD Space = lie down),
HOLD Right Mouse = phone, number keys = codes, A/D choose, Left Mouse open/select (hold = send), Q back,
1/2/3 answer (phone down first).

## Art pass (Editor/ClearviewArt.cs)
Procedural textures (walls with water stains / mould / peeling paint, vinyl floor, ceiling tiles, laminate),
Clearview palette (clinical white, blue-green dado, dirty green furniture, deep red = authority, ironic pastels),
fluorescent grid (some flicker), posters/signs (typewriter font), cameras with blinking LEDs, PA speakers,
working wall clocks, URP post (Day / Night profiles in Generated/Clearview). All sound is synthesised (AudioSynth.cs).

## Tune (Inspector)
- PLAYER > LevelXXDirector: noticeSeconds, chances, subtitle timings, chapter-specific settings
- PLAYER > PhoneController: codes, hints, contacts, photo captions
- Teacher/Monitor/Warden: TeacherAI / PatrolAI timings, TeacherVision viewAngle & viewRange
