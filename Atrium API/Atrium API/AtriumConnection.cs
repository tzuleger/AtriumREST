using System;
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
        private const String GET_REQUEST_TEMPLATE = "sid=@SessionID&cmd=@Command&login_user=@LoginUser&login_pass=@LoginPass";

        private const String GET_SESSION = "./templates/login_get.xml";
        private const String GET_LOGIN = "./templates/login_attempt.xml";
        private const String GET_ADD_USER = "./templates/add_user.xml";
        private const String GET_ADD_CARD = "./templates/add_card.xml";

        private const String LOGIN_URL = "/login_sdk.xml";
        private const String DATA_URL = "/sdk.xml";

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

        // Other
        private int _transactionNum = 0;

        public AtriumConnection(String username, String password, String address)
        {
            _client = new HttpClient();
            _address = address;

            // Fetch Session info to establish temporary Session Key
            var httpContent = AtriumConnection.FetchXMLHttpContent(AtriumConnection.GET_SESSION);
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
            httpContent = AtriumConnection.FetchXMLHttpContent(AtriumConnection.GET_LOGIN, "@User", loginUser, "@Pass", loginPass);
            do_POST_Async(AtriumConnection.LOGIN_URL, httpContent).Wait();

            // Officially logged in.
        }

        // Insert user into atrium controller associated with AtriumConnection object.
        public void InsertUser(String firstName, String lastName, Guid id, DateTime actDate, DateTime expDate)
        {
            var httpContent = AtriumConnection.FetchXMLHttpContent(
                AtriumConnection.GET_ADD_USER,
                "@tid", _transactionNum++.ToString(),
                "@SerialNo", _serialNo,
                "@FirstName", firstName,
                "@LastName", lastName,
                "@UserID", id.ToString(),
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16)
            );
            do_POST_Async(AtriumConnection.DATA_URL, httpContent).Wait();
        }

        // Insert card into atrium controller associated with AtriumConnection object.
        public void InsertCard(String displayName, Guid cardId, Guid userId, int famNum, int memNum, DateTime actDate, DateTime expDate)
        {
            var httpContent = AtriumConnection.FetchXMLHttpContent(
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
           do_POST_Async(AtriumConnection.DATA_URL, httpContent).Wait();
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
        private async Task<XElement> do_POST_Async(String subdomain, StringContent httpContent)
        {
            
            var response = await _client.PostAsync(_address + subdomain, httpContent);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RESPONSE:\n" + responseString + "\n");
            var xml = XElement.Parse(responseString);
            check_Err(xml);
            return xml;
        }

        // Performs a POST request to the connection associated with the AtriumConnection object
        private async Task<XElement> do_GET_Async(String subdomain)
        {
            var response = await _client.GetAsync(_address + subdomain);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("RESPONSE:\n" + responseString + "\n");
            var xml = XElement.Parse(responseString);
            check_Err(xml);
            return xml;
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
            using(MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
            return hash;
        }

        // Fetches XML File and converts it to a StringContent to be used as an HttpContent file.
        // If args are entered, then every even index in args should be the replacement key and every odd should be the replacement value.
        private static StringContent FetchXMLHttpContent(String fileName, params String[] args)
        {
            var fileContent = File.ReadAllText(fileName, Encoding.ASCII);
            for(int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i+1]);
            }

            Console.WriteLine(fileContent);

            return new StringContent(
                File.ReadAllText(fileName, Encoding.ASCII),
                Encoding.ASCII,
                "text/xml"
            );
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
    }
}
