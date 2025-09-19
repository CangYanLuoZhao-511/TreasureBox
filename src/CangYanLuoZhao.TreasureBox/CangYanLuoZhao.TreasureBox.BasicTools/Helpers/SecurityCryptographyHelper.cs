#region << 版 本 注 释 >>
/*----------------------------------------------------------------
 * 版权所有 (c) 2025 苍煙落照保留所有权利。
 * CLR版本：4.0.30319.42000
 *************************************
 * 命名空间：CangYanLuoZhao.TreasureBox.BasicTools.Helpers
 * 唯一标识：ccd133a5-94cc-444b-b166-e0b35a06e83f
 * 文件名：SecurityCryptographyHelper
 * 创建者：苍煙落照
 * 电子邮箱：543730731@qq.com
 * 创建时间：2025/9/18 22:08:27
 * 版本：V1.0.0
 * 描述：
 *
 * ----------------------------------------------------------------
 * 修改人：苍煙落照
 * 时间：2025/9/18 22:08:27
 * 修改说明：
 *
 * 版本：V1.0.1
 *----------------------------------------------------------------*/
#endregion << 版 本 注 释 >>

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CangYanLuoZhao.TreasureBox.BasicTools.Helpers
{
    /// <summary>
    /// 安全加密工具类，基于.NET Standard 2.1
    /// 包含AES、RSA、SHA哈希、MD5等常用加密算法
    /// </summary>
    public class SecurityCryptographyHelper : IDisposable
    {
        #region 字段与构造函数

        private readonly Aes _aes;
        private readonly RSA _rsa;
        private bool _disposed = false;

        /// <summary>
        /// 初始化加密工具类
        /// </summary>
        public SecurityCryptographyHelper()
        {
            // 初始化AES加密服务，使用CBC模式和PKCS7填充
            _aes = Aes.Create();
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;

            // 初始化RSA加密服务，使用2048位密钥
            _rsa = RSA.Create(2048);
        }

        #endregion

        #region AES加密解密 (对称加密)

        /// <summary>
        /// 生成AES密钥和向量
        /// </summary>
        /// <param name="key">输出的AES密钥</param>
        /// <param name="iv">输出的AES向量</param>
        public void GenerateAesKeyAndIV(out byte[] key, out byte[] iv)
        {
            _aes.GenerateKey();
            _aes.GenerateIV();
            key = _aes.Key;
            iv = _aes.IV;
        }

        /// <summary>
        /// 使用AES加密字符串
        /// </summary>
        /// <param name="plainText">要加密的明文</param>
        /// <param name="key">AES密钥</param>
        /// <param name="iv">AES向量</param>
        /// <returns>加密后的Base64字符串</returns>
        public string AesEncrypt(string plainText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (key == null || key.Length == 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length == 0)
                throw new ArgumentNullException(nameof(iv));

            using (var encryptor = _aes.CreateEncryptor(key, iv))
            {
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(inputBytes, 0, inputBytes.Length);
                        cs.FlushFinalBlock();

                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// 使用AES解密字符串
        /// </summary>
        /// <param name="cipherText">要解密的Base64密文</param>
        /// <param name="key">AES密钥</param>
        /// <param name="iv">AES向量</param>
        /// <returns>解密后的明文</returns>
        public string AesDecrypt(string cipherText, byte[] key, byte[] iv)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));
            if (key == null || key.Length == 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length == 0)
                throw new ArgumentNullException(nameof(iv));

            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (var decryptor = _aes.CreateDecryptor(key, iv))
            {
                using (var ms = new MemoryStream(cipherBytes))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs, Encoding.UTF8))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        #endregion

        #region RSA加密解密 (非对称加密)

        /// <summary>
        /// 导出RSA公钥 (XML格式)
        /// </summary>
        /// <returns>公钥XML字符串</returns>
        public string ExportRsaPublicKeyXml()
        {
            return _rsa.ToXmlString(false);
        }

        /// <summary>
        /// 导出RSA私钥 (XML格式)
        /// </summary>
        /// <returns>私钥XML字符串</returns>
        public string ExportRsaPrivateKeyXml()
        {
            return _rsa.ToXmlString(true);
        }

        /// <summary>
        /// 导入RSA公钥 (XML格式)
        /// </summary>
        /// <param name="publicKeyXml">公钥XML字符串</param>
        public void ImportRsaPublicKeyXml(string publicKeyXml)
        {
            if (string.IsNullOrEmpty(publicKeyXml))
                throw new ArgumentNullException(nameof(publicKeyXml));

            _rsa.FromXmlString(publicKeyXml);
        }

        /// <summary>
        /// 导入RSA私钥 (XML格式)
        /// </summary>
        /// <param name="privateKeyXml">私钥XML字符串</param>
        public void ImportRsaPrivateKeyXml(string privateKeyXml)
        {
            if (string.IsNullOrEmpty(privateKeyXml))
                throw new ArgumentNullException(nameof(privateKeyXml));

            _rsa.FromXmlString(privateKeyXml);
        }

        /// <summary>
        /// 使用RSA公钥加密数据
        /// </summary>
        /// <param name="plainText">要加密的明文</param>
        /// <returns>加密后的Base64字符串</returns>
        public string RsaEncrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = _rsa.Encrypt(inputBytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 使用RSA私钥解密数据
        /// </summary>
        /// <param name="cipherText">要解密的Base64密文</param>
        /// <returns>解密后的明文</returns>
        public string RsaDecrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = _rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion

        #region 哈希算法 (SHA系列与MD5)

        /// <summary>
        /// 计算字符串的MD5哈希值
        /// 注意：MD5安全性较低，不建议用于密码存储等敏感场景，仅推荐用于文件校验等非安全场景
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>哈希值的十六进制字符串</returns>
        public string ComputeMd5Hash(string input)
        {
            return ComputeHash(input, MD5.Create());
        }

        /// <summary>
        /// 计算字符串的SHA256哈希值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>哈希值的十六进制字符串</returns>
        public string ComputeSha256Hash(string input)
        {
            return ComputeHash(input, SHA256.Create());
        }

        /// <summary>
        /// 计算字符串的SHA512哈希值
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>哈希值的十六进制字符串</returns>
        public string ComputeSha512Hash(string input)
        {
            return ComputeHash(input, SHA512.Create());
        }

        /// <summary>
        /// 计算带盐值的哈希值 (用于密码存储)
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="salt">盐值</param>
        /// <returns>带盐值的哈希结果</returns>
        public string ComputeSaltedHash(string input, out byte[] salt)
        {
            // 生成随机盐值
            salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // 组合输入和盐值
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] inputWithSaltBytes = new byte[inputBytes.Length + salt.Length];

            Buffer.BlockCopy(inputBytes, 0, inputWithSaltBytes, 0, inputBytes.Length);
            Buffer.BlockCopy(salt, 0, inputWithSaltBytes, inputBytes.Length, salt.Length);

            // 计算哈希
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputWithSaltBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 验证带盐值的哈希
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="salt">盐值</param>
        /// <param name="expectedHash">预期的哈希值</param>
        /// <returns>验证结果</returns>
        public bool VerifySaltedHash(string input, byte[] salt, string expectedHash)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] inputWithSaltBytes = new byte[inputBytes.Length + salt.Length];

            Buffer.BlockCopy(inputBytes, 0, inputWithSaltBytes, 0, inputBytes.Length);
            Buffer.BlockCopy(salt, 0, inputWithSaltBytes, inputBytes.Length, salt.Length);

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputWithSaltBytes);
                string computedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return computedHash == expectedHash;
            }
        }

        /// <summary>
        /// 计算哈希的通用方法
        /// </summary>
        private string ComputeHash(string input, HashAlgorithm hashAlgorithm)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = hashAlgorithm.ComputeHash(inputBytes);

            // 将字节数组转换为十六进制字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        #endregion

        #region 随机数生成

        /// <summary>
        /// 生成安全的随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns>随机字符串</returns>
        public string GenerateSecureRandomString(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            char[] result = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[length];
                rng.GetBytes(data);

                for (int i = 0; i < length; i++)
                {
                    result[i] = chars[data[i] % chars.Length];
                }
            }

            return new string(result);
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源
                _aes?.Dispose();
                _rsa?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~SecurityCryptographyHelper()
        {
            Dispose(false);
        }

        #endregion
    }
}
