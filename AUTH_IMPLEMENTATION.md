# ASP.NET Core Identity + JWT Authentication Implementation

This document explains the authentication system implemented in this Web API project.

## What We Implemented

### 1. **NuGet Packages Added**
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` - Core Identity framework
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT Bearer token authentication
- `System.IdentityModel.Tokens.Jwt` - JWT token creation and validation

### 2.**User Entity (`Entities/User.cs`)**
```csharp
public class User : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```
- Inherits from `IdentityUser` which provides built-in authentication fields
- Added custom properties for user profile information

### 3. **Database Context Update (`ApplicationDbContext.cs`)**
- Changed from `DbContext` to `IdentityDbContext<User>`
- This automatically includes all Identity tables (Users, Roles, Claims, etc.)

### 4. **Identity Configuration (`Program.cs`)**
- **Identity Service**: Configured password requirements and user management
- **JWT Authentication**: Set up token validation with secret key
- **Middleware Order**: Added `UseAuthentication()` before `UseAuthorization()`

### 5. **Authentication Controller (`Controllers/AuthController.cs`)**
Two main endpoints:

#### **POST /api/auth/register**
- Creates new user account
- Validates password requirements
- Returns success message or validation errors

#### **POST /api/auth/login**
- Validates email/password credentials
- Generates JWT token on success
- Returns token + user info or unauthorized error

### 6. **JWT Token Generation**
The `GenerateJwtToken()` method creates tokens containing:
- User ID, Email, Username as claims
- Configurable expiry time (default: 60 minutes)
- HMAC-SHA256 signature for security

### 7. **Configuration (`appsettings.json`)**
Added JWT settings:
```json
"JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "ExpiryInMinutes": 60
}
```

### 8. **Database Migration**
- Created Identity tables with `dotnet ef migrations add AddIdentity`
- Applied to database with `dotnet ef database update`

## How Authentication Works

1. **Registration**: User provides email, password, first/last name
2. **Login**: User provides email/password, receives JWT token
3. **Authorized Requests**: Include token in `Authorization: Bearer <token>` header
4. **Token Validation**: API automatically validates token on protected endpoints

## Security Features

- Password requirements (uppercase, lowercase, digit, 6+ characters)
- JWT tokens with expiration
- Secure token signing with HMAC-SHA256
- Built-in protection against common attacks via Identity framework

## Next Steps for Learning

1. **Add Authorization**: Use `[Authorize]` attribute on controllers/actions
2. **Role-Based Security**: Add roles and role-based authorization
3. **Token Refresh**: Implement refresh token mechanism
4. **Email Confirmation**: Add email verification for registration
5. **Password Reset**: Implement forgot password functionality

## API Endpoints

- `POST /api/auth/register` - Create new user account
- `POST /api/auth/login` - Authenticate and get JWT token

## Testing the API

Use tools like Postman or curl to test:

```bash
# Register
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","firstName":"John","lastName":"Doe"}'

# Login
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```