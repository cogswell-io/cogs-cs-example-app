namespace Gambit.Client
{
	using System;
	using System.Configuration;
	using System.IO;
	using System.Xml;
	using SystemConfiguration = System.Configuration.Configuration;

    /// <summary>
    /// A class for holding the System coniguration
    /// </summary>
	public class SystemSettings
    {
        private static SystemConfiguration _common;
        private static string root = AppDomain.CurrentDomain.BaseDirectory;

        private const string fileName = "keys.config";

        /// <summary>
        /// Initializes the SystemSettings class
        /// </summary>
        private static void init()
        {
            //if (root == null) throw new ArgumentException(nameof(root)); //breaks visual studio 2012 -josh
            if (root == null)
            {
                throw new ArgumentException("root.ToString"); //works in vs2012 -josh
            }

            if (_common != null)
            {
                return;
            }

            string path = Path.Combine(root, fileName);

            if (!File.Exists(path))
            {
                CreateConfigFile(path);
            }

            var fileMap = new ConfigurationFileMap(path);

            _common = ConfigurationManager.OpenMappedMachineConfiguration(fileMap);
        }

        /// <summary>
        /// Returns a setting by its key
        /// </summary>
        /// <param name="key">Key to be returned</param>
        /// <returns>Returns the value or an empty string</returns>
        public static string Load(string key)
        {
            init();
           
            if (_common != null)
            {
                //return _common.AppSettings.Settings[key]?.Value ?? ""; //breaks visual studio 2012 -josh
                return _common.AppSettings.Settings[key].Value ?? ""; //works in vs2012 -josh
            }

            return "";
        }

        /// <summary>
        /// Updates a configuration value with a new value
        /// </summary>
        /// <param name="key">Key of the setting to be updated</param>
        /// <param name="value">The new value that should be set</param>
        /// <returns>Returns true if the update is successful</returns>
        public static bool Update(string key, string value)
        {
            try
            {
                init();
                if (_common != null)
                {
                    _common.AppSettings.Settings[key].Value = value;
                    _common.Save();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }          
        }

        /// <summary>
        /// Creates a configuration file
        /// </summary>
        /// <param name="path">Path to the file that should be created</param>
        private static void CreateConfigFile(string path)
        {
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlNode configurationNode = doc.CreateElement("configuration");
            doc.AppendChild(configurationNode);

            XmlNode configSectionsNode = doc.CreateElement("configSections");
            configurationNode.AppendChild(configSectionsNode);
            XmlNode sectionNode = doc.CreateElement("section");
            XmlAttribute sectionNameAttribute = doc.CreateAttribute("name");
            sectionNameAttribute.Value = "appSettings";
            sectionNode.Attributes.Append(sectionNameAttribute);
            XmlAttribute sectionTypeAttribute = doc.CreateAttribute("type");
            sectionTypeAttribute.Value =
                "System.Configuration.AppSettingsSection, System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            sectionNode.Attributes.Append(sectionTypeAttribute);
            configSectionsNode.AppendChild(sectionNode);

            XmlNode appSettingsNode = doc.CreateElement("appSettings");
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "save", "false"));
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "accessKey", ""));
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "secretKey", ""));
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "clientSalt", ""));
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "clientSecret", ""));
            appSettingsNode.AppendChild(CreateNodeWithKeyAndValueAttributes(doc, "namespace", ""));

            configurationNode.AppendChild(appSettingsNode);
            doc.Save(path);
        }

        /// <summary>
        /// Creates a new XmlNode with key and value
        /// </summary>
        /// <param name="doc">The existing document that the key-value pair should be inserted into</param>
        /// <param name="key">The key for the pair</param>
        /// <param name="value">The value for the pair</param>
        /// <returns>An XmlNode</returns>
        private static XmlNode CreateNodeWithKeyAndValueAttributes(XmlDocument doc, string key, string value)
        {
            XmlNode addNode = doc.CreateElement("add");
            XmlAttribute keyAttribute = doc.CreateAttribute("key");
            keyAttribute.Value = key;
            addNode.Attributes.Append(keyAttribute);
            XmlAttribute valueAttribute = doc.CreateAttribute("value");
            valueAttribute.Value = value;
            addNode.Attributes.Append(valueAttribute);

            return addNode;
        }
    }
}
