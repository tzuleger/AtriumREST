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
        /// Card object that resembles the appearance of Card in Atrium (and attributes)
        /// </summary>
        public sealed class Card : AtriumObject
        {
            /// <summary>
            /// Display Name of the Card.
            /// </summary>
            public String DisplayName { get; set; }
            /// <summary>
            /// 26 bit Integer where upper 10 bits is the Family number and lower 16 bits is the card number.
            /// </summary>
            public int CardNumber { get; set; } = -1;
            /// <summary>
            /// Activation Date of the Card.
            /// </summary>
            public DateTime ActivationDate { get; set; }
            /// <summary>
            /// Expiration Date of the Card.
            /// </summary>
            public DateTime ExpirationDate { get; set; }
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
        /// <returns>List of all Cards that are in the Atrium Controller</returns>
        public List<Card> GetAllCards(int increment = 100)
        {
            int sIdx = 0;
            int eIdx = increment;

            List<Card> cards = GetAllCardsByIndex(sIdx, eIdx);
            while (cards != null && cards.Count >= eIdx)
            {
                sIdx += increment;
                eIdx += increment;
                cards.AddRange(GetAllCardsByIndex(sIdx, eIdx));
            }

            return cards;
        }

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
        public String InsertCard(
            String displayName,
            Guid cardId,
            Guid userId,
            String objectId,
            int cardNum,
            DateTime actDate,
            DateTime expDate)
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
            var insertedRecords = from e
                                  in xml.Elements(AtriumController.XML_EL_RECORDS)
                                  select e.Element(AtriumController.XML_EL_RECORD);
            CheckAllAnswers(insertedRecords, AtriumController.XML_EL_DATA);
            return insertedRecords?.First()?.Element(AtriumController.XML_EL_DATA)?.Attribute("id")?.Value;
        }

        /// <summary>
        /// Inserts a Card C# object into the Atrium Controller's database.
        /// </summary>
        /// <param name="card">The Card object to be inserted into the Atrium Controller's database.</param>
        /// <returns>String object that is of real type int representing the Object ID as assigned by the Atrium Controller when user is inserted.</returns>
        public String InsertCard(Card card)
        {
            String objectId = InsertCard(
                card.DisplayName,
                card.ObjectGuid,
                card.EntityRelationshipGuid.Value,
                card.EntityRelationshipId,
                card.CardNumber,
                card.ActivationDate,
                card.ExpirationDate);

            card.ObjectId = objectId;
            return objectId;
        }

        /// <summary>
        /// Updates a User, specified by the Atrium Controller's defined Object ID, to a new First Name, Last Name, Activation Date, and Expiration Date.
        /// </summary>
        /// <param name="objectId">Object ID, as defined by the Atrium Controller, of what User to update.</param>
        /// <param name="displayName">The new name to replace current Display Name on the card.</param>
        /// <param name="actDate">New expiration date of the User to update.</param>
        /// <param name="expDate">New activation date of the User to update.</param>
        /// <returns>Boolean indicating whether the card was successfully updated or not.</returns>
        public bool UpdateCard(
            String objectId,
            String displayName,
            DateTime actDate,
            DateTime expDate)
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
        /// Updates a User given a C# Card Object. Object ID must already exist in the database.
        /// </summary>
        /// <param name="card">Card object to update</param>
        /// <returns>Boolean indicating whether the card was updated successfully or not.</returns>
        public bool UpdateCard(Card card)
        {
            return UpdateCard(
                card.ObjectId,
                card.DisplayName,
                card.ActivationDate,
                card.ExpirationDate);
        }


        // Gets all Cards in the Atrium Controller specified by the object indices provided
        private List<Card> GetAllCardsByIndex(int startIdx, int endIdx)
        {
            var content = FetchAndEncryptXML(AtriumController.READ_CARD,
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
                                 || e.Attribute("obj_status")?.Value == "deleted"
                              select new Card
                              {
                                  Status = e.Attribute("obj_status")?.Value,
                                  ObjectId = e.Attribute("id")?.Value,
                                  ObjectGuid = Guid.Parse(e.Attribute("guid2")?.Value),
                                  IsValid = e.Attribute("valid")?.Value == "1",
                                  DisplayName = e.Attribute("label3")?.Value,
                                  CardNumber = Convert.ToInt32(e.Attribute("hexv5")?.Value, 16),
                                  // Dates need to be converted from Unix Timestamps to Local Times.
                                  ActivationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                                                    .AddSeconds(Convert.ToInt32(e.Attribute("utc_time22")?.Value, 16))
                                                    .ToLocalTime(),
                                  ExpirationDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                                                    .AddSeconds(Convert.ToInt32(e.Attribute("utc_time23")?.Value, 16))
                                                    .ToLocalTime(),
                              };

            return listRecords.ToList();
        }

    }
}
