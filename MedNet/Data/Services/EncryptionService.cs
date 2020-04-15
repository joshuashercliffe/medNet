using System;
using System.IO;
using NSec.Cryptography;
using System.Security.Cryptography;

namespace MedNet.Data.Services
{
    public class EncryptionService
    {
        public static void getNewBlockchainUser(out string signPrivateKey, out string signPublicKey,
            out string agreePrivateKey, out string agreePublicKey)
        {
            var signAlgorithm = SignatureAlgorithm.Ed25519;
            var agreeAlgorithm = KeyAgreementAlgorithm.X25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            var signKey = Key.Create(signAlgorithm, parameters);
            var agreeKey = Key.Create(agreeAlgorithm, parameters);
            signPrivateKey = Convert.ToBase64String(signKey.Export(KeyBlobFormat.NSecPrivateKey));
            signPublicKey = Convert.ToBase64String(signKey.Export(KeyBlobFormat.NSecPublicKey));
            agreePrivateKey = Convert.ToBase64String(agreeKey.Export(KeyBlobFormat.NSecPrivateKey));
            agreePublicKey = Convert.ToBase64String(agreeKey.Export(KeyBlobFormat.NSecPublicKey));
        }

        public static Key getSignKeyFromPrivate(string signPrivateKey)
        {
            var signAlgorithm = SignatureAlgorithm.Ed25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            return Key.Import(signAlgorithm, Convert.FromBase64String(signPrivateKey), KeyBlobFormat.NSecPrivateKey, parameters);
        }

        public static Key getAgreeKeyFromPrivate(string agreePrivateKey)
        {
            var signAlgorithm = KeyAgreementAlgorithm.X25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            return Key.Import(signAlgorithm, Convert.FromBase64String(agreePrivateKey), KeyBlobFormat.NSecPrivateKey, parameters);
        }

        public static PublicKey getSignPublicKeyFromString(string signPublicKey)
        {
            var signAlgorithm = SignatureAlgorithm.Ed25519;
            return PublicKey.Import(signAlgorithm, Convert.FromBase64String(signPublicKey), KeyBlobFormat.NSecPublicKey);
        }

        public static PublicKey getAgreePublicKeyFromString(string agreePublicKey)
        {
            var signAlgorithm = KeyAgreementAlgorithm.X25519; ;
            return PublicKey.Import(signAlgorithm, Convert.FromBase64String(agreePublicKey), KeyBlobFormat.NSecPublicKey);
        }

        public static string getSignPublicKeyStringFromPrivate(string signPrivateKey)
        {
            var signAlgorithm = SignatureAlgorithm.Ed25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            var signPrivKey = Key.Import(signAlgorithm, Convert.FromBase64String(signPrivateKey), KeyBlobFormat.NSecPrivateKey, parameters);
            return Convert.ToBase64String(signPrivKey.PublicKey.Export(KeyBlobFormat.NSecPublicKey));
        }

        public static string getAgreePublicKeyStringFromPrivate(string agreePrivateKey)
        {
            var signAlgorithm = KeyAgreementAlgorithm.X25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            var agreePrivKey = Key.Import(signAlgorithm, Convert.FromBase64String(agreePrivateKey), KeyBlobFormat.NSecPrivateKey, parameters);
            return Convert.ToBase64String(agreePrivKey.PublicKey.Export(KeyBlobFormat.NSecPublicKey));
        }

        public static string hashPassword(string password, int iterations = 1000)
        {
            //generate a random salt for hashing
            var salt = new byte[24];
            new RNGCryptoServiceProvider().GetBytes(salt);

            //hash password given salt and iterations (default to 1000)
            //iterations provide difficulty when cracking
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
            byte[] hash = pbkdf2.GetBytes(24);

            //return delimited string with salt | #iterations | hash
            return Convert.ToBase64String(salt) + "|" + iterations + "|" +
                Convert.ToBase64String(hash);

        }

        public static bool verifyPassword(string testPassword, string origDelimHash)
        {
            //extract original values from delimited hash text
            var origHashedParts = origDelimHash.Split('|');
            var origSalt = Convert.FromBase64String(origHashedParts[0]);
            var origIterations = Int32.Parse(origHashedParts[1]);
            var origHash = origHashedParts[2];

            //generate hash from test password and original salt and iterations
            var pbkdf2 = new Rfc2898DeriveBytes(testPassword, origSalt, origIterations);
            byte[] testHash = pbkdf2.GetBytes(24);

            //if hash values match then return success
            if (Convert.ToBase64String(testHash) == origHash)
                return true;

            //no match return false
            return false;

        }

        public static string encryptPrivateKeys(string id, string passphrase, string signPrivateKey, string agreePrivateKey)
        {
            string result = null;
            byte[] encrypted;
            string data = signPrivateKey + "|" + agreePrivateKey;
            var salt = new byte[24];
            new RNGCryptoServiceProvider().GetBytes(salt);
            string phrase = id + passphrase;
            var rfc = new Rfc2898DeriveBytes(phrase, salt, 1000);
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Key = rfc.GetBytes(aes.KeySize / 8);
                aes.GenerateIV();
                var enc = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(msEncrypt, enc, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(cryptoStream))
                        {
                            swEncrypt.Write(data);
                        }
                    }
                    encrypted = msEncrypt.ToArray();
                }
                result = Convert.ToBase64String(encrypted);
                result = result + "|" + Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(aes.IV);
            }
            return result;
        }

        public static string getRawBase58PublicKey(string signPublicKey)
        {
            var signAlgorithm = SignatureAlgorithm.Ed25519;
            var pubKey = PublicKey.Import(signAlgorithm, Convert.FromBase64String(signPublicKey), KeyBlobFormat.NSecPublicKey);
            return SimpleBase.Base58.Bitcoin.Encode(pubKey.Export(KeyBlobFormat.RawPublicKey));
        }

        public static void getPrivateKeyFromIDKeyword(string id, string passphrase, string hashedKeys,out string signPrivateKey, out string agreePrivateKey)
        {
            var origHashedParts = hashedKeys.Split('|');
            var keyHash = Convert.FromBase64String(origHashedParts[0]);
            var salt = Convert.FromBase64String(origHashedParts[1]);
            var iv = Convert.FromBase64String(origHashedParts[2]);
            string joinedKeys = null;
            string phrase = id + passphrase;
            var rfc = new Rfc2898DeriveBytes(phrase, salt, 1000);
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Key = rfc.GetBytes(aes.KeySize / 8);
                aes.IV = iv;
                var dec = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var msDecrypt = new MemoryStream(keyHash))
                {
                    using (var cryptoStream = new CryptoStream(msDecrypt, dec, CryptoStreamMode.Read)) 
                    {
                        using (StreamReader srDecrypt = new StreamReader(cryptoStream)) 
                        {
                            joinedKeys = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            var keyParts = joinedKeys.Split('|');
            signPrivateKey = keyParts[0];
            agreePrivateKey = keyParts[1];
        }

        public static string getEncryptedAssetData(string data, out string encryptionKey)
        {
            string result = null;
            byte[] encrypted;
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                encryptionKey = Convert.ToBase64String(aes.Key);
                aes.GenerateIV();
                var enc = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(msEncrypt, enc, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(cryptoStream))
                        {
                            swEncrypt.Write(data);
                        }
                    }
                    encrypted = msEncrypt.ToArray();
                }
                result = Convert.ToBase64String(encrypted);
                result = result + "|"  + Convert.ToBase64String(aes.IV);
            }
            return result;
        }

        public static string getDecryptedAssetData(string hashedData, string encryptionKey)
        {
            var origHashedParts = hashedData.Split('|');
            var data = Convert.FromBase64String(origHashedParts[0]);
            var iv = Convert.FromBase64String(origHashedParts[1]);
            string result = null;
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Key = Convert.FromBase64String(encryptionKey);
                aes.IV = iv;
                var dec = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var msDecrypt = new MemoryStream(data))
                {
                    using (var cryptoStream = new CryptoStream(msDecrypt, dec, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(cryptoStream))
                        {
                            result = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return result;
        }

        public static string getEncryptedEncryptionKey(string encryptionKey, string senderAgreePrivateKey, string recieverAgreePublicKey)
        {
            var algorithm = KeyAgreementAlgorithm.X25519;
            var parameters = new KeyCreationParameters();
            var byteData = Convert.FromBase64String(encryptionKey);
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            Key senderKey = getAgreeKeyFromPrivate(senderAgreePrivateKey);
            PublicKey recieverKey = getAgreePublicKeyFromString(recieverAgreePublicKey);
            var secret = algorithm.Agree(senderKey,recieverKey);
            var derivedKey = KeyDerivationAlgorithm.HkdfSha256.DeriveKey(secret, null, null, AeadAlgorithm.Aes256Gcm, parameters);
            var nonce = new Nonce(0, 12);
            var encryptedByteData = AeadAlgorithm.Aes256Gcm.Encrypt(derivedKey,nonce,null,byteData);
            var encryptedData = Convert.ToBase64String(encryptedByteData);
            return encryptedData + "|" + getAgreePublicKeyStringFromPrivate(senderAgreePrivateKey);
        }

        public static string getDecryptedEncryptionKey(string hashedData, string recieverAgreePrivateKey)
        {
            var origHashedParts = hashedData.Split('|');
            var encryptedByteData = Convert.FromBase64String(origHashedParts[0]);
            var senderPublicKey = origHashedParts[1];

            var algorithm = KeyAgreementAlgorithm.X25519;
            var parameters = new KeyCreationParameters();
            parameters.ExportPolicy = KeyExportPolicies.AllowPlaintextExport;
            Key recieverKey = getAgreeKeyFromPrivate(recieverAgreePrivateKey);
            PublicKey senderKey = getAgreePublicKeyFromString(senderPublicKey);
            var secret = algorithm.Agree(recieverKey, senderKey);
            var derivedKey = KeyDerivationAlgorithm.HkdfSha256.DeriveKey(secret, null, null, AeadAlgorithm.Aes256Gcm, parameters);
            var nonce = new Nonce(0, 12);
            byte[] decryptedByteData = new byte[encryptedByteData.Length - AeadAlgorithm.Aes256Gcm.TagSize];
            AeadAlgorithm.Aes256Gcm.Decrypt(derivedKey, nonce, null, encryptedByteData, decryptedByteData);
            var decryptedData = Convert.ToBase64String(decryptedByteData);
            return decryptedData;
        }
    }
}
