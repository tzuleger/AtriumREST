using System;

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
            [SdkDataType(Name = "utc_time24", RelatedAttribute = "ActivationDate")]
            public String ActivationDateHexUTC { get => AtriumController.ToUTC(ActivationDate); }
            /// <summary>
            /// Activation Date of the Card.
            /// </summary>
            public DateTime ActivationDate { get; set; }
            /// <summary>
            /// Activation Date of the Card in HEX UTC.
            /// </summary>
            [SdkDataType(Name = "utc_time25", RelatedAttribute = "ExpirationDate")]
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
}
