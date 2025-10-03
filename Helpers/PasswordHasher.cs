
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Project_Version1.Helpers
{
    public static class PasswordHasher
    {
        // Produces salted hash: salt.hash
        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
            string saltStr = Convert.ToBase64String(salt);
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            return $"{saltStr}.{hashed}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var expected = parts[1];

            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100_000,
                numBytesRequested: 256 / 8));

            return hashed == expected;
        }
    }
}