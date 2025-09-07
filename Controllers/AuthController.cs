using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Entities;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "User registered successfully" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                return Ok(new LoginResponse
                {
                    Email = user?.Email,
                    FirstName = user?.FirstName,
                    LastName = user?.LastName,
                    ProfilePicture = user?.ProfilePicture
                });
            }

            return Unauthorized(new { message = "Invalid email or password" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(currentEmail))
                return Unauthorized(new { message = "User not authenticated" });
            
            var user = await _userManager.FindByEmailAsync(currentEmail);

            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if email is being changed and if it's already taken
            if (model.Email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                    return BadRequest(new { message = "Email is already in use" });
            }

            // Update user properties
            user.Email = model.Email;
            user.UserName = model.Email; // Keep username same as email
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // If email changed, sign out and require re-login for security
                if (model.Email != currentEmail)
                {
                    await _signInManager.SignOutAsync();
                    return Ok(new { message = "Profile updated successfully. Please log in again with your new email.", requireReLogin = true });
                }

                return Ok(new { 
                    message = "Profile updated successfully",
                    user = new
                    {
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        ProfilePicture = user.ProfilePicture
                    }
                });
            }

            return BadRequest(new { message = "Failed to update profile", errors = result.Errors });
        }

        [HttpPost("upload-profile-picture")]
        [Authorize]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { message = "Only JPG and PNG files are allowed" });

            // Validate file size (5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File size cannot exceed 5MB" });

            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(currentEmail))
                return Unauthorized(new { message = "User not authenticated" });

            var user = await _userManager.FindByEmailAsync(currentEmail);
            if (user == null)
                return NotFound(new { message = "User not found" });

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine("wwwroot", "uploads", "profile-pictures");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Delete old profile picture if it exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldFilePath = Path.Combine("wwwroot", user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                // Generate unique filename
                var fileName = $"{user.Id}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update user profile picture path (relative to wwwroot)
                user.ProfilePicture = $"/uploads/profile-pictures/{fileName}";
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { 
                        message = "Profile picture uploaded successfully",
                        profilePicture = user.ProfilePicture
                    });
                }

                return BadRequest(new { message = "Failed to update profile picture" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }
    }

    public class RegisterModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

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

    public class UpdateProfileModel
    {
        public string Email { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}