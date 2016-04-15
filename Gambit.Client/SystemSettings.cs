namespace Gambit.Client
{
	using System;
	using System.Configuration;
	using System.IO;
	using System.Xml;
	using SystemConfiguration = System.Configuration.Configuration;

	public class SystemSettings
    {
        private static SystemConfiguration _common;
        private static string root = AppDomain.CurrentDomain.BaseDirectory;

        private const string fileName = "keys.config";

        private static void init()
        {
            //if (root == null) throw new ArgumentException(nameof(root)); //breaks visual studio 2012 -josh
            if (root == null) throw new ArgumentException("root.ToString"); //works in vs2012 -josh

            if (_common != null) return;

            string path = Path.Combine(root, fileName);

            if (!File.Exists(path))
            {
                CreateConfigFile(path);
            }

            var fileMap = new ConfigurationFileMap(path);

            _common = ConfigurationManager.OpenMappedMachineConfiguration(fileMap);
        }

        

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
