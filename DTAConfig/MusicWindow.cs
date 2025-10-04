using ClientCore;
using ClientGUI;
using DTAConfig.Entity;
using Localization.Tools;
using Microsoft.Xna.Framework;
using NAudio.Wave;
using NAudio.Flac;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;
using System.Windows.Forms;

using Rampastring.Tools;
using NAudio.Vorbis;

namespace DTAConfig
{
    public class MusicWindow(WindowManager windowManager) : XNAWindow(windowManager)
    {
        private XNAClientButton btnPlay;
        private XNAClientButton btnStop;
        private XNAClientButton btnRemove;
        private XNAListBox listBox;
        private XNAMultiColumnListBox multiColumnListBox;
        private XNAContextMenu _menu;
        private AudioFileReader audioFile;
        private WaveOutEvent outputDevice;

        private string GetMusicIniPath() => Path.Combine(ProgramConstants.GamePath, "Resources/thememd/thememd.ini");

        public override void Initialize()
        {
            Name = "MusicWindow";
            ClientRectangle = new Rectangle(0, 0, 600, 450);
            BackgroundTexture = AssetLoader.LoadTextureUncached("hotkeyconfigbg.png");

            btnPlay = new XNAClientButton(WindowManager) { Text = "播放", X = 30, Y = 20, Width = UIDesignConstants.BUTTON_WIDTH_92 };
            btnPlay.LeftClick += BtnPlay_LeftClick;

            btnStop = new XNAClientButton(WindowManager) { Text = "停止", X = btnPlay.Right + 20, Y = 20, Width = UIDesignConstants.BUTTON_WIDTH_92 };
            btnStop.LeftClick += BtnStop_LeftClick;

            var btnAdd = new XNAClientButton(WindowManager) { Text = "添加", X = btnStop.Right + 20, Y = 20, Width = UIDesignConstants.BUTTON_WIDTH_92 };
            btnAdd.LeftClick += BtnAdd_Click;

            btnRemove = new XNAClientButton(WindowManager) { Text = "删除", X = btnAdd.Right + 20, Y = 20, Width = UIDesignConstants.BUTTON_WIDTH_92 };
            btnRemove.LeftClick += BtnRemove_LeftClick;

            var btnReLoad = new XNAClientButton(WindowManager) { Text = "刷新", X = btnRemove.Right + 20, Y = 20, Width = UIDesignConstants.BUTTON_WIDTH_92 };
            btnReLoad.LeftClick += (_, _) => ReLoad();

            listBox = new XNAListBox(WindowManager)
            {
                X = 30,
                Y = btnPlay.Bottom + 20,
                Width = UIDesignConstants.BUTTON_WIDTH_121,
                Height = 320,
                FontIndex = 1,
                LineHeight = 25
            };

            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            listBox.RightClick += ListBox_RightClick;

            multiColumnListBox = new XNAMultiColumnListBox(WindowManager)
            {
                X = listBox.Right + 40,
                Y = btnPlay.Bottom + 20,
                Width = 350,
                Height = 320
            }
                .AddColumn("属性", 60)
                .AddColumn("信息", 360);

            var btnSave = new XNAClientButton(WindowManager)
            {
                Text = "返回",
                X = Right - UIDesignConstants.BUTTON_WIDTH_92 - 30,
                Y = Bottom - 40,
                Width = UIDesignConstants.BUTTON_WIDTH_92
            };
            btnSave.LeftClick += (_, _) => { btnStop.OnLeftClick(new InputEventArgs()); Disable(); };

            _menu = new XNAContextMenu(windowManager);
            _menu.Name = nameof(_menu);
            _menu.Width = 100;

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "修改",
                SelectAction = () => {
                    var editWindow = new EditWindow(WindowManager, listBox.SelectedItem.Tag as Music)
                    {
                        ClientRectangle = new Rectangle(0, 0, 400, 130)
                    };
                    DarkeningPanel.AddAndInitializeWithControl(WindowManager, editWindow);
                    editWindow.EnabledChanged += (_, _) => ReLoad();
                    editWindow.Enable();
                }
            });

            AddChild(btnPlay);
            AddChild(btnStop);
            AddChild(btnAdd);
            AddChild(btnRemove);
            AddChild(btnReLoad);
            AddChild(listBox);
            AddChild(multiColumnListBox);
            AddChild(btnSave);
            AddChild(_menu);

            ReLoad();

            base.Initialize();

            CenterOnParent();

            ListBox_SelectedIndexChanged(null, null);
        }

        /// <summary>
        /// 右击打开菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_RightClick(object sender, EventArgs e)
        {
            listBox.SelectedIndex = listBox.HoveredIndex;
            _menu.Open(GetCursorPoint());
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStop_LeftClick(object sender, EventArgs e)
        {
            try
            {
                outputDevice?.Stop();
                outputDevice?.Dispose();
                outputDevice = null;
                audioFile?.Dispose();
                audioFile = null;
            }
            catch (Exception ex)
            {
                Logger.Log("MusicWindow", $"停止播放时出错：{ex}");
            }
        }

        /// <summary>
        /// 删除音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRemove_LeftClick(object sender, EventArgs e)
        {
            if (listBox.SelectedItem == null) return;

            var music = listBox.SelectedItem.Tag as Music;

            var box = new XNAMessageBox(WindowManager, "信息", $"您确定要删除音乐 {music.CName} 吗？", XNAMessageBoxButtons.YesNo);
            box.YesClickedAction += (_) =>
            {
                try
                {
                    if (File.Exists(music.Path))
                        File.Delete(music.Path);
                    new IniFile(GetMusicIniPath())
                        .RemoveKey("Themes", music.Section)
                        .RemoveSection(music.Section)
                        .WriteIniFile();
                    XNAMessageBox.Show(WindowManager, "信息", "删除成功!");
                    ReLoad();
                }
                catch
                {
                    XNAMessageBox.Show(WindowManager, "错误", "删除失败，可能是音乐文件被占用了。");
                }

            };
            box.Show();
        }


        /// <summary>
        /// 添加音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "音频文件 (*.mp3;*.wav;*.flac;*.aac;*.wma;*.ogg)|*.mp3;*.wav;*.flac;*.aac;*.wma;*.ogg";
            openFileDialog.Title = "选择音频文件";
            openFileDialog.Multiselect = true; // 允许多选

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string targetDirectory = Path.Combine(ProgramConstants.GamePath, "Resources/thememd");
                Directory.CreateDirectory(targetDirectory);

                var inifile = new IniFile(GetMusicIniPath());

                foreach (string sourceFilePath in openFileDialog.FileNames)
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    string uniqueId = Guid.NewGuid().ToString("N")[..8]; // 生成唯一文件名
                    string targetFilePath = Path.Combine(targetDirectory, $"_music{uniqueId}.wav");

                    try
                    {
                        using var reader = CreateAudioFileReader(sourceFilePath);
                        var targetFormat = new WaveFormat(22050, 16, reader.WaveFormat.Channels);

                        using var resampler = new MediaFoundationResampler(reader, targetFormat)
                        {
                            ResamplerQuality = 60 // 设置重采样质量
                        };

                        WaveFileWriter.CreateWaveFile(targetFilePath, resampler);
                    }
                    catch (Exception ex)
                    {
                        XNAMessageBox.Show(WindowManager, "转换音频出错", $"文件: {fileName}\n错误信息: {ex.Message}");
                        Logger.Log("MusicWindow", $"转换音频出错: {ex}");
                        continue; // 继续处理下一个文件
                    }

                    var section = $"_music{uniqueId}";

                    inifile.
                        SetValue("Themes", section, section).
                        AddSection(section).
                        SetValue("Name", $"THEME:{section}").
                        SetValue("CName", Path.GetFileNameWithoutExtension(fileName)).
                        SetValue("Sound", section).
                        SetValue("Normal", true);
                }

                inifile.WriteIniFile();
                ReLoad();
            }
        }

        private static WaveStream CreateAudioFileReader(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".mp3" => new Mp3FileReader(filePath),
                ".wav" => new MediaFoundationReader(filePath),
                ".ogg" => new VorbisWaveReader(filePath),
                ".flac" => new FlacReader(filePath),
                ".wma" => new MediaFoundationReader(filePath),
                ".aac" => new MediaFoundationReader(filePath),
                _ => throw new InvalidOperationException("不支持的文件格式"),
            };
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPlay_LeftClick(object sender, EventArgs e)
        {
            try
            {
                if (listBox.SelectedItem == null || listBox.SelectedItem.Tag is not Music music || string.IsNullOrEmpty(music.Path) || !File.Exists(music.Path))
                {
                    XNAMessageBox.Show(WindowManager, "错误", "请选择有效的音乐项");
                    return;
                }
                // 先释放旧资源
                outputDevice?.Stop();
                outputDevice?.Dispose();
                outputDevice = null;
                audioFile?.Dispose();
                audioFile = null;

                audioFile = new AudioFileReader(music.Path);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();
            }
            catch (Exception ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", $"播放失败：{ex.Message}");
                Logger.Log("MusicWindow", $"播放失败：{ex}");
            }
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox.SelectedItem == null)
            {
                btnRemove.Enabled = btnPlay.Enabled = false;
                return;
            }

            var music = listBox.SelectedItem.Tag as Music;

            btnRemove.Enabled = listBox.SelectedIndex != -1 && listBox.SelectedIndex < listBox.Items.Count;
            btnPlay.Enabled = btnRemove.Enabled && File.Exists(music.Path);

            multiColumnListBox.ClearItems();
            multiColumnListBox.
                AddItem(["名称", music.CName], true).
                AddItem(["时长", music.Length], true).
                AddItem(["大小", music.Size], true);

            string side = music.Side switch
            {
                "GDI" => "盟军",
                "NOD" => "苏军",
                "ThirdSide" => "尤里",
                "" => "所有",
                _ => throw new NotImplementedException(),
            };
            multiColumnListBox.AddItem(["可用", side], true);
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void ReLoad()
        {
            try
            {
                string musicDir = Path.Combine(ProgramConstants.GamePath, "Resources/thememd");
                if (!Directory.Exists(musicDir))
                    Directory.CreateDirectory(musicDir);

                if (Directory.GetFiles(musicDir).Length == 0)
                    Mix.UnPackMix(musicDir + "/", Path.Combine(ProgramConstants.GamePath, "thememd.mix"));

                listBox.Clear();

                UserINISettings.Instance.MusicNameDictionary = [];
                var inifile = new IniFile(GetMusicIniPath());
                var csfDictionary = new CSF(Path.Combine(ProgramConstants.GamePath, "ra2md.csf")).GetCsfDictionary();
                var iniSection = inifile.GetSectionValues("Themes");
                if (iniSection != null)
                    foreach (var section in iniSection)
                    {
                        if (!inifile.SectionExists(section)) continue;

                        var Sound = inifile.GetValue(section, "Sound", section);

                        var PathStr = string.Empty;
                        var Size = string.Empty;
                        var Length = inifile.GetValue(section, "Length", string.Empty);
                        var Side = inifile.GetValue(section, "Side", string.Empty);

                        if (string.IsNullOrEmpty(Length))
                        {
                            var path = Path.Combine(musicDir, Sound + ".wav");
                            if (File.Exists(path))
                            {
                                try
                                {
                                    Length = new AudioFileReader(path).TotalTime.ToString();
                                    Size = new FileInfo(path).Length.ToFileSizeString(2) + " MB";
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log("MusicWindow", $"获取文件信息失败：{ex}");
                                    Size = "未知";
                                }
                                PathStr = path;
                            }
                        }

                        var music = new Music()
                        {
                            Name = inifile.GetValue(section, "Name", $"THEME:{section}"),
                            Section = section,
                            CName = inifile.GetValue(section, "CName", csfDictionary?.TryGetValue($"THEME:{section}", out var value) == true ? value : section),
                            Sound = inifile.GetValue(section, "Sound", string.Empty),
                            Normal = inifile.GetValue(section, "Normal", string.Empty),
                            Length = Length,
                            Size = Size,
                            Scenario = inifile.GetValue(section, "Scenario", string.Empty),
                            Side = Side,
                            Repeat = inifile.GetValue(section, "Repeat", string.Empty),
                            Path = PathStr
                        };

                        listBox.AddItem(music.CName, tag: music);
                        UserINISettings.Instance.MusicNameDictionary.Add($"THEME:{section}", music.CName);
                    }

                multiColumnListBox.ClearItems();
                ListBox_SelectedIndexChanged(null, null);
            }
            catch (Exception ex)
            {
                XNAMessageBox.Show(WindowManager, "错误", "载入音乐失败，详情可查看日志");
                Logger.Log("MusicWindow", $"载入音乐失败：{ex}");
            }
        }
    }

    public class EditWindow(WindowManager windowManager, Music _music) : XNAWindow(windowManager)
    {
        private string GetMusicIniPath() => Path.Combine(ProgramConstants.GamePath, "Resources/thememd/thememd.ini");

        public override void Initialize()
        {
            base.Initialize();
            Name = "MusicWindow";

            CenterOnParent();

            var lblName = new XNALabel(WindowManager)
            {
                Name = "lblName",
                Text = "名称",
                ClientRectangle = new Rectangle(20, 30, 0, 0)
            };

            var ctbName = new XNATextBox(WindowManager)
            {
                Name = "ctbName",
                Text = _music.CName,
                ClientRectangle = new Rectangle(lblName.Right + 50, lblName.Y, 100, 20)
            };

            var lblSide = new XNALabel(WindowManager)
            {
                Name = "lblSide",
                Text = "阵营",
                ClientRectangle = new Rectangle(ctbName.Right + 35, ctbName.Y, 0, 0)
            };

            var ddSide = new XNAClientDropDown(WindowManager)
            {
                Name = "ddSide",
                Text = _music.Side,
                ClientRectangle = new Rectangle(lblSide.Right + 50, lblSide.Y, 100, 30)
            };

            foreach (var side in new string[4] { "所有", "盟军", "苏军", "尤里" })
                ddSide.AddItem(side);

            ddSide.SelectedIndex = _music.Side switch
            {
                "GDI" => 1,
                "NOD" => 2,
                "ThirdSide" => 3,
                _ => 0,
            };

            var btnOK = new XNAClientButton(WindowManager)
            {
                Name = "btnOK",
                Text = "确定",
                X = lblName.X,
                Y = ddSide.Bottom + 20
            };

            btnOK.LeftClick += (_, _) =>
            {
                var Side = string.Empty;

                Side = ddSide.SelectedIndex switch
                {
                    0 => string.Empty,
                    1 => "GDI",
                    2 => "NOD",
                    3 => "ThirdSide",
                    _ => throw new NotImplementedException(),
                };

                var iniFile = new IniFile(GetMusicIniPath());
                iniFile.SetValue(_music.Section, "CName", ctbName.Text);
                if (Side == string.Empty)
                    iniFile.RemoveKey(_music.Section, "Side");
                else iniFile.SetValue(_music.Section, "Side", Side);
                iniFile.WriteIniFile();

                Disable();
                Dispose();
            };

            var btnCanael = new XNAClientButton(WindowManager)
            {
                Name = "btnCanael",
                Text = "取消",
                X = btnOK.Right + 40,
                Y = btnOK.Y
            };

            btnCanael.LeftClick += (_, _) => { Disable(); Dispose(); };

            AddChild(lblName);
            AddChild(ctbName);
            AddChild(lblSide);
            AddChild(ddSide);
            AddChild(btnOK);
            AddChild(btnCanael);
        }
    }
}