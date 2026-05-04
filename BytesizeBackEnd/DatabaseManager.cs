using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Exception = System.Exception;

// use NuGet to get this

namespace BytesizeBackEnd;

public class DatabaseManager(string connectionString)
{
    private string _connectionString = connectionString;
    
    public string ExecuteString(string query, params CommandParameter[] parameters)
    {
        MySqlConnection connection = new MySqlConnection(_connectionString);
        connection.Open();
        MySqlCommand cmd = new MySqlCommand(query, connection);
        foreach (CommandParameter parameter in parameters)
        {
            cmd.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }
        // Console.WriteLine("----------------------------");
        // Console.WriteLine(cmd.CommandText);
        MySqlDataReader rdr = cmd.ExecuteReader();
        List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
    
        while (rdr.Read())
        {
            Dictionary<string, object> row = new Dictionary<string, object>();
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                row[rdr.GetName(i)] = rdr.GetValue(i);
            }
            rows.Add(row);
        }
    
        rdr.Close();
        cmd.Dispose();
        connection.Close();
        
        return JsonConvert.SerializeObject(rows);
    }

    public string GetSalt(string username)
    {
        try
        {
            var user = GetUserFromUsername(username);
            return (string)user["PasswordSalt"];
        }
        catch (Exception)
        {
            return "";
        }
    }
    
    public string GetSalt(int userID)
    {
        try
        {
            var user = GetUser(userID);
            return (string)user["PasswordSalt"];
        }
        catch (Exception)
        {
            return "";
        }
    }

    private int InsertInto(string table, List<string> keys, List<object> values)
    {
        string[] valueReferences = keys.ToArray();
        CommandParameter[] parameters = new CommandParameter[valueReferences.Length];
        for (int i = 0; i < valueReferences.Length; i++)
        {
            valueReferences[i] = "@" + valueReferences[i] + "Value";
            parameters[i] = new CommandParameter(valueReferences[i], values[i]);
        }

        using MySqlConnection connection = new MySqlConnection(_connectionString);
        connection.Open();

        // Insert
        using (MySqlCommand cmd = new MySqlCommand(
                   $"insert into {table}({string.Join(", ", keys)}) values ({string.Join(", ", valueReferences)})",
                   connection))
        {
            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Name, p.Value);
            cmd.ExecuteNonQuery();
        }
        
        using (MySqlCommand cmd = new MySqlCommand("SELECT LAST_INSERT_ID() AS id", connection))
        {
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    public void UpdateRecord(string table, string idFieldName, object idFieldValue, Dictionary<string, object> values)
    {
        if (values == null || values.Count == 0)
            throw new ArgumentException("No values provided for update.");

        List<string> setClauses = new List<string>();
        List<CommandParameter> parameters = new List<CommandParameter>();

        foreach (var kvp in values)
        {
            string paramName = "@" + kvp.Key + "Value";
            setClauses.Add($"{kvp.Key} = {paramName}");
            parameters.Add(new CommandParameter(paramName, kvp.Value));
        }
        
        string idParamName = "@IdValue";
        parameters.Add(new CommandParameter(idParamName, idFieldValue));

        string query = $"UPDATE {table} SET {string.Join(", ", setClauses)} WHERE {idFieldName} = {idParamName}";

        ExecuteString(query, parameters.ToArray());
    }

    public int CreateUser(string username, string emailAddress, string passwordHash, string passwordSalt)
    {
        // Preferences
        int preferencesID = InsertInto("Preferences", new List<string>(), new List<object>());
        // UserInformation
        int userInformationID = InsertInto("UserInformation",
            new List<string>{"Username", "EmailAddress", "ProfilePictureURL"},
            new List<object>{username, emailAddress, "HeadphoneJake.png"});
        // Statistics
        int statisticsID = InsertInto("Statistics", new List<string>(), new List<object>());
        // SecurityInformation
        int securityInformationID = InsertInto("SecurityInformation",
            new List<string>{"PasswordSalt", "PasswordHash"},
            new List<object>{passwordSalt, passwordHash});
        
        // User
        int userID = InsertInto("User",
            new List<string>{"PreferencesID", "UserInformationID", "StatisticsID", "SecurityInformationID"},
            new List<object>{preferencesID, userInformationID, statisticsID, securityInformationID});
        return userID;
    }

    public Dictionary<string, string> Login(string username, string passwordHash)
    {
        Dictionary<string, object>? result = GetFirstEntry(
            ExecuteString(@"select UserID from User
            inner join UserInformation on User.UserInformationID=UserInformation.UserInformationID
            inner join SecurityInformation on User.SecurityInformationID=SecurityInformation.SecurityInformationID
            where UserInformation.Username = @Username and SecurityInformation.PasswordHash = @PasswordHash",
                new CommandParameter("@Username", username), new CommandParameter("@PasswordHash", passwordHash))
        );
        if (result == null) return new Dictionary<string, string> {{ "message", "Invalid credentials." }};
        int userID = Convert.ToInt32(result["UserID"]);
        
        // credentials valid, create token
        return Security.TokenReturnBody(GenerateToken(userID));
    }

    public string GenerateToken(int userID)
    {
        string token = Security.GenerateToken(32);
        InsertInto("Session", 
            new List<string>{"SessionToken", "UserID"},
            new List<object>{token, userID});
        return token;
    }
    
    public Dictionary<string, object> GetUserFromUsername(string username)
    {
        string encoded = ExecuteString(@"select * from User
            inner join UserInformation on User.UserInformationID=UserInformation.UserInformationID
            inner join SecurityInformation on User.SecurityInformationID=SecurityInformation.SecurityInformationID
            where UserInformation.Username = @Username", new CommandParameter("@Username", username));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return result;
    }

    public int GetUserInformationID(int userID)
    {
        string encoded = ExecuteString(@"
            select UserInformationID from User
            where UserID = @UserID",
            new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return Convert.ToInt32(result["UserInformationID"]);
    }
    
    public int GetSecurityInformationID(int userID)
    {
        string encoded = ExecuteString(@"
            select SecurityInformationID from User
            where UserID = @UserID",
            new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return Convert.ToInt32(result["SecurityInformationID"]);
    }

    public void SetUserIcon(int userID, string icon)
    {
        var user = GetUser(userID);
        if (user == null) throw new Exception();
        UpdateRecord("UserInformation", "UserInformationID", user["UserInformationID"], new Dictionary<string, object>
        {
            ["ProfilePictureURL"] = icon+".png"
        });
    }

    public bool UsernameInUse(string username)
    {
        string encoded = ExecuteString(@"select UserInformationID
            from UserInformation
            where UserInformation.Username = @Username", new CommandParameter("@Username", username));
        var result = ToEntryList(encoded);
        if (result == null) throw new Exception();
        return result.Count > 0;
    }
    
    public bool EmailInUse(string emailAddress)
    {
        string encoded = ExecuteString(@"select UserInformationID
            from UserInformation
            where UserInformation.EmailAddress = @Email", new CommandParameter("@Email", emailAddress));
        var result = ToEntryList(encoded);
        if (result == null) throw new Exception();
        return result.Count > 0;
    }
    
    public Dictionary<string, object> GetUserStats(int userID)
    {
        string encoded = ExecuteString(@"select Statistics.Experience,
            Statistics.LessonCount, Statistics.CourseCount, Statistics.JoinDate
            from User inner join Statistics on User.StatisticsID=Statistics.StatisticsID
            where User.UserID = @UserID", new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return result;
    }
    
    public Dictionary<string, object> GetUserInformation(int userID)
    {
        string encoded = ExecuteString(@"select UserInformation.Username, UserInformation.ProfilePictureURL,
            UserInformation.FirstName, UserInformation.LastName, UserInformation.EmailAddress, UserInformation.DateOfBirth
            from User inner join UserInformation on User.UserInformationID=UserInformation.UserInformationID
            where User.UserID = @UserID", new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return result;
    } 

    public int? GetUserIDFromToken(string sessionToken)
    {
        string encoded = GetSession(sessionToken);
        var result = GetFirstEntry(encoded);
        if (result == null) return null;
        return Convert.ToInt32(result["UserID"]);
    }
    
    public Dictionary<string, object> GetUser(int userID)
    {
        string encoded = ExecuteString(@"
            select * from User
            where User.UserID = @UserID
            ", new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return result;
    } 

    public bool EndSession(string sessionToken)
    {
        return ExecuteString(@"delete from Session
            where Session.SessionToken = @Token", new CommandParameter("@Token", sessionToken)) != null;
    }

    public void SecureSignOut(int userID)
    {
        ExecuteString(@"
            delete from Session
            where Session.UserID = @UserID
            ", new CommandParameter("@UserID", userID));
    }
    
    public int GetStatsID(int userID)
    {
        string encoded = ExecuteString(@"
            select StatisticsID from User
            where UserID = @UserID",
            new CommandParameter("@UserID", userID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return Convert.ToInt32(result["StatisticsID"]);
    }

    public int GetExperience(int userID)
    {
        int statsID = GetStatsID(userID);
        string encoded = ExecuteString(@"
            select Experience from Statistics
            where StatisticsID = @statsID",
            new CommandParameter("@statsID", statsID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return Convert.ToInt32(result["Experience"]);
    }

    public void AwardExperience(int userID, int experience)
    {
        int statsID = GetStatsID(userID);
        int currentExperience = GetExperience(userID);
        UpdateRecord("Statistics", "StatisticsID", statsID, new Dictionary<string, object>
        {
            ["Experience"] = currentExperience + experience
        });
    }
    
    public int GetLessonsCompleted(int userID)
    {
        int statsID = GetStatsID(userID);
        string encoded = ExecuteString(@"
            select LessonCount from Statistics
            where StatisticsID = @statsID",
            new CommandParameter("@statsID", statsID));
        var result = GetFirstEntry(encoded);
        if (result == null) throw new Exception();
        return Convert.ToInt32(result["LessonCount"]);
    }

    public void AwardLessonCompletion(int userID)
    {
        int statsID = GetStatsID(userID);
        int currentLessons = GetLessonsCompleted(userID);
        UpdateRecord("Statistics", "StatisticsID", statsID, new Dictionary<string, object>
        {
            ["LessonCount"] = currentLessons + 1
        });
    }

    public string GetSession(string sessionToken)
    {
        return ExecuteString(@"select User.UserID from Session
            inner join User on Session.UserID=User.UserID
            inner join UserInformation on User.UserInformationID=UserInformation.UserInformationID
            where Session.SessionToken = @Token", new CommandParameter("@Token", sessionToken));
    }

    public Dictionary<string, object>? GetFirstEntry(string json)
    {
        var list = ToEntryList(json);
        if (list == null || list.Count == 0) return null;
        return list[0];
    }
    
    public List<Dictionary<string, object>>? ToEntryList(string json)
    {
        return JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
    }
}