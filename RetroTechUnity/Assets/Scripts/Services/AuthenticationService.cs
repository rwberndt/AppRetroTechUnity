using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RetroTech;
using UnityEngine;

namespace RetroTech.Services
{
    /// <summary>
    /// Simple local authentication provider backed by Unity's <see cref="PlayerPrefs"/>.
    /// Passwords are salted and hashed with SHA-256 so that even the local cache does not store
    /// plain text secrets.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private const string UsersKey = "RetroTech.Auth.Users";
        private const string LastUserKey = "RetroTech.Auth.LastUser";

        [Serializable]
        private class UserDatabase
        {
            public List<UserRecord> Users = new();
        }

        [Serializable]
        private class UserRecord
        {
            public string DisplayName;
            public string Email;
            public string NormalizedEmail;
            public string Salt;
            public string PasswordHash;
        }

        private readonly UserDatabase _database;

        public AuthenticationService()
        {
            _database = LoadDatabase();
        }

        public bool TryRegister(string displayName, string email, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            displayName = (displayName ?? string.Empty).Trim();
            var originalEmail = (email ?? string.Empty).Trim();
            email = NormalizeEmail(email);
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errorMessage = "Informe seu nome completo.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                errorMessage = "Informe um e-mail válido.";
                return false;
            }

            if (password.Length < 6)
            {
                errorMessage = "A senha deve ter pelo menos 6 caracteres.";
                return false;
            }

            if (_database.Users.Any(u => string.Equals(u.NormalizedEmail, email, StringComparison.Ordinal)))
            {
                errorMessage = "Já existe uma conta com este e-mail.";
                return false;
            }

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            _database.Users.Add(new UserRecord
            {
                DisplayName = displayName,
                Email = originalEmail,
                NormalizedEmail = email,
                Salt = salt,
                PasswordHash = hash
            });

            SaveDatabase();
            return true;
        }

        public bool TrySignIn(string email, string password, out UserProfile profile, out string errorMessage)
        {
            profile = null;
            errorMessage = string.Empty;

            email = NormalizeEmail(email);
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                errorMessage = "Informe um e-mail válido.";
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Informe sua senha.";
                return false;
            }

            var record = _database.Users.FirstOrDefault(u => string.Equals(u.NormalizedEmail, email, StringComparison.Ordinal));
            if (record == null)
            {
                errorMessage = "Nenhuma conta foi encontrada para o e-mail informado.";
                return false;
            }

            var hash = HashPassword(password, record.Salt);
            if (!string.Equals(hash, record.PasswordHash, StringComparison.Ordinal))
            {
                errorMessage = "Senha incorreta. Tente novamente.";
                return false;
            }

            profile = new UserProfile(record.DisplayName, record.Email);
            PlayerPrefs.SetString(LastUserKey, record.NormalizedEmail);
            PlayerPrefs.Save();
            return true;
        }

        public bool TryAutoSignIn(out UserProfile profile)
        {
            profile = null;
            var lastEmail = PlayerPrefs.GetString(LastUserKey, string.Empty);
            if (string.IsNullOrWhiteSpace(lastEmail))
            {
                return false;
            }

            var record = _database.Users.FirstOrDefault(u => string.Equals(u.NormalizedEmail, lastEmail, StringComparison.Ordinal));
            if (record == null)
            {
                PlayerPrefs.DeleteKey(LastUserKey);
                PlayerPrefs.Save();
                return false;
            }

            profile = new UserProfile(record.DisplayName, record.Email);
            return true;
        }

        public void SignOut()
        {
            PlayerPrefs.DeleteKey(LastUserKey);
            PlayerPrefs.Save();
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }

        private static UserDatabase LoadDatabase()
        {
            var json = PlayerPrefs.GetString(UsersKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new UserDatabase();
            }

            try
            {
                var database = JsonUtility.FromJson<UserDatabase>(json);
                return database ?? new UserDatabase();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Falha ao carregar usuários salvos: {ex.Message}. Reiniciando base local.");
                return new UserDatabase();
            }
        }

        private void SaveDatabase()
        {
            var json = JsonUtility.ToJson(_database);
            PlayerPrefs.SetString(UsersKey, json);
            PlayerPrefs.Save();
        }

        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hashBytes = sha.ComputeHash(combined);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
