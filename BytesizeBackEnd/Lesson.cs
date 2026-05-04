namespace BytesizeBackEnd;

public class Lesson
{
    private static Dictionary<int, int[]> lessons = new Dictionary<int, int[]>
    {
        { 1, [3, 2, 4, 3, 3]},
        { 2, [5, 3, 2, 2]},
        { 3, [5, 2, 3, 4]},
        { 4, [3, 3, 4, 5, 4, 2]},
        { 5, [1, 4, 2, 4, 3]},
        { 6, [4, 1]},
        { 7, [2, 3]},
        { 8, [2, 4, 1]},
        { 9, [3, 4]},
        { 10, [1, 1]},
        { 11, [3, 2, 3, 5]}
    };
    static Dictionary<int, Lesson> userLessons = new Dictionary<int, Lesson>();
    
    private List<Question> questions = [];
    private int userID;
    
    public Lesson(int[] questionExperience, int userID)
    {
        for (int i = 0; i < questionExperience.Length; i++)
        {
            Question question = new Question(questionExperience[i]);
            questions.Add(question);
        }
        this.userID = userID;
        userLessons[userID] = this;
    }

    public bool CompleteQuestion(DatabaseManager db, int userID, bool success)
    {
        if (questions.Count == 0) return false;
        Question question = questions[0];
        question.OnCompletion(db, userID, success);
        questions.RemoveAt(0);
        if (questions.Count == 0)
        {
            db.AwardLessonCompletion(this.userID);
            userLessons.Remove(userID);
        }

        return true;
    }

    public static Lesson? GetLesson(int userID)
    {
        return userLessons.ContainsKey(userID) ? userLessons[userID] : null;
    }

    public static bool StartLesson(int userID, int lessonNumber)
    {
        Console.WriteLine($"Starting Lesson {lessonNumber}");
        if (!lessons.ContainsKey(lessonNumber))
        {
            Console.WriteLine($"Couldn't find lesson {lessonNumber}");
            return false;
        }
        int[] lessonExperience = lessons[lessonNumber];
        Lesson lesson = new Lesson(lessonExperience, userID);
        Console.WriteLine(GetLesson(userID) == null ? "failed" : "succeeded");
        return true;
    }
}