# .NET Web API Backend - Beginner's Guide

## 📋 Table of Contents
1. [Project Overview](#project-overview)
2. [.NET Commands Explained](#net-commands-explained)
3. [Project Structure](#project-structure)
4. [Understanding Entity Framework](#understanding-entity-framework)
5. [Database Migrations](#database-migrations)
6. [Authentication System](#authentication-system)
7. [Controllers Explained](#controllers-explained)
8. [Models and Entities](#models-and-entities)
9. [How Everything Works Together](#how-everything-works-together)

---

## 🎯 Project Overview

This is a .NET 8 Web API that provides authentication services and CRUD operations. It uses Entity Framework Core for database access, ASP.NET Core Identity for authentication, and cookie-based authentication instead of JWT tokens.

**What this API does:**
- User registration and login with cookies
- Profile management with file uploads
- CRUD operations for laptop entities
- Database management with Entity Framework
- File storage for profile pictures

---

## 🚀 .NET Commands Explained

### Essential Commands You Need to Know

```bash
# Build the project (compiles code, checks for errors)
dotnet build
# → Like Build Solution in Visual Studio
# → Run this when you change code to check for errors

# Run the application (builds + starts server)
dotnet run
# → Like F5 in Visual Studio
# → Automatically builds if needed, then starts on http://localhost:5195
# → Keep this running while developing

# Restore packages (like NuGet restore)
dotnet restore
# → Downloads all packages listed in .csproj
# → Run after cloning or when packages change
```

### Database Migration Commands

```bash
# Create a new migration when you change models
dotnet ef migrations add MigrationName
# → Example: dotnet ef migrations add AddProfilePicture
# → Creates files that describe database changes

# Apply migrations to database
dotnet ef database update
# → Applies all pending migrations to the database
# → Creates database if it doesn't exist

# Remove the last migration (if not applied yet)
dotnet ef migrations remove

# Drop entire database (for development only!)
dotnet ef database drop --force
```

### When to Use Each Command

- **`dotnet build`**: Check for compilation errors without running
- **`dotnet run`**: Start the API server for development/testing
- **`dotnet ef migrations add`**: After changing Entity models (like adding ProfilePicture field)
- **`dotnet ef database update`**: After creating migrations to apply changes to database
- **`dotnet restore`**: After cloning project or when .csproj changes

### Build vs Run - What's the Difference?

```bash
# BUILD: Only compiles code
dotnet build
# ✅ Checks for errors
# ✅ Creates executable files
# ❌ Doesn't start the server

# RUN: Builds AND starts the server
dotnet run
# ✅ Builds automatically
# ✅ Starts the web server
# ✅ Shows "Now listening on: http://localhost:5195"
```

---

## 📁 Project Structure

```
webapitemplate/
├── Controllers/                  # API endpoints
│   └── AuthController.cs        # Authentication endpoints (/api/auth/*)
│   └── LaptopsController.cs     # CRUD operations for laptops
├── Entities/                    # Database models
│   ├── User.cs                  # User table structure
│   └── Laptop.cs                # Laptop table structure
├── Migrations/                  # Database version history
│   ├── 20240907_AddProfilePicture.cs  # Auto-generated migration files
│   └── ApplicationDbContextModelSnapshot.cs  # Current database state
├── wwwroot/                     # Static files
│   └── uploads/                 # Uploaded files (profile pictures)
├── Program.cs                   # Application startup configuration
├── ApplicationDbContext.cs      # Database connection setup
├── appsettings.json            # Configuration (database connection, CORS)
└── WebAPI.csproj               # Project file (like packages.config)
```

### What Each File Does

- **`Controllers/`**: Define API endpoints (like `/api/auth/login`)
- **`Entities/`**: Define database table structures
- **`Migrations/`**: Track database changes over time (never edit manually!)
- **`Program.cs`**: Configure services, authentication, database connection
- **`ApplicationDbContext.cs`**: Tell Entity Framework about your database tables

---

## 🗄️ Understanding Entity Framework

### What Is Entity Framework?

Entity Framework (EF) is like a translator between your C# code and the database:

```csharp
// Instead of writing SQL like this:
// SELECT * FROM Users WHERE Email = 'user@example.com'

// You write C# like this:
var user = await _userManager.FindByEmailAsync("user@example.com");
```

### How EF Works

1. **Entities** = Database Tables
```csharp
public class User : IdentityUser  // This becomes the Users table
{
    public string? FirstName { get; set; }    // Column: FirstName
    public string? LastName { get; set; }     // Column: LastName
    public string? ProfilePicture { get; set; } // Column: ProfilePicture
}
```

2. **DbContext** = Database Connection
```csharp
public class ApplicationDbContext : IdentityDbContext<User>
{
    public DbSet<Laptop> Laptops { get; set; }  // Creates Laptops table
    // Users table created automatically by Identity
}
```

3. **Migrations** = Database Version Control
```csharp
// When you add ProfilePicture to User entity and run:
// dotnet ef migrations add AddProfilePicture
// EF creates a migration that adds the column to the database
```

---

## 🔄 Database Migrations

### What Are Migrations?

Migrations are like Git commits for your database. They track every change to the database structure.

### Migration Workflow

```bash
# 1. Change your Entity model (add/remove properties)
public class User {
    public string? ProfilePicture { get; set; }  // ← New property
}

# 2. Create migration
dotnet ef migrations add AddProfilePicture
# → Creates migration files in Migrations/ folder

# 3. Apply migration to database
dotnet ef database update
# → Actually adds the column to the database
```

### Understanding Migration Files

```csharp
// Auto-generated migration file
public partial class AddProfilePicture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // What to do when applying this migration
        migrationBuilder.AddColumn<string>(
            name: "ProfilePicture",
            table: "AspNetUsers",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // What to do when rolling back this migration
        migrationBuilder.DropColumn(
            name: "ProfilePicture",
            table: "AspNetUsers");
    }
}
```

### When Do You Need Migrations?

- ✅ Adding/removing properties from entities
- ✅ Creating new entities
- ✅ Changing property types
- ❌ Changing business logic (only structure changes)

### Common Migration Issues

```bash
# Error: "Pending model changes"
# → You changed a model but didn't create migration
dotnet ef migrations add YourChangeName

# Error: "Column already exists"
# → Drop database and recreate (development only!)
dotnet ef database drop --force
dotnet ef database update
```

---

## 🔐 Authentication System

### How ASP.NET Identity + Cookies Work

1. **Identity System** manages users, passwords, roles
2. **Cookies** store encrypted authentication info
3. **No JWT tokens needed** - cookies are simpler for web apps

### Authentication Flow

```csharp
// 1. User logs in
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginModel model)
{
    // 2. Check password
    var result = await _signInManager.PasswordSignInAsync(
        model.Email, 
        model.Password, 
        isPersistent: true,    // Remember login
        lockoutOnFailure: false
    );
    
    if (result.Succeeded)
    {
        // 3. Cookie automatically created and sent to browser
        return Ok(new { /* user data */ });
    }
}
```

### How Cookie Authentication is Configured

```csharp
// In Program.cs
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;           // Security: prevent JavaScript access
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in development
    options.Cookie.SameSite = SameSiteMode.Lax;          // CORS compatibility
    options.Cookie.Name = "AuthCookie";       // Cookie name
    options.ExpireTimeSpan = TimeSpan.FromHours(24);     // Cookie lifetime
});
```

### Why Cookies Instead of JWT?

| Feature | Cookies | JWT Tokens |
|---------|---------|------------|
| **Security** | Stored securely by browser | Must be manually stored |
| **Automatic sending** | Browser sends automatically | Must add to every request |
| **Expiration** | Server controls | Client must handle |
| **Setup complexity** | Simple | More complex |

---

## 🎮 Controllers Explained

### What Are Controllers?

Controllers handle HTTP requests and return responses. Each method is an API endpoint.

```csharp
[Route("api/[controller]")]        // Base URL: /api/auth
[ApiController]                    // Enables automatic model validation
public class AuthController : ControllerBase
{
    [HttpPost("login")]            // POST /api/auth/login
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        // Handle login logic
        return Ok(new { message = "Login successful" });
    }
    
    [HttpGet("me")]               // GET /api/auth/me
    [Authorize]                   // Requires authentication
    public async Task<IActionResult> GetCurrentUser()
    {
        // Return current user data
    }
}
```

### Controller Method Patterns

```csharp
// GET endpoints - retrieve data
[HttpGet]
public async Task<IActionResult> GetData()
{
    var data = await _service.GetDataAsync();
    return Ok(data);  // Returns 200 + data
}

// POST endpoints - create new data
[HttpPost]
public async Task<IActionResult> CreateData([FromBody] Model model)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);  // Returns 400 + validation errors
    
    var result = await _service.CreateAsync(model);
    return Ok(result);  // Returns 200 + result
}

// File upload endpoints
[HttpPost("upload")]
public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
{
    // Handle file upload
}
```

### HTTP Status Codes

```csharp
return Ok(data);                    // 200 - Success
return BadRequest("Error message"); // 400 - Client error
return Unauthorized();              // 401 - Not authenticated
return NotFound();                  // 404 - Resource not found
return StatusCode(500, "Error");    // 500 - Server error
```

---

## 📋 Models and Entities

### Entities vs Models

```csharp
// ENTITY: Represents database table
public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// MODEL: Data transfer object (what APIs send/receive)
public class LoginModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicture { get; set; }
}
```

### Why Separate Entities and Models?

- **Security**: Don't expose password hashes or internal IDs
- **API Design**: Only send data the frontend needs
- **Validation**: Different validation rules for input vs storage

### Entity Relationships

```csharp
public class User : IdentityUser
{
    // Navigation property - User can have many laptops
    public List<Laptop> Laptops { get; set; } = new();
}

public class Laptop
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    
    // Foreign key
    public string UserId { get; set; } = "";
    public User User { get; set; } = null!;
}
```

---

## 🔄 How Everything Works Together

### Complete Request Flow: Profile Picture Upload

1. **Angular sends request**
```typescript
// Frontend
const formData = new FormData();
formData.append('file', file);
http.post('/api/auth/upload-profile-picture', formData, {
  withCredentials: true  // Sends authentication cookie
});
```

2. **ASP.NET receives request**
```csharp
[HttpPost("upload-profile-picture")]
[Authorize]  // Validates cookie, gets user identity
public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
```

3. **Authentication middleware validates cookie**
```csharp
// Automatic - configured in Program.cs
app.UseAuthentication();  // Validates cookie
app.UseAuthorization();   // Checks [Authorize] attributes
```

4. **Controller processes request**
```csharp
// Get current user from cookie
var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
var user = await _userManager.FindByEmailAsync(currentEmail);

// Save file
var fileName = $"{user.Id}_{Guid.NewGuid()}.jpg";
var filePath = Path.Combine("wwwroot", "uploads", "profile-pictures", fileName);
using (var stream = new FileStream(filePath, FileMode.Create))
{
    await file.CopyToAsync(stream);
}

// Update database
user.ProfilePicture = $"/uploads/profile-pictures/{fileName}";
await _userManager.UpdateAsync(user);
```

5. **Response sent back**
```csharp
return Ok(new { 
    message = "Profile picture uploaded successfully",
    profilePicture = user.ProfilePicture
});
```

6. **Angular receives response**
```typescript
// Frontend receives response and updates UI
this.currentProfilePicture = response.profilePicture;
```

---

## 🔧 Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    // Database connection - change "YourAppName" to your database name
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=YourAppName;Trusted_Connection=true;MultipleActiveResultSets=true;"
  },
  "Cors": {
    // Allowed frontend URLs
    "AllowedOrigins": [
      "https://yourapp.vercel.app",  // Production
      "http://localhost:4200"        // Development
    ]
  }
}
```

### Program.cs Configuration

```csharp
// Database connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("name=DefaultConnection"));

// Identity + Authentication
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Angular dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // Required for cookies
    });
});

// Static files (for uploaded images)
app.UseStaticFiles();
```

---

## 🐛 Common Issues & Solutions

### Database Issues

```bash
# Error: "No migrations found"
dotnet ef migrations add InitialCreate
dotnet ef database update

# Error: "Database already exists"
dotnet ef database drop --force
dotnet ef database update

# Error: "Pending model changes"
dotnet ef migrations add DescribeYourChanges
dotnet ef database update
```

### Authentication Issues

```csharp
// Error: 401 Unauthorized
// Check:
// 1. Cookie configuration
options.Cookie.SameSite = SameSiteMode.Lax;  // Not None in HTTP
options.Cookie.SecurePolicy = CookieSecurePolicy.None;  // Allow HTTP

// 2. CORS configuration
.AllowCredentials();  // Required for cookies

// 3. Frontend sends cookies
{ withCredentials: true }  // In Angular HTTP requests
```

### File Upload Issues

```csharp
// Make sure directory exists
var uploadsPath = Path.Combine("wwwroot", "uploads", "profile-pictures");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

// Enable static files
app.UseStaticFiles();  // In Program.cs
```

---

## 🚀 Production Considerations

### Security

```csharp
// Production cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
    options.Cookie.SameSite = SameSiteMode.Strict;           // Stricter CSRF protection
});

// Add HTTPS redirection
app.UseHttpsRedirection();
```

### Environment Variables

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=ProdDB;User Id=user;Password=pass;"
  }
}
```

### File Storage

```csharp
// Consider cloud storage for production
// Azure Blob Storage, AWS S3, etc.
// Don't store files on the web server filesystem in production
```

---

This README covers everything you need to understand how this .NET Web API works. Start with the commands section to get comfortable with the development workflow, then dive into the architecture sections to understand how the code is organized!