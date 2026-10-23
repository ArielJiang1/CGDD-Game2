using System.Collections.Generic;
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// What the teacher says, and what he may ask. Questions are only asked about things he has
    /// ALREADY said - so listening to him matters. Edit freely in the Inspector (on the Teacher).
    /// </summary>
    [System.Serializable]
    public class LessonLine
    {
        [TextArea] public string text;
        public LessonLine(string t) { text = t; }
    }

    [System.Serializable]
    public class LessonQuestion
    {
        [Tooltip("Index of the lesson line this question is about. -1 = can be asked any time.")]
        public int aboutLine = -1;
        [TextArea] public string question;
        public string[] options = new string[3];
        [Tooltip("0, 1 or 2")]
        public int correct;

        public LessonQuestion(int line, string q, string a, string b, string c, int right)
        {
            aboutLine = line; question = q; options = new[] { a, b, c }; correct = right;
        }
    }

    public static class LessonDefaults
    {
        // The phone code is the founding year (line 1); the Photos code is lights-out on a 24-hour clock (line 5).
        public static List<LessonLine> Lines()
        {
            return new List<LessonLine>
            {
                new LessonLine("Today we continue with the Founding Charter of our school."),
                new LessonLine("Our school was founded in 1952. Nineteen fifty-two. Far away from the noise of the city."),
                new LessonLine("The Charter has four rules. Rule one: what is seen, is safe."),
                new LessonLine("Every device in this building is monitored. That is how we keep you safe."),
                new LessonLine("Rule two: no voice leaves these walls."),
                new LessonLine("Rule three: lights out is at nine. Twenty-one hundred. Exactly."),
                new LessonLine("The pigeons in the courtyard are not monitored. ...Yet."),
                new LessonLine("The founder planted the oak tree in the courtyard on the very first day."),
                new LessonLine("Seven classes. Seven bells. Every single day."),
                new LessonLine("Whoever drew a moustache on the founder's portrait: I know you are left-handed."),
                new LessonLine("Rule four: we do not keep things from each other."),
                new LessonLine("A quiet room is a good room. Remember that."),
                new LessonLine("And the back row, by the window... I sat in that seat once. Before you. Before all of you."),
            };
        }

        /// <summary>Chapter 4 - Inspection Day. Same teacher, same rhythm; he finally says who he is.</summary>
        public static List<LessonLine> InspectionLines()
        {
            return new List<LessonLine>
            {
                new LessonLine("Inspection Day was established in 1953. One year after our founding."),
                new LessonLine("The Charter's first rule: what is seen, is safe."),
                new LessonLine("An inspected desk is a happy desk."),
                new LessonLine("Last year we found four hundred and twelve forbidden items. All of them are safe now."),
                new LessonLine("I have taught in this room for thirty-one years. I sat in it before that."),
                new LessonLine("There is nothing in this school I have not seen."),
                new LessonLine("Rule four: we do not keep things from each other. It is my favourite rule."),
                new LessonLine("Some of you think you are the first. You are never the first."),
            };
        }

        public static List<LessonQuestion> InspectionQuestions()
        {
            return new List<LessonQuestion>
            {
                new LessonQuestion(-1, "What is six times seven?", "42", "36", "48", 0),
                new LessonQuestion(0, "When was Inspection Day established?", "1952", "1953", "1979", 1),
                new LessonQuestion(2, "An inspected desk is a...?", "Happy desk", "Safe desk", "Clean desk", 0),
                new LessonQuestion(3, "How many forbidden items were found last year?", "214", "421", "412", 2),
                new LessonQuestion(4, "How long has he taught in this room?", "13 years", "31 years", "30 years", 1),
                new LessonQuestion(6, "Which rule is his favourite?", "Rule one", "Rule two", "Rule four", 2),
            };
        }

        public static List<LessonQuestion> Questions()
        {
            return new List<LessonQuestion>
            {
                new LessonQuestion(-1, "What is six plus seven?", "11", "13", "15", 1),
                new LessonQuestion(1, "In what year was our school founded?", "1925", "1962", "1952", 2),
                new LessonQuestion(2, "What is Rule one of the Charter?", "What is seen, is safe", "Silence is golden", "Hands on the desk", 0),
                new LessonQuestion(5, "When is lights out?", "22:00", "21:00", "20:00", 1),
                new LessonQuestion(7, "What did the founder plant in the courtyard?", "An oak tree", "A pine tree", "A rose garden", 0),
                new LessonQuestion(8, "How many bells ring every day?", "Five", "Nine", "Seven", 2),
                new LessonQuestion(4, "What is Rule two?", "No voice leaves these walls", "No running in the halls", "No phones after nine", 0),
                new LessonQuestion(6, "Which of these is NOT monitored?", "The library", "The courtyard pigeons", "The hallway", 1),
            };
        }
    }
}
