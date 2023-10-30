using System;

namespace Terraria.Plugins.Common
{
    /// <summary>
    /// Allows to create xml comments directly before
    /// declaring a property, You need to specify the
    /// serialized name of your property if its not the
    /// one you made on declaration.
    /// </summary>

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XmlCommentAttribute : Attribute
    {
        public string Value { get; set; }
        public string CustomAttributeName { get; set; } = "";
    }
}