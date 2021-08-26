using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;

using ThreeRiversTech.Zuleger.Atrium.REST.Security;
using ThreeRiversTech.Zuleger.Atrium.REST.Exceptions;

namespace ThreeRiversTech.Zuleger.Atrium.REST
{
    /// <summary>
    /// Used to communicate with an Atrium Controller using Atrium SDK and HTTP.
    /// </summary>
    public partial class AtriumController
    {
        #region Public Class Attributes
        /// <summary>
        /// Maximum number of attempts to establish a session. (by default: 10. Maximum amount that can be set is 50.)
        /// </summary>
        public static int MaxAttempts { get; set; } = 10;
        /// <summary>
        /// The time in seconds to wait for the next attempt to log into the controller. (by default: 10 seconds)
        /// </summary>
        public static int DelayBetweenAttempts { get; set; } = 10;


        #endregion

        #region Private Class Attributes
        // Namespace used when grabbing certain XElements.
        private static XNamespace XmlNameSpace = "https://www.cdvi.ca/";

        // Elements used when interacting with data inside Atrium Controller
        private static XName XML_EL_RECORDS = AtriumController.XmlNameSpace + "RECORDS";
        private static XName XML_EL_RECORD = AtriumController.XmlNameSpace + "REC";
        private static XName XML_EL_DATA = AtriumController.XmlNameSpace + "DATA";

        // Elements used when logging in
        private static XName XML_EL_CONNECTION = "CONNECTION";
        private static XName XML_EL_DEVICE = "DEVICE";
        private static XName XML_EL_SDK_CFG = "SDK_CFG";

        // XML File Templates
        private const String ADD_USER = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='add' sernum='@SerialNo' type='user' id='0' rec='cfg'><DATA valid='1' guid2='@UserID' label3='@FirstName' label4='@LastName' utc_time22='@ActivationDate' utc_time23='@ExpirationDate' word24_0='@AccessLevel1' word24_1='@AccessLevel2' word24_2='@AccessLevel3' word24_3='@AccessLevel4' word24_4='@AccessLevel5'/></REC></RECORDS></SDK>";
        private const String ADD_CARD = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='add' sernum='@SerialNo' type='card' id='0' rec='cfg'><DATA valid='1' dword4='@ObjectID' guid2='@CardID' label3='@DisplayName' hexv5='@MemberNumber' utc_time24='@ActivationDate' utc_time25='@ExpirationDate' guid26='@UserID'/></REC></RECORDS></SDK>";
        private const String UPDATE_USER = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='write' sernum='@SerialNo' type='user' id='@ObjectID' rec='cfg'><DATA label3='@FirstName' label4='@LastName' utc_time22='@ActivationDate' utc_time23='@ExpirationDate'/></REC></RECORDS></SDK>";
        private const String UPDATE_CARD = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='write' sernum='@SerialNo' type='user' id='@ObjectID' rec='cfg'><DATA label3='@DisplayName' utc_time22='@ActivationDate' utc_time23='@ExpirationDate'/></REC><RECORDS></SDK>";
        private const String READ_USER = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='read' sernum='@SerialNo' type='user' id_min='@min' id_max='@max' rec='cfg'></REC></RECORDS></SDK>";
        private const String READ_CARD = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SDK xmlns='https://www.cdvi.ca/'><RECORDS><REC trans_id='@tid' cmd='read' sernum='@SerialNo' type='card' id_min='@min' id_max='@max' rec='cfg'></REC></RECORDS></SDK>";
        
        // Atrium SDK Subdomain URLs
        private const String LOGIN_URL = "login_sdk.xml";
        private const String DATA_URL = "sdk.xml";
        #endregion

        #region Public Instance Attributes
        // Other
        /// <summary>
        /// Stores the Request XML data to the last POST request made.
        /// </summary>
        public String RequestText { get; set; } = "No request made.";
        /// <summary>
        /// Stores the Response String from the last POST response received.
        /// </summary>
        public String ResponseText { get; set; } = "No response received.";
        /// <summary>
        /// Serial number of the Atrium Controller that the AtriumController object is connected to.
        /// Stores the Request XML data after encryption to the last POST request made.
        /// </summary>
        public String EncryptedRequest { get; set; } = "No request made.";
        /// <summary>
        /// Stores the Response XML data before decryption to the last POST response received.
        /// </summary>
        public String EncryptedResponse { get; set; } = "No response received.";

        /// <summary>
        /// Serial number of the Atrium Controller that the AtriumConnection object is connected to.
        /// </summary>
        public String SerialNumber { get => _serialNo; }
        /// <summary>
        /// Product name of the Atrium Controller that the AtriumController object is connected to.
        /// </summary>
        public String ProductName { get => _product; }
        /// <summary>
        /// Product label of the Atrium Controller that the AtriumController object is connected to.
        /// </summary>
        public String ProductLabel { get => _label; }
        /// <summary>
        /// Product version of the Atrium Controller that the AtriumController object is connected to.
        /// </summary>
        public String ProductVersion { get => _version; }
        /// <summary>
        /// Session ID of the Atrium Controller that the AtriumController object is connected to.
        /// </summary>
        public String SessionID { get => _sessionId; }

        /// <summary>
        /// Generates a random Guid (128-bit ID) in the format of "########-####-####-####-############".
        /// This should never be the same string when called.
        /// </summary>
        public Guid GenerateGuid { get => Guid.NewGuid(); }
        #endregion

        #region Private Instance Attributes
        // Connection info
        private readonly CookieContainer _cookies;
        private readonly HttpClientHandler _handler;
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

        // Keeps track of the number of transactions being sent over the current Connection.
        private int _transactionNum = 1;
        #endregion

