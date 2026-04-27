using MySql.Data.MySqlClient;

namespace BytesizeBackEnd;

public class CommandParameter(string name, object value)
{
    public readonly string Name = name;
    public readonly object Value = value;
}