using BCrypt.Net;
using UserManagementSystem.Application.Services;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UserManagementSystem.Infrastructure.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly ILogger<PasswordService> _logger;

        public PasswordService(ILogger<PasswordService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Hash a password using BCrypt with salt
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password</returns>
        public string HashPassword(string password)
        {
            try
            {
                _logger.LogDebug("Hashing password");

                // Generate salt and hash password
                // WorkFactor 12 is a good balance between security and performance
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

                _logger.LogDebug("Password hashed successfully");
                return hashedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw new InvalidOperationException("Failed to hash password", ex);
            }
        }

        /// <summary>
        /// Verify a password against its hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hash">Stored hash</param>
        /// <returns>True if password matches</returns>
        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                _logger.LogDebug("Verifying password");

                if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                {
                    _logger.LogWarning("Password or hash is null/empty");
                    return false;
                }

                var isValid = BCrypt.Net.BCrypt.Verify(password, hash);

                _logger.LogDebug("Password verification result: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        /// <summary>
        /// Check if password meets security requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if password is strong enough</returns>
        public bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogDebug("Password is null or empty");
                return false;
            }

            // Password requirements:
            // - At least 8 characters
            // - At least one uppercase letter
            // - At least one lowercase letter  
            // - At least one digit
            // - At least one special character
            var hasMinLength = password.Length >= 8;
            var hasUpperCase = Regex.IsMatch(password, @"[A-Z]");
            var hasLowerCase = Regex.IsMatch(password, @"[a-z]");
            var hasDigits = Regex.IsMatch(password, @"[0-9]");
            var hasSpecialChar = Regex.IsMatch(password, @"[\W_]");

            var isStrong = hasMinLength && hasUpperCase && hasLowerCase && hasDigits && hasSpecialChar;

            _logger.LogDebug("Password strength check - Length: {HasMinLength}, Upper: {HasUpperCase}, Lower: {HasLowerCase}, Digits: {HasDigits}, Special: {HasSpecialChar}, Strong: {IsStrong}",
                hasMinLength, hasUpperCase, hasLowerCase, hasDigits, hasSpecialChar, isStrong);

            return isStrong;
        }

        /// <summary>
        /// Generate a cryptographically secure random password
        /// </summary>
        /// <param name="length">Password length</param>
        /// <returns>Secure random password</returns>
        public string GenerateSecurePassword(int length = 12)
        {
            if (length < 8)
                throw new ArgumentException("Password length must be at least 8 characters", nameof(length));

            const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var allChars = upperCase + lowerCase + digits + specialChars;

            using var rng = RandomNumberGenerator.Create();
            var password = new StringBuilder();

            // Ensure at least one character from each category
            password.Append(GetRandomChar(upperCase, rng));
            password.Append(GetRandomChar(lowerCase, rng));
            password.Append(GetRandomChar(digits, rng));
            password.Append(GetRandomChar(specialChars, rng));

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                password.Append(GetRandomChar(allChars, rng));
            }

            // Shuffle the password
            return ShuffleString(password.ToString(), rng);
        }

        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            return chars[(int)(randomValue % chars.Length)];
        }

        private string ShuffleString(string str, RandomNumberGenerator rng)
        {
            var array = str.ToCharArray();
            for (int i = array.Length - 1; i > 0; i--)
            {
                var randomBytes = new byte[4];
                rng.GetBytes(randomBytes);
                var randomValue = BitConverter.ToUInt32(randomBytes, 0);
                var j = (int)(randomValue % (i + 1));
                (array[i], array[j]) = (array[j], array[i]);
            }
            return new string(array);
        }
    }
}