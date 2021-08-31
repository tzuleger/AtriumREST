using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Xml.Linq;

using ThreeRiversTech.Zuleger.Atrium.REST.Security;
using ThreeRiversTech.Zuleger.Atrium.REST.Objects;
using ThreeRiversTech.Zuleger.Atrium.REST.Exceptions;

namespace ThreeRiversTech.Zuleger.Atrium.REST
{
    /// <summary>
    /// Used to communicate with an Atrium Controller using Atrium SDK and HTTP.
    /// </summary>
    public partial class AtriumController
    {
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
            _transactionNum = 0;
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
        private static XName XML_EL_SDK = AtriumController.XmlNameSpace + "SDK";
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
        public static String To26BitCardNumber(int familyNumber, int memberNumber)
        {
            return Convert.ToString((((familyNumber << 16) & 0x3FF0000 ) | (memberNumber & 0xFFFF)) & 0x03FFFFFF, 16).ToUpper();
        }

        /// <summary>
        /// Converts a DateTime to its UTC counterpart in Hexadecimal notation as a String
        /// </summary>
        /// <param name="dt">DateTime to convert to Hexadecimal UTC</param>
        /// <returns>String representing UTC time of the datetime passed in. Is in Hexadecimal notation</returns>
        public static String ToUTC(DateTime dt)
        {
            return Convert.ToString(((DateTimeOffset)dt).ToUnixTimeSeconds(), 16);
        }

        /// <summary>
        /// Converts a UTC Hexadecimal String to its DateTime counterpart.
        /// </summary>
        /// <param name="utc">UTC String that is to be converted to a DateTime.</param>
        /// <returns>DateTime representing local time of converted UTC string. If the String cannot be parsed, then default is returned.</returns>
        public static DateTime? FromUTC(String utc)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(Convert.ToInt32(utc, 16)).ToLocalTime();
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
        public String GenerateGuid { get => Guid.NewGuid().ToString().Replace("-", "").ToUpper(); }
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

        #region Public Instance Methods
        /// <summary>
        /// Inserts a single Atrium Object into the controller.
        /// </summary>
        /// <param name="o">Atrium Object to be inserted.</param>
        public String Insert(AtriumObject o)
        {
            var type = o.SdkType;
            var rec = new XElement(XML_EL_RECORD);
            rec.SetAttributeValue("trans_id", _transactionNum);
            rec.SetAttributeValue("cmd", "add");
            rec.SetAttributeValue("sernum", _serialNo);
            rec.SetAttributeValue("type", type);
            rec.SetAttributeValue("rec", "cfg");

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    XML_EL_SDK,
                    new XElement(XML_EL_RECORDS,
                        rec
                    )
                )
            );

            rec.Add(GetDataElement(o));

