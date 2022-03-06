using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Translator.Helper
{
    public static class EncryptionHelper
    {
        private static readonly byte[] SALT = new byte[] { 0x8B, 0x4A, 0x19, 0xF1, 0x01, 0xEB, 0xEE, 0xBA, 0xDD, 0x64, 0x86, 0x81, 0xD2 };
        private const string KEY = "sgjkf8G0ahS94zr897n0EFG==";

        public static string Encrypt(this string clearText, string encryptionKey = KEY)
        {
            if (string.IsNullOrEmpty(clearText))
                return string.Empty;

            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, SALT);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public static string Decrypt(this string cipherText, string encyptionKey = KEY)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encyptionKey, SALT);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}