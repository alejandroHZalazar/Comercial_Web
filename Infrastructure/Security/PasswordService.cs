using Application.Security;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Infrastructure.Security
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string Hash(string password)
            => _hasher.HashPassword(null, password);

        public bool Verify(string hash, string password)
        {
            try
            {
                return _hasher.VerifyHashedPassword(null, hash, password)
                       == PasswordVerificationResult.Success;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public bool IsHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.Length < 40)
                return false;

            // Base64 safe check
            return Convert.TryFromBase64String(value, new Span<byte>(new byte[value.Length]), out _);
        }
    }
}