            var xml = $"{doc.Declaration}\n{doc.ToString().Replace("\"", "'")}";
            var postEncParams = FetchAndEncryptXML(xml);
            var req = DoPOSTAsync(DATA_URL, postEncParams, setSessionCookie:true, encryptedExchange:true);
            req.Wait();
            var res = req.Result;

            var insertedRecords = from e
                                  in res.Elements(AtriumController.XML_EL_RECORDS)
                                  select e.Element(AtriumController.XML_EL_RECORD);

            CheckAllAnswers(insertedRecords, XML_EL_DATA);

            var first = insertedRecords?.First()?.Element(AtriumController.XML_EL_DATA);

            o.ObjectId = first?.Attribute("id")?.Value;
            o.ObjectGuid = first?.Attribute("guid2")?.Value;

            return o.ObjectId;
        }

        /// <summary>
        /// Updates a single Atrium Object into the controller. If the object does not exist (defined by GUID being equal) then no update occurs.
        /// </summary>
        /// <param name="o">Object to be updated in the controller.</param>
        /// <returns>True if the object was successfully updated. False otherwise.</returns>
        public bool Update(AtriumObject o)
        {
            var type = o.SdkType;
            var rec = new XElement(XML_EL_RECORD);
            rec.SetAttributeValue("trans_id", _transactionNum);
            rec.SetAttributeValue("cmd", "write");
            rec.SetAttributeValue("sernum", _serialNo);
            rec.SetAttributeValue("type", type);
            rec.SetAttributeValue("rec", "cfg");

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    XML_EL_SDK,
                    new XElement(XML_EL_RECORDS,
                        rec
                    )
                )
            );

            rec.Add(GetDataElement(o));

            if (rec.Element(XML_EL_DATA).Attribute("id")?.Value != null)
            {
                var attr = rec.Element(XML_EL_DATA).Attribute("id");
                if(attr != null)
                {
                    rec.SetAttributeValue("id", attr.Value);
                    attr.Remove();
                }
            }

            var xml = $"{doc.Declaration}\n{doc.ToString().Replace("\"", "'")}";
            
            var postEncParams = FetchAndEncryptXML(xml);
            var req = DoPOSTAsync(DATA_URL, postEncParams, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var res = req.Result;

            var updatedRecords = from e
                                  in res.Elements(AtriumController.XML_EL_RECORDS)
                                  select e.Element(AtriumController.XML_EL_RECORD);

            var first = updatedRecords?.First()?.Element(AtriumController.XML_EL_DATA);

            o.ObjectId = first?.Attribute("id")?.Value;
            o.ObjectGuid = first?.Attribute("guid2")?.Value;

            return CheckAllAnswers(updatedRecords, XML_EL_DATA, throwException: false);
        }

        /// <summary>
        /// Grabs all records between the specified minimum and maximum object IDs in the controller.
        /// </summary>
        /// <typeparam name="T">AtriumObject child that is to be grabbed from the Controller</typeparam>
        /// <param name="startIndex">Start index of what objects to grab from in the Controller.</param>
        /// <param name="endIndex">End index of what objects to grab from in the Controller.</param>
        /// <returns>List of the specified AtriumObject objects that were grabbed from the Controller.</returns>
        public List<T> GetAll<T>(int startIndex=0, int endIndex=100) where T : AtriumObject, new()
        {
            var rec = new XElement(XML_EL_RECORD);
            rec.SetAttributeValue("trans_id", _transactionNum);
            rec.SetAttributeValue("cmd", "read");
            rec.SetAttributeValue("sernum", _serialNo);
            rec.SetAttributeValue("type", new T().SdkType);
            rec.SetAttributeValue("id_min", startIndex);
            rec.SetAttributeValue("id_max", endIndex);
            rec.SetAttributeValue("rec", "cfg");

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(
                    XML_EL_SDK,
                    new XElement(XML_EL_RECORDS,
                        rec
                    )
                )
            );

            var data = new XElement(XML_EL_DATA);
            var parentProperties = typeof(AtriumObject).GetProperties();
            foreach (var pi in parentProperties)
            {
                SdkDataType sdkAttr = (SdkDataType)Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                if(String.IsNullOrWhiteSpace(sdkAttr?.Name))
                {
                    continue;
                }
                data.SetAttributeValue(sdkAttr.Name, "");
            }
            foreach (var pi in typeof(T).GetProperties())
            {
                SdkDataType sdkAttr = (SdkDataType)Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                if(parentProperties.Contains(pi) || String.IsNullOrWhiteSpace(sdkAttr?.Name))
                {
                    continue;
                }
                data.SetAttributeValue(sdkAttr.Name, "");
            }


            var xml = $"{doc.Declaration}\n{doc.ToString().Replace("\"", "'")}";
            var postEncParams = FetchAndEncryptXML(xml);
            var req = DoPOSTAsync(DATA_URL, postEncParams, setSessionCookie:true, encryptedExchange: true);
            req.Wait();
            var res = req.Result;
            var records = from e in res.Elements(AtriumController.XML_EL_RECORDS)
                          select e.Element(AtriumController.XML_EL_RECORD);

            CheckAllAnswers(records, null, throwException: false);

            List<T> ts = new List<T>();
            foreach(var r in records)
            {
                foreach(var record in r.Elements().Where(theRecord => theRecord.Attribute("obj_status")?.Value == "used"))
                {
                    T t = new T();
                    var props = typeof(T).GetProperties();
                    foreach (var pi in props)
                    {
                        SdkDataType sdkAttr = (SdkDataType)Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                        if(String.IsNullOrWhiteSpace(sdkAttr?.Name))
                        {
                            continue;
                        }

                        if (pi.CanWrite)
                        {
                            if (pi.PropertyType == typeof(Int32) && Int32.TryParse(record.Attribute(sdkAttr.Name)?.Value, out _))
                            {
                                pi.SetValue(t, Int32.Parse(record.Attribute(sdkAttr.Name)?.Value));
                            }
                            else
                            {
                                pi.SetValue(t, record.Attribute(sdkAttr.Name)?.Value);
                            }
                        }
                        else if(sdkAttr?.RelatedAttribute != null)
                        {
                            // If the PI does not have Write Access, it may be a referential attribute.
                            var relatedPi = props.Where(prop => prop.Name == sdkAttr.RelatedAttribute).First();
                            if(sdkAttr.RelatedType == "DateTime")
                            {
                                relatedPi.SetValue(t, FromUTC(record.Attribute(sdkAttr.Name)?.Value));
                            }
                        }
                    }
                    ts.Add(t);
                }
            }

            return ts;
        }
        #endregion

        #region Private Instance Methods
        // Constructs a Data XML Element based on the AtriumObject passed in.
        private XElement GetDataElement(AtriumObject o)
        {
            var data = new XElement(XML_EL_DATA);
            var parentProperties = o.GetType().BaseType.GetProperties();
            foreach (var pi in parentProperties)
            {
                SdkDataType sdkAttr = (SdkDataType)Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                if (!String.IsNullOrEmpty(sdkAttr?.Name))
                {
                    if (String.IsNullOrEmpty(pi.GetValue(o)?.ToString()))
                    {
                        data.SetAttributeValue(sdkAttr.Name, "");
                    }
                    else
                    {
                        data.SetAttributeValue(sdkAttr.Name, pi.GetValue(o));
                    }
                }
            }
            foreach (var pi in o.GetType().GetProperties())
            {
                if(parentProperties.Contains(pi))
                {
                    continue;
                }
                SdkDataType sdkAttr = (SdkDataType) Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                if (!String.IsNullOrEmpty(sdkAttr?.Name))
                {
                    if (!String.IsNullOrEmpty(pi.GetValue(o)?.ToString()))
                    {
                        data.SetAttributeValue(sdkAttr.Name, pi.GetValue(o));
                    }
                }
            }

            return data;
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
