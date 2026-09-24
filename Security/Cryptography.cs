using D365.Community.Ps.Automation.Net;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace D365.Community.Ps.Automation.Security
{
    internal static class Cryptography
    {
        internal static string Encrypt(string plainText)
        {
            var psKey = Environment.GetEnvironmentVariable("D365_PS_KEY");//portable ;-)
            if (string.IsNullOrWhiteSpace(psKey) || psKey.Length < 4)
            {
                psKey = Environment.MachineName;
                if (psKey.Length < 4)
                {
                    psKey = "ps-automation-dll";
                }
            }

            var iv = new byte[16];
            byte[] array;

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Hash(psKey));
                aes.IV = iv;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    array = ms.ToArray();
                }
            }

            return Convert.ToBase64String(array);
        }

        internal static string Decrypt(string cipherText)
        {
            var psKey = Environment.GetEnvironmentVariable("D365_PS_KEY");//portable ;-)
            if (string.IsNullOrWhiteSpace(psKey) || psKey.Length < 4)
            {
                psKey = Environment.MachineName;
                if (psKey.Length < 4)
                {
                    psKey = "ps-automation-dll";
                }
            }

            var iv = new byte[16];
            var buffer = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Hash(psKey));
                aes.IV = iv;
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static string Hash(string text)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var hashBuilder = new StringBuilder();
                foreach (var b in result) hashBuilder.Append(b.ToString("x2"));
                return hashBuilder.ToString();//32byte -> 256bit
            }
        }

        /// <summary>
        /// Create new JWT signature based on RSA private key.
        /// </summary>
        /// <param name="header">as Base64Url</param>
        /// <param name="payload">as Base64Url</param>
        /// <param name="certificatePath"></param>
        /// <param name="certificatePassword"></param>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        internal static string JWTSignature(string header, string payload, string certificatePath, SecureString certificatePassword)
        {
            using (var certificate = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable))
            {
                return Base64Url.Encode(certificate.GetRSAPrivateKey().SignData(Encoding.UTF8.GetBytes($"{header}.{payload}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
        }

        /// <summary>
        /// Get X509 certificate hash code.
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="certificatePassword"></param>
        /// <returns></returns>
        // ReSharper disable once InconsistentNaming
        internal static byte[] X509CertificateDER(string certificatePath, SecureString certificatePassword)
        {
            using (var certificate = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable))
            {
                return certificate.GetCertHash();
            }
        }

        internal static DateTime X509CertificateDate(string certificatePath, SecureString certificatePassword)
        {
            using (var certificate = new X509Certificate2(certificatePath, certificatePassword, X509KeyStorageFlags.Exportable))
            {
                return certificate.NotAfter;
            }
        }
    }
}
