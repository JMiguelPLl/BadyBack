namespace BadyBackend.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Genera un hash seguro con algoritmo BCrypt y salt automático.
        /// </summary>
        public static string HashPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                return string.Empty;

            return BCrypt.Net.BCrypt.HashPassword(plainPassword.Trim());
        }

        /// <summary>
        /// Verifica la contraseña ingresada contra la almacenada en la base de datos.
        /// Soporta tanto hashes BCrypt como texto plano para compatibilidad hacia atrás con usuarios existentes.
        /// </summary>
        public static bool VerifyPassword(string plainPassword, string storedPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedPassword))
                return false;

            // Si la contraseña almacenada es un hash BCrypt ($2a$, $2b$, $2y$)
            if (IsHashed(storedPassword))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(plainPassword.Trim(), storedPassword);
                }
                catch
                {
                    return false;
                }
            }

            // Compatibilidad hacia atrás: si la contraseña en BD sigue en texto plano
            return plainPassword.Trim() == storedPassword.Trim();
        }

        /// <summary>
        /// Comprueba si una cadena ya tiene formato de hash BCrypt.
        /// </summary>
        public static bool IsHashed(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.StartsWith("$2a$") || password.StartsWith("$2b$") || password.StartsWith("$2y$");
        }
    }
}
