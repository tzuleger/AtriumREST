using System;
using System.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Objects
{
    /// <summary>
    /// Custom attribute that assists with mapping Object Attributes to XML SDK Datatypes.
    /// </summary>
    public class SdkDataType : Attribute
    {
        /// <summary>
        /// Name of the SDK DatatType.
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// The related Attribute that is to be used
        /// </summary>
        public String RelatedAttribute { get; set; } = null;
    }

    /// <summary>
    /// The base object containing attributes that all Atrium objects have.
    /// </summary>
    public abstract class AtriumObject
    {
        /// <summary>
        /// Type as defined inside of the SDK.
        /// </summary>
        public abstract String SdkType { get; }
        /// <summary>
        /// Status of the Object in the Controller.
        /// </summary>
        [SdkDataType(Name = "obj_status")]
        public String Status { get; set; } = "used";
        /// <summary>
        /// Object ID of where the Object is located in the Atrium Controller.
        /// </summary>
        [SdkDataType(Name = "id")]
        public String ObjectId { get; set; } = null;
        /// <summary>
        /// Boolean specifying if the Object is valid in the Controller.
        /// </summary>
        [SdkDataType(Name = "valid")]
        public int IsValid { get; set; } = 1;
        /// <summary>
        /// Read Only attribute
        /// </summary>
        [SdkDataType(Name = "ro")]
        public int IsReadOnly { get; set; }
        /// <summary>
        /// Object cannot be deleted (except by an InstalleR)
        /// </summary>
        [SdkDataType(Name = "protect")]
        public int CannotBeDeleted { get; set; }
        /// <summary>
        /// Object's GUID.
        /// </summary>
        [SdkDataType(Name = "guid2")]
        public String ObjectGuid { get => _objectGuid; set => _objectGuid = value?.ToUpper().Replace("-", ""); }
        private String _objectGuid = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
        /// <summary>
        /// Related Entity's Object ID.
        /// </summary>
        [SdkDataType(Name = "dword4")]
        public String EntityRelationshipId { get; set; }
        /// <summary>
        /// Related Entity's GUID
        /// </summary>
        [SdkDataType(Name = "guid26")]
        public String EntityRelationshipGuid { get; set; }

        /// <summary>
        /// Returns all Properties separated by a newline character and in format of "PropertyName: Value"
        /// </summary>
        /// <param name="beautify">Specifies whether to beautify the JSON output or not. By default: true.</param>
        /// <returns>String that contains all properties in JSON format.</returns>
        public String Jsonify(bool beautify=true)
        {
            String s = "{" + (beautify ? "\n" : " ");
            int i = 0;
            var props = this.GetType().GetProperties();
            foreach (var pi in props)
            {
                s += (beautify ? "\t" : "") 
                    + $"\"{pi.Name}\": \"{pi.GetValue(this, null)}\"" 
                    + (i++ < props.Length-1 ? ", " : "") 
                    + (beautify ? "\n" : "");
            }
            return s + (beautify ? " }" : "\n}");
        }

        /// <summary>
        /// Same as "AllProperties()" but is the default AtriumObject ToString method.
        /// </summary>
        /// <returns>String that contains all properties in JSON format.</returns>
        public override String ToString() => Jsonify();
    }
}
