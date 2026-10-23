# Tunables

Generated from `@tune` headers by the menu `CGDD/Export Trace Docs`. Hand edits are overwritten on the next export.

| Script | Field | Type | Range | Default | Note |
| --- | --- | --- | --- | --- | --- |
| AmbientAudio | droneVolume | float | 0-0.5 | 0.16 | background drone level |
| AmbientAudio | ventVolume | float | 0-0.5 | 0.09 | ventilation hum level |
| Fluorescent | buzzVolume | float | 0-0.3 | 0.07 | fluorescent buzz loudness |
| Footsteps | stepLength | float | 0.3-1.2 | 0.7 | metres per step sound |
| Footsteps | volume | float | 0-1 | 0.55 | footstep loudness |
| Footsteps | maxDistance | float | 5-30 | 18 | how far footsteps carry (m) |
| Footsteps | keysEverySteps | int | 0-10 | 0 | key jingle every N steps (0 = never) |
| HumSource | volume | float | 0-1 | 0.8 | humming loudness |
| HumSource | maxDistance | float | 2-15 | 6.5 | from how far you can start to hear her (m) |
| Level01Director | answerSeconds | float | 4-20 | 10 | time to answer when the teacher calls on you |
| Level01Director | firstCallAtBootSecond | float | 0-5 | 3 | the first question comes this many seconds into the phone start-up |
| Level02Director | reachDistance | float | 2-8 | 4 | how close you must be to the girl for the message to send |
| Level03Director | lightNoticeSeconds | float | 0.5-4 | 1.6 | seconds his flashlight needs on the phone screen |
| Level03Director | awakeNoticeSeconds | float | 1-6 | 3.2 | seconds he needs to see you sitting up during a bed check |
| Level04Director | answerSeconds | float | 4-20 | 10 | time to answer his questions |
| Level04Director | inspectionFallbackSeconds | float | 60-300 | 100 | the inspection starts anyway this long after you take the phone |
| Level04Director | choiceSeconds | float | 5-30 | 14 | time to decide when he is one desk away |
| LevelDirectorBase | chances | int | 1-5 | 3 | warnings before the phone is taken (never shown as a number) |
| LevelDirectorBase | noticeSeconds | float | 0.5-6 | 2.5 | seconds a watcher must see the phone before a warning |
| LevelDirectorBase | dangerDecayPerSecond | float | 0.1-1 | 0.35 | how fast danger drains once nobody is looking |
| LevelDirectorBase | graceAfterWarning | float | 0-6 | 3 | seconds of safety right after a warning |
| LevelDirectorBase | typeCharsPerSecond | float | 20-120 | 55 | subtitle typewriter speed |
| PatrolAI | scanAngle | float | 0-90 | 55 | how far he looks left/right while standing |
| PatrolAI | scanSpeed | float | 0.1-2 | 0.6 | how fast he looks around |
| PhoneController | bootSeconds | float | 1-10 | 5 | start-up time (lowering the phone resets it) |
| PhoneController | raiseSeconds | float | 0.1-1 | 0.25 | time to raise the phone |
| PhoneController | readCharsPerSecond | float | 5-60 | 22 | message reading speed (only while the phone is up) |
| PhoneController | glowIntensity | float | 0-4 | 1.6 | how much the screen lights your face (and gives you away at night) |
| PlayerRig | walkSpeed | float | 1-5 | 2.8 | walking speed (m/s) |
| PlayerRig | sensitivity | float | 1-10 | 5 | mouse look speed |
| PlayerRig | pullSpeed | float | 0.5-3 | 1.3 | speed when the class watches you walk to your seat |
| TeacherAI | lectureWeight | float | 0-1 | 0.35 | chance he lectures facing the class |
| TeacherAI | boardWeight | float | 0-1 | 0.3 | chance he turns to the board (safe window) |
| TeacherVision | viewAngle | float | 30-180 | 90 | width of the view cone (degrees) |
| TeacherVision | viewRange | float | 3-20 | 12 | how far he can see (m) |

