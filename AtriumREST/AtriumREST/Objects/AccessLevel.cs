using System;


namespace ThreeRiversTech.Zuleger.Atrium.REST.Objects
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AccessLevel : AtriumObject
    {
        /// <summary>
        /// 
        /// </summary>
        public override String SdkType { get => "access_lvl"; }
        /// <summary>
        /// 
        /// </summary>
        [SdkDataType(Name = "label3")]
        public String DisplayName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [SdkDataType(Name = "word4")]
        public int ScheduleIndex { get; set; }
    }
}
