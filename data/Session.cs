namespace Rentalin;

static class Session
{
    public static int UserId { get; private set; }
    public static string Name { get; private set; } = "";
    public static string Role { get; private set; } = "";

    public static void Start(int id, string name, string role)
    {
        UserId = id;
        Name = name;
        Role = role;
    }

    public static void End()
    {
        UserId = 0;
        Name = "";
        Role = "";
    }
}