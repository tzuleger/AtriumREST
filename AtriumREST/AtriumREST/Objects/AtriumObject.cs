using System;
using System.Linq;

namespace ThreeRiversTech.Zuleger.Atrium.REST.Objects
{
    /// <summary>
    /// 
    /// </summary>
    public class SdkDataType : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public String RelatedAttribute { get; set; } = null;
        /// <summary>
        /// 
        /// </summary>
        public String RelatedType { get; set; } = null;
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
        /// 
        /// </summary>
        [SdkDataType(Name = "ro")]
        public int IsReadOnly { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [SdkDataType(Name = "protect")]
        public int CannotBeDeleted { get; set; }
        /// <summary>
        /// Object's GUID.
        /// </summary>
        [SdkDataType(Name = "guid2")]
        public String ObjectGuid { get; set; } = Guid.NewGuid().ToString();
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
        /// Update this object's attributes to be the same as o's attributes.
        /// </summary>
        /// <param name="o">The Atrium Object that its attributes are to be copied to this object.</param>
        /// <param name="overwrite">Boolean specifying whether overwriting should occur on parameters on "this" object where values already exist. By default: false./param>
        /// <param name="overwriteIds"/>Boolean specifying whether overwriting should occur on Id type parameters. By default: true.</param>
        /// <returns>"this" after copying has finished.</returns>
        public AtriumObject Copy(AtriumObject o, bool overwrite = false, bool overwriteIds = true)
        {
            var props = this.GetType().GetProperties();
            foreach (var pi in props)
            {
                if (pi.GetValue(this) == (pi.PropertyType.IsValueType ? Activator.CreateInstance(pi.PropertyType) : null) || overwrite)
                {
                    if (pi.GetValue(o) != (pi.PropertyType.IsValueType ? Activator.CreateInstance(pi.PropertyType) : null))
                    {
                        if((pi.Name == "ObjectId" 
                            || pi.Name == "ObjectGuid"
                            || pi.Name == "EntityRelationshipId"
                            || pi.Name == "EntityRelationshipGuid") && !overwriteIds)
                        {
                            continue;
                        }
                        SdkDataType sdkAttr = (SdkDataType)Attribute.GetCustomAttribute(pi, typeof(SdkDataType));
                        if (pi.CanWrite)
                        {
                            pi.SetValue(this, pi.GetValue(o));
                        }
                        else if (sdkAttr?.RelatedAttribute != null)
                        {
                            var relatedPi = props.Where(prop => prop.Name == sdkAttr.RelatedAttribute).First();
                            if (sdkAttr.RelatedType == "DateTime")
                            {
                                relatedPi.SetValue(this, relatedPi.GetValue(o, null));
                            }
                        }
                    }
                }
            }
            return this;
        }

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
