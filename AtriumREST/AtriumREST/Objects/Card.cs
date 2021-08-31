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
            /// Inherited: Type as defined in the SDK.
            /// </summary>
            public override String SdkType { get => "card"; }
            /// <summary>
            /// Display Name of the Card.
            /// </summary>
            [SdkDataType(Name = "label3")]
            public String DisplayName { get; set; }
            /// <summary>
            /// 26 bit Integer where upper 10 bits is the Family number and lower 16 bits is the card number.
            /// </summary>
            [SdkDataType(Name = "hexv5")]
            public String CardNumberLo { get; set; } = "000000";
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "hexv6")]
            public String CardNumberHi { get; set; } = "000000";
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "key7")]
            public String Code { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "byte8")]
            public String Format { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit9")]
            public int OptionCanArm { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit10")]
            public int OptionCanDisarm { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit11")]
            public int OptionCanAccess { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit13")]
            public int OptionInterlockOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit14")]
            public int OptionExtendDelay { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit15")]
            public int OptionAntiPassbackOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit16")]
            public int OptionAccessCount { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit17")]
            public int OptionTrace { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit18")]
            public int OptionGuardTour { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit19")]
            public int OptionCapacityOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit20")]
            public int OptionProgramming { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit21")]
            public int OptionLost { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit22")]
            public int OptionStolen { get; set; }
            /// <summary>
            /// Activation Date of the Card in HEX UTC.
            /// </summary>
            [SdkDataType(Name = "utc_time24", RelatedAttribute = "ActivationDate", RelatedType = "DateTime")]
            public String ActivationDateHexUTC { get => AtriumController.ToUTC(ActivationDate); }
            /// <summary>
            /// Activation Date of the Card.
            /// </summary>
            public DateTime ActivationDate { get; set; }
            /// <summary>
            /// Activation Date of the Card in HEX UTC.
            /// </summary>
            [SdkDataType(Name = "utc_time25", RelatedAttribute = "ExpirationDate", RelatedType = "DateTime")]
            public String ExpirationDateHexUTC { get => AtriumController.ToUTC(ExpirationDate); }
            /// <summary>
            /// Expiration Date of the Card.
            /// </summary>
            public DateTime ExpirationDate { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit27")]
            public int OptionLockdownActivation { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit28")]
            public int OptionLockdownDeactivation { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit29")]
            public int OptionLockdownOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit30")]
            public int OptionLockdownPartition { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit31")]
            public int OptionRequirePIN { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit32")]
            public int OptionUseCounter { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit33")]
            public int OptionDoubleSwipe { get; set; }
            public override String ToString() => $"{DisplayName} : {CardNumberLo}-{CardNumberHi}";
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
            String cardId,
            String userId,
            String objectId,
            String cardNum,
            DateTime actDate,
            DateTime expDate)
        {
            var content = FetchAndEncryptXML(
                AtriumController.ADD_CARD,
                "@tid", _transactionNum.ToString(),
                "@SerialNo", _serialNo,
                "@DisplayName", displayName,
                "@ObjectID", objectId,
                "@CardID", cardId,
                "@UserID", userId,
                "@MemberNumber", cardNum,
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
                card.EntityRelationshipGuid,
                card.EntityRelationshipId,
                card.CardNumberLo,
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
                                  ObjectGuid = e.Attribute("guid2").Value,
                                  IsValid = Int32.Parse(e.Attribute("valid")?.Value),
                                  DisplayName = e.Attribute("label3")?.Value,
                                  CardNumberLo = e.Attribute("hexv5")?.Value,
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
