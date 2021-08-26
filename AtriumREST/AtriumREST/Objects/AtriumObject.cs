using System;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Objects
{
    /// <summary>
    /// The base object containing attributes that all Atrium objects have.
    /// </summary>
    public abstract class AtriumObject
    {
        /// <summary>
        /// 
        /// </summary>
        public String Status { get; set; }
        /// <summary>
        /// Object ID of where the Object is located in the Atrium Controller.
        /// </summary>
        public String ObjectId { get; set; } = null;
        /// <summary>
        /// Object's GUID.
        /// </summary>
        public Guid ObjectGuid { get; set; }
        /// <summary>
        /// Related Entity's Object ID.
        /// </summary>
        public String EntityRelationshipId { get; set; } = null;
        /// <summary>
        /// Related Entity's GUID
        /// </summary>
        public Guid? EntityRelationshipGuid { get; set; } = null;
        /// <summary>
        /// Boolean specifying if the Object is valid in the Controller.
        /// </summary>
        public bool IsValid { get; set; } = true;
        /// <summary>
        /// Update this object's attributes to be the same as o's attributes.
        /// </summary>
        /// <param name="o"></param>
        public void Update(AtriumObject o)
        {
            foreach (var pi in this.GetType().GetProperties())
            {
                if (pi.GetValue(o) != null)
                {
                    pi.SetValue(this, pi.GetValue(o));
                }
            }
        }
    }
}
