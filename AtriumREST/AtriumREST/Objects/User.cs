using System;

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
            /// Inherited: Type as defined in the SDK.
            /// </summary>
            public override String SdkType { get => "user"; }
            /// <summary>
            /// First Name of the User.
            /// </summary>
            [SdkDataType(Name = "label3")]
            public String FirstName { get; set; }
            /// <summary>
            /// Last Name of the User.
            /// </summary>
            [SdkDataType(Name = "label4")]
            public String LastName { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit5")]
            public int OptionDualArming { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit6")]
            public int OptionAccess { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit7")]
            public int OptionDuress { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit8")]
            public int OptionInterlockOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit9")]
            public int OptionExtendedDelay { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit10")]
            public int OptionAntiPassbackOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit11")]
            public int OptionAccessCount { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit12")]
            public int OptionTrace { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit13")]
            public int OptionGuardTour { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit14")]
            public int OptionCapacityOverride { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit15")]
            public int OptionProgramming { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit16")]
            public int OptionUserCounter { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit18")]
            public int OptionCanDisarm { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit19")]
            public int OptionVisitor { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "bit20")]
            public int OptionAsset { get; set; }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word21")]
            public String SupervisorLevelID { get; set; }

            /// <summary>
            /// Activation Date of the User in HEX UTC.
            /// </summary>
            [SdkDataType(Name = "utc_time22", RelatedAttribute = "ActivationDate")]
            public String ActivationDateHexUTC { get => AtriumController.ToUTC(ActivationDate); }
            /// <summary>
            /// Activation Date of the User.
            /// </summary>
            public DateTime ActivationDate { get; set; }
            /// <summary>
            /// Activation Date of the User in HEX UTC.
            /// </summary>
            [SdkDataType(Name = "utc_time23", RelatedAttribute = "ExpirationDate")]
            public String ExpirationDateHexUTC { get => AtriumController.ToUTC(ExpirationDate); }
            /// <summary>
            /// Expiration Date of the User.
            /// </summary>
            public DateTime ExpirationDate { get; set; }
            /// <summary>
            /// Integer array (expected size of at most 5) specifying the (up to) five Levels (specification of Object ID) this User has access to.
            /// </summary>
            public int[] AccessLevelObjectIds { get; set; } = null;
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word24_0")]
            public String AccessLevel1 { get => AccessLevelObjectIds?[0].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word24_1")]
            public String AccessLevel2 { get => AccessLevelObjectIds?[1].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word24_2")]
            public String AccessLevel3 { get => AccessLevelObjectIds?[2].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word24_3")]
            public String AccessLevel4 { get => AccessLevelObjectIds?[3].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word24_4")]
            public String AccessLevel5 { get => AccessLevelObjectIds?[4].ToString(); }
            /// <summary>
            /// Language interpreted as. 0=English, 1=French, 2=Spanish, 3=Chinese.
            /// </summary>
            [SdkDataType(Name = "word25")]
            public int Language { get; set; }
            /// <summary>
            /// Integer array (expected size of at most 5) specifying the (up to) five Levels (specification of Object ID) this User has access to.
            /// </summary>
            public int[] FloorLevelObjectIds { get; set; } = null;
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word25_0")]
            public String FloorLevel1 { get => FloorLevelObjectIds?[0].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word25_1")]
            public String FloorLevel2 { get => FloorLevelObjectIds?[1].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word25_2")]
            public String FloorLevel3 { get => FloorLevelObjectIds?[2].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word25_3")]
            public String FloorLevel4 { get => FloorLevelObjectIds?[3].ToString(); }
            /// <summary>
            ///
            /// </summary>
            [SdkDataType(Name = "word25_4")]
            public String FloorLevel5 { get => FloorLevelObjectIds?[4].ToString(); }
            /// <summary>
            /// Overriden ToString() to return FirstName concatenated with LastName separated by a space. (e.g. "John Doe").
            /// </summary>
            /// <returns>String of User's FirstName concatenated with LastName separated with a space.</returns>
            public override String ToString() => $"{FirstName} {LastName}";
        }
    }
}
