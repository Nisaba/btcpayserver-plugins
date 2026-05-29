using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace BTCPayServer.Plugins.Satora.Models;

public class SatoraSettings
{
    [Key]
    public string StoreId { get; set; }


    [Display(Name = "Enabled in checkout")]
    public bool Enabled { get; set; }

    public string? CryptedSeed { get; set; }

    private string _seed;

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    [NotMapped]
    public string Seed
    {
        get
        {
            if (!string.IsNullOrEmpty(_seed))
                return _seed;

            var fullData = Convert.FromBase64String(CryptedSeed);

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(fullData, 0, salt, 0, SaltSize);

            var key = Rfc2898DeriveBytes.Pbkdf2(StoreId, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            using var aes = Aes.Create();
            aes.Key = key;

            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(fullData, SaltSize, iv, 0, iv.Length);
            aes.IV = iv;

            var cipherBytes = new byte[fullData.Length - SaltSize - iv.Length];
            Buffer.BlockCopy(fullData, SaltSize + iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            _seed = Encoding.UTF8.GetString(plainBytes);
            return _seed;
        }

        set
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(StoreId, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(value);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[SaltSize + iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(iv, 0, result, SaltSize, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, SaltSize + iv.Length, cipherBytes.Length);

            CryptedSeed = Convert.ToBase64String(result);

        }
    }


}
