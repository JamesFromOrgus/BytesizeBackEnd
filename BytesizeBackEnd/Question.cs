namespace BytesizeBackEnd;


public class Question(int experience)
{
    private int experience = experience;

    public void OnCompletion(DatabaseManager db, int userID, bool success)
    {
        if (!success) return;
        db.AwardExperience(userID, experience);
    }
}