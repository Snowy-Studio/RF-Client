using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClientCore;
using Rampastring.Tools;

namespace DTAConfig.Settings
{
    public class DataObject
    {
        public string modID { get; set; }
        public Dictionary<string, List<string>> Primary { get; set; }
        public Dictionary<string, List<string>> Secondary { get; set; }
        public Dictionary<string, List<string>> Warhead { get; set; }
        public Dictionary<string, List<string>> Projectile { get; set; }

        public DataObject() {
            Primary = new Dictionary<string, List<string>>();
            Secondary = new Dictionary<string, List<string>>();
            Warhead = new Dictionary<string, List<string>>();
            Projectile = new Dictionary<string, List<string>>();
        }
    }
    public class Rulesmd
    {

        protected readonly string JSONFILE = Path.Combine(ProgramConstants.GamePath, "Resources/rules.json");
        public string modID { get; set; }

        public string rulesFile { get; set; }

        public IniFile rules;

        public DataObject dataObject { get; set; }

        public Rulesmd(string rulesFile, string modID) {
            
            this.rulesFile = rulesFile;
            rules = new IniFile(rulesFile);
            this.modID = modID;
            Initialization();
        }

        public IniSection GetrulesSection(string sectionName)
        {
           
            return rules.GetSection(sectionName);
        }

        private void Initialization()
        {

            List<DataObject> objects = readJson();
            var foundObject = objects.FirstOrDefault(obj => obj.modID == modID);
            if (objects != null && foundObject != null)
            {
                dataObject = foundObject;
            }
            else
            {
                dataObject = new DataObject();
                dataObject.modID = modID;
                IniFile iniFile = new IniFile(rulesFile);
                foreach (var section in iniFile.GetSections())
                {
                    var currentSection = iniFile.GetSection(section);

                    SetIfKeyExists(dataObject.Primary, currentSection, "Primary");
                    SetIfKeyExists(dataObject.Secondary, currentSection, "Secondary");
                    SetIfKeyExists(dataObject.Warhead, currentSection, "Warhead");
                    SetIfKeyExists(dataObject.Projectile, currentSection, "Projectile");
                }
               

                objects.Add(dataObject);
                writeJson(objects);
            }

        }

        private void SetIfKeyExists(Dictionary<string, List<string>> dictionary, IniSection section, string key)
        {
           

            if (section.KeyExists(key))
            {
               
                
                if (!dictionary.ContainsKey(section.GetValue(key, string.Empty)))
                    dictionary.Add(section.GetValue(key, string.Empty), new List<string>());

                dictionary[section.GetValue(key, string.Empty)].Add(section.GetValue("UIName", $"Name:{section.SectionName}"));

            }
        }

        public List<DataObject> readJson()
        {
            if (!File.Exists(JSONFILE) || new FileInfo(JSONFILE).Length == 0)
            {
               
                return new List<DataObject>();
            }


            string jsonString = File.ReadAllText(JSONFILE);

            return JsonSerializer.Deserialize<List<DataObject>>(jsonString);

        }

        public void writeJson(List<DataObject> objects)
        {

            string updatedJson = JsonSerializer.Serialize(objects);

            File.WriteAllText(JSONFILE, updatedJson);
        }

    }
}
