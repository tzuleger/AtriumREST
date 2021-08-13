using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreeRiversTech.Zuleger.Atrium.API.Objects
{
    /// <summary>
    /// The base object containing attributes that all Atrium objects have.
    /// </summary>
    public abstract class BaseObject
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
        public void Update(BaseObject o)
        {
            foreach(var pi in this.GetType().GetProperties())
            {
                if (pi.GetValue(o) != null)
                {
                    pi.SetValue(this, pi.GetValue(o));
                }
            }
        }
    }
    /// <summary>
    /// User object that resembles the appearance of User in Atrium (and attributes)
    /// </summary>
    public sealed class User : BaseObject
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

    /// <summary>
    /// Card object that resembles the appearance of Card in Atrium (and attributes)
    /// </summary>
    public sealed class Card : BaseObject
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
