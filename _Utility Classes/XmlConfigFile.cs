using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TShockAPI.Configuration;

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
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, Settings);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config file: {ex.Message}");
            }
        }
        /// <summary>
        /// Reads the config files if the file is not missing
        /// any attributes
        /// </summary>
        /// <returns> bool informing if the configs need to be written </returns>
        public bool Read()
        {
            try
            {
                if (IsMissingConfigs())
                    return true;

                var serializer = new XmlSerializer(typeof(TSettings));
                using (var reader = new StreamReader(filePath))
                {
                    Settings = (TSettings)serializer.Deserialize(reader);
                }
                OnConfigRead?.Invoke(this);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config file: {ex.Message}");
                return true;
            }
        }

        public bool IsMissingConfigs()
        {
            // Load the config file
            XmlDocument configsToVerify = new XmlDocument();
            configsToVerify.Load(filePath);

            // Load the current configs
            XmlDocument currentConfigs = SerializeToXmlDocument(Settings);

            // Extract field names from the first XML file
            XmlNodeList fields1 = configsToVerify.SelectNodes("//*");

            // Extract field names from the second XML file
            XmlNodeList fields2 = currentConfigs.SelectNodes("//*");

            // Compare the field names
            if (fields1.Count != fields2.Count)
            {
                return true; // Files have different number of fields
            }

            foreach (XmlAttribute field1 in fields1)
            {
                bool found = false;
                foreach (XmlAttribute field2 in fields2)
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

        public XmlDocument SerializeToXmlDocument(object input)
        {
            XmlSerializer ser = new XmlSerializer(input.GetType());

            XmlDocument xd = null;

            using (MemoryStream memStm = new MemoryStream())
            {
                ser.Serialize(memStm, input);

                memStm.Position = 0;

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;

                using (var xtr = XmlReader.Create(memStm, settings))
                {
                    xd = new XmlDocument();
                    xd.Load(xtr);
                }
            }

            return xd;
        }
    }
}