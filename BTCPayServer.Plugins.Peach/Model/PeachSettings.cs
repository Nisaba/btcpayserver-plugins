using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BTCPayServer.Plugins.Peach.Model;

public class PeachSettings
{

    [Key]
    public string StoreId { get; set; }

    [Display(Name = "Your Peach Public Key")]
    public string PublicKey { get; set; }

    public string PrivateKey { get; set; }

    public bool IsRegistered { get; set; }



    [NotMapped]
    public string Pwd { get; set; }


    [NotMapped]
    public bool isConfigured
    {
        get
        {
            return !string.IsNullOrEmpty(PublicKey) && !string.IsNullOrEmpty(PrivateKey);
        }
    }

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    [NotMapped]
    public string PrivKey
    {
        get
        {
            if (string.IsNullOrEmpty(PrivateKey) || string.IsNullOrEmpty(Pwd))
                return string.Empty;

            var fullData = Convert.FromBase64String(PrivateKey);

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(fullData, 0, salt, 0, SaltSize);

            using var keyDerivation = new Rfc2898DeriveBytes(Pwd, salt, Iterations, HashAlgorithmName.SHA256);
            var key = keyDerivation.GetBytes(KeySize);

            using var aes = Aes.Create();
            aes.Key = key;

            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(fullData, SaltSize, iv, 0, iv.Length);
            aes.IV = iv;

            var cipherBytes = new byte[fullData.Length - SaltSize - iv.Length];
            Buffer.BlockCopy(fullData, SaltSize + iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        set
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(Pwd))
            {
                PrivateKey = string.Empty;
                return;
            }
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var keyDerivation = new Rfc2898DeriveBytes(Pwd, salt, Iterations, HashAlgorithmName.SHA256);
            var key = keyDerivation.GetBytes(KeySize);

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

            PrivateKey = Convert.ToBase64String(result);

        }
    }
}
