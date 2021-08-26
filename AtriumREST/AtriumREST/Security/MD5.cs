using System;
using System.Linq;
using System.Text;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Security
{
    /// <summary>
    /// Performs MD5 hash algorithm.
    /// </summary>
    public class MD5
    {
        /// <summary>
        /// Hashes the given string using MD5 algorithms.
        /// </summary>
        /// <param name="text">String to be hashed</param>
        /// <returns>Hashed string in hexadecimal</returns>
        public static String Hash(String text)
        {
            return toHex(hash(text));
        }

        // Hashes a given String of text and returns a byte array.
        private static byte[] hash(String text)
        {
            byte[] hash;
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
            return hash;
        }

        // Converts an array of bytes to a hexadecimal string.
        private static String toHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
