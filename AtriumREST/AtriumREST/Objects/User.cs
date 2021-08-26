using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using ThreeRiversTech.Zuleger.Atrium.REST.Objects;

namespace ThreeRiversTech.Zuleger.Atrium.REST
{
    namespace Objects
    {
        /// <summary>
        /// User object that resembles the appearance of User in Atrium (and attributes)
        /// </summary>
        public sealed class User : AtriumObject
        {
            /// <summary>
            /// First Name of the User.
            /// </summary>
            public String FirstName { get; set; }
            /// <summary>
            /// Last Name of the User.
            /// </summary>
            public String LastName { get; set; }
            /// <summary>
            /// Activation Date of the User.
            /// </summary>
            public DateTime ActivationDate { get; set; }
            /// <summary>
            /// Expiration Date of the User.
            /// </summary>
            public DateTime ExpirationDate { get; set; }
            /// <summary>
            /// Integer array (expected size of at most 5) specifying the (up to) five Levels (specification of Object ID) this User has access to.
            /// </summary>
            public int[] AccessLevelObjectIds { get; set; }
            /// <summary>
            /// Overriden ToString() to return FirstName concatenated with LastName separated by a space. (e.g. "John Doe").
            /// </summary>
            /// <returns>String of User's FirstName concatenated with LastName separated with a space.</returns>
            public override String ToString() => $"{FirstName} {LastName}";
        }
    }
   
    public partial class AtriumController
    {

        /// <summary>
        /// Gets all Users that exist in the Atrium database in increments. Best used when the number of Users in the database is unknown.
        /// Larger increments increases speed through HTTP but could be worse if the number of records that actually exist are on the short end of the increment.
        /// (e.g. 1034 records exist with a 1000 increment would force two HTTP requests searching for 2000 total records.
        /// An increment of 1035 or better would be best here)
        /// By default 100.
        /// </summary>
        /// <param name="increment">Increment of users to grab.
        /// Larger numbers increases speed through HTTP but could be worse if the number of records that actually exist are on the short end of the increment.
        /// (e.g. 1034 records exist with a 1000 increment would force two HTTP requests searching for 2000 total records.
        /// An increment of 1035 or better would be best here)
        /// By default 100.</param>
        /// <returns>List of all Users that are in the Atrium Controller</returns>
        public List<User> GetAllUsers(int increment = 100)
        {
            int sIdx = 1;
            int eIdx = increment;

            List<User> users = GetAllUsersByIndex(sIdx, eIdx);
            while (users != null && users.Count >= eIdx)
            {
                sIdx += increment;
                eIdx += increment;
                users.AddRange(GetAllUsersByIndex(sIdx, eIdx));
            }

            return users;
        }

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
        public String InsertUser(
            String firstName,
            String lastName,
            Guid id,
            DateTime actDate,
            DateTime expDate,
            int[] accessLevels)
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
            var insertedRecords = from e
                                  in xml.Elements(AtriumController.XML_EL_RECORDS)
                                  select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(insertedRecords, AtriumController.XML_EL_DATA);

            return insertedRecords.First().Element(AtriumController.XML_EL_DATA).Attribute("id")?.Value;
        }

        /// <summary>
        /// Inserts a User C# object into the Atrium Controller's database.
        /// </summary>
        /// <param name="user">The User object to insert into the Atrium database.</param>
        /// <returns>String object that is of real type int representing the Object ID as assigned by the Atrium Controller when user is inserted.</returns>
        public String InsertUser(User user)
        {
            String objectId = InsertUser(
                user.FirstName,
                user.LastName,
                user.ObjectGuid,
                user.ActivationDate,
                user.ExpirationDate,
                user.AccessLevelObjectIds);

            user.ObjectId = objectId;
            return objectId;
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
        public bool UpdateUser(
            String objectId,
            String firstName,
            String lastName,
            DateTime actDate,
            DateTime expDate,
            int[] accessLevels)
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
            var updatedRecords = from e
                                 in xml.Elements(AtriumController.XML_EL_RECORDS)
                                 select e.Element(AtriumController.XML_EL_RECORD);
            return CheckAllAnswers(updatedRecords, AtriumController.XML_EL_DATA, throwException: false);
        }

        /// <summary>
        /// Updates a User based off an already existing User C# object.
        /// </summary>
        /// <param name="user">User object that is to be updated. (Object ID of "user" must already exist in the Controller as a User)</param>
        /// <returns>Boolean indicating whether the user was successfully updated or not.</returns>
        public bool UpdateUser(User user)
        {
            return UpdateUser(
                user.ObjectId,
                user.FirstName,
                user.LastName,
                user.ActivationDate,
                user.ExpirationDate,
                user.AccessLevelObjectIds);
        }

        // Gets all Users in the Atrium Controller specified by the object indices provided
        private List<User> GetAllUsersByIndex(int startIdx, int endIdx)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_USER,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@min", startIdx.ToString(),
                "@max", endIdx.ToString()
            );

            var req = DoPOSTAsync(
                AtriumController.DATA_URL,
                content,
                setSessionCookie: true,
                encryptedExchange: true);
            req.Wait();
            var xml = req.Result;

            var checkRecords = from e
                               in xml.Elements(AtriumController.XML_EL_RECORDS)
                               select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(checkRecords, AtriumController.XML_EL_DATA);

            var listRecords = from e
                              in xml.Elements(AtriumController.XML_EL_RECORDS)
                                           .Elements(AtriumController.XML_EL_RECORD)
                                           .Elements(AtriumController.XML_EL_DATA)
                              where e.Attribute("obj_status")?.Value == "used"
                              select new User
                              {
                                  Status = e.Attribute("obj_status")?.Value,
                                  ObjectGuid = Guid.Parse(e.Attribute("guid2")?.Value),
                                  ObjectId = e.Attribute("id")?.Value,
                                  IsValid = e.Attribute("valid")?.Value == "1",
                                  FirstName = e.Attribute("label3")?.Value,
                                  LastName = e.Attribute("label4")?.Value,
                                  ActivationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                                                    .AddSeconds(Convert.ToInt32(e.Attribute("utc_time22")?.Value, 16))
                                                    .ToLocalTime(),
                                  ExpirationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                                                    .AddSeconds(Convert.ToInt32(e.Attribute("utc_time23")?.Value, 16))
                                                    .ToLocalTime(),
                                  AccessLevelObjectIds = new int[]
                                  {
                                      Int32.Parse(e.Attribute("word24_0")?.Value),
                                      Int32.Parse(e.Attribute("word24_1")?.Value),
                                      Int32.Parse(e.Attribute("word24_2")?.Value),
                                      Int32.Parse(e.Attribute("word24_3")?.Value),
                                      Int32.Parse(e.Attribute("word24_4")?.Value)
                                  }
                              };

            return listRecords.ToList();
        }
    }
}
