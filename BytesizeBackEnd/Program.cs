using System.Runtime.InteropServices.JavaScript;
using BytesizeBackEnd;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
// use NuGet to install these that u don't have ^^^

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "Paste your token here"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();
var api = app.MapGroup("/api");  // means all api endpoints will be preceded by /api
var profile = api.MapGroup("/profile");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    Console.WriteLine("Running development build.");
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

string? GetToken(string authHeader)
{
    if (!authHeader?.StartsWith("Bearer ") ?? true)
        return null;
    var token = authHeader["Bearer ".Length..];
    return token;
}

string? connectionString = app.Configuration.GetConnectionString("Default");
string? adminPassword = Environment.GetEnvironmentVariable("AdminPassword");
DatabaseManager db = new DatabaseManager(connectionString);

api.MapGet("/echo", (string msg) => Results.Ok(msg));

api.MapPost("/parse", (ParseRequest request) =>
{
    if (request.Password != adminPassword) return "Invalid password.";
    return db.ExecuteString(request.Query);
});

api.MapPost("/login", (LoginRequest request) =>
{
    Console.WriteLine($"Login: {request.Username}, Password: {request.Password}");
    string salt = db.GetSalt(request.Username);
    Console.WriteLine($"Salt: {salt}");
    string hash = Security.HashPassword(request.Password, salt);
    Console.WriteLine($"{request.Password} -> {hash}");
    Console.WriteLine(hash.Length);
    return Results.Ok(db.Login(request.Username, hash));
});

api.MapPost("/register", (RegisterRequest request) =>
{
    try
    {
        Console.WriteLine($"Register hit - Username: '{request.Username}', Email: '{request.Email}'");
        if (db.UsernameInUse(request.Username)) return Results.BadRequest("Username already in use.");
        if (db.EmailInUse(request.Email)) return Results.BadRequest("Email address already in use.");
        if (!Security.IsValidEmail(request.Email)) return Results.BadRequest("Invalid email address.");
        string salt = Security.GenerateToken(255);
        string hash = Security.HashPassword(request.Password, salt);
        int userID = db.CreateUser(request.Username, request.Email, hash, salt);
        return Results.Ok(Security.TokenReturnBody(db.GenerateToken(userID)));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CAUGHT: {ex}");
        return Results.Ok(ex.ToString());
    }
});

api.MapPost("/passwordreset", ([FromHeader(Name = "Authorization")] string authorization, PasswordChangeRequest request) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");

    int securityInformationID = db.GetSecurityInformationID((int)userID);
    
    string salt = db.GetSalt((int)userID);
    string hash = Security.HashPassword(request.NewPassword, salt);
    db.UpdateRecord("SecurityInformation", "SecurityInformationID", securityInformationID, new Dictionary<string, object>
    {
        ["PasswordHash"] = hash
    });
    db.SecureSignOut((int)userID);
    return Results.Ok();
});

api.MapPost("/logout", ([FromHeader(Name = "Authorization")] string authorization) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Ok("Token missing.");
    return db.EndSession(token) ? Results.Ok("Successfully signed out.") : Results.Problem("Failed to sign out.");
});

profile.MapGet("/info", ([FromHeader(Name = "Authorization")] string authorization) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");
    return Results.Ok(db.GetUserInformation((int)userID));
});

profile.MapPost("/info", ([FromHeader(Name = "Authorization")] string authorization, UserInformationChangeRequest request) =>
{
    Console.WriteLine(request.DateOfBirth);
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");
    int userInformationID = db.GetUserInformationID((int)userID);
    var updates = new Dictionary<string, object>();

    if (request.EmailAddress != null)
    {
        if (!Security.IsValidEmail(request.EmailAddress)) return Results.BadRequest("Invalid email address.");
        if (db.EmailInUse(request.EmailAddress)) return Results.BadRequest("Email already in use.");
        updates.Add("EmailAddress", request.EmailAddress);
    }

    if (request.FirstName != null)
    {
        if (!Security.IsAlphabetical(request.FirstName)) return Results.BadRequest("Invalid first name.");
        updates.Add("FirstName", request.FirstName.ToLower());
    }

    if (request.LastName != null)
    {
        if (!Security.IsAlphabetical(request.LastName)) return Results.BadRequest("Invalid last name.");
        updates.Add("LastName", request.LastName.ToLower());
    }

    if (request.Username != null)
    {
        if (!Security.IsValidUsername(request.Username)) return Results.BadRequest("Invalid username.");
        if (db.UsernameInUse(request.Username)) return Results.BadRequest("Username already in use.");
        updates.Add("Username", request.Username);
    }

    if (request.DateOfBirth != null)
    {
        updates.Add("DateOfBirth", request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue));
    }
    
    db.UpdateRecord("UserInformation", "UserInformationID", userInformationID, updates);
    
    return Results.Ok(updates);
});

profile.MapGet("/stats", ([FromHeader(Name = "Authorization")] string authorization) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");
    return Results.Ok(db.GetUserStats((int)userID));
});

profile.MapGet("/icon", ([FromHeader(Name = "Authorization")] string authorization) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");
    
    var userInformation = db.GetUserInformation((int)userID);
    string iconName = (string)userInformation["ProfilePictureURL"];
    var basePath = Directory.GetCurrentDirectory();
    var imagePath = Path.Combine(basePath, "ProfilePictures", iconName);

    if (!File.Exists(imagePath))
    {
        return Results.NotFound("Image not found.");
    }

    var bytes = File.ReadAllBytes(imagePath);
    return Results.File(bytes, "image/png");
});

string[] icons = new[] { "HeadphoneJake", "BMO", "BreadJake", "Gunter" };

profile.MapPost("/icon", ([FromHeader(Name = "Authorization")] string authorization, IconChangeRequest request) =>
{
    string? token = GetToken(authorization);
    if (token == null) return Results.Unauthorized();
    
    int? userID = db.GetUserIDFromToken(token);
    if (userID == null) return Results.NotFound("User not found.");

    string iconName = icons.ElementAtOrDefault(request.IconID);
    db.SetUserIcon((int)userID, iconName);
    return Results.Ok();
});

app.Run();

record LoginRequest(string Username, string Password);
record RegisterRequest(string Email, string Username, string Password);
record ParseRequest(string Query, string Password);

record IconChangeRequest(int IconID);
record UserInformationChangeRequest
(
    string? FirstName, string? LastName,
    string? EmailAddress,
    string? Username,
    DateOnly? DateOfBirth
);
record PasswordChangeRequest
(
    string NewPassword
);