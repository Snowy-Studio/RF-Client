using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.Tools;

namespace DTAConfig.Settings
{

    /// <summary>
    /// 游戏对象.
    /// </summary>
    public class GameObject
    {

        private Rulesmd rules;
        private IniSection session;
        private Dictionary<string, string> csf;
        private string type;
        /// <summary>
        /// Initializes a new instance of the <see cref="GameObject"/> class.
        /// </summary>
        /// <param name="id">注册名</param>
        public GameObject(string id, Dictionary<string, string> csf , IniSection session, Rulesmd rulesmd)
        {
            ID = id;
            rules = rulesmd;
            this.session = session;
            this.csf = csf;
            
            foreach (var key in session.Keys)
            {
                var value = session.GetValue(key.Key, string.Empty);

                if (Unit.ContainsKey(key.Key) && value != string.Empty)
                {
                    type = "unit";
                    Unit[key.Key][1] = value;
                }
                else if (Weapon.ContainsKey(key.Key) && value != string.Empty)
                {
                  //  Console.WriteLine("是武器");
                    type = "weapon";
                    Weapon[key.Key][1] = value;

                }
            }
        }

        private Dictionary<string, List<string>> Unit = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
               {
                { "Strength", new List<string> { "血量", string.Empty } },
                { "Cost", new List<string> { "价格", string.Empty } },
                { "Speed", new List<string> { "速度", string.Empty } },
                { "TechLevel", new List<string> { "科技等级", string.Empty } }
               };

        private Dictionary<string, List<string>> Weapon = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
               {
                { "Damage", new List<string> { "伤害", string.Empty } },
                { "ROF", new List<string> { "攻击间隔", string.Empty } },
                { "Range", new List<string> { "射程", string.Empty } }
               };

        public string GetInfo()
        {
            string info = string.Empty;
            IniSection iniSection = rules.GetrulesSection(session.SectionName);
            

            if (type == "unit")
            {

                if (iniSection == null)
                {
                    return string.Empty;
                }

                info += csf.GetValueOrDefault(session.GetValue("UIName", $"Name:{session.SectionName}"), session.SectionName) + " ";

                    foreach (var value in Unit)
                {
                    string key = value.Key;

                    if (!string.IsNullOrEmpty(value.Value[1]))

                        if (iniSection != null)
                        {
                            info += value.Value[0] + $"从{iniSection.GetValue(key, string.Empty)}变为" + value.Value[1] + " ";
                        }
                        else
                        {
                            info += value.Value[0] + "为" + value.Value[1] + " ";
                        }
                    
                }
                
            }
            else if (type == "weapon")
            {

                info = string.Empty;
                List<string> list;

                if (rules.dataObject.Primary.TryGetValue(session.SectionName, out list))
                {
                    foreach (string value2 in list)
                    {
                        info += csf.GetValueOrDefault(value2, value2) + " ";
                    }

                    info += "的主武器";
                }
                
               // Console.WriteLine(info);

                foreach (List<string> value in Weapon.Values.ToList())
                {
                    if (!string.IsNullOrEmpty(value[1]))
                    {
                        info += $"{value[0]}变为" + value[1] + " ";
                    }
                }
              
            }
         
            return info;
        }

        /// <summary>
        /// Gets 对象的注册名.
        /// </summary>
        public string ID { get; }

        ///// <summary>
        ///// Gets or Sets 对象的血量.
        ///// </summary>
        //public Unit Strength { get; set; }

        ///// <summary>
        ///// Gets or Sets 对象的价格.
        ///// </summary>
        //public Unit Cost { get; set; }

        ///// <summary>
        ///// Gets or Sets 对象的速度.
        ///// </summary>
        //public Unit Speed { get; set; }
    }
}