        #region Constructor / Deconstructor
        /// <summary>
        /// Creates an AtriumController object connected to the specified address under the specified username and password.
        /// </summary>
        /// <param name="username">Username to log in as.</param>
        /// <param name="password">Password to log into Atrium under specified username</param>
        /// <param name="address">Atrium Controller Address to connect to</param>
        public AtriumController(
            String username,
            String password,
            String address)
        {
            _cookies = new CookieContainer();
            _handler = new HttpClientHandler() { CookieContainer=_cookies};
            _client = new HttpClient(_handler);
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _client.DefaultRequestHeaders.Add("Accept-Language", "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _client.DefaultRequestHeaders.Add("Accept", "*/*");

            _address = address;

            // Fetch Session info to establish temporary Session Key
            var xml = DoGETAsync(AtriumController.LOGIN_URL, encryptedExchange: false).Result;
            CheckAnswer(xml, AtriumController.XML_EL_CONNECTION);

            // Get device information from the response.
            _serialNo = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("serial")?.Value;
            _product = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("product")?.Value;
            _label = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("mdl_label")?.Value;
            _version = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("version")?.Value;

            // Get session id from the response.
            _sessionId = xml.Element(AtriumController.XML_EL_CONNECTION).Attribute("session")?.Value;

            // Use MD5 and RC4 to get Session Key. Use Session Key to get loginUser and loginPass.
            _sessionKey   = MD5.Hash(_serialNo + _sessionId);
            var loginUser = RC4.Encrypt(_sessionKey, username);
            var loginPass = MD5.Hash(_sessionKey + password);

            // Post login information.
            int i = Math.Min(MaxAttempts, 50);
            while (i > 0) // Attempt to establish a session up to Max number of attempts.
            {
                var parameters = new Dictionary<String, String>
                {
                    { "sid", _sessionId },
                    { "cmd", "login" },
                    { "login_user", loginUser },
                    { "login_pass", loginPass }
                };
                var req = DoPOSTAsync(AtriumController.LOGIN_URL, parameters);
                req.Wait();
                xml = req.Result;

                // If "ok" then break the loop, otherwise, keep trying. (Sessions may not be available).
                if (CheckAnswer(xml, AtriumController.XML_EL_CONNECTION, throwException:false))
                {
                    break;
                }
                else
                {
                    Thread.Sleep(DelayBetweenAttempts * 1000);
                    i--;
                }
            }

            // Update session ID
            _sessionId = xml.Element(AtriumController.XML_EL_CONNECTION).Attribute("session")?.Value;

            // Get user info
            _userId = xml.Element(AtriumController.XML_EL_SDK_CFG).Attribute("user_id")?.Value;

            if(_userId == "-1")
            {
                throw new FailedToLoginException();
            }

            // Officially logged in.
        }

        /// <summary>
        /// Closes the AtriumController's connection. If this function is called, the connection cannot be reopened.
        /// </summary>
        /// <returns>Boolean indicating whether the Controller closed the connection or not.</returns>
        public bool Close()
        {
            var parameters = new Dictionary<String, String>
            {
                { "sid", _sessionId },
                { "cmd", "logout" }
            };

            var req = DoPOSTAsync(AtriumController.LOGIN_URL, parameters);
            req.Wait();
            var xml = req.Result;

            return CheckAnswer(xml, AtriumController.XML_EL_CONNECTION, throwException: false);
        }
        #endregion

        #region Public Class Methods
        /// <summary>
        /// Constructs an integer array of the 5 integer arguments. If an argument is not provided, then that Access Level is ignored.
        /// </summary>
        /// <param name="al1">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al2">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al3">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al4">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al5">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <returns></returns>
        public static int[] ACCESS_LEVELS(
            int al1=-1,
            int al2=-1,
            int al3=-1,
            int al4=-1,
            int al5=-1)
        {
            return new int[] { al1, al2, al3, al4, al5 };
        }

        /// <summary>
        /// Converts two 32-bit integers into one 26-bit integer. Specifically used for converting a Family Number and Member Number into
        /// it's 26-bit variant, where the Family Number is the upper 10 bits and the Member Number is the lower 16 bits.
        /// </summary>
        /// <param name="familyNumber">10-bit Int32 that corresponds to the Family Number on the card.</param>
        /// <param name="memberNumber">16-bit Int32 that corresponds to the Member Number that belongs to the related family.</param>
        /// <returns>26-bit integer directly converted from the family number bits concatenated with the member number bits.
        /// If family number or member number is less than 0, then -1 is returned.</returns>
        public static int To26BitCardNumber(int familyNumber, int memberNumber)
        {
            return (((familyNumber << 16) & 0x3FF0000 ) | (memberNumber & 0xFFFF)) & 0x03FFFFFF;
        }
        #endregion

        #region Private Class Methods
        // Pads a string to the left with a provided specific character to a provided total length.
        private static String PadLeft(String s, char c, int length)
        {
            while(s.Length < length)
            {
                s = c + s;
            }
            return s;
        }
        #endregion
    
        #region TEST - Validate Encryption
        /// <summary>
        /// Tests for Encryption given the Session ID, Serial Number, XML (or data to encrypt), username (optional), and password (optional).
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="sno"></param>
        /// <param name="xml"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public static void CHECK_ENCRYPTION(String sid, String sno, String xml, String username="admin", String password="admin")
        {
            var skey = MD5.Hash(sno + sid);
            var postEnc = RC4.Encrypt(skey, xml);
            var postChk = RC4.CheckSum(xml);
            var userEnc = RC4.Encrypt(skey, username);
            var passEnc = MD5.Hash(skey + password);

            Console.WriteLine($"Session ID={sid}\nSession Key={skey}\nPOST (enc)={postEnc}\nPOST (chk)={postChk}");
            Console.WriteLine($"Username={userEnc}, Password={passEnc}");
        }
        #endregion
    }
}
