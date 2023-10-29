using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Terraria.Plugins.Common._Utility_Classes;
using TShockAPI;

namespace Terraria.Plugins.Common
{
    /// <summary>
    ///  An implementation of TShock's `TShockAPI.Configuration`
    ///  for XML configurations
    /// </summary>
    public class XmlConfigFile<TSettings> where TSettings : new()
    {
        public static Action<XmlConfigFile<TSettings>> OnConfigRead;

        private string filePath;

        public virtual TSettings Settings { get; set; } = new TSettings();

        public XmlConfigFile(string path)
        {
            filePath = path;
        }

        /// <summary>
        /// Saves the config file to the path
        /// </summary>
        public void Write()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(TSettings));
                using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings() { Indent = true, NewLineOnAttributes = true }))
                {
                    serializer.Serialize(writer, Settings);
                }

                WriteComments(Settings, filePath);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Error loading config file at {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the config files if the file is not missing
        /// any attributes and if reading was successful
        /// </summary>
        /// <returns> returns true if an error occurs or if the configs are missing </returns>
        public bool Read()
        {
            try
            {
                if (IsMissingConfigs())
                    return true;

                var serializer = new XmlSerializer(typeof(TSettings));
                using (var reader = XmlReader.Create(filePath, new() { IgnoreComments = true, IgnoreWhitespace = true  }))
                {
                    Settings = (TSettings)serializer.Deserialize(reader);
                }
                OnConfigRead?.Invoke(this);
                return false;
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Error loading config file at {filePath}: {ex.Message}");
                return true;
            }
        }

        public bool IsMissingConfigs()
        {
            // Load the config file
            XmlDocument configsToVerify = new XmlDocument();
            using (var reader = XmlReader.Create(filePath, new() { IgnoreComments = true, IgnoreWhitespace = true }))
            {
                configsToVerify.Load(reader);
            }

            // Load the current configs
            XmlDocument currentConfigs = SerializeToXmlDocument(Settings);

            // Extract field names from the first XML file
            var fields1 = configsToVerify.SelectNodes("//*")
                            .Cast<XmlNode>()
                            .Where(i => i.NodeType == XmlNodeType.Element);

            // Extract field names from the second XML file
            var fields2 = currentConfigs.SelectNodes("//*")
                            .Cast<XmlNode>()
                            .Where(i => i.NodeType == XmlNodeType.Element);
            // Compare the field names
            if (fields1.Count() != fields2.Count())
            {
                return true; // Files have different number of fields
            }

            foreach (XmlElement field1 in fields1)
            {
                bool found = false;
                foreach (XmlElement field2 in fields2)
                {
                    if (field1.Name == field2.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return true; // Field name not found in the second file
                }
            }

            return false; // Files have the same fields
        }

        public static XmlDocument SerializeToXmlDocument(object input)
        {
            XmlSerializer ser = new XmlSerializer(input.GetType());

            XmlDocument xd = null;

            using (MemoryStream memStm = new MemoryStream())
            {
                ser.Serialize(memStm, input);

                memStm.Position = 0;

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                settings.IgnoreComments = true;

                using (var xtr = XmlReader.Create(memStm, settings))
                {
                    xd = new XmlDocument();
                    xd.Load(xtr);
                }
            }

            return xd;
        }

        private static void WriteComments(object objectToSerialize, string path)
        {
            try
            {
                var propertyComments = GetPropertiesAndComments(objectToSerialize);
                if (!propertyComments.Any()) return;

                var doc = new XmlDocument();
                doc.Load(path);

                var parent = doc.SelectSingleNode(objectToSerialize.GetType().Name);
                if (parent == null) return;

                var childNodes = parent.ChildNodes.Cast<XmlNode>().Where(n => propertyComments.ContainsKey(n.Name));
                foreach (var child in childNodes)
                {
                    parent.InsertBefore(doc.CreateComment(propertyComments[child.Name]), child);
                }

                doc.Save(path);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Error writing config file at {path}: {ex.Message}");
            }
        }

        private static Dictionary<string, string> GetPropertiesAndComments(object objectToSerialize)
        {
            var propertyComments = objectToSerialize.GetType().GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(XmlCommentAttribute), false).Any())
                .Select(v => new
                {
                    Name = (((XmlCommentAttribute)v.GetCustomAttributes(typeof(XmlCommentAttribute),
                    false)[0]).CustomAttributeName == "")
                    ? v.Name
                    : ((XmlCommentAttribute)v.GetCustomAttributes(typeof(XmlCommentAttribute), false)[0]).CustomAttributeName,
                    Value = ((XmlCommentAttribute)v.GetCustomAttributes(typeof(XmlCommentAttribute), false)[0]).Value
                })
                 .ToDictionary(t => t.Name, t => t.Value);
            return propertyComments;
        }
    }
}