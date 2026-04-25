using System;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.IO;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.Authentication.Infrastructure.Utilities
{
    public class CryptoService: ICryptoService
    {
        private readonly byte[] key = Convert.FromBase64String("p2bLboAc64ilnLmUvuF8aQ==");
        private readonly byte[] iv = Convert.FromBase64String("V7If6fWnVxUu6EpFluF6Aw==");

        public string EncryptObject<T>(T payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            byte[] encryptedBytes = EncryptStringToBytes(json, key, iv);
            return Convert.ToBase64String(encryptedBytes);
        }

        public T DecryptObject<T>(string base64CipherText)
        {
            byte[] cipherBytes = Convert.FromBase64String(base64CipherText);
            string json = DecryptStringFromBytes(cipherBytes, key, iv);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {

                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (var sw = new StreamWriter(cs))
                            {
                                sw.Write(plainText);
                            }

                        }
                        return ms.ToArray();
                    }
                }
            }
        }


        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var ms = new MemoryStream(cipherText))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }

            }
        }
    }
}
