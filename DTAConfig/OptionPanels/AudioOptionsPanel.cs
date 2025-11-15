using System;
using System.IO;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class AudioOptionsPanel : XNAOptionsPanel
    {
        private const int VOLUME_MIN = 0;
        private const int VOLUME_MAX = 10;
        private const int VOLUME_SCALE = 10;
        private const int PADDING_X = 12;
        private const int PADDING_Y = 14;
        private const int TRACKBAR_X_PADDING = 20;
        private const int TRACKBAR_Y_PADDING = 20;
        private const int TRACKBAR_Y_OFFSET = 2; //trackbars sit slightly higher than their labels.
        private const int TRACKBAR_HEIGHT = 22;
        private const int CHECKBOX_SPACING = 12;
        private const int GROUP_SPACING = 22;
        private const float VOLUME_EPSILON = 0.001f; // 用于浮点数比较的容差值

        public AudioOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        private XNATrackbar trbScoreVolume;
        private XNATrackbar trbSoundVolume;
        private XNATrackbar trbVoiceVolume;

        private XNALabel lblScoreVolumeValue;
        private XNALabel lblSoundVolumeValue;
        private XNALabel lblVoiceVolumeValue;

        private XNAClientCheckBox chkScoreShuffle;

        private XNALabel lblClientVolumeValue;
        private XNATrackbar trbClientVolume;

        private XNAClientCheckBox chkMainMenuMusic;
        private XNAClientCheckBox chkStopMusicOnMenu;
        private XNAClientCheckBox chkStopGameLobbyMessageAudio;
        private MusicWindow musicWindow;
        private XNAClientDropDown ddVoice;

        public override void Initialize()
        {
            base.Initialize();

            Name = "AudioOptionsPanel";
            // 音乐音量
            var lblScoreVolume = new XNALabel(WindowManager);
            lblScoreVolume.Name = nameof(lblScoreVolume);
            lblScoreVolume.ClientRectangle = new Rectangle(PADDING_X, PADDING_Y, 0, 0);
            lblScoreVolume.Text = "Music Volume:".L10N("UI:DTAConfig:MusicVolume");

            lblScoreVolumeValue = new XNALabel(WindowManager);
            lblScoreVolumeValue.Name = nameof(lblScoreVolumeValue);
            lblScoreVolumeValue.FontIndex = 1;
            lblScoreVolumeValue.Text = "5";
            lblScoreVolumeValue.ClientRectangle = new Rectangle(
                Width - lblScoreVolumeValue.Width - PADDING_X,
                lblScoreVolume.Y, 0, 0);

            trbScoreVolume = new XNATrackbar(WindowManager);
            trbScoreVolume.Name = nameof(trbScoreVolume);
            trbScoreVolume.ClientRectangle = new Rectangle(
                lblScoreVolume.Right + TRACKBAR_X_PADDING,
                lblScoreVolume.Y - TRACKBAR_Y_OFFSET,
                lblScoreVolumeValue.X - TRACKBAR_X_PADDING - lblScoreVolume.Right - TRACKBAR_X_PADDING,
                TRACKBAR_HEIGHT);
            trbScoreVolume.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScoreVolume.MinValue = VOLUME_MIN;
            trbScoreVolume.MaxValue = VOLUME_MAX;
            trbScoreVolume.ValueChanged += TrbScoreVolume_ValueChanged;

            // 音效音量
            var lblSoundVolume = new XNALabel(WindowManager);
            lblSoundVolume.Name = nameof(lblSoundVolume);
            lblSoundVolume.ClientRectangle = new Rectangle(lblScoreVolume.X,
                trbScoreVolume.Bottom + TRACKBAR_Y_PADDING + TRACKBAR_Y_OFFSET, 0, 0);
            lblSoundVolume.Text = "Sound Volume:".L10N("UI:DTAConfig:SoundVolume");

            lblSoundVolumeValue = new XNALabel(WindowManager);
            lblSoundVolumeValue.Name = nameof(lblSoundVolumeValue);
            lblSoundVolumeValue.FontIndex = 1;
            lblSoundVolumeValue.Text = "8";
            lblSoundVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.X,
                lblSoundVolume.Y, 0, 0);

            trbSoundVolume = new XNATrackbar(WindowManager);
            trbSoundVolume.Name = nameof(trbSoundVolume);
            trbSoundVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.X,
                trbScoreVolume.Bottom + TRACKBAR_Y_PADDING,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbSoundVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbSoundVolume.MinValue = VOLUME_MIN;
            trbSoundVolume.MaxValue = VOLUME_MAX;
            trbSoundVolume.ValueChanged += TrbSoundVolume_ValueChanged;

            // 语音音量
            var lblVoiceVolume = new XNALabel(WindowManager);
            lblVoiceVolume.Name = nameof(lblVoiceVolume);
            lblVoiceVolume.ClientRectangle = new Rectangle(lblScoreVolume.X,
                trbSoundVolume.Bottom + TRACKBAR_Y_PADDING + TRACKBAR_Y_OFFSET, 0, 0);
            lblVoiceVolume.Text = "Voice Volume:".L10N("UI:DTAConfig:VoiceVolume");

            lblVoiceVolumeValue = new XNALabel(WindowManager);
            lblVoiceVolumeValue.Name = nameof(lblVoiceVolumeValue);
            lblVoiceVolumeValue.FontIndex = 1;
            lblVoiceVolumeValue.Text = "8";
            lblVoiceVolumeValue.ClientRectangle = new Rectangle(
                lblScoreVolumeValue.X,
                lblVoiceVolume.Y, 0, 0);

            trbVoiceVolume = new XNATrackbar(WindowManager);
            trbVoiceVolume.Name = nameof(trbVoiceVolume);
            trbVoiceVolume.ClientRectangle = new Rectangle(
                trbSoundVolume.X,
                trbSoundVolume.Bottom + TRACKBAR_Y_PADDING,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbVoiceVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbVoiceVolume.MinValue = VOLUME_MIN;
            trbVoiceVolume.MaxValue = VOLUME_MAX;
            trbVoiceVolume.ValueChanged += TrbVoiceVolume_ValueChanged;

            // 客户端音量
            var lblClientVolume = new XNALabel(WindowManager);
            lblClientVolume.Name = nameof(lblClientVolume);
            lblClientVolume.ClientRectangle = new Rectangle(lblVoiceVolume.X,
                trbVoiceVolume.Bottom + GROUP_SPACING + TRACKBAR_Y_OFFSET, 0, 0);
            lblClientVolume.Text = "Client Volume:".L10N("UI:DTAConfig:ClientVolume");

            lblClientVolumeValue = new XNALabel(WindowManager);
            lblClientVolumeValue.Name = nameof(lblClientVolumeValue);
            lblClientVolumeValue.FontIndex = 1;
            lblClientVolumeValue.Text = "3";
            lblClientVolumeValue.ClientRectangle = new Rectangle(
                lblVoiceVolumeValue.X,
                lblClientVolume.Y, 0, 0);

            trbClientVolume = new XNATrackbar(WindowManager);
            trbClientVolume.Name = nameof(trbClientVolume);
            trbClientVolume.ClientRectangle = new Rectangle(
                trbScoreVolume.X,
                lblClientVolume.Y - TRACKBAR_Y_OFFSET,
                trbScoreVolume.Width,
                trbScoreVolume.Height);
            trbClientVolume.BackgroundTexture = trbScoreVolume.BackgroundTexture;
            trbClientVolume.MinValue = VOLUME_MIN;
            trbClientVolume.MaxValue = VOLUME_MAX;
            trbClientVolume.ValueChanged += TrbClientVolume_ValueChanged;

            // 随机播放音乐
            chkScoreShuffle = new XNAClientCheckBox(WindowManager);
            chkScoreShuffle.Name = nameof(chkScoreShuffle);
            chkScoreShuffle.ClientRectangle = new Rectangle(
                lblClientVolume.X,
                trbClientVolume.Bottom + TRACKBAR_Y_PADDING, 0, 0);
            chkScoreShuffle.Text = "Shuffle Music".L10N("UI:DTAConfig:ShuffleMusic");
            AddChild(chkScoreShuffle);

            var lblVoice = new XNALinkLabel(WindowManager);
            lblVoice.Name = "lblVoice";
            lblVoice.ClientRectangle = new Rectangle(chkScoreShuffle.X + 400, trbClientVolume.Bottom + 16, 0, 0);
            lblVoice.Text = "Voice:".L10N("UI:Main:Voice");
            lblVoice.DoubleLeftClick += LblVoice_DoubleLeftClick;
            AddChild(lblVoice);

            ddVoice = new XNAClientDropDown(WindowManager);
            ddVoice.Name = "ddVoice";
            ddVoice.ClientRectangle = new Rectangle(lblVoice.Right + 12, lblVoice.Top - 2, 160, 20);
            AddChild(ddVoice);

            foreach (string voice in Directory.GetDirectories("Resources/Voice"))
            {
                ddVoice.AddItem(Path.GetFileName(voice));
            }


            // 主菜单音乐
            chkMainMenuMusic = new XNAClientCheckBox(WindowManager);
            chkMainMenuMusic.Name = nameof(chkMainMenuMusic);
            chkMainMenuMusic.Text = "Main menu music".L10N("UI:DTAConfig:MainMenuMusic");
            chkMainMenuMusic.ClientRectangle = new Rectangle(
                chkScoreShuffle.X,
                chkScoreShuffle.Bottom + PADDING_Y, 0, 0);
            chkMainMenuMusic.CheckedChanged += ChkMainMenuMusic_CheckedChanged;
            AddChild(chkMainMenuMusic);

            // 大厅中停止播放音乐
            chkStopMusicOnMenu = new XNAClientCheckBox(WindowManager);
            chkStopMusicOnMenu.Name = nameof(chkStopMusicOnMenu);
            chkStopMusicOnMenu.Text = "Don't play main menu music in lobbies".L10N("UI:DTAConfig:NoLobbiesMusic");
            chkStopMusicOnMenu.ClientRectangle = new Rectangle(
                chkMainMenuMusic.X, chkMainMenuMusic.Bottom + CHECKBOX_SPACING, 0, 0);
            AddChild(chkStopMusicOnMenu);

            chkStopGameLobbyMessageAudio = new XNAClientCheckBox(WindowManager);
            chkStopGameLobbyMessageAudio.Name = nameof(chkStopGameLobbyMessageAudio);
            chkStopGameLobbyMessageAudio.Text = "Don't play lobby message audio when game is running".L10N("UI:DTAConfig:NoGameLobbyMessageAudio");
            chkStopGameLobbyMessageAudio.ClientRectangle = new Rectangle(
                lblScoreVolume.X, chkStopMusicOnMenu.Bottom + CHECKBOX_SPACING, 0, 0);
            AddChild(chkStopGameLobbyMessageAudio);

            musicWindow = new MusicWindow(WindowManager);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, musicWindow);
            musicWindow.Disable();

            var btnMusicWindow = new XNAClientButton(WindowManager)
            {
                Name = "btnMusicWindow",
                X = lblScoreVolume.X,
                Y = chkStopMusicOnMenu.Y + 60,
                Text = "Manage in-game music".L10N("UI:DTAConfig:ManageingameMusic"),
            };
            btnMusicWindow.LeftClick += (_, _) => { musicWindow.Enable(); };

            AddChild(lblScoreVolume);
            AddChild(lblScoreVolumeValue);
            AddChild(trbScoreVolume);
            AddChild(lblSoundVolume);
            AddChild(lblSoundVolumeValue);
            AddChild(trbSoundVolume);
            AddChild(lblVoiceVolume);
            AddChild(lblVoiceVolumeValue);
            AddChild(trbVoiceVolume);

            AddChild(lblClientVolume);
            AddChild(lblClientVolumeValue);
            AddChild(trbClientVolume);

            AddChild(btnMusicWindow);
        }

        private void LblVoice_DoubleLeftClick(object sender, EventArgs e)
        {
            string folderPath = $"{ProgramConstants.GamePath}/Resources/Voice";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = folderPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void ChkMainMenuMusic_CheckedChanged(object sender, EventArgs e)
        {
            chkStopMusicOnMenu.AllowChecking = chkMainMenuMusic.Checked;
            chkStopMusicOnMenu.Checked = chkMainMenuMusic.Checked;
        }

        private void TrbScoreVolume_ValueChanged(object sender, EventArgs e)
        {
            lblScoreVolumeValue.Text = trbScoreVolume.Value.ToString();
        }

        private void TrbSoundVolume_ValueChanged(object sender, EventArgs e)
        {
            lblSoundVolumeValue.Text = trbSoundVolume.Value.ToString();
        }

        private void TrbVoiceVolume_ValueChanged(object sender, EventArgs e)
        {
            lblVoiceVolumeValue.Text = trbVoiceVolume.Value.ToString();
        }

        private void TrbClientVolume_ValueChanged(object sender, EventArgs e)
        {
            lblClientVolumeValue.Text = trbClientVolume.Value.ToString();
            float volume = ConvertToVolumeFloat(trbClientVolume.Value);
            WindowManager.SoundPlayer?.SetVolume(volume);
        }

        public override void Load()
        {
            base.Load();

            trbScoreVolume.Value = ConvertToTrackbarValue(IniSettings.ScoreVolume);
            trbSoundVolume.Value = ConvertToTrackbarValue(IniSettings.SoundVolume);
            trbVoiceVolume.Value = ConvertToTrackbarValue(IniSettings.VoiceVolume);
            trbClientVolume.Value = ConvertToTrackbarValue(IniSettings.ClientVolume);

            ApplyInitialClientVolume();

            lblScoreVolumeValue.Text = trbScoreVolume.Value.ToString();
            lblSoundVolumeValue.Text = trbSoundVolume.Value.ToString();
            lblVoiceVolumeValue.Text = trbVoiceVolume.Value.ToString();
            lblClientVolumeValue.Text = trbClientVolume.Value.ToString();

            chkScoreShuffle.Checked = IniSettings.IsScoreShuffle;
            chkMainMenuMusic.Checked = IniSettings.PlayMainMenuMusic;
            chkStopMusicOnMenu.Checked = IniSettings.StopMusicOnMenu;
            chkStopGameLobbyMessageAudio.Checked = IniSettings.StopGameLobbyMessageAudio;

            var voiceValue = UserINISettings.Instance.Voice.Value;
            var i = ddVoice.Items.FindIndex(item => item.Text == voiceValue);
            if (i >= 0)
                ddVoice.SelectedIndex = i;
            else
                ddVoice.SelectedIndex = 0;
        }

        private void ApplyInitialClientVolume()
        {
            float clientVolume = ConvertToVolumeFloat(trbClientVolume.Value);
            WindowManager.SoundPlayer?.SetVolume(clientVolume);
        }

        public override bool Save()
        {
            bool restartRequired = base.Save();

            IniSettings.ScoreVolume.Value = ConvertToVolumeFloat(trbScoreVolume.Value);
            IniSettings.SoundVolume.Value = ConvertToVolumeFloat(trbSoundVolume.Value);
            IniSettings.VoiceVolume.Value = ConvertToVolumeFloat(trbVoiceVolume.Value);
            IniSettings.ClientVolume.Value = ConvertToVolumeFloat(trbClientVolume.Value);

            IniSettings.IsScoreShuffle.Value = chkScoreShuffle.Checked;
            IniSettings.PlayMainMenuMusic.Value = chkMainMenuMusic.Checked;
            IniSettings.StopMusicOnMenu.Value = chkStopMusicOnMenu.Checked;
            IniSettings.StopGameLobbyMessageAudio.Value = chkStopGameLobbyMessageAudio.Checked;
            UserINISettings.Instance.Voice.Value = ddVoice.SelectedItem?.Text;

            return restartRequired;
        }

        // 从轨道条值转换为浮点数音量
        private float ConvertToVolumeFloat(int trackbarValue)
        {
            // 处理边界值
            if (trackbarValue <= VOLUME_MIN) 
                return 0f;
            if (trackbarValue >= VOLUME_MAX) 
                return 1f;

            // 使用精确的浮点转换
            float volume = trackbarValue / (float)VOLUME_SCALE;

            // 四舍五入保留一位小数
            return (float)Math.Round(volume, 1);
        }

        // 从浮点数音量转换为轨道条值
        private int ConvertToTrackbarValue(double volumeValue)
        {
            // 处理边界情况
            if (volumeValue < VOLUME_EPSILON) 
                return VOLUME_MIN;
            if (volumeValue > 1.0 - VOLUME_EPSILON) 
                return VOLUME_MAX;

            // 四舍五入到最近的整数
            return (int)Math.Round(volumeValue * VOLUME_SCALE);
        }
    }
}