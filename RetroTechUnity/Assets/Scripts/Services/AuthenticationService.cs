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
            public string Username;
            public string NormalizedUsername;
            public string Salt;
            public string PasswordHash;
        }

        private readonly UserDatabase _database;

        public AuthenticationService()
        {
            _database = LoadDatabase();
        }

        public bool TryRegister(string username, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            username = (username ?? string.Empty).Trim();
            var normalizedUsername = NormalizeUsername(username);
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Informe um nome de usuário.";
                return false;
            }

            if (username.Length < 3)
            {
                errorMessage = "O nome de usuário deve ter pelo menos 3 caracteres.";
                return false;
            }

            if (password.Length < 6)
            {
                errorMessage = "A senha deve ter pelo menos 6 caracteres.";
                return false;
            }

            if (_database.Users.Any(u => string.Equals(u.NormalizedUsername, normalizedUsername, StringComparison.Ordinal)))
            {
                errorMessage = "Já existe uma conta com este nome de usuário.";
                return false;
            }

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            _database.Users.Add(new UserRecord
            {
                Username = username,
                NormalizedUsername = normalizedUsername,
                Salt = salt,
                PasswordHash = hash
            });

            SaveDatabase();
            return true;
        }

        public bool TrySignIn(string username, string password, out UserProfile profile, out string errorMessage)
        {
            profile = null;
            errorMessage = string.Empty;

            username = NormalizeUsername(username);
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Informe seu nome de usuário.";
                return false;
            }

            if (string.IsNullOrEmpty(password))
            {
                errorMessage = "Informe sua senha.";
                return false;
            }

            var record = _database.Users.FirstOrDefault(u => string.Equals(u.NormalizedUsername, username, StringComparison.Ordinal));
            if (record == null)
            {
                errorMessage = "Nenhuma conta foi encontrada para o nome de usuário informado.";
                return false;
            }

            var hash = HashPassword(password, record.Salt);
            if (!string.Equals(hash, record.PasswordHash, StringComparison.Ordinal))
            {
                errorMessage = "Senha incorreta. Tente novamente.";
                return false;
            }

            profile = new UserProfile(record.Username);
            PlayerPrefs.SetString(LastUserKey, record.NormalizedUsername);
            PlayerPrefs.Save();
            return true;
        }

        public bool TryAutoSignIn(out UserProfile profile)
        {
            profile = null;
            var lastUsername = PlayerPrefs.GetString(LastUserKey, string.Empty);
            if (string.IsNullOrWhiteSpace(lastUsername))
            {
                return false;
            }

            var record = _database.Users.FirstOrDefault(u => string.Equals(u.NormalizedUsername, lastUsername, StringComparison.Ordinal));
            if (record == null)
            {
                PlayerPrefs.DeleteKey(LastUserKey);
                PlayerPrefs.Save();
                return false;
            }

            profile = new UserProfile(record.Username);
            return true;
        }

        public void SignOut()
        {
            PlayerPrefs.DeleteKey(LastUserKey);
            PlayerPrefs.Save();
        }

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username)
                ? string.Empty
                : username.Trim().ToLowerInvariant();
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
