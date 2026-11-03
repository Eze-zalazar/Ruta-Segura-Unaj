namespace Infrastructure.Services;

using Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;
    private const char SegmentDelimiter = ':';

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithm,
            KeySize);

        return $"PBKDF2{SegmentDelimiter}{Iterations}{SegmentDelimiter}{Convert.ToBase64String(salt)}{SegmentDelimiter}{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // Formato seguro PBKDF2
        if (passwordHash.StartsWith($"PBKDF2{SegmentDelimiter}"))
        {
            var segments = passwordHash.Split(SegmentDelimiter);
            if (segments.Length != 4)
                return false;

            if (!int.TryParse(segments[1], out int iterations))
                return false;

            byte[] salt;
            byte[] expectedHash;
            try
            {
                salt = Convert.FromBase64String(segments[2]);
                expectedHash = Convert.FromBase64String(segments[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                HashAlgorithm,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        // Compatibilidad retroactiva con Base64 directo de usuarios existentes
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(passwordHash));
            if (decoded == password)
                return true;
        }
        catch
        {
            // Ignorar errores de decoding y continuar
        }

        // Comparación directa en texto plano (en caso extremo de testing previo)
        return password == passwordHash;
    }
}
