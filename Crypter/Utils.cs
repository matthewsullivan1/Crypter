using System.Security.Cryptography;

namespace Crypter
{
    internal class Utils
    {
        public static byte[] Encrypt(byte[] dataToEncrypt, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                        csEncrypt.FlushFinalBlock();
                        return msEncrypt.ToArray();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key, byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(encrypted))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        byte[] decryptedBytes = new byte[encrypted.Length];
                        int bytesRead = csDecrypt.Read(decryptedBytes, 0, decryptedBytes.Length);
                        return decryptedBytes;
                    }
                }
            }
        }


        public static string GenerateStub(string outputBasePath, string extension)
        {
            string uniqueFileName = $"Stub_{Guid.NewGuid().ToString()}{extension}";
            string uniqueFilePath = Path.Combine(outputBasePath, uniqueFileName);

            // Copy the template to the new unique file path
            try
            {
                File.Copy("C:\\Users\\18163\\source\\new\\Crypter\\Crypter\\stub.txt", uniqueFilePath);
                Console.WriteLine("Stub source file created at " +  uniqueFilePath + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create stub source file");
                Console.WriteLine(ex.Message);
            }

            return uniqueFilePath;
        }
        public static (byte[] key, byte[] iv) GenerateKeyAndIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();
                return (aesAlg.Key, aesAlg.IV);
            }
        }




    }
}