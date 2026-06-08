using KitaKo.Data;
using KitaKo.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace KitaKo.Services
{
    public class AuthService
    {
        private const long MaxProfilePhotoBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif"
        };

        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif"
        };

        private readonly ApplicationDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        public User? Register(string username, string email, string password)
        {
            username = username.Trim();
            email = email.Trim();

            if (_dbContext.Users.Any(u => u.Email == email || u.Username == username))
            {
                return null;
            }

            var user = new User
            {
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return user;
        }

        public User? Login(string emailOrUsername, string password)
        {
            emailOrUsername = emailOrUsername.Trim();
            var user = _dbContext.Users.FirstOrDefault(u =>
                u.Email == emailOrUsername || u.Username == emailOrUsername);

            if (user == null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _dbContext.SaveChanges();
                return user;
            }

            if (result == PasswordVerificationResult.Success)
            {
                return user;
            }

            if (VerifyLegacySha256Password(password, user.PasswordHash))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _dbContext.SaveChanges();
                return user;
            }

            return null;
        }

        public User? GetUserById(int userId)
        {
            return _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        }

        public bool UpdateProfile(int userId, string username, string storeName, string profilePhotoUrl)
        {
            username = username.Trim();
            storeName = storeName.Trim();

            var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            if (_dbContext.Users.Any(u => u.Username == username && u.Id != userId))
            {
                return false;
            }

            user.Username = username;
            user.StoreName = storeName;
            user.ProfilePhotoUrl = profilePhotoUrl;
            _dbContext.SaveChanges();
            return true;
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
            var validCurrentPassword = result == PasswordVerificationResult.Success ||
                                       result == PasswordVerificationResult.SuccessRehashNeeded ||
                                       VerifyLegacySha256Password(currentPassword, user.PasswordHash);

            if (!validCurrentPassword)
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _dbContext.SaveChanges();
            return true;
        }

        public string? SaveProfilePhoto(IFormFile photo, int userId)
        {
            if (photo == null || photo.Length == 0)
            {
                return null;
            }

            if (photo.Length > MaxProfilePhotoBytes)
            {
                throw new InvalidOperationException("Profile photo must be 5MB or smaller.");
            }

            var fileExtension = Path.GetExtension(photo.FileName);
            if (!AllowedImageExtensions.Contains(fileExtension) ||
                !AllowedImageContentTypes.Contains(photo.ContentType) ||
                !HasAllowedImageSignature(photo))
            {
                throw new InvalidOperationException("Profile photo must be a valid JPG, PNG, or GIF image.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"user_{userId}_{Guid.NewGuid()}{fileExtension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.CreateNew))
            {
                photo.CopyTo(stream);
            }

            return $"/uploads/profiles/{fileName}";
        }

        private static bool VerifyLegacySha256Password(string password, string hash)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var legacyHash = Convert.ToBase64String(bytes);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacyHash),
                Encoding.UTF8.GetBytes(hash));
        }

        private static bool HasAllowedImageSignature(IFormFile photo)
        {
            Span<byte> header = stackalloc byte[8];
            using var stream = photo.OpenReadStream();
            var bytesRead = stream.Read(header);

            return bytesRead >= 3 &&
                   ((header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) ||
                    (bytesRead >= 8 &&
                     header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                     header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) ||
                    (bytesRead >= 6 &&
                     header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 &&
                     header[3] == 0x38 && (header[4] == 0x37 || header[4] == 0x39) && header[5] == 0x61));
        }
    }
}
