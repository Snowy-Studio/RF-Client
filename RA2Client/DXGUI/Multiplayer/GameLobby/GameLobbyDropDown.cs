using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAConfig.Entity;
using Localization;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.DXGUI.Generic;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option drop-down for the game lobby.
    /// </summary>
    public class GameLobbyDropDown : XNAClientDropDown
    {
        public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

        public string OptionName { get; private set; }

        public int HostSelectedIndex { get; set; }

        public int UserSelectedIndex { get; set; }

        private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

        private string spawnIniOption = string.Empty;

        public int defaultIndex;

        public bool Special { get; private set; } = false;
        public bool expandable { get; private set; }

        public List<string> RandomSelectors = new List<string>();
        public List<List<string>> RandomSidesIndex = new List<List<string>>();

        public List<string> Sides;


        public string[] DisallowedSideIndiex;
        public string[] DisallowedSide;

        public List<string> ControlName;

        public List<string> ControlIndex;

        public bool Ares;

        public bool Phobos;

        public override void Initialize()
        {
            // Find the game lobby that this control belongs to and register ourselves as a game option.

            XNAControl parent = Parent;
            while (true)
            {
                if (parent == null)
                    break;
                if (parent is CampaignSelector campaignSelector)
                {
                    if (campaignSelector.DropDowns.Find(chk => chk?.Name == this.Name) == null)
                        campaignSelector.DropDowns.Add(this);
                    break;
                }
                // oh no, we have a circular class reference here!
                if (parent is GameLobbyBase gameLobby)
                {
                    gameLobby.DropDowns.Add(this);
                    break;
                }

                parent = parent.Parent;
            }

            base.Initialize();
        }

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {

            switch (key)
            {

                case "Items":

                    string[] itemlabels = iniFile.GetStringValue(Name, "ItemLabels", "").Split(',');
                    string[] items = value.Split(',');
                    if (itemlabels.Length == 0)
                    {
                        items = value.L10N("UI:Main:" + OptionName).Split(',');
                    }
                    else
                    {
                        itemlabels = iniFile.GetStringValue(Name, "ItemLabels", "").Split(',');
                    }



                    for (int i = 0; i < items.Length; i++)
                    {
                        XNADropDownItem item = new XNADropDownItem();
                        if (itemlabels.Length > i && !String.IsNullOrEmpty(itemlabels[i]))
                        {
                            item.Text = itemlabels[i].L10N("UI:GameOption:" + itemlabels[i]);


                            item.Tag = new string[3] { items[i], string.Empty, string.Empty };
                        }
                        else item.Text = items[i];
                        AddItem(item);
                    }
                    return;

                case "Special":
                    Special = bool.Parse(value);
                    break;
                case "DataWriteMode":
                    if (value.ToUpper() == "INDEX")
                        dataWriteMode = DropDownDataWriteMode.INDEX;
                    else if (value.ToUpper() == "BOOLEAN")
                        dataWriteMode = DropDownDataWriteMode.BOOLEAN;
                    else if (value.ToUpper() == "MAPCODE")
                        dataWriteMode = DropDownDataWriteMode.MAPCODE;
                    else
                        dataWriteMode = DropDownDataWriteMode.STRING;
                    return;
                case "SpawnIniOption":
                    spawnIniOption = value;
                    return;

                case "Ares":
                    Ares = Conversions.BooleanFromString(value, false);
                    return;
                case "Phobos":
                    Phobos = Conversions.BooleanFromString(value, false);
                    return;
                case "Expandable":
                    expandable = Conversions.BooleanFromString(value, false);
                    return;

                case "DefaultIndex":
                    SelectedIndex = int.Parse(value);
                    defaultIndex = SelectedIndex;
                    HostSelectedIndex = SelectedIndex;
                    UserSelectedIndex = SelectedIndex;
                    return;
                case "OptionName":
                    OptionName = value;
                    return;
                case "ControlName":
                    ControlName = value.Split(',').ToList();
                    return;
                case "ControlIndex":
                    ControlIndex = value.Split(',').ToList();
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        /// <summary>
        /// Applies the drop down's associated code to spawn.ini.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            if (dataWriteMode == DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            if (String.IsNullOrEmpty(spawnIniOption))
            {
                Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
                return;
            }

            switch (dataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    spawnIni.SetValue("Settings", spawnIniOption, SelectedIndex > 0);
                    break;
                case DropDownDataWriteMode.INDEX:
                    spawnIni.SetValue("Settings", spawnIniOption, SelectedIndex);
                    break;
                default:
                case DropDownDataWriteMode.STRING:
                    if (Items[SelectedIndex].Tag != null)
                    {
                        spawnIni.SetValue("Settings", spawnIniOption, ((string[])Items[SelectedIndex].Tag)[0]);
                    }
                    else
                    {
                        spawnIni.SetValue("Settings", spawnIniOption, Items[SelectedIndex].Text);
                    }
                    break;
            }

        }
        /// <summary>
        /// Applies the drop down's associated code to the map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (dataWriteMode != DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count) return;

            string customIniPath = string.Empty;
             
           
            if (Items[SelectedIndex].Tag != null) {
                if (Items[SelectedIndex].Tag is Mod)
                {
                    //Console.WriteLine("string111");
                    customIniPath = ((Mod)Items[SelectedIndex].Tag).INI;
                }
                else
                {
                    //Console.WriteLine("string");
                    customIniPath = ((string[])Items[SelectedIndex].Tag)[0];
                }
            }
 
            else customIniPath = Items[SelectedIndex].Text;

            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

        public override void OnLeftClick()
        {
            if (!AllowDropDown)
                return;

            base.OnLeftClick();
            UserSelectedIndex = SelectedIndex;
        }


        public void ApplyDisallowedSideIndex(bool[] disallowedArray)
        {

            if (DisallowedSideIndiex == null || DisallowedSideIndiex.Length == 0 || SelectedIndex >= DisallowedSideIndiex.Length)
                return;
            int[] sideNotAllowed;
            DisallowedSide = DisallowedSideIndiex[SelectedIndex].Split('-');

            if (DisallowedSide.Length != 0)
            {

                sideNotAllowed = Array.ConvertAll(DisallowedSide, int.Parse);
                for (int j = 0; j < DisallowedSide.Length; j++)
                    disallowedArray[sideNotAllowed[j]] = true;
            }
        }

    }

}
