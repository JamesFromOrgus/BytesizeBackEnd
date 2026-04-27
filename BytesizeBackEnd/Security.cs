using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konscious.Security.Cryptography;
using Org.BouncyCastle.Asn1.Cmp;

namespace BytesizeBackEnd;

public class Security
{
    //private static readonly string _tokenCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateToken(int length)
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(length/2));
    }

    public static bool IsAlphabetical(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z]+$");
    }
    
    public static bool IsValidUsername(string name)
    {
        return name.Length > 5 && Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            MailAddress m = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static Dictionary<string, string> TokenReturnBody(string token)
    {
        Dictionary<string, string> body = new Dictionary<string, string>
        {
            {"access_token", token},
            {"token_type", "bearer"}
        };
        return body;
    }

    public static string HashPassword(string password, string salt)
    {
        byte[] passwordAsBytes = Encoding.ASCII.GetBytes(password);
        byte[] saltAsBytes = Encoding.ASCII.GetBytes(salt);
        var hashingAlgorithm = new Argon2id(passwordAsBytes);
        hashingAlgorithm.DegreeOfParallelism = 4;
        hashingAlgorithm.Iterations = 1;
        hashingAlgorithm.MemorySize = 65536;
        hashingAlgorithm.Salt = saltAsBytes;

        byte[] hashAsBytes = hashingAlgorithm.GetBytes(256);
        string hash = Encoding.UTF8.GetString(hashAsBytes);
        return hash;
    }
}