using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    public class UTF8
    {
        static private bool IsUtf8(Stream stream)
        {
            int count = 4 * 1024;
            byte[] buffer;
            int read;
            while (true)
            {
                buffer = new byte[count];
                stream.Seek(0, SeekOrigin.Begin);
                read = stream.Read(buffer, 0, count);
                if (read < count)
                {
                    break;
                }
                buffer = null;
                count *= 2;
            }
            return IsUtf8(buffer, read);
        }

        static private bool IsUtf8(byte[] buffer, int length)
        {
            int position = 0;
            int bytes = 0;
            while (position < length)
            {
                if (!IsValid(buffer, position, length, ref bytes))
                {
                    return false;
                }
                position += bytes;
            }
            return true;
        }

        static public bool IsUtf8(string aString)
        {
            using (var stream = GenerateStreamFromString(aString))
            {
                if (IsUtf8(stream))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        static private bool IsValid(byte[] buffer, int position, int length, ref int bytes)
        {
            if (length > buffer.Length)
            {
                throw new ArgumentException("Invalid length");
            }

            if (position > length - 1)
            {
                bytes = 0;
                return true;
            }

            byte ch = buffer[position];

            if (ch <= 0x7F)
            {
                bytes = 1;
                return true;
            }

            if (ch >= 0xc2 && ch <= 0xdf)
            {
                if (position >= length - 2)
                {
                    bytes = 0;
                    return false;
                }
                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 2;
                return true;
            }

            if (ch == 0xe0)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0xa0 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }
                bytes = 3;
                return true;
            }


            if (ch >= 0xe1 && ch <= 0xef)
            {
                if (position >= length - 3)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 3;
                return true;
            }

            if (ch == 0xf0)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x90 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch == 0xf4)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0x8f ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            if (ch >= 0xf1 && ch <= 0xf3)
            {
                if (position >= length - 4)
                {
                    bytes = 0;
                    return false;
                }

                if (buffer[position + 1] < 0x80 || buffer[position + 1] > 0xbf ||
                    buffer[position + 2] < 0x80 || buffer[position + 2] > 0xbf ||
                    buffer[position + 3] < 0x80 || buffer[position + 3] > 0xbf)
                {
                    bytes = 0;
                    return false;
                }

                bytes = 4;
                return true;
            }

            return false;
        }

        static private Stream GenerateStreamFromString(string aString)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(aString);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static public string CleanUTF8(string aString)
        {            
            if (!String.IsNullOrEmpty(aString))
            {
                var encodingInfo = System.Text.Encoding.GetEncodings();

                if (encodingInfo != null)
                {
                    var encode = encodingInfo.FirstOrDefault(i => i.Name == "iso-8859-1");

                    if (encode != null)
                    {
                        var bytes = System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(aString);
                        return System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    }
                }                
            }

            return aString;      
        }

        static public string TransformUTF8(string aString)
        {
            if (!String.IsNullOrEmpty(aString))
            {
                var encodingInfo = System.Text.Encoding.GetEncodings();

                if (encodingInfo != null)
                {
                    var encode = encodingInfo.FirstOrDefault(i => i.Name == "iso-8859-1");

                    if (encode != null)
                    {                        
                        var bytes = Encoding.UTF8.GetBytes(aString);
                        return System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes, 0, bytes.Length);                        
                    }
                }
            }

            return aString;
        }

        static public string StringToUTF8(string aUnicodeString)
        {            
            if (!string.IsNullOrEmpty(aUnicodeString) && !IsUtf8(aUnicodeString))
            {
                byte[] unicodeBytes = Encoding.Unicode.GetBytes(aUnicodeString);

                byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, unicodeBytes);

                // Convert the new byte[] into a char[] and then into a string.
                char[] utf8Chars = new char[Encoding.UTF8.GetCharCount(utf8Bytes, 0, utf8Bytes.Length)];
                Encoding.UTF8.GetChars(utf8Bytes, 0, utf8Bytes.Length, utf8Chars, 0);
                string utf8String = new string(utf8Chars);

                int index = utf8String.IndexOf('\0');

                if (index != -1)
                {                    
                    return utf8String.Substring(0, index);
                }
                else
                {
                    return utf8String;
                }
            }
         
            return aUnicodeString;
        }

        public static string StringToUnicode(string aUTF8String)
        {            
            if (!string.IsNullOrEmpty(aUTF8String) && IsUtf8(aUTF8String))
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(aUTF8String);

                byte[] unicodeBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, utf8Bytes);

                // Convert the new byte[] into a char[] and then into a string.
                char[] unicodeChars = new char[Encoding.Unicode.GetCharCount(unicodeBytes, 0, unicodeBytes.Length)];
                Encoding.Unicode.GetChars(unicodeBytes, 0, unicodeBytes.Length, unicodeChars, 0);
                string unicodeString = new string(unicodeChars);

                int index = unicodeString.IndexOf('\0');

                if (index != -1)
                {
                    return unicodeString.Substring(0, index);
                }
                else
                {
                    return unicodeString;
                }
            }

            return aUTF8String;
        }
    }
}
