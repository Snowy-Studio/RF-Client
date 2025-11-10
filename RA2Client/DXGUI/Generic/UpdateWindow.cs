using System;
using ClientCore;
using ClientCore.Settings;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Ra2Client.Domain;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Generic
{
    /// <summary>
    /// The update window, displaying the update progress to the user.
    /// </summary>
    public class UpdateWindow : XNAWindow
    {
        public delegate void UpdateCancelEventHandler(object sender, EventArgs e);
        public event UpdateCancelEventHandler UpdateCancelled;

        public delegate void UpdateCompletedEventHandler(object sender, EventArgs e);
        public event UpdateCompletedEventHandler UpdateCompleted;

        public delegate void UpdateFailureEventHandler(object sender, UpdateFailureEventArgs e);
        public event UpdateFailureEventHandler UpdateFailed;

        delegate void UpdateProgressChangedDelegate(string fileName, int filePercentage, int totalPercentage);
        delegate void FileDownloadCompletedDelegate(string archiveName);

        private const double DOT_TIME = 0.66;
        private const int MAX_DOTS = 5;

        public UpdateWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private XNALabel lblDescription;
        private XNALabel lblCurrentFileProgressPercentageValue;
        private XNALabel lblTotalProgressPercentageValue;
        private XNALabel lblCurrentFile;
        private XNALabel lblUpdaterStatus;

        private XNAProgressBar prgCurrentFile;
        private XNAProgressBar prgTotal;

        private bool isStartingForceUpdate;

        bool infoUpdated = false;

        string currFileName = string.Empty;
        int currFilePercentage = 0;
        int totalPercentage = 0;
        int dotCount = 0;
        double currentDotTime = 0.0;

        private static readonly object locker = new object();

        public override void Initialize()
        {
            Name = "UpdateWindow";
            ClientRectangle = new Rectangle(0, 0, 446, 270);
            BackgroundTexture = AssetLoader.LoadTexture("updaterbg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Text = string.Empty;
            lblDescription.ClientRectangle = new Rectangle(12, 9, 0, 0);
            lblDescription.Name = nameof(lblDescription);

            var lblCurrentFileProgressPercentage = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentage.Text = "Progress percentage of current file:".L10N("UI:Main:CurrentFileProgressPercentage");
            lblCurrentFileProgressPercentage.ClientRectangle = new Rectangle(12, 90, 0, 0);
            lblCurrentFileProgressPercentage.Name = nameof(lblCurrentFileProgressPercentage);

            lblCurrentFileProgressPercentageValue = new XNALabel(WindowManager);
            lblCurrentFileProgressPercentageValue.Text = "0%";
            lblCurrentFileProgressPercentageValue.ClientRectangle = new Rectangle(409, lblCurrentFileProgressPercentage.Y, 0, 0);
            lblCurrentFileProgressPercentageValue.Name = nameof(lblCurrentFileProgressPercentageValue);

            prgCurrentFile = new XNAProgressBar(WindowManager);
            prgCurrentFile.Name = nameof(prgCurrentFile);
            prgCurrentFile.Maximum = 100;
            prgCurrentFile.ClientRectangle = new Rectangle(12, 110, 422, 30);
            prgCurrentFile.SmoothForwardTransition = true;
            prgCurrentFile.SmoothTransitionRate = 10;

            lblCurrentFile = new XNALabel(WindowManager);
            lblCurrentFile.Name = nameof(lblCurrentFile);
            lblCurrentFile.ClientRectangle = new Rectangle(12, 142, 0, 0);

            var lblTotalProgressPercentage = new XNALabel(WindowManager);
            lblTotalProgressPercentage.Text = "Total progress percentage:".L10N("UI:Main:TotalProgressPercentage");
            lblTotalProgressPercentage.ClientRectangle = new Rectangle(12, 170, 0, 0);
            lblTotalProgressPercentage.Name = nameof(lblTotalProgressPercentage);

            lblTotalProgressPercentageValue = new XNALabel(WindowManager);
            lblTotalProgressPercentageValue.Text = "0%";
            lblTotalProgressPercentageValue.ClientRectangle = new Rectangle(409, lblTotalProgressPercentage.Y, 0, 0);
            lblTotalProgressPercentageValue.Name = nameof(lblTotalProgressPercentageValue);

            prgTotal = new XNAProgressBar(WindowManager);
            prgTotal.Name = nameof(prgTotal);
            prgTotal.Maximum = 100;
            prgTotal.ClientRectangle = new Rectangle(12, 190, prgCurrentFile.Width, prgCurrentFile.Height);

            lblUpdaterStatus = new XNALabel(WindowManager);
            lblUpdaterStatus.Name = nameof(lblUpdaterStatus);
            lblUpdaterStatus.Text = "Preparing".L10N("UI:Main:StatusPreparing");
            lblUpdaterStatus.ClientRectangle = new Rectangle(12, 240, 0, 0);

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.ClientRectangle = new Rectangle(301, 240, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblDescription);
            AddChild(lblCurrentFileProgressPercentage);
            AddChild(lblCurrentFileProgressPercentageValue);
            AddChild(prgCurrentFile);
            AddChild(lblCurrentFile);
            AddChild(lblTotalProgressPercentage);
            AddChild(lblTotalProgressPercentageValue);
            AddChild(prgTotal);
            AddChild(lblUpdaterStatus);
            AddChild(btnCancel);

            base.Initialize();
            CenterOnParent();

            Updater.FileIdentifiersUpdated += Updater_FileIdentifiersUpdated;
            Updater.OnUpdateCompleted += Updater_OnUpdateCompleted;
            Updater.OnUpdateFailed += Updater_OnUpdateFailed;
            Updater.UpdateProgressChanged += Updater_UpdateProgressChanged;
            Updater.LocalFileCheckProgressChanged += Updater_LocalFileCheckProgressChanged;
            Updater.OnFileDownloadCompleted += Updater_OnFileDownloadCompleted;
        }

        private void Updater_FileIdentifiersUpdated()
        {
            if (!isStartingForceUpdate)
                return;

            if (Updater.versionState == VersionState.UNKNOWN)
            {
                XNAMessageBox.Show(WindowManager, "Force Update Failure".L10N("UI:Main:ForceUpdateFailureTitle"), "Checking for updates failed.".L10N("UI:Main:ForceUpdateFailureText"));
                AddCallback(new Action(CloseWindow), null);
                return;
            }
            else if (Updater.versionState == VersionState.OUTDATED && Updater.ManualUpdateRequired)
            {
                UpdateCancelled?.Invoke(this, EventArgs.Empty);
                AddCallback(new Action(CloseWindow), null);
                return;
            }

            SetData(Updater.ServerGameVersion);
            isStartingForceUpdate = false;
            Updater.StartUpdate();
        }

        private void Updater_LocalFileCheckProgressChanged(int checkedFileCount, int totalFileCount)
        {
            AddCallback(new Action<int>(UpdateFileProgress), (checkedFileCount * 100 / totalFileCount));
        }

        private void UpdateFileProgress(int value)
        {
            prgCurrentFile.Value = value;
            lblCurrentFileProgressPercentageValue.Text = value + "%";
        }

        private void Updater_UpdateProgressChanged(string currFileName, int currFilePercentage, int totalPercentage)
        {
            lock (locker)
            {
                infoUpdated = true;
                this.currFileName = currFileName;
                this.currFilePercentage = currFilePercentage;
                this.totalPercentage = totalPercentage;
            }
        }

        private void HandleUpdateProgressChange()
        {
            if (!infoUpdated)
                return;

            infoUpdated = false;

            if (currFilePercentage < 0 || currFilePercentage > prgCurrentFile.Maximum)
                prgCurrentFile.Value = 0;
            else
                prgCurrentFile.Value = currFilePercentage;

            if (totalPercentage < 0 || totalPercentage > prgTotal.Maximum)
                prgTotal.Value = 0;
            else
                prgTotal.Value = totalPercentage;

            lblCurrentFileProgressPercentageValue.Text = prgCurrentFile.Value.ToString() + "%";
            lblTotalProgressPercentageValue.Text = prgTotal.Value.ToString() + "%";
            lblCurrentFile.Text = "Current file:".L10N("UI:Main:CurrentFile") + " " + currFileName;
            lblUpdaterStatus.Text = "Downloading files".L10N("UI:Main:DownloadingFiles");

            try
            {
                TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.Normal);
                TaskbarProgress.Instance.SetValue(prgTotal.Value, prgTotal.Maximum);
            }
            catch
            {
            }
        }

        private void Updater_OnFileDownloadCompleted(string archiveName)
        {
            AddCallback(new FileDownloadCompletedDelegate(HandleFileDownloadCompleted), archiveName);
        }

        private void HandleFileDownloadCompleted(string archiveName)
        {
            lblUpdaterStatus.Text = "Unpacking archive".L10N("UI:Main:UnpackingArchive");
        }

        private void Updater_OnUpdateCompleted()
        {
            AddCallback(new Action(HandleUpdateCompleted), null);
        }

        private void HandleUpdateCompleted()
        {
            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
            UpdateCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void Updater_OnUpdateFailed(string strMsg)
        {
            AddCallback(new Action<string>(HandleUpdateFailed), strMsg);
        }

        private void HandleUpdateFailed(string updateFailureErrorMessage)
        {
            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
            UpdateFailed?.Invoke(this, new UpdateFailureEventArgs(updateFailureErrorMessage));
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            if (!isStartingForceUpdate)
                Updater.StopUpdate();

            CloseWindow();
        }

        private void CloseWindow()
        {
            isStartingForceUpdate = false;
            TaskbarProgress.Instance.SetState(TaskbarProgress.TaskbarStates.NoProgress);
            UpdateCancelled?.Invoke(this, EventArgs.Empty);
        }

        public void SetData(string newGameVersion)
        {
            lblDescription.Text = string.Format(("Please wait while {0} is updated to version {1}.\nThis window will automatically close once the update is complete.\n\nThe client may also restart after the update has been downloaded.").L10N("UI:Main:UpdateVersionPleaseWait"), MainClientConstants.GAME_NAME_LONG, newGameVersion);
            lblUpdaterStatus.Text = "Preparing".L10N("UI:Main:StatusPreparing");
        }

        public void ForceUpdate()
        {
            isStartingForceUpdate = true;
            lblDescription.Text = string.Format("Force updating {0} to latest version...".L10N("UI:Main:ForceUpdateToLatest"), MainClientConstants.GAME_NAME_SHORT);
            lblUpdaterStatus.Text = "Connecting".L10N("UI:Main:UpdateStatusConnecting");
            Updater.CheckForUpdates();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            lock (locker)
            {
                HandleUpdateProgressChange();
            }

            currentDotTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (currentDotTime > DOT_TIME)
            {
                currentDotTime = 0.0;
                dotCount++;
                if (dotCount > MAX_DOTS)
                    dotCount = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            float xOffset = 3.0f;

            for (int i = 0; i < dotCount; i++)
            {
                var wrect = lblUpdaterStatus.RenderRectangle();
                Renderer.DrawStringWithShadow(".", lblUpdaterStatus.FontIndex,
                    new Vector2(wrect.Right + xOffset, wrect.Bottom - 15.0f), lblUpdaterStatus.TextColor);
                xOffset += 3.0f;
            }
        }
    }

    public class UpdateFailureEventArgs : EventArgs
    {
        public UpdateFailureEventArgs(string reason)
        {
            this.reason = reason;
        }

        string reason = String.Empty;

        /// <summary>
        /// The returned error message from the update failure.
        /// </summary>
        public string Reason
        {
            get { return reason; }
        }
    }
}