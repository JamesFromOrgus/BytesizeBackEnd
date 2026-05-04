# bytesize. API

> A secure, scalable RESTful API built with .NET 8 and C#

The bytesize. API powers the mobile-first educational platform, providing robust backend services for course management, user authentication, progress tracking, and lesson delivery. Built with enterprise-grade security and modern .NET practices.

---

## Features

- **Secure Authentication**: Argon2id password hashing with session token-based authentication
- **RESTful Architecture**: Clean, predictable API endpoints following REST conventions
- **Server-Side Validation**: Comprehensive input validation and sanitization
- **Course Management**: CRUD operations for courses, lessons, and learning content
- **Progress Tracking**: User progress persistence and analytics
- **User Management**: Account creation, authentication, and profile management
- **Database Integration**: MySQL with Entity Framework Core ORM
- **Error Handling**: Structured error responses with appropriate HTTP status codes
- **CORS Support**: Configured for cross-origin requests from mobile clients

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 9.0+](https://dev.mysql.com/downloads/)
- IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)

### For Developers

```bash
# Clone the repository
git clone https://github.com/YourOrg/BytesizeBackend.git
cd BytesizeBackend

# Restore dependencies
dotnet restore

# Update database connection string in appsettings.json
# See Configuration section below

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

The API will be available at `https://localhost:7001` (HTTPS) and `http://localhost:5000` (HTTP)

---

## Tech Stack

- **Framework**: .NET 8
- **Language**: C# 12
- **Database**: MySQL 9.0+
- **ORM**: Entity Framework Core 8
- **Authentication**: Argon2id password hashing + Session tokens
- **API Documentation**: Swagger/OpenAPI
- **Testing**: xUnit (optional)
- **Logging**: Serilog or built-in ILogger

---

## Project Structure

```
BytesizeAPI/
├── Controllers/                # API endpoint controllers
│   ├── AuthController.cs       # Authentication endpoints
│   ├── CoursesController.cs    # Course management
│   ├── LessonsController.cs    # Lesson delivery
│   ├── ProgressController.cs   # User progress tracking
│   └── UsersController.cs      # User management
│
├── Models/                     # Data models and DTOs
│   ├── Course.cs               # Course entity
│   ├── Lesson.cs               # Lesson entity
│   ├── User.cs                 # User entity
│   ├── UserProgress.cs         # Progress tracking entity
│   ├── Session.cs              # Session token entity
│   └── DTOs/                   # Data Transfer Objects
│       ├── LoginRequest.cs
│       ├── RegisterRequest.cs
│       ├── CourseResponse.cs
│       └── LessonResponse.cs
│
├── Services/                   # Business logic layer
│   ├── IAuthService.cs         # Authentication interface
│   ├── AuthService.cs          # Authentication implementation
│   ├── ICourseService.cs       # Course service interface
│   ├── CourseService.cs        # Course service implementation
│   ├── IPasswordHasher.cs      # Password hashing interface
│   └── Argon2PasswordHasher.cs # Argon2 implementation
│
├── Data/                       # Database context and configurations
│   ├── BytesizeDbContext.cs    # EF Core database context
│   ├── Migrations/             # EF Core migrations
│   └── Configurations/         # Entity configurations
│       ├── CourseConfiguration.cs
│       ├── UserConfiguration.cs
│       └── LessonConfiguration.cs
│
├── Middleware/                 # Custom middleware
│   ├── AuthenticationMiddleware.cs  # Token validation
│   ├── ErrorHandlingMiddleware.cs   # Global error handler
│   └── LoggingMiddleware.cs         # Request/response logging
│
├── Validators/                 # Input validation
│   ├── LoginValidator.cs
│   ├── RegisterValidator.cs
│   └── CourseValidator.cs
│
├── Properties/                 # Project properties
│   └── launchSettings.json     # Development server settings
│
├── appsettings.json            # Application configuration
├── appsettings.Development.json # Development-specific config
├── Program.cs                  # Application entry point
├── Startup.cs                  # Service configuration (if using)
└── BytesizeAPI.csproj          # Project file
```

---

## Configuration

### Database Setup

1. **Install MySQL** (version 9.0 or higher)

2. **Create Database**:
```sql
CREATE DATABASE bytesize_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

3. **Update Connection String** in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=bytesize_db;User=root;Password=yourpassword;"
  }
}
```

4. **Run Migrations**:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Environment Variables

For production, set these environment variables instead of using `appsettings.json`:

```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=bytesize_db;User=api_user;Password=secure_password;"
export JWT__Secret="your-256-bit-secret-key-here"
export JWT__Issuer="https://api.bytesize.app"
export JWT__Audience="https://bytesize.app"
```

### CORS Configuration

Update `Program.cs` to allow requests from your frontend:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApp", policy =>
    {
        policy.WithOrigins("exp://192.168.1.100:8081") // Expo development
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

---

## API Documentation

### Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Create new user account | No |
| POST | `/api/auth/login` | Authenticate user | No |
| POST | `/api/auth/logout` | Invalidate session token | Yes |
| GET | `/api/auth/me` | Get current user info | Yes |

### Course Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/courses` | List all courses | No |
| GET | `/api/courses/{id}` | Get course details | No |
| GET | `/api/courses/{id}/lessons` | Get course lessons | No |
| POST | `/api/courses` | Create new course | Yes (Admin) |
| PUT | `/api/courses/{id}` | Update course | Yes (Admin) |
| DELETE | `/api/courses/{id}` | Delete course | Yes (Admin) |

### Lesson Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/lessons/{id}` | Get lesson details | No |
| POST | `/api/lessons/{id}/complete` | Mark lesson complete | Yes |

### Progress Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/progress` | Get user progress | Yes |
| GET | `/api/progress/course/{id}` | Get course progress | Yes |
| POST | `/api/progress` | Update progress | Yes |

### Example Request/Response

**POST** `/api/auth/register`

Request:
```json
{
  "email": "student@example.com",
  "password": "SecurePass123!",
  "username": "learner123"
}
```

Response (201 Created):
```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "email": "student@example.com",
  "username": "learner123",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

---

## Security

### Password Hashing

Uses **Argon2id** (winner of the Password Hashing Competition) with secure defaults:
- Memory cost: 65536 KB (64 MB)
- Time cost: 4 iterations
- Parallelism: 4 threads
- Salt length: 128 bits
- Hash length: 256 bits

```csharp
// Example implementation
public class Argon2PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 4
        };
        
        return Convert.ToBase64String(argon2.GetBytes(32));
    }
}
```

### Session Tokens

- Tokens expire after 24 hours of inactivity
- Stored in database with user association
- Validated on each authenticated request
- Automatically cleaned up on logout

### Input Validation

All endpoints validate:
- Required fields
- Data types
- String length limits
- Email format
- Password strength (min 8 chars, uppercase, lowercase, number)
- SQL injection prevention (parameterized queries)
- XSS prevention (input sanitization)

---

## Available Scripts

| Command | Description |
|---------|-------------|
| `dotnet run` | Start development server |
| `dotnet watch run` | Start with hot reload |
| `dotnet build` | Compile the application |
| `dotnet test` | Run unit tests |
| `dotnet ef migrations add <Name>` | Create new migration |
| `dotnet ef database update` | Apply pending migrations |
| `dotnet publish -c Release` | Build for production |

---

## Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id VARCHAR(36) PRIMARY KEY,
    Email VARCHAR(255) UNIQUE NOT NULL,
    Username VARCHAR(50) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);
```

### Courses Table
```sql
CREATE TABLE Courses (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT,
    Color VARCHAR(20),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### Lessons Table
```sql
CREATE TABLE Lessons (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CourseId INT NOT NULL,
    Title VARCHAR(255) NOT NULL,
    Order INT NOT NULL,
    Content JSON,
    FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
);
```

### UserProgress Table
```sql
CREATE TABLE UserProgress (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId VARCHAR(36) NOT NULL,
    LessonId INT NOT NULL,
    CompletedAt DATETIME,
    Score DECIMAL(5,2),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (LessonId) REFERENCES Lessons(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_lesson (UserId, LessonId)
);
```

---

## Git Workflow

**Active Branch**: `main`

```bash
# Always work on main branch
git switch main

# Before starting work
git pull origin main

# After making changes
git add .
git commit -m "feat: add course enrollment endpoint"
git push origin main
```

**Commit Message Convention**:
- `feat:` New features
- `fix:` Bug fixes
- `refactor:` Code restructuring
- `docs:` Documentation updates
- `test:` Adding or updating tests
- `chore:` Maintenance tasks

---

## Resources

- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Argon2 Password Hashing](https://github.com/kmaragon/Konscious.Security.Cryptography)
- [REST API Best Practices](https://restfulapi.net/)
- [MySQL Documentation](https://dev.mysql.com/doc/)

---

## Contributing

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/user-analytics`
3. **Commit** your changes: `git commit -m 'feat: add user analytics endpoints'`
4. **Push** to the branch: `git push origin feature/user-analytics`
5. **Open** a Pull Request to `main`
   
---

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

---

## License

This project is part of an educational initiative. All rights reserved.

---

**Frontend Repository**: [ByteSize Frontend](https://github.com/JamesFromOrgus/BytesizeFrontEnd)
