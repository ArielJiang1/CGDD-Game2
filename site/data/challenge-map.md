# How the challenge changes

Look first at whether win and loss change, then at whether the player's verb changes. If neither changes, it is the same game, just tighter or looser.

| Change | Win/loss changed | Verb changed | Data | Script.field |
| --- | --- | --- | --- | --- |
| Start: Ch.1 classroom, one teacher who watches and asks questions | — (baseline: win = contact the student in front; loss = 3 warnings, phone taken) | — (watch, peek, act, hide) | noticeSeconds=2.5, chances=3 | LevelDirectorBase.noticeSeconds, LevelDirectorBase.chances |
| Teacher questions about the lecture | No (a wrong answer costs a warning) | Adds *listen* | answerSeconds=10 | Level01Director.answerSeconds |
| Ch.2 cafeteria: a sweeping camera + a roaming Lunch Monitor | Win changes (deliver the message, reach the STAFF door) | Adds *walk & position*: where you stand decides who sees you | reachDistance=4, viewAngle=90 | Level02Director.reachDistance, TeacherVision.viewAngle |
| Ch.2 the girl you find by sound | No | Adds *listen to locate* | maxDistance=6.5 | HumSource.maxDistance |
| Ch.3 dark dorm, the warden is mostly heard, not seen | Win changes (flash the window 3 times) | Adds *pretend to sleep* (hold Space) | lightNoticeSeconds=1.6, awakeNoticeSeconds=3.2 | Level03Director.lightNoticeSeconds, Level03Director.awakeNoticeSeconds |
| Ch.4 desk inspection + final choice | Win splits into two endings (hide it / send it to everyone) | Adds *decide* | choiceSeconds=14, inspectionFallbackSeconds=100 | Level04Director.choiceSeconds, Level04Director.inspectionFallbackSeconds |
| Less frequent warden visits | No | No (looser pressure) | wait 24–38 s between visits | Level03Director (WardenLoop) |
