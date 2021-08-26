using System;
using System.Text;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Security
{
    /// <summary>
    /// Performs RC4 Encryption/Decryption algorithms. Also includes a CheckSum calculator.
    /// </summary>
    public class RC4
    {
        /// <summary>
        /// Encrypts a given string with a given secret key. Secret key must be in hexadecimal.
        /// </summary>
        /// <param name="hexKey">Secret key to encrypt with</param>
        /// <param name="plaintext">String to be encrypted</param>
        /// <returns>Encrypted string in hexadecimal.</returns>
        public static String Encrypt(String hexKey, String plaintext)
        {
            return toHex(rc4(toBytes(hexKey), toBytes(plaintext)));
        }

        /// <summary>
        /// Decrypts a given string with a given secret key. String and secret key must be in hexadecimal.
        /// </summary>
        /// <param name="hexKey">Secret key to decrypt with</param>
        /// <param name="ciphertext">String to be decrypted</param>
        /// <returns>Decrypted string in ASCII</returns>
        public static String Decrypt(String hexKey, String ciphertext)
        {
            return Encoding.ASCII.GetString(rc4(toBytes(hexKey), toBytes(ciphertext)));
        }

        /// <summary>
        /// Performs a Check Sum on the given string, str, where the checksum is the summation of all character codes rolling over 0xFFFF.
        /// </summary>
        /// <param name="str">String to perform the check sum on.</param>
        /// <returns>Returns a String formatted like "0x####" where #### is the summation of all character codes in str.</returns>
        public static String CheckSum(String str)
        {
            int chk = 0;
            for (int i = 0; i < str.Length; i++)
            {
                chk += str[i];
            }
            var chkSumString = (chk & 0xFFFF).ToString("X");
            chkSumString = pad(chkSumString, '0', 4);
            return chkSumString;
        }

        // Performs RC4 encryption on a byte array given a secret key as a byte array
        private static byte[] rc4(byte[] pwd, byte[] data)
        {
            int a, i, j, k, tmp;
            int[] key, box;
            byte[] cipher;

            key = new int[256];
            box = new int[256];
            cipher = new byte[data.Length];

            for (i = 0; i < 256; i++)
            {
                key[i] = pwd[i % pwd.Length];
                box[i] = i;
            }
            for (j = i = 0; i < 256; i++)
            {
                j = (j + box[i] + key[i]) % 256;
                tmp = box[i];
                box[i] = box[j];
                box[j] = tmp;
            }
            for (a = j = i = 0; i < data.Length; i++)
            {
                a = (a + 1) % 256;
                j = (j + box[a]) % 256;
                tmp = box[a];
                box[a] = box[j];
                box[j] = tmp;
                k = box[((box[a] + box[j]) % 256)];
                cipher[i] = (byte)(data[i] ^ k);
            }
            return cipher;
        }

        // Converts an array of bytes to a hexadecimal string.
        private static String toHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        ///Converts a hexadecimal String to an array of bytes.
        private static byte[] toBytes(String hex)
        {
            return Encoding.ASCII.GetBytes(hex);
        }

        // Pads a string s to the left with the given character c to the total length of l
        private static String pad(String s, char c, int l)
        {
            while (s.Length < l)
            {
                s = c + s;
            }
            return s;
        }
    }
}
