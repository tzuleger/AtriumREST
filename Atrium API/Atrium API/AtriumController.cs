﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.API
{
    /// <summary>
    /// Used to communicate with an Atrium Controller using Atrium SDK and HTTP.
    /// </summary>
    public class AtriumController
    {
        /// <summary>
        /// Maximum number of attempts to establish a session. (by default: 10. Maximum amount that can be set is 50.)
        /// </summary>
        public static int MaxAttempts { get; set; } = 10;
        /// <summary>
        /// The time in seconds to wait for the next attempt to log into the controller. (by default: 10 seconds)
        /// </summary>
        public static int DelayBetweenAttempts { get; set; } = 10;

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
        private const String ADD_USER = "./templates/add_user.xml";
        private const String ADD_CARD = "./templates/add_card.xml";
        private const String UPDATE_USER = "./templates/update_user.xml";
        private const String UPDATE_CARD = "./templates/update_card.xml";
        private const String READ_USER = "./templates/read_user.xml";
        private const String READ_CARD = "./templates/read_card.xml";

        // For Random Guid generation.
        private static Random _random = new Random();

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

        // Other
        /// <summary>
        /// Stores the Request XML data to the last POST request made.
        /// </summary>
        public String RequestText { get; set; } = "No request made.";

        /// <summary>
        /// Stores the Response String from the last POST request made.
        /// </summary>
        public String ResponseText { get; set; } = "No response received.";

        /// <summary>
        /// Serial number of the Atrium Controller that the AtriumConnection object is connected to.
        /// </summary>
        public String SerialNumber { get => _serialNo; }

        /// <summary>
        /// Product name of the Atrium Controller that the AtriumConnection object is connected to.
        /// </summary>
        public String ProductName { get => _product; }

        /// <summary>
        /// Product label of the Atrium Controller that the AtriumConnection object is connected to.
        /// </summary>
        public String ProductLabel { get => _label; }

        /// <summary>
        /// Product version of the Atrium Controller that the AtriumConnection object is connected to.
        /// </summary>
        public String ProductVersion { get => _version; }

        /// <summary>
        /// Generates a random Guid (128-bit ID) in the format of "########-####-####-####-############"
        /// </summary>
        public Guid GenerateGuid { get => AtriumController.GenerateRandomId(); }

        private int _transactionNum = 1;

        /// <summary>
        /// Constructs an integer array of the 5 integer arguments. If an argument is not provided, then that Access Level is ignored.
        /// </summary>
        /// <param name="al1">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al2">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al3">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al4">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <param name="al5">Integer that represents the Object ID for an Access Level. By default: -1 for no access level</param>
        /// <returns></returns>
        public static int[] ACCESS_LEVELS(int al1=-1, int al2=-1, int al3=-1, int al4=-1, int al5=-1)
        {
            return new int[] { al1, al2, al3, al4, al5 };
        }

        /// <summary>
        /// Creates an AtriumConnection object connected to the specified address under the specified username and password.
        /// </summary>
        /// <param name="username">Username to log in as.</param>
        /// <param name="password">Password to log into Atrium under specified username</param>
        /// <param name="address">Atrium Controller Address to connect to</param>
        public AtriumController(String username, String password, String address)
        {
            _client = new HttpClient();
            _address = address;

            // Fetch Session info to establish temporary Session Key
            var xml = DoGETAsync(AtriumController.LOGIN_URL).Result;
            CheckAnswer(xml, AtriumController.XML_EL_CONNECTION);

            // Get device information from the response.
            _serialNo = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("serial").Value;
            _product = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("product").Value;
            _label = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("mdl_label").Value;
            _version = xml.Element(AtriumController.XML_EL_DEVICE).Attribute("version").Value;

            // Get session id from the response.
            _sessionId = xml.Element(AtriumController.XML_EL_CONNECTION).Attribute("session").Value;

            // Use MD5 and RC4 to get Session Key. Use Session Key to get loginUser and loginPass.
            _sessionKey = AtriumController.ByteArrayToHexString(AtriumController.Md5(_serialNo + _sessionId));
            var loginUser = AtriumController.ByteArrayToHexString(AtriumController.Rc4(Encoding.ASCII.GetBytes(_sessionKey), Encoding.ASCII.GetBytes(username)));
            var loginPass = AtriumController.ByteArrayToHexString(AtriumController.Md5(_sessionKey + password));

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
                if (CheckAnswer(xml, AtriumController.XML_EL_CONNECTION, throwException:false)) // If "ok" then break the loop, otherwise, keep trying. (Sessions may not be available).
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
            _sessionId = xml.Element(AtriumController.XML_EL_CONNECTION).Attribute("session").Value;


            // Get user info
            _userId = xml.Element(AtriumController.XML_EL_SDK_CFG).Attribute("user_id").Value;

            if(_userId == "-1")
            {
                throw new FailedToLoginException();
            }

            // Officially logged in.
        }

        // Insert user into atrium controller associated with AtriumConnection object.
        /// <summary>
        /// Inserts a new User into the Atrium Controller with the provided information.
        /// </summary>
        /// <param name="firstName">First name of the user to be inserted.</param>
        /// <param name="lastName">Last name of the user to be inserted.</param>
        /// <param name="id">User GUID that is to be attached to the user.</param>
        /// <param name="actDate">Activation date of the User.</param>
        /// <param name="expDate">Expiration date of the User.</param>
        /// <param name="accessLevels">Array of 5 integers between 0-9,999 (inclusive) that represent the Object ID for each Access Level.</param>
        /// <returns>String object that is of real type int representing the Object ID as assigned by the Atrium Controller when user is inserted.</returns>
        public String InsertUser(String firstName, String lastName, Guid id, DateTime actDate, DateTime expDate, int[] accessLevels)
        {
            var content = FetchAndEncryptXML(
                AtriumController.ADD_USER,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@FirstName", firstName,
                "@LastName", lastName,
                "@UserID", id.ToString(),
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16),
                "@AccessLevel1", accessLevels[0] == -1 ? "" : accessLevels[0].ToString(),
                "@AccessLevel2", accessLevels[1] == -1 ? "" : accessLevels[1].ToString(),
                "@AccessLevel3", accessLevels[2] == -1 ? "" : accessLevels[2].ToString(),
                "@AccessLevel4", accessLevels[3] == -1 ? "" : accessLevels[3].ToString(),
                "@AccessLevel5", accessLevels[4] == -1 ? "" : accessLevels[4].ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;
            var insertedRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS) 
                                  select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(insertedRecords, AtriumController.XML_EL_DATA);
            return insertedRecords.First().Element(AtriumController.XML_EL_DATA).Attribute("id").Value;
        }

        /// <summary>
        /// Retrieves all Users on the Atrium Controller.
        /// </summary>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>Dictionary where Key is FirstName space LastName (e.g. "John Doe")
        /// and value is the full Dictionary of information on the respective User. 
        /// If multiple records exist with the same Key, then the first User is the only one that stays.</returns>
        public Dictionary<String, Dictionary<String, String>> GetAllUsers(int startIdx=1, int endIdx=5000)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_USER,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@min", startIdx.ToString(),
                "@max", endIdx.ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;

            var checkRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS)
                               select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(checkRecords, AtriumController.XML_EL_DATA);

            var listRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS).Elements(AtriumController.XML_EL_RECORD).Elements(AtriumController.XML_EL_DATA)
                          where e.Attribute("obj_status").Value == "used"
                          select new Dictionary<String, String>
                          {
                              { "userID", e.Attribute("guid2").Value },
                              { "objectID", e.Attribute("id").Value },
                              { "isValid", e.Attribute("valid").Value },
                              { "firstName", e.Attribute("label3").Value },
                              { "lastName", e.Attribute("label4").Value },
                              { "actDate", e.Attribute("utc_time22").Value },
                              { "expDate", e.Attribute("utc_time23").Value },
                              { "accessLevel1", e.Attribute("word24_0").Value },
                              { "accessLevel2", e.Attribute("word24_1").Value },
                              { "accessLevel3", e.Attribute("word24_2").Value },
                              { "accessLevel4", e.Attribute("word24_3").Value },
                              { "accessLevel5", e.Attribute("word24_4").Value }
                          };

            var records = new Dictionary<String, Dictionary<String, String>>();
            foreach(var record in listRecords)
            {
                if(records.ContainsKey(record["firstName"] + " " + record["lastName"]))
                {
                    records[record["firstName"] + " " + record["lastName"]]["Count"]
                        = (Int32.Parse(records[record["firstName"] + " " + record["lastName"]]["Count"])+1).ToString();
                }
                else
                {
                    record.Add("Count", "1");
                    records.Add(record["firstName"] + " " + record["lastName"], record);
                }
            }
            return records.Count() > 0 ? records : null;
        }

        /// <summary>
        /// Retrieves all users on the Atrium Controller referenced by First Name and Last Name being equal (case insensitive).
        /// </summary>
        /// <param name="firstName">First name of the User to search for. If null, then only searches by Last name. One must be provided.</param>
        /// <param name="lastName">Last name of the User to search for. If null, then only searches by First name. One must be provided.</param>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>List of Dictionaries holding Key Value Pairs of String to Strings where the Key is {userID, objectID, isValid, firstName, lastName, actDate, expDate, accessLevel{1-5}}.</returns>
        public List<Dictionary<String, String>> GetAllUsersByName(String firstName, String lastName, int startIdx=1, int endIdx=5000)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_USER,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@min", startIdx.ToString(),
                "@max", endIdx.ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;

            var checkRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS) 
                               select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(checkRecords, AtriumController.XML_EL_DATA);

            var records = from e in xml.Elements(AtriumController.XML_EL_RECORDS).Elements(AtriumController.XML_EL_RECORD).Elements(AtriumController.XML_EL_DATA)
                          where e.Attribute("obj_status").Value == "used" &&
                                e.Attribute("label3").Value.ToLower() == firstName.ToLower() &&
                                e.Attribute("label4").Value.ToLower() == lastName.ToLower()
                          select new Dictionary<String, String>
                          {
                              { "userID", e.Attribute("guid2").Value },
                              { "objectID", e.Attribute("id").Value },
                              { "isValid", e.Attribute("valid").Value },
                              { "firstName", e.Attribute("label3").Value },
                              { "lastName", e.Attribute("label4").Value },
                              { "actDate", e.Attribute("utc_time22").Value },
                              { "expDate", e.Attribute("utc_time23").Value },
                              { "accessLevel1", e.Attribute("word24_0").Value },
                              { "accessLevel2", e.Attribute("word24_1").Value },
                              { "accessLevel3", e.Attribute("word24_2").Value },
                              { "accessLevel4", e.Attribute("word24_3").Value },
                              { "accessLevel5", e.Attribute("word24_4").Value }
                          };

            return records.Count() > 0 ? records.ToList() : null;
        }

        /// <summary>
        /// Retrieves a user on the Atrium Controller referenced by the First Name and Last Name being equal. (case insensitive)
        /// If multiple users exist with the same name, then the first User that appears in Object ID order (ascending) is returned.
        /// If no user is found, then null is returned.
        /// </summary>
        /// <param name="firstName">First name of the User to search for. If null, then only searches by Last name. One must be provided.</param>
        /// <param name="lastName">Last name of the User to search for. If null, then only searches by First name. One must be provided.</param>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>Dictionary holding Key Value Pairs of String to Strings where the Key is {objectID, isValid, displayName, actDate, expDate}.</returns>
        public Dictionary<String, String> GetUserByName(String firstName, String lastName, int startIdx = 1, int endIdx = 5000)
        {
            return this.GetAllUsersByName(firstName, lastName, startIdx, endIdx)?.First();
        }

        /// <summary>
        /// Updates a User, specified by the Atrium Controller's defined Object ID, to a new First Name, Last Name, Activation Date, and Expiration Date.
        /// </summary>
        /// <param name="objectId">Object ID, as defined by the Atrium Controller, of what User to update.</param>
        /// <param name="firstName">New first name of the User to update.</param>
        /// <param name="lastName">New last name of the User to update.</param>
        /// <param name="actDate">New expiration date of the User to update.</param>
        /// <param name="expDate">New activation date of the User to update.</param>
        /// <param name="accessLevels">Array of 5 integers between 0-9,999 (inclusive) that represent the Object ID for each Access Level.</param>
        /// <returns>Boolean indicating whether the user was successfully updated or not.</returns>
        public bool UpdateUser(String objectId, String firstName, String lastName, DateTime actDate, DateTime expDate, int[] accessLevels)
        {
            var content = FetchAndEncryptXML(
                AtriumController.UPDATE_USER,
                "@tid", _transactionNum.ToString(),
                "@ObjectID", objectId,
                "@SerialNo", _serialNo,
                "@FirstName", firstName,
                "@LastName", lastName,
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16),
                "@AccessLevel1", accessLevels[0] == -1 ? "" : accessLevels[0].ToString(),
                "@AccessLevel2", accessLevels[1] == -1 ? "" : accessLevels[1].ToString(),
                "@AccessLevel3", accessLevels[2] == -1 ? "" : accessLevels[2].ToString(),
                "@AccessLevel4", accessLevels[3] == -1 ? "" : accessLevels[3].ToString(),
                "@AccessLevel5", accessLevels[4] == -1 ? "" : accessLevels[4].ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;
            var updatedRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS)
                                 select e.Element(AtriumController.XML_EL_RECORD);
            return CheckAllAnswers(updatedRecords, AtriumController.XML_EL_DATA, throwException: false);
        }

        // Insert card into atrium controller associated with AtriumConnection object.
        /// <summary>
        /// Inserts a new Card into Atrium under the provided information.
        /// </summary>
        /// <param name="displayName">Display Name that the card should be under.</param>
        /// <param name="cardId">Card GUID that the card should be under.</param>
        /// <param name="userId">User GUID that the card is attached to.</param>
        /// <param name="objectId">Atrium ObjectID that the Card should be attached to.</param>
        /// <param name="cardNum">Number of the card that is to be used.</param>
        /// <param name="actDate">Activation Date of the card.</param>
        /// <param name="expDate">Expiration Date of the card.</param>
        /// <returns>String object that is of real type int representing the Object ID as assigned by the Atrium Controller when card is inserted.</returns>
        public String InsertCard(String displayName, Guid cardId, Guid userId, String objectId, int cardNum, DateTime actDate, DateTime expDate)
        {
            var content = FetchAndEncryptXML(
                AtriumController.ADD_CARD,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@DisplayName", displayName,
                "@ObjectID", objectId,
                "@CardID", cardId.ToString(),
                "@UserID", userId.ToString(),
                "@MemberNumber", Convert.ToString(cardNum, 16),
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16)
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;
            var insertedRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS) 
                                  select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(insertedRecords, AtriumController.XML_EL_DATA);
            return insertedRecords?.First()?.Element(AtriumController.XML_EL_DATA)?.Attribute("id")?.Value;
        }

        /// <summary>
        /// Retrieves all Cards on the Atrium Controller.
        /// </summary>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>Dictionary where Key is userObjectID
        /// and value is the full Dictionary of information on the respective User. 
        /// If multiple records exist with the same Key, then the first Card is the only one that stays.</returns>
        public Dictionary<String, Dictionary<String, String>> GetAllCards(int startIdx = 1, int endIdx = 5000)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_CARD,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@min", startIdx.ToString(),
                "@max", endIdx.ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;

            var checkRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS)
                               select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(checkRecords, AtriumController.XML_EL_DATA);

            var listRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS).Elements(AtriumController.XML_EL_RECORD).Elements(AtriumController.XML_EL_DATA)
                          where e.Attribute("obj_status").Value == "used"
                          select new Dictionary<String, String>
                          {
                              { "objectID", e.Attribute("id").Value },
                              { "userObjectID", e.Attribute("dword4").Value },
                              { "isValid", e.Attribute("valid").Value },
                              { "displayName", e.Attribute("label3").Value },
                              { "cardNumber", e.Attribute("hexv5").Value },
                              { "actDate", e.Attribute("utc_time22").Value },
                              { "expDate", e.Attribute("utc_time23").Value },
                          };
            var records = new Dictionary<String, Dictionary<String, String>>();
            foreach(var record in listRecords)
            {
                if(records.ContainsKey(record["userObjectID"] + "-" + record["objectID"]))
                {
                    records[record["userObjectID"] + "-" + record["objectID"]]["Count"] 
                        = Int32.Parse(records[record["userObjectID"] + "-" + record["objectID"]]["Count"]+1).ToString();
                }
                else
                {
                    records.Add(record["userObjectID"], record);
                    record.Add("Count", "1");
                }
            }

            return records.Count() > 0 ? records : null;
        }

        /// <summary>
        /// Retrieves all cards on the Atrium Controller referenced by the UserID attached to the card. If no cards are found, then null is returned.
        /// </summary>
        /// <param name="userID">Guid of the User that attached cards are being searched for.</param>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>A list of Dictionaries holding Key Value Pairs of String to Strings where the Key is {objectID, isValid, displayName, actDate, expDate}.</returns>
        public List<Dictionary<String, String>> GetAllCardsByUserID(String userID, int startIdx = 1, int endIdx = 5000)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_CARD,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@min", startIdx.ToString(),
                "@max", endIdx.ToString()
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;

            var checkRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS)
                               select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(checkRecords, AtriumController.XML_EL_DATA);

            var records = from e in xml.Elements(AtriumController.XML_EL_RECORDS).Elements(AtriumController.XML_EL_RECORD).Elements(AtriumController.XML_EL_DATA)
                          where e.Attribute("obj_status").Value == "used" &&
                                e.Attribute("guid26").Value.ToLower() == userID
                          select new Dictionary<String, String>
                          {
                              { "objectID", e.Attribute("id").Value },
                              { "isValid", e.Attribute("valid").Value },
                              { "displayName", e.Attribute("label3").Value },
                              { "actDate", e.Attribute("utc_time22").Value },
                              { "expDate", e.Attribute("utc_time23").Value },
                          };

            return records.Count() > 0 ? records.ToList() : null;
        }

        /// <summary>
        /// Retrieves a card on the Atrium Controller referenced by the UserID attached to the card. 
        /// If a UserID has multiple cards attached, then the first Card that appears in Object ID order (ascending) is returned.
        /// If no card is found, then null is returned.
        /// </summary>
        /// <param name="userID">Guid of the User that attached cards are being searched for.</param>
        /// <param name="startIdx">Optional: Start index of the objects to search through as defined in the Atrium Controller. (by default: 1) 0 represents Admin.</param>
        /// <param name="endIdx">Optional: End index of the objects to search through as defined in the Atrium Controller. (by default: 5000)</param>
        /// <returns>Dictionary holding Key Value Pairs of String to Strings where the Key is {objectID, isValid, displayName, actDate, expDate}.</returns>
        public Dictionary<String, String> GetCardByUserID(String userID, int startIdx = 1, int endIdx = 5000)
        {
            return this.GetAllCardsByUserID(userID, startIdx, endIdx)?.First();
        }

        /// <summary>
        /// Updates a User, specified by the Atrium Controller's defined Object ID, to a new First Name, Last Name, Activation Date, and Expiration Date.
        /// </summary>
        /// <param name="objectId">Object ID, as defined by the Atrium Controller, of what User to update.</param>
        /// <param name="displayName">The new name to replace current Display Name on the card.</param>
        /// <param name="actDate">New expiration date of the User to update.</param>
        /// <param name="expDate">New activation date of the User to update.</param>
        /// <returns>Boolean indicating whether the card was successfully updated or not.</returns>
        public bool UpdateCard(String objectId, String displayName, DateTime actDate, DateTime expDate)
        {
            var content = FetchAndEncryptXML(
                AtriumController.UPDATE_CARD,
                "@tid", _transactionNum.ToString(),
                "@ObjectID", objectId,
                "@SerialNo", _serialNo,
                "@FirstName", displayName,
                "@ActivationDate", Convert.ToString(((DateTimeOffset)actDate).ToUnixTimeSeconds(), 16),
                "@ExpirationDate", Convert.ToString(((DateTimeOffset)expDate).ToUnixTimeSeconds(), 16)
            );
            var req = DoPOSTAsync(AtriumController.DATA_URL, content, setSessionCookie: true, encryptedExchange: true);
            req.Wait();
            var xml = req.Result;
            var updatedRecords = from e in xml.Elements(AtriumController.XML_EL_RECORDS)
                                 select e.Element(AtriumController.XML_EL_RECORD);
            return CheckAllAnswers(updatedRecords, AtriumController.XML_EL_DATA, throwException: false);
        }

        /// <summary>
        /// Checks an element in an XML Response String that it has an "ok" answer.
        /// </summary>
        /// <param name="xml">XElement that is to be checked for the "ok"</param>
        /// <param name="elementName">Subelement name to check inside of xml</param>
        /// <param name="attr">Optional: Attribute that the "ok" should be under. (by default: "err")</param>
        /// <param name="throwException">Optional: If true, throws an exception. Otherwise, returns a boolean indicating success or not. (by default: true)</param>
        /// <returns>Boolean value indicating that "ok" is in the response string.</returns>
        private bool CheckAnswer(XElement xml, XName elementName, String attr="err", bool throwException=true)
        {
            var e = xml.Element(elementName);
            if(attr == null)
            {
                throw new AttributeDoesNotExistException(xml, attr);
            }
            var res = e.Attribute(attr);
            if(res.Value != "ok")
            {
                if(throwException)
                {
                    throw new AnswerNotOkException(res.Value);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks a set of elements in an XML Response String that it has an "ok" answer.
        /// </summary>
        /// <param name="xmlElements">Enumerable XML elements that are typically of "REC" type.</param>
        /// <param name="elementName">Subelement name to check inside of xml</param>
        /// <param name="attr">Expected attribute that displays the response/answer message. (by default: "res")</param>
        /// <param name="throwException">Optional: If true, throws an exception. Otherwise, returns a boolean indicating success or not. (by default: true)</param>
        /// <returns>Boolean value indicating that "ok" is in the response string.</returns>
        private bool CheckAllAnswers(IEnumerable<XElement> xmlElements, XName elementName, String attr = "res", bool throwException = true)
        {
            foreach (var el in xmlElements)
            {
                if (!CheckAnswer(el, elementName, attr, throwException))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Performs a POST request to the specified Subdomain (under the Address provided from construction) with specific parameters to send.
        /// </summary>
        /// <param name="subdomain">The subdomain that the GET request is to be sent.</param>
        /// <param name="parameters">Dictionary of parameters that are to be sent with the POST request.</param>
        /// <param name="setSessionCookie">Optional: If true, sets a Cookie that stores the SessionID. (by default: false)</param>
        /// <param name="encryptedExchange">Optional: If true, expects the response to be encrypted and decrypts it upon reception. (by default: false)</param>
        /// <returns>An asynchronous task that returns an XElement of the response. (XML Response)</returns>
        private async Task<XElement> DoPOSTAsync(String subdomain, Dictionary<String, String> parameters, bool setSessionCookie = false, bool encryptedExchange = false)
        {
            var encodedContent = new FormUrlEncodedContent(parameters);
            if(setSessionCookie)
            {
                encodedContent.Headers.Add("Cookie", $"Session={_sessionId}-{AtriumController.PadLeft(_userId, '0', 2)}");
            }
            var response = await _client.PostAsync(_address + subdomain, encodedContent);
            var responseString = await response.Content.ReadAsStringAsync();
            if (encryptedExchange)
            {
                if(!responseString.Contains("post_enc="))
                {
                    throw new HttpRequestException(responseString);
                }
                var postEnc = responseString.Split("post_enc=")[1].Split("&")[0];
                responseString = Encoding.ASCII.GetString(AtriumController.Rc4(Encoding.ASCII.GetBytes(_sessionKey), HexStringToByteArray(postEnc)));
            }
            var xml = XElement.Parse(responseString);
            StringBuilder sb = new StringBuilder();
            foreach (var el in xml.Nodes())
            {
                sb.AppendLine(el.ToString());
            }
            _transactionNum++;

            ResponseText = xml.ToString();
            return xml;
        }

        // Performs a POST request to the connection associated with the AtriumConnection object
        /// <summary>
        /// Performs a GET request to the specified Subdomain (under the Address provided from construction)
        /// </summary>
        /// <param name="subdomain">The subdomain that the GET request is to be sent.</param>
        /// <returns>An asynchronous task that returns an XElement of the response. (XML Response)</returns>
        private async Task<XElement> DoGETAsync(String subdomain)
        {
            RequestText = "GET " + _address + subdomain;
            var response = await _client.GetAsync(_address + subdomain);
            var responseString = await response.Content.ReadAsStringAsync();
            var xml = XElement.Parse(responseString);
            _transactionNum++;

            ResponseText = xml.ToString();
            return xml;
        }

        /// <summary>
        /// Fetches an XML File and substitutes provided arguments and converts it to an HttpContent object.
        /// </summary>
        /// <param name="fileName">File name of the XML template to be used.</param>
        /// <param name="args">Variable arguments of String objects that are used to substitute arguments in the XML template.
        /// The size of the amount of Strings to pass should be divisible by two where every even argument is what should be replaced by the odd argument.</param>
        /// <returns>StringContent item that is to be used in the next POST request.</returns>
        private StringContent FetchXMLAsHttpContent(String fileName, params String[] args)
        {
            var fileContent = File.ReadAllText(fileName, Encoding.ASCII);
            for(int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i+1]);
            }

            RequestText = fileContent;

            return new StringContent(
                fileContent,
                Encoding.ASCII,
                "text/xml"
            );
        }

        /// <summary>
        /// Fetches an XML File and substitutes provided arguments, encrypts it under the RC4 Encryption Algorithm, then creates an encrypted request to be sent.
        /// </summary>
        /// <param name="fileName">File name of the XML template to use.</param>
        /// <param name="args">Variable arguments of String objects that are used to substitute arguments in the XML template.
        /// The size of the amount of Strings to pass should be divisible by two where every even argument is what should be replaced by the odd argument.</param>
        /// <returns>A Dictionary of parameters that are to be used in a parameterized POST request.</returns>
        private Dictionary<String, String> FetchAndEncryptXML(String fileName, params String[] args)
        {
            Dictionary<String, String> parameters = new Dictionary<String, String>();
            var fileContent = File.ReadAllText(fileName, Encoding.ASCII);
            for (int i = 0; i < args.Length; i += 2)
            {
                fileContent = fileContent.Replace(args[i], args[i + 1]);
            }

            RequestText = fileContent;

            var postEnc = AtriumController.ByteArrayToHexString(AtriumController.Rc4(Encoding.ASCII.GetBytes(_sessionKey), Encoding.ASCII.GetBytes(fileContent)));
            var postChk = AtriumController.CheckSum(fileContent);
            parameters.Add("sid", _sessionId);
            parameters.Add("post_enc", postEnc);
            parameters.Add("post_chk", postChk);

            return parameters;
        }

        /// <summary>
        /// Performs RC4 encryption/decryption on a specified byte array with a specified byte array as the key.
        /// </summary>
        /// <param name="pwd">Key that is used to encrypt/decrypt the data.</param>
        /// <param name="data">Data that is to be encrypted/decrypted</param>
        /// <returns>Byte array that represents the ciphertext of data after encryption/decryption.</returns>
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

        /// <summary>
        /// Performs an MD5 Hash algorithm on a String.
        /// </summary>
        /// <param name="text">String to perform the MD5 hash on.</param>
        /// <returns>A byte array that is the result of hashing text.</returns>
        private static byte[] Md5(String text)
        {
            byte[] hash;
            using (MD5 md5 = MD5.Create())
            {
                hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
            }
            return hash;
        }

        /// <summary>
        /// Counts the character codes of every character in given String to create a CheckSum.
        /// </summary>
        /// <param name="str">String to be checksummed.</param>
        /// <returns>A 16 bit Hexadecimal String that is built from the checksum of str.</returns>
        private static String CheckSum(String str)
        {
            int chk = 0;
            for (int i = 0; i < str.Length; i++)
            {
                chk += str[i];
            }
            var chkSumString = (chk & 0xFFFF).ToString("X");
            chkSumString = AtriumController.PadLeft(chkSumString, '0', 4);
            return chkSumString;
        }

        /// <summary>
        /// Pads a string with a provided specific character to a provided total length.
        /// </summary>
        /// <param name="s">String that is to be padded.</param>
        /// <param name="c">Character that is used when padding s.</param>
        /// <param name="length">Total length of what s should be.</param>
        /// <returns>String s padded with c to the desired length.</returns>
        private static String PadLeft(String s, char c, int length)
        {
            while(s.Length < length)
            {
                s = c + s;
            }
            return s;
        }

        /// <summary>
        /// Converts an array of bytes to a hexadecimal string.
        /// </summary>
        /// <param name="bytes">Byte array to be converted to hexadecimal string</param>
        /// <returns>A hexadecimal String as interpreted by the bytes.</returns>
        private static string ByteArrayToHexString(byte[] bytes)
        {
          return BitConverter.ToString(bytes).Replace("-","");
        }

        /// <summary>
        /// Converts a hexadecimal String to an array of bytes.
        /// </summary>
        /// <param name="hex">Hexadecimal string to convert into a byte array.</param>
        /// <returns>Byte array that was converted from the hexadecimal string.</returns>
        private static byte[] HexStringToByteArray(String hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static Guid GenerateRandomId()
        {
            byte[] buf = new byte[16];
            _random.NextBytes(buf);
            String result = String.Concat(buf.Select(x => x.ToString("X2")).ToArray());

            String s1 = result.Substring(0, 8);
            String s2 = result.Substring(7, 4);
            String s3 = result.Substring(11, 4);
            String s4 = result.Substring(15, 4);
            String s5 = result.Substring(19, 12);

            return Guid.Parse($"{s1}-{s2}-{s3}-{s4}-{s5}");
        }

        // Custom Exceptions

        /// <summary>
        /// Thrown when an Atrium Answer failed to interpret the XML request correctly.
        /// </summary>
        public class AnswerNotOkException : Exception
        {
            /// <summary>
            /// Set when AnswerNotOkException is thrown and is determined by what the Response message is.
            /// </summary>
            public override String Message { get => _message; }

            private String _message;

            /// <summary>
            /// Thrown when an Answer from a Response is not "ok".
            /// </summary>
            /// <param name="msg">Message to be shown in base Message.</param>
            public AnswerNotOkException(String msg)
            {
                if(msg == "err_alloc_fail")
                {
                    _message = "No sessions available. Try again later.";
                }
                else
                {
                    _message = $"HTTP Response did not result in err='ok'.\n\"{msg}\"";
                }
            }
        }

        /// <summary>
        /// Thrown when an HTTP Request fails, usually when some encryption went wrong.
        /// </summary>
        public class HttpRequestException : Exception
        {
            /// <summary>
            /// Thrown when an HTTP Request is made using Encryption but the Response did not contain expected Encryption variables.
            /// </summary>
            /// <param name="responseString"></param>
            public HttpRequestException(String responseString) : base("Request failed. " + responseString) { }
        }

        /// <summary>
        /// Thrown when the UserID returned from the second Atrium Controller Answer is -1.
        /// </summary>
        public class FailedToLoginException : Exception
        {
            /// <summary>
            /// Thrown when a login fails where it gets past all phases but the User ID returned is "-1".
            /// </summary>
            public FailedToLoginException() : base("Login failed.") { }
        }

        /// <summary>
        /// Thrown when an Attribute does not exist inside of an XML Element.
        /// </summary>
        public class AttributeDoesNotExistException : Exception
        {
            /// <summary>
            /// Returns the XML output that triggered the Exception.
            /// </summary>
            public String XmlString { get; private set; }
            /// <summary>
            /// Thrown when an Attribute does not exist inside of an XML request/response.
            /// </summary>
            /// <param name="xml">The XML documentation element that should contain the attribute.</param>
            /// <param name="attr">The attribute that was searched for in the XML.</param>
            public AttributeDoesNotExistException(XElement xml, String attr) : base($"Attribute \"{attr}\" does not exist.") { XmlString = xml.ToString(); }
        }
    }
}
