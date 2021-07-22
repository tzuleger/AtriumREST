using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    public class AtriumConnection
    {
        // XML File Templates
        private const String GET_SESSION = "./templates/login_get.xml";
        private const String GET_LOGIN = "./templates/login_attempt.xml";
        private const String GET_ADD_USER = "./templates/add_user.xml";
        private const String GET_ADD_CARD = "./templates/add_card.xml";

        // String GET/POST templates
        private const String ENC_TEMPLATE = "sid=@SessionID&post_enc=@EncryptedData&post_chk=@CheckSum";

        // Atrium SDK Subdomain URLs
        private const String LOGIN_URL = "login_sdk.xml";
        private const String DATA_URL = "sdk.xml";

        // Connection info
        private readonly HttpClient _client;
        private readonly String _address;

        // Controller info
        private readonly String _serialNo;
        private readonly String _product;
        private readonly String _label;
        private readonly String _version;

        // Session info
        private readonly String _sessionKey;
        private readonly String _sessionId;
        private readonly String _userId;
        private readonly String _username;

        // Other
        private int _transactionNum = 1;

        public AtriumConnection(String username, String password, String address)
        {
            _client = new HttpClient();
            _address = address;

            // Fetch Session info to establish temporary Session Key
            var xml = do_GET_Async(AtriumConnection.LOGIN_URL).Result;

            // Get device information from the response.
            _serialNo = xml.Element("DEVICE").Attribute("serial").Value;
            _product = xml.Element("DEVICE").Attribute("product").Value;
            _label = xml.Element("DEVICE").Attribute("mdl_label").Value;
            _version = xml.Element("DEVICE").Attribute("version").Value;

            // Get session id from the response.
            _sessionId = xml.Element("CONNECTION").Attribute("session").Value;

            // Use MD5 and RC4 to get Session Key. Use Session Key to get loginUser and loginPass.
            _sessionKey = AtriumConnection.ByteArrayToHexString(AtriumConnection.Md5(_serialNo + _sessionId));
            var loginUser = AtriumConnection.ByteArrayToHexString(AtriumConnection.Rc4(Encoding.ASCII.GetBytes(_sessionKey), Encoding.ASCII.GetBytes(username)));
            var loginPass = AtriumConnection.ByteArrayToHexString(AtriumConnection.Md5(_sessionKey + password));

            // Post login information.
            var parameters = new Dictionary<String, String>
            {
                { "sid", _sessionId },
                { "cmd", "login" },
                { "login_user", loginUser },
                { "login_pass", loginPass }
            };
            var req = do_POST_Async(AtriumConnection.LOGIN_URL , parameters);
            req.Wait();
            xml = req.Result;

            // Update session ID
            _sessionId = xml.Element("CONNECTION").Attribute("session").Value;

            // Get user info
            _userId = xml.Element("SDK_CFG").Attribute("user_id").Value;
            _username = xml.Element("SDK_CFG").Attribute("username").Value;

            // Officially logged in.
        }

        // Insert user into atrium controller associated with AtriumConnection object.
        public void InsertUser(String firstName, String lastName, Guid id, DateTime actDate, DateTime expDate)
        {
            var content = FetchAndEncryptXML(
                AtriumConnection.GET_ADD_USER,
                "@tid", _transactionNum++.ToString(),
                "@SerialNo", _serialNo,
                "@FirstName", firstName,
                "@LastName", lastName,
                "@UserID", id.ToString(),
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16)
            );
            do_POST_Async(AtriumConnection.DATA_URL, content, setSessionCookie: true, encryptedExchange: true).Wait();
        }

        // Insert card into atrium controller associated with AtriumConnection object.
        public void InsertCard(String displayName, Guid cardId, Guid userId, int famNum, int memNum, DateTime actDate, DateTime expDate)
        {
            var content = FetchAndEncryptXML(
                AtriumConnection.GET_ADD_CARD,
                "@tid", _transactionNum++.ToString(),
                "@SerialNo", _serialNo,
                "@DisplayName", displayName,
                "@CardID", cardId.ToString(),
                "@UserID", userId.ToString(),
                "@FamilyNumber", Convert.ToString(famNum, 16),
                "@MemberNumber", Convert.ToString(memNum, 16),
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16)
            );
           do_POST_Async(AtriumConnection.DATA_URL, content, setSessionCookie: true, encryptedExchange: true).Wait();
        }

        // Checks to see if the 'err' attributer is 'ok'. Throws exception if not.
        private void check_Err(XElement xml)
        {
            if(xml.Element("CONNECTION").Attribute("err").Value != "ok")
            {
                throw new AnswerNotOkException();
            }
        }

        // Performs a POST request to the connection associated with the AtriumConnection object
        private async Task<XElement> do_POST_Async(String subdomain, StringContent httpContent, bool setSessionCookie=false)
        {
            if (setSessionCookie)
            {
                httpContent.Headers.Add("Cookie", $"Session={_sessionId}-{AtriumConnection.PadLeft(_userId, '0', 2)};");
                Console.WriteLine(httpContent.Headers);
            }
            var response = await _client.PostAsync(_address + subdomain, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RESPONSE:\n" + responseString + "\n");
            var xml = XElement.Parse(responseString);
            check_Err(xml);
            return xml;
        }

        // Performs a POST request to the connection associated with the AtriumConnection object
        private async Task<XElement> do_POST_Async(String subdomain, Dictionary<String, String> parameters, bool setSessionCookie = false, bool encryptedExchange = false)
        {
            var encodedContent = new FormUrlEncodedContent(parameters);
            if(setSessionCookie)
            {
                encodedContent.Headers.Add("Cookie", $"Session=\"{_sessionId}-{AtriumConnection.PadLeft(_userId, '0', 2)}\";");
                Console.WriteLine(encodedContent.Headers);
            }
            var response = await _client.PostAsync(_address + subdomain, encodedContent);
            var responseString = await response.Content.ReadAsStringAsync();
            if (encryptedExchange)
            {
                if(!responseString.Contains("&post_enc"))
                {
                    throw new HttpRequestException(responseString);
                }
                var postEnc = responseString.Split("&post_enc=")[1].Split("&")[0];
                responseString = Encoding.ASCII.GetString(AtriumConnection.Rc4(Encoding.ASCII.GetBytes(_sessionKey), Encoding.ASCII.GetBytes(postEnc)));
            }
            Console.WriteLine("RESPONSE:\n" + responseString + "\n");
            var xml = XElement.Parse(responseString);
            check_Err(xml);
            return xml;
        }

        // Performs a POST request to the connection associated with the AtriumConnection object
        private async Task<XElement> do_GET_Async(String subdomain)
        {
            Console.WriteLine("GET: " + _address + subdomain);
            var response = await _client.GetAsync(_address + subdomain);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RESPONSE:\n" + responseString + "\n");
            var xml = XElement.Parse(responseString);
            check_Err(xml);
            return xml;
        }

        // Fetches XML File and converts it to a StringContent to be used as an HttpContent file.
        // If args are entered, then every even index in args should be the replacement key and every odd should be the replacement value.
        private StringContent FetchXMLAsHttpContent(String fileName, params String[] args)
        {
            var fileContent = File.ReadAllText(fileName, Encoding.ASCII);
            for(int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i+1]);
            }

            Console.WriteLine("REQUEST: " + fileContent);

            return new StringContent(
                fileContent,
                Encoding.ASCII,
                "text/xml"
            );
        }

        private Dictionary<String, String> FetchAndEncryptXML(String fileName, params String[] args)
        {
            Dictionary<String, String> parameters = new Dictionary<String, String>();
            var fileContent = File.ReadAllText(fileName, Encoding.ASCII);
            for (int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i + 1]);
            }

            Console.WriteLine("REQUEST (unecrypted): " + fileContent);

            var postEnc = AtriumConnection.ByteArrayToHexString(AtriumConnection.Rc4(Encoding.ASCII.GetBytes(_sessionKey), Encoding.ASCII.GetBytes(fileContent)));
            var postChk = AtriumConnection.CheckSum(_sessionKey + fileContent);
            parameters.Add("post_enc", postEnc);
            parameters.Add("post_chk", postChk);

            Console.WriteLine($"REQUEST (encrypted): post_enc={parameters["post_enc"]}&post_chk={parameters["post_chk"]}");

            return parameters;
        }

        // Rc4 encryption algorithm
        private static byte[] Rc4(byte[] pwd, byte[] data)
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
                a++;
                a %= 256;
                j += box[a];
                j %= 256;
                tmp = box[a];
                box[a] = box[j];
                box[j] = tmp;
                k = box[((box[a] + box[j]) % 256)];
                cipher[i] = (byte)(data[i] ^ k);
            }
            return cipher;
        }

        // Md5 hash algorithm that takes in a String
        private static byte[] Md5(String text)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
            return hash;
        }

        private static String CheckSum(String str)
        {
            var chk = 0; 
            for (int i = 0; i < str.Length; i++) 
            {
                chk += str[i];
            }
            var chkSumString = (chk & 0xFFFF).ToString("X");
            chkSumString = AtriumConnection.PadLeft(chkSumString, '0', 4);
            return chkSumString;
        }

        private static String PadLeft(String s, char c, int length)
        {
            while(s.Length < length)
            {
                s = c + s;
            }
            return s;
        }

        // Converts a byte array to a hexadecimal string
        public static string ByteArrayToHexString(byte[] bytes)
        {
          return BitConverter.ToString(bytes).Replace("-","");
        }

        // Custom Exceptions

        public class AnswerNotOkException : Exception
        {
            public AnswerNotOkException() : base("HTTP Response did not result in err='ok'.") { }
        }

        public class HttpRequestException : Exception
        {
            public HttpRequestException(String responseString) : base("Request failed. " + responseString) { }
        }
    }
}
