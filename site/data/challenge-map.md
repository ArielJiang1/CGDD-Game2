# How the challenge changes

Look first at whether win and loss change, then at whether the player's verb changes. If neither changes, it is the same game, just tighter or looser.

| Change | Win/loss changed | Verb changed | Data | Script.field |
| --- | --- | --- | --- | --- |
| Start: Ch.1 classroom, one teacher who watches and asks questions | — (baseline: win = contact the student in front; loss = 3 warnings, phone taken) | — (watch, peek, act, hide) | noticeSeconds=2.5, chances=3 | LevelDirectorBase.noticeSeconds, LevelDirectorBase.chances |
| Teacher questions about the lecture | No (a wrong answer costs a warning) | Adds *listen* (or look out of the window: the oak is there) | answerSeconds=10 | Level01Director.answerSeconds |
| Ch.2 cafeteria: a sweeping camera + a roaming Lunch Monitor | Win changes (deliver the message, reach the STAFF door) | Adds *walk & position*: where you stand decides who sees you | reachDistance=4, viewAngle=90 | Level02Director.reachDistance, TeacherVision.viewAngle |
| Ch.2 the girl you find by sound | No | Adds *listen to locate* | maxDistance=6.5 | HumSource.maxDistance |
| Ch.3 dark dorm, the warden is mostly heard, not seen | Win changes (flash the window 3 times) | Adds *pretend to sleep* (hold Space) | lightNoticeSeconds=1.6, awakeNoticeSeconds=3.2 | Level03Director.lightNoticeSeconds, Level03Director.awakeNoticeSeconds |
| Less frequent warden visits | No | No (looser pressure) | wait 24–38 s between visits | Level03Director (WardenLoop) |
| Posters you can read (walk up and look at one) | No | Adds *read*: every poster, sign and label gives a thought, a clue, a Search word, or changes when read | readSeconds=0.6 | LevelDirectorBase.readSeconds, Readable.range |
| New Ch.4 Picture Day (gym): prefects who report, the photographer, CAMERA app | Win changes (photograph 3 things, send them, stand in the final class photo) | Adds *photograph*: aim, the shutter can be heard, the flash can be seen | shutterHearing=2.2, flashRange=15 | PictureDayDirector.shutterHearing, PictureDayDirector.flashRange |
| Ch.4 class-photo rhythm: free time, then a countdown to a yellow X that moves every time | No (a missed / moving / unsmiling / phone-in-photo shot = a warning) | Adds *be in place on time* + *smile* (hold Space) + *find your new spot* | freeSeconds=42, firstFreeSeconds=55, callSeconds=10 | PictureDayDirector.freeSeconds, PictureDayDirector.callSeconds |
| Ch.4 prefects must be close to notice | No | No (looser) | shutterHearing=2.2, prefect view 6 m / 90° | PictureDayDirector.shutterHearing, TeacherVision.viewRange |
| New Ch.5 The Archive (library): the librarian's noise meter, SEARCH app | Win changes (find and read Records, vol. 31) | Adds *move quietly* and *search with words you have read* | noiseWalk=0.08, noiseRun=0.45, noiseDecay=0.12 | ArchiveDirector.noiseWalk, ArchiveDirector.noiseRun, ArchiveDirector.noiseDecay |
| Ch.5 silent mode that fails at random, quiet steps, turning old pages, a second librarian | No | Adds *silence the phone* (M, and check the bell every time), *sneak* (Ctrl/C), *read slowly* (A/D pages; fast = loud) | silentFailChance=0.3, noiseSneak=0.012, flipNoise=0.16, fastFlipFactor=2.6 | ArchiveDirector.silentFailChance, ArchiveDirector.noiseSneak, ArchiveDirector.flipNoise, ArchiveDirector.fastFlipFactor |
| Ch.5 walking noise builds up exponentially | No | No (tighter: stop often) | doubles every 2 s, up to 5x | ArchiveDirector.walkDoubling, ArchiveDirector.walkMaxFactor |
| Ch.5 pigeons on the window ledge get noisy; the librarian comes to shoo them | No (standing near the window when she comes = a warning) | Adds *keep away from the windows* when the flock gets loud | every 40–60 s, pigeonNear=2.6 | ArchiveDirector.pigeonEvery, ArchiveDirector.pigeonNear |
| Ch.5 research must be complete: SEARCH all 8 words (progress n / 8 in the objective) | Win changes (record read AND all 8 words searched) | Adds *gather every word before the ending* | 8 words | ArchiveDirector.AllWords |
| Inspection Day moves to Ch.6 (Friday, "day five") | No (same two endings) | No | — | Level04Director |
| Ch.6 desk inspection + final choice | Win splits into two endings (hide it / send it to everyone) | Adds *decide* | choiceSeconds=14, inspectionFallbackSeconds=100 | Level04Director.choiceSeconds, Level04Director.inspectionFallbackSeconds |
| 2x story speed (Tab / title button) | No | No (player-chosen pacing of dialogue and waits; watchers' timers you play against stay real-time, except the Ch.3 warden) | Factor 2 | GameSpeed.Factor |
