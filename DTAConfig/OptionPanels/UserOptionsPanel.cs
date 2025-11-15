using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ClientCore;
using ClientCore.CnCNet5;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.Entity;
using Localization;
using Localization.Tools;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Component = DTAConfig.Entity.Component;

namespace DTAConfig.OptionPanels
{
    public class UserOptionsPanel(WindowManager windowManager, UserINISettings iniSettings) : XNAOptionsPanel(windowManager, iniSettings)
    {

        private readonly List<XNAControl> UserControls = [], WorkshopControls = [];
        private XNAClientTabControl tabControl;
        private XNALinkLabel lblNameValue, lblcertify;
        private XNALabel lblIDValue, lblSideValue;
        private XNADropDown ddType, ddStatus;
        private XNAMultiColumnListBox mlbWorkshop;
        private XNASuggestionTextBox tbSearch;
        private XNAClientButton 出题按钮,出题记录按钮;
        private XNATextBlock btnImg,等级图标;
        private List<string> Sides = ["苏军","盟军","尤里","中立"];
        private List<Component> 该用户所有组件 = [];
        private List<Component> 筛选后组件 = [];
        private XNALinkLabel 徽章值;
        private XNADropDown dd徽章值;
        private XNAProgressBar 经验进度条;
        private XNALabel 经验值;
        private List<Badge> _徽章列表 = [];

        public List<Badge> 可选的徽章
        {
            get => _徽章列表;
            set
            {
                _徽章列表.Clear();  // 先清空当前的徽章列表
                if (value != null)
                {
                    dd徽章值.Items.Clear();
                    value.ForEach(v =>
                    {
                        var item = new XNADropDownItem()
                        {
                            Text = v.name,
                            Tag = v.id
                        };
                        dd徽章值.AddItem(item);
                    });  // 这里可以添加到你的dd徽章值对象
                    dd徽章值.SelectedIndex = dd徽章值.Items.FindIndex(i => i.Text == 徽章值.Text);
                    _徽章列表.AddRange(value);  // 同时添加到内部字段中
                }
            }
        }

        public override void Initialize()
        {
            Name = "UserOptionsPanel";
            base.Initialize();

            tabControl = new XNAClientTabControl(WindowManager)
            {
                Name = "tabControl",
                ClientRectangle = new Rectangle(12, 10, 0, 0),
                ClickSound = new EnhancedSoundEffect("button.wav"),
                FontIndex = 1,
            };

            tabControl
                .AddTab("Profile".L10N("UI:DTAConfig:MyProfile"), UIDesignConstants.BUTTON_WIDTH_133)
                .AddTab("Workshop".L10N("UI:DTAConfig:MyWorkshop"), UIDesignConstants.BUTTON_WIDTH_133)
                .SelectedIndexChanged += Tab切换事件;

            AddChild(tabControl);

            #region 我的资料
            btnImg = new XNATextBlock(WindowManager)
            {
                ClientRectangle = new Rectangle(30, 60, 125, 125),
                DrawBorders = false
            };

            var lblID = new XNALabel(WindowManager)
            {
                Text = "Account:".L10N("UI:DTAConfig:WorkshopAccount"),
                ClientRectangle = new Rectangle(btnImg.X + 150, btnImg.Y, 0, 0),
                FontIndex = 0,
            };

            lblIDValue = new XNALabel(WindowManager)
            {
                Text = string.Empty,
                ClientRectangle = new Rectangle(btnImg.Right + 70, lblID.Y, 0, 0),
                FontIndex = 0,
            };

            var lblName = new XNALabel(WindowManager)
            {
                Text = "NickName:".L10N("UI:DTAConfig:WorkshopNickName"),
                ClientRectangle = new Rectangle(btnImg.X + 150, lblID.Y + 40, 0, 0),
                FontIndex = 0,
            };

            lblNameValue = new XNALinkLabel(WindowManager)
            {
                Text = "Click Sign in or Register".L10N("UI:DTAConfig:SignInOrRegister"),
                ClientRectangle = new Rectangle(btnImg.Right + 70, lblName.Y, 0, 0),
                FontIndex = 0,
            };

            var lbl徽章 = new XNALabel(WindowManager)
            {
                Text = "Badge:".L10N("UI:DTAConfig:Badge"),
                ClientRectangle = new Rectangle(lblName.X, lblName.Y + 40, 0, 0),
                FontIndex = 0,
            };

            徽章值 = new XNALinkLabel(WindowManager)
            {
                Text = "",
                ClientRectangle = new Rectangle(btnImg.Right + 70, lbl徽章.Y, 0, 0),
                FontIndex = 0,
            };


            dd徽章值 = new XNADropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(btnImg.Right + 70, lbl徽章.Y, 100, 20),
                FontIndex = 0,
                Visible = false,
            };
            dd徽章值.RightClick += 反转徽章显示状态;


            等级图标 = new XNATextBlock(WindowManager)
            {
                ClientRectangle = new Rectangle(徽章值.Right + 100, 徽章值.Y, 34, 17),
                BackgroundTexture = AssetLoader.LoadTextureUncached("chat_ic_lv0.png"),
                DrawBorders = false,
            };

            经验进度条 = new XNAProgressBar(WindowManager)
            {
                ClientRectangle = new Rectangle(等级图标.Right + 14, 等级图标.Y, 100, 17),
            };

            经验值 = new XNALabel(WindowManager)
            {
                Text = "",
                ClientRectangle = new Rectangle(经验进度条.Right + 10, 经验进度条.Y, 0, 0),
                FontIndex = 0,
            };

            var lblSide = new XNALabel(WindowManager)
            {
                Text = "Camp:".L10N("UI:DTAConfig:Camp"),
                ClientRectangle = new Rectangle(btnImg.X + 150, lbl徽章.Y + 40, 0, 0),
                FontIndex = 0,
            };

            lblSideValue = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(btnImg.Right + 70, lblSide.Y, 0, 0),
                FontIndex = 0,
            };

            lblcertify = new XNALinkLabel(WindowManager)
            {
                Text = "Click here for certification".L10N("UI:DTAConfig:Certification"),
                ClientRectangle = new Rectangle(lblName.X, lblSide.Bottom + 40, 0, 0),
            };
            lblcertify.LeftClick += 跳转答题窗口;

            出题按钮 = new XNAClientButton(WindowManager)
            {
                Text = "I'll come up with the question".L10N("UI:DTAConfig:ComeUpQuestion"),
                X = 等级图标.X,
                Y = lblcertify.Y - 5,
                Enabled = false
            };
            出题按钮.LeftClick += 跳转出题窗口;

            出题记录按钮 = new XNAClientButton(WindowManager)
            {
                Text = "Transcript of the question".L10N("UI:DTAConfig:TranscriptQuestion"),
                X = 出题按钮.Right + 40,
                Y = 出题按钮.Y,
                Enabled = false
            };
            出题记录按钮.LeftClick += 跳转出题记录窗口;

            UserControls.AddRange([btnImg, lblID, lblSide, lblName, lblIDValue, lbl徽章, 徽章值, dd徽章值, 等级图标, 经验进度条, 经验值, lblNameValue, lblSideValue, lblcertify, 出题按钮, 出题记录按钮]);

            AddChild(UserControls);

            #endregion

            #region 我的工坊
            var lblType = new XNALabel(WindowManager)
            {
                Text = "Type".L10N("UI:DTAConfig:Type"),
                ClientRectangle = new Rectangle(40, 60, 0, 0),
                Visible = false
            };

            ddType = new XNADropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(lblType.Right + 50, lblType.Y, 70, 30),
                Visible = false,
                SelectedIndex = 0
            };

            添加组件类型筛选();

            _ = Task.Run(async () => { Sides = await 获取所有阵营类型(); });

            var lblState = new XNALabel(WindowManager)
            {
                Text = "Status".L10N("UI:DTAConfig:Status"),
                ClientRectangle = new Rectangle(ddType.Right + 30, ddType.Y, 0, 0),
                Visible = false
            };

            ddStatus = new XNADropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(lblState.Right + 50, lblState.Y, 70, 30),
                Visible = false,
                SelectedIndex = 0
            };

            ddStatus.AddItem("All".L10N("UI:DTAConfig:All"));
            ddStatus.AddItem("Passed".L10N("UI:DTAConfig:Passed"));
            ddStatus.AddItem("Failed".L10N("UI:DTAConfig:Failed"));

            ddStatus.SelectedIndexChanged += 筛选;

            tbSearch = new XNASuggestionTextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(ddStatus.Right + 30, ddStatus.Y, 130, 25),
                Visible = false
            };

            var btnSearch = new XNAClientButton(WindowManager)
            {
                Text = "搜索",
                ClientRectangle = new Rectangle(tbSearch.Right + 20, tbSearch.Y - 2, UIDesignConstants.BUTTON_WIDTH_75, UIDesignConstants.BUTTON_HEIGHT),
                Visible = false
            };
            btnSearch.LeftClick += 筛选;

            var btnUpload = new XNAClientButton(WindowManager)
            {
                Text = "上传".L10N("UI:DTAConfig:ButtonUpload"),
                ClientRectangle = new Rectangle(btnSearch.Right + 20, tbSearch.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
                Visible = false,
            };
            btnUpload.LeftClick += 跳转上传窗口;

            mlbWorkshop = new XNAMultiColumnListBox(WindowManager)
            {
                ClientRectangle = new Rectangle(lblType.X, lblType.Y + 45, 650, 260),
                Visible = false
            };

            mlbWorkshop
                .AddColumn("名称", 225)
                .AddColumn("标签", 100)
                .AddColumn("最后更新时间", 200)
                .AddColumn("下载次数", 100);

            var _menu = new XNAContextMenu(WindowManager);
            _menu.Name = nameof(_menu);
            _menu.Width = 100;

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "Refresh".L10N("UI:DTAConfig:Refresh"),
                SelectAction = () =>
                {
                    _ = 获取所有阵营类型();
                    获取该用户所有组件();
                }
            });

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "Edit".L10N("UI:DTAConfig:Edit"),
                SelectAction = 编辑
            });

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "Delete".L10N("UI:DTAConfig:Delete"),
                SelectAction = 删除
            });


            AddChild(_menu);

            mlbWorkshop.RightClick += (_, _) =>
            {
                mlbWorkshop.SelectedIndex = mlbWorkshop.HoveredIndex;
                _menu.Open(GetCursorPoint());
            };

            WorkshopControls.AddRange([lblType, ddType, lblState, ddStatus, tbSearch, btnSearch, btnUpload, mlbWorkshop]);
            AddChild(WorkshopControls);


            登录();
            #endregion

            #region 函数


            void 添加组件类型筛选()
            {
                Task.Run(async () =>
                {
                    var Types = (await NetWorkINISettings.Get<string>("dict/getValue?section=component&key=type")).Item1?.Split(",") ?? [];

                    ddType.Items.Clear();

                    ddType.AddItem("所有");

                    foreach (var item in Types)
                    {
                        ddType.AddItem(item);
                    }
                    if (Types.Length > 0)
                    {
                        ddType.SelectedIndex = 0;
                        ddType.SelectedIndexChanged += 筛选;
                    }
                });
            }
            #endregion
        }

        private void 更新徽章值(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                var user = UserINISettings.Instance.User;

                if (user == null || dd徽章值.SelectedItem == null) return;
                await NetWorkINISettings.Post<bool?>("user/updUserBadge", new BadgeDto()
                {
                    userId = user.id,
                    badgeId = (string)dd徽章值.SelectedItem.Tag
                });
                徽章值.Text = dd徽章值.SelectedItem.Text;
            反转徽章显示状态(null, null);
            });
            
        }

        private void 反转徽章显示状态(object sender, EventArgs e)
        {
            (dd徽章值.Visible, 徽章值.Visible) = (徽章值.Visible, dd徽章值.Visible);
        }

        private void 登录()
        {
            Task.Run(async () =>
            {
                var user = await 使用缓存Token登录();
                if (user != null) 登录成功(user);
                else
                {
                    退出登录();
                }

            });

        }

#nullable enable
        static async Task<User?> 使用缓存Token登录()
        {
            var token = UserINISettings.Instance.Token.Value;
            if (!string.IsNullOrEmpty(token))
            {
                var tokenVo = (await NetWorkINISettings.Post<TokenVo>("user/loginByToken", token)).Item1;

                if (tokenVo != default)
                {
                    return tokenVo.user;
                }
            }
            return null;
        }
#nullable disable

        private static async Task<List<string>> 获取所有阵营类型()  => (await NetWorkINISettings.Get<string>("dict/getValue?section=user&key=side")).Item1?.Split(",").ToList() ?? [];
        
        private void 跳转编辑用户窗口(object sender, EventArgs e)
        {

            Task.Run(async () => {

                if (Sides.Count == 0)
                {
                    lblNameValue.Enabled = false;
                    Sides = await 获取所有阵营类型();
                    lblNameValue.Enabled = true;
                    if (Sides.Count == 0)
                    {
                        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Failed to get faction type".L10N("UI:DTAConfig:FailedGetFactionType"));
                        return;
                    }
                }
                var editWindow = new 编辑窗口(WindowManager, Sides);
                editWindow.退出登录 += 退出登录;
                editWindow.重新登录 += () => {
                    登录();
                };

                DarkeningPanel.AddAndInitializeWithControl(WindowManager, editWindow);

            });

        }

        private void 退出登录()
        {
            lblNameValue.Text = string.Empty;

            lblIDValue.Text = string.Empty;
            lblSideValue.Text = string.Empty;
            lblNameValue.Text = "点击登录或注册";
            lblNameValue.LeftClick += 跳转登录窗口;
            lblNameValue.LeftClick -= 跳转编辑用户窗口;
            出题按钮.Enabled = false;
            出题记录按钮.Enabled = false;
            dd徽章值.SelectedIndexChanged -= 更新徽章值;
            徽章值.LeftClick -= 反转徽章显示状态;
            徽章值.Visible = false;
            等级图标.BackgroundTexture = AssetLoader.LoadTexture($"chat_ic_lv0.png");
            等级图标.Visible = false;
            经验进度条.Visible = false;
            经验值.Text = string.Empty;
            btnImg.Visible = false;
            lblcertify.Text = "点此认证";
            lblcertify.Enabled = false;
            UserINISettings.Instance.Token.Value = string.Empty;
            UserINISettings.Instance.SaveSettings();

            tabControl.MakeUnselectable(1);
        }

        private void 筛选(object sender, EventArgs e)
        {
            筛选后组件 = 该用户所有组件.FindAll(c =>
            (ddType.SelectedIndex == 0 || c.type == ddType.SelectedIndex - 1) &&
            (ddStatus.SelectedIndex == 0 || (c.passTime == string.Empty) == (ddStatus.SelectedIndex == 2)) &&
            (tbSearch.Text == string.Empty || c.name.Contains(tbSearch.Text))
            );

            将组件列表显示在界面上();
        }
        
        private void 获取该用户所有组件()
        {
            Task.Run(() =>
            {
                该用户所有组件 = NetWorkINISettings.Get<List<Component>>("component/getAllComponent?" +
                                    $"id={UserINISettings.Instance.User.id}")
                                    .GetAwaiter().GetResult().Item1 ?? [];
                筛选(null, null);
            });
        }

        private void 将组件列表显示在界面上()
        {
            mlbWorkshop.ClearItems();
            筛选后组件.ForEach(component =>
        {

            string[] item = [component.name, component.typeName, component.uploadTime, component.downCount.ToString()];
            mlbWorkshop.AddItem(item, true);
        });
        }

        private void Tab切换事件(object sender, EventArgs e)
        {
 
            List<XNAControl> list = [徽章值, dd徽章值, 等级图标, 经验进度条, 经验值];

            if (tabControl.SelectedTab == 1)
            {
                RemoveChild(list);
            }
            else
            {
                AddChild(list);
            }

            List<XNAControl> allControls = [.. UserControls, .. WorkshopControls];
            allControls.ForEach(c => c.Visible = !c.Visible);

            //UserControls.ForEach(c => c.Visible = tabControl.SelectedTab == 0);
            //WorkshopControls.ForEach(c => c.Visible = tabControl.SelectedTab != 0);

        }

        private void 跳转上传窗口(object sender, EventArgs e)
        {
            //XNAMessageBox.Show(WindowManager, "信息", "该模块正在修改中，计划采用网页上传，请等待。");
            //return;
#if DEBUG
            FunExtensions.OpenUrl("http://localhost:83/workshop/submit/upload?token=" + UserINISettings.Instance.Token);
#else
            FunExtensions.OpenUrl("https://creator.yra2.com/workshop/submit/upload?token=" + UserINISettings.Instance.Token);
#endif
            return;

            if (lblNameValue.Text == "点击登录或注册")
            {
                tabControl.SelectedTab = 0;
                lblNameValue.OnLeftClick();
                return;
            }

            if(lblcertify.Text == "点此认证")
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "You have not passed the answer verification and do not have upload permissions".L10N("UI:DTAConfig:NotPassedAnswerVerificationUpload"));
                return;
            }

            var uploadWindow = new UploadWindow(WindowManager);

            uploadWindow.EnabledChanged += (_,_) =>
            {
                if (!uploadWindow.Enabled && uploadWindow.Uploaded)
                {
                    uploadWindow.Uploaded = false;
                    获取该用户所有组件();
                }
            }
            ;

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, uploadWindow);
        }

        private void 编辑()
        {
            if (mlbWorkshop.SelectedIndex == -1) return;

            var uploadWindow = new UploadWindow(WindowManager, 筛选后组件[mlbWorkshop.SelectedIndex]);

            uploadWindow.EnabledChanged += (_, _) =>
            {
                if (!uploadWindow.Enabled && uploadWindow.Uploaded)
                {
                    uploadWindow.Uploaded = false;
                    获取该用户所有组件();
                }
            };

            DarkeningPanel.AddAndInitializeWithControl(WindowManager, uploadWindow);

        }
        private void 删除()
        {
            if (mlbWorkshop.SelectedIndex == -1) return;

            var c = 筛选后组件[mlbWorkshop.SelectedIndex];
            var messageBox = new XNAMessageBox(WindowManager, "Tips".L10N("UI:Main:Tips"), $"您确定要删除此组件?\n{c.name}", XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) =>
            {

                Task.Run(async () =>
                {
                    var r = await NetWorkINISettings.Post<bool>($"component/delComponent", c);
                    if (r.Item1)
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Deleted successfully!".L10N("UI:DTAConfig:DeletedSuccessfully"));
                    }
                    else
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), $"删除失败!原因: {r.Item2}");
                    }
                    获取该用户所有组件();
                });
            };
            messageBox.Show();
        }

        private void 跳转答题窗口(object sender, EventArgs e)
        {
            if(lblNameValue.Text == "点击登录或注册")
            {
                lblNameValue.OnLeftClick();
                XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), "Please login before authenticating".L10N("UI:DTAConfig:LoginAuthenticating"));
                return;
            }
            lblcertify.Enabled = false;
            var questionTypes = NetWorkINISettings.Get<string>("dict/getValue?section=question&key=type").GetAwaiter().GetResult().Item1?.Split(",") ?? [];
            lblcertify.Enabled = true;
            if (questionTypes.Length == 0)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Failed to get question bank".L10N("UI:DTAConfig:FailedGetQuestion"));
                return;
            }

            var msgWindow = new 创作者认证答题须知窗口(WindowManager, questionTypes);
            msgWindow.pass += 登录;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, msgWindow);
        }

        private void 跳转登录窗口(object sender, EventArgs e)
        {
            
            Task.Run(async () => {
                
                if (Sides.Count == 0)
                {
                    lblNameValue.Enabled = false;
                    Sides = await 获取所有阵营类型();
                    lblNameValue.Enabled = true;
                    if (Sides.Count == 0)
                    {
                        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Failed to get faction type".L10N("UI:DTAConfig:FailedGetFactionType"));
                        return;
                    }
                }
                var loginWindow = new 登录窗口(WindowManager, Sides);
                loginWindow.loginSuss += 登录成功;
                DarkeningPanel.AddAndInitializeWithControl(WindowManager, loginWindow);
                
                
            });
        }

        private void 跳转出题窗口(object sender, EventArgs e)
        {
            var questionTypes = NetWorkINISettings.Get<string>("dict/getValue?section=question&key=type").GetAwaiter().GetResult().Item1?.Split(",") ?? [];
            if (questionTypes.Length == 0)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Failed to get question bank".L10N("UI:DTAConfig:FailedGetQuestion"));
                return;
            }
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, new 出题窗口(WindowManager, questionTypes));
        }

        private void 跳转出题记录窗口(object sender, EventArgs e) => DarkeningPanel.AddAndInitializeWithControl(WindowManager, new 出题记录窗口(WindowManager));

        private void 登录成功(User user)
        {
            if ("0" == user.id && null == user.username)
                return;

            Task.Run(() =>
            {
                var badge = NetWorkINISettings.Get<BadgeVo>($"user/getUserExp?userId={user.id}").GetAwaiter().GetResult().Item1;
                if (badge != null)
                {
                    徽章值.Text = badge.badgeName;
                    经验进度条.Maximum = badge.nextLevelExp;
                    经验进度条.Value = badge.exp;
                    经验值.Text = $"{badge.exp}/{badge.nextLevelExp}";
                    可选的徽章 = badge.canUseBadges;
                    等级图标.BackgroundTexture = AssetLoader.LoadTexture($"chat_ic_lv{badge.level}.png");
                    徽章值.Visible = true;
                    等级图标.Visible = true;
                    经验进度条.Visible = true;
                    徽章值.LeftClick -= 反转徽章显示状态;
                    徽章值.LeftClick += 反转徽章显示状态;
                    dd徽章值.SelectedIndexChanged -= 更新徽章值;
                    dd徽章值.SelectedIndexChanged += 更新徽章值;
                }
            });

            UserINISettings.Instance.User = user;
            lblIDValue.Text = user.id.ToString();
            lblNameValue.Text = user.username;
           
            if(null != Sides && user.side < Sides.Count)
                lblSideValue.Text = Sides[user.side];
            if (!string.IsNullOrEmpty(user.role))
            {
                lblcertify.Text = "已通过答题";
                lblcertify.Enabled = false;
                出题按钮.Enabled = true;
                出题记录按钮.Enabled = true;
            }
            else
            {
                lblcertify.Text = "点此认证";
                出题按钮.Enabled = false;
                出题记录按钮.Enabled = false;
                lblcertify.Enabled = true;
            }
            btnImg.Visible = true;
            var s = user.side switch
            {
                0 => "Soviet",
                1 => "Allied",
                2 => "Yuri",
                3 => "中立",
                _ => "中立",
            };
            btnImg.BackgroundTexture = AssetLoader.LoadTexture($"Resources/{s}.png");
            tabControl.MakeSelectable(1);
            lblNameValue.LeftClick -= 跳转登录窗口;
            lblNameValue.LeftClick -= 跳转编辑用户窗口;
            lblNameValue.LeftClick += 跳转编辑用户窗口;
            获取该用户所有组件();
        }
    }

    public class 用户信息窗口(WindowManager windowManager, List<string> sides) : XNAWindow(windowManager)
    {
        public XNALabel lblConnectToCnCNet;
        public XNATextBox tbPlayerName;
        public XNAPasswordBox tbPassword;
        public XNAPasswordBox tbPassword2;
        public XNATextBox tbEmail;
        public XNATextBox tbCode;
        public XNADropDown ddSide;
        public XNALinkLabel linkLabel;
        public XNALinkLabel ResetLabel;
        public XNAClientButton btnLogin;
        public XNALabel lblPlayerName, lblPassword, lblPassword2 , lblEmail, lblCode,lblSide;
        public XNAClientButton btnCancel;
        public XNAClientButton btnSign;

        public List<XNAControl> dyControls;
        public override void Initialize()
        {
         
            ClientRectangle = new Rectangle(0, 0, 400, 290);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            lblConnectToCnCNet = new XNALabel(WindowManager)
            {
                Name = "lblConnectToCnCNet",
                FontIndex = 1,
                Text = "登录",
                ClientRectangle = new Rectangle(180, 15, 0, 0)
            };

            tbPlayerName = new XNATextBox(WindowManager)
            {
                Name = "tbPlayerName",
                ClientRectangle = new Rectangle(Width - 270, 50, 160, 19),
                // MaximumTextLength = ClientConfiguration.Instance.MaxNameLength
            };

            lblPlayerName = new XNALabel(WindowManager)
            {
                Name = "lblPlayerName",
                FontIndex = 1,
                Text = "账号/邮箱:".L10N("Client:Main:PlayerName"),
                ClientRectangle = new Rectangle(30, tbPlayerName.Y + 1, 0, 0)
            };

            tbPassword = new XNAPasswordBox(WindowManager)
            {
                Name = "tbPassword",
                ClientRectangle = new Rectangle(Width - 270, 80, 160, 19),
                MaximumTextLength = ClientConfiguration.Instance.MaxNameLength
            };

            lblPassword = new XNALabel(WindowManager)
            {
                Name = "lblPassword",
                FontIndex = 1,
                Text = "密码:".L10N("Client:Main:Password"),
                ClientRectangle = new Rectangle(30, tbPassword.Y + 1,
                   0, 0)
            };

            tbPassword2 = new XNAPasswordBox(WindowManager)
            {
                Name = "tbPassword2",
                ClientRectangle = new Rectangle(Width - 270, 110, 160, 19),
                MaximumTextLength = ClientConfiguration.Instance.MaxNameLength,
                Visible = false
            };

            lblPassword2 = new XNALabel(WindowManager)
            {
                Name = "lblPassword2",
                FontIndex = 1,
                Text = "确认密码:".L10N("Client:Main:Password2"),
                ClientRectangle = new Rectangle(30, tbPassword2.Y + 1,
                    0, 0),
                Visible = false
            };

            tbEmail = new XNATextBox(WindowManager)
            {
                Name = "tbEmail",
                ClientRectangle = new Rectangle(Width - 270, 140, 160, 19),
                Visible = false
            };

            linkLabel = new XNALinkLabel(WindowManager)
            {
                Name = "linkLabel",
                Text = "发送验证码",
                ClientRectangle = new Rectangle(tbEmail.X + tbEmail.Width + 20, 140, 75, 14),
                Visible = false
            };
          

            ResetLabel = new XNALinkLabel(WindowManager)
            {
                Name = "ResetLabel",
                Text = "重置密码",
                ClientRectangle = new Rectangle(tbEmail.X + tbEmail.Width + 20, 80, 50, 14)
            };
          
            lblEmail = new XNALabel(WindowManager)
            {
                Name = "lblEmail",
                FontIndex = 1,
                Text = "邮箱:".L10N("Client:Main:Email"),
                ClientRectangle = new Rectangle(30, tbEmail.Y + 1,
                    0, 0),
                Visible = false
            };

            tbCode = new XNATextBox(WindowManager)
            {
                Name = "tbCode",
                ClientRectangle = new Rectangle(Width - 270, 170, 160, 19),
                MaximumTextLength = 6,
                Visible = false
            };

            lblCode = new XNALabel(WindowManager)
            {
                Name = "lblCode",
                FontIndex = 1,
                Text = "验证码:".L10N("Client:Main:Code"),
                ClientRectangle = new Rectangle(30, tbCode.Y + 1, 0, 0),
                Visible = false
            };

            ddSide = new XNADropDown(WindowManager)
            {
                Name = "ddSide",
                ClientRectangle = new Rectangle(Width - 270, 200, 160, 19),
                Visible = false
            };

            sides.ForEach(s => ddSide.AddItem(s));
            if (sides.Count > 3) ddSide.SelectedIndex = 3;

            lblSide = new XNALabel(WindowManager)
            {
                Name = "lblSide",
                FontIndex = 1,
                Text = "加入阵营:",
                ClientRectangle = new Rectangle(30, ddSide.Y + 1, 0, 0),
                Visible = false
            };

            btnLogin = new XNAClientButton(WindowManager)
            {
                Name = "btnLogin",
                ClientRectangle = new Rectangle(12, Height - 35, 110, 23),
                Text = "登录".L10N("Client:Main:ButtonConnect")
            };
           
            btnSign = new XNAClientButton(WindowManager)
            {
                Name = "btnSign",
                Text = "注册".L10N("Client:Main:Sign"),
                ClientRectangle = new Rectangle((Width - 110) / 2, btnLogin.Y, 110, 23)
            };
           

            btnCancel = new XNAClientButton(WindowManager)
            {
                Name = "btnCancel",
                ClientRectangle = new Rectangle(Width - 122, btnLogin.Y, 110, 23),
                Text = "取消".L10N("Client:Main:ButtonCancel")
            };

            btnCancel.LeftClick += BtnCancel_LeftClick;

            dyControls = [lblPassword2, tbPassword2, lblEmail, tbEmail, lblCode, linkLabel, tbCode, ddSide, lblSide, ResetLabel, btnSign];

            AddChild([lblConnectToCnCNet, tbPlayerName, lblPlayerName, tbPassword, lblPassword, btnLogin, btnCancel, .. dyControls]);

            base.Initialize();

            CenterOnParent();
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BtnCancel_LeftClick(object sender, EventArgs e) => Disable();
    }

    public partial class 登录窗口(WindowManager windowManager,List<string> sides) : 用户信息窗口(windowManager, sides)
    {

        public Action<User> loginSuss;
        private string verificationCode;
        private System.Timers.Timer timer;
        private int countdownSeconds = 60;

        public override void Initialize()
        {
            Name = "登录窗口";

            base.Initialize();

            linkLabel.LeftClick += LinkLabel_LeftClick;

            ResetLabel.LeftClick += ResetLabel_LeftClick;

            btnLogin.LeftClick += BtnConnect_LeftClick;

            btnSign.LeftClick += BtnSign_LeftClick;

            CenterOnParent();
        }

        /// <summary>
        /// 跳转到重置功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetLabel_LeftClick(object sender, EventArgs e)
        {
            lblConnectToCnCNet.Text = "重置密码";
            Reversal();
            lblPlayerName.Visible = false;
            tbPlayerName.Visible = false;
            btnLogin.Text = "重置";
            lblSide.Visible = false;
            ddSide.Visible = false;
            btnLogin.LeftClick -= Sign;
            btnLogin.LeftClick -= BtnConnect_LeftClick;
            btnLogin.LeftClick += Reset;
            btnCancel.LeftClick -= BtnCancel_LeftClick;
            btnCancel.LeftClick += SignCanel;
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnect_LeftClick(object sender, EventArgs e)
        {
            var name = tbPlayerName.Text;
            var pwd = tbPassword.Password;
            //登录
           var (token,msg) = NetWorkINISettings.Post<TokenVo>("user/login", new LoginDto()
            {
                name = name,
                pwd = pwd
            }).GetAwaiter().GetResult();

            if(token == null)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), msg);
                return;
            }

            UserINISettings.Instance.Token.Value = token.access_token;

            XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Login successful!".L10N("UI:DTAConfig:LoginSuccessful"));

            UserINISettings.Instance.SaveSettings();

            loginSuss(token.user);
            Disable();
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset(object sender, EventArgs e)
        {
            var checkString = CheckInPut(true);
            if (checkString != null)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), checkString);
                return;
            }

            
            var (r, msg) = NetWorkINISettings.Post<bool>("user/reset", new User()
            {
                email = tbEmail.Text,
                password = tbPassword.Password,
            }).GetAwaiter().GetResult();

            if (!r)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), msg);
                return;
            }

            XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Reset successful!".L10N("UI:DTAConfig:ResetSuccessful"));
            btnCancel.OnLeftClick();
        }

        /// <summary>
        /// 点击发送验证码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LinkLabel_LeftClick(object sender, EventArgs e)
        {

            if (!tbEmail.Text.IsValidEmail())
            {
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "The email format is incorrect!".L10N("UI:DTAConfig:EmailFormatIncorrect"));
                return;
            }

            //发送验证码
            var (code, msg) = (string.Empty,string.Empty);
            timer = new(1000);
            
            timer.Elapsed += new ElapsedEventHandler(Timer_Tick);
            Task.Run(() =>
            {
                if (lblConnectToCnCNet.Text == "注册")
                    (code, msg) = NetWorkINISettings.Get<string>($"user/getSignCode?email={tbEmail.Text.Trim()}").GetAwaiter().GetResult();
                else
                    (code, msg) = NetWorkINISettings.Get<string>($"user/getResetCode?email={tbEmail.Text.Trim()}").GetAwaiter().GetResult();

                if (code == null)
                {
                    XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), msg);
                    return;
                }
                // 禁用 LinkLabel 防止重复点击
                linkLabel.Enabled = false;

                verificationCode = code;

                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Send successfully!".L10N("UI:DTAConfig:SendSuccessfully"));

                // 初始化倒计时
                countdownSeconds = 60;
                // 启动计时器
                timer.Start();
            });

        }

        /// <summary>
        /// 验证码发送倒计时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;

            if (countdownSeconds > 0)
            {
                // 更新 LinkLabel 显示倒计时
                linkLabel.Text = $"{countdownSeconds} 秒后重发";
            }
            else
            {
                // 倒计时结束，恢复 LinkLabel 文本并启用
                linkLabel.Text = "发送验证码";
                linkLabel.Enabled = true;

                // 停止计时器
                timer.Stop();
            }
        }


        /// <summary>
        /// 检查输入是否合法
        /// </summary>
        /// <returns></returns>
        private string CheckInPut(bool 是重置 = false)
        {
            if (string.IsNullOrEmpty(tbPassword.Password)) return "密码不能为空";

            if (tbPassword.Password != tbPassword2.Password) return "两次密码不一致";

            if (string.IsNullOrEmpty(verificationCode)) return "请先发送验证码";

            if (verificationCode != tbCode.Text) return "验证码不正确";

            if (tbPlayerName.Text.Length == 0 && !是重置) return "用户名不能为空";
              
            var s = NameValidator.IsNameValid(tbPlayerName.Text);
            if(!是重置 &&s != null) return s;

            return null;

        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sign(object sender, EventArgs e)
        {
            var checkString = CheckInPut();
            if(checkString != null)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), checkString);
                return;
            }

            //注册
            var (r,msg) = NetWorkINISettings.Post<bool?>("user/register", new User()
            {
                username = tbPlayerName.Text,
                password = tbPassword.Password,
                email = tbEmail.Text,
                side = ddSide.SelectedIndex
            }).GetAwaiter().GetResult();

            if(r != true) {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), msg);
                return;
            }

            XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Registration is successful".L10N("UI:DTAConfig:RegistrationSuccessful"));
            btnCancel.OnLeftClick();

        }

        /// <summary>
        /// 跳转注册
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSign_LeftClick(object sender, EventArgs e)
        {
            lblConnectToCnCNet.Text = "注册";
            Reversal();
            lblPlayerName.Text = "用户名";
            btnLogin.Text = "注册";
            lblSide.Visible = true;
            ddSide.Visible = true;
            btnLogin.LeftClick -= BtnConnect_LeftClick;
            btnLogin.LeftClick -= Reset;
            btnLogin.LeftClick += Sign;
            btnCancel.LeftClick -= BtnCancel_LeftClick;
            btnCancel.LeftClick += SignCanel;
        }

        /// <summary>
        /// 返回，跳转登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignCanel(object sender, EventArgs e)
        {
            lblConnectToCnCNet.Text = "登录";
            Reversal();
            lblPlayerName.Visible = true;
            tbPlayerName.Visible = true;
            btnLogin.Text = "登录";
            lblSide.Visible = false;
            ddSide.Visible = false;
            lblPlayerName.Text = "用户";
            btnLogin.LeftClick -= Sign;
            btnLogin.LeftClick -= Reset;
            btnLogin.LeftClick += BtnConnect_LeftClick;
            btnCancel.LeftClick -= SignCanel;
            btnCancel.LeftClick += BtnCancel_LeftClick;
        }

        /// <summary>
        /// 反转状态
        /// </summary>
        private void Reversal() => dyControls.ForEach(x => x.Visible = !x.Visible);

        //private static readonly Regex 用户名规则实例 = new Regex(@"^[^\d@][^ @]*$", RegexOptions.Compiled);

        //public static Regex 用户名规则()
        //{
        //    return 用户名规则实例;
        //}
    }

    public class 编辑窗口(WindowManager windowManager, List<string> sides) : 用户信息窗口(windowManager, sides)
    {

       public  Action 退出登录;
        public Action 重新登录;

        public override void Initialize()
        {
            Name = "编辑窗口";
            base.Initialize();
            dyControls.ForEach(c => { c.Visible = true; });

            lblConnectToCnCNet.Text = "编辑";

            

            lblEmail.Visible = false;
            lblCode.Visible = false;
            tbCode.Visible = false;
            linkLabel.Visible = false;
            tbEmail.Visible = false;

            ResetLabel.Text = "退出登录";
            ResetLabel.LeftClick += 退出登录事件;

            btnLogin.Text = "修改";
            修改信息事件(null, null);
            ddSide.SelectedIndex = UserINISettings.Instance.User.side;
            tbPlayerName.Text = UserINISettings.Instance.User.username;

        }

        private void 修改密码事件(object sender, EventArgs e)
        {
            lblPlayerName.Text = "原密码:";
            lblPassword.Visible = true;
            lblPassword2.Visible = true;
            tbPassword.Visible = true;
            tbPassword2.Visible = true;
            ResetLabel.Visible = false;
            lblSide.Visible = false;
            ddSide.Visible = false;

            btnSign.Text = "修改信息";
            btnSign.LeftClick -= 修改密码事件;
            btnSign.LeftClick += 修改信息事件;

            btnLogin.LeftClick -= 修改信息;
            btnLogin.LeftClick += 修改密码;
        }

        private void 修改信息事件(object sender, EventArgs e)
        {
            lblPlayerName.Text = "用户名:";
            lblPassword.Visible = false;
            lblPassword2.Visible = false;
            tbPassword.Visible = false;
            tbPassword2.Visible = false;
            lblSide.Visible = true;
            ddSide.Visible = true;
            ResetLabel.Visible = true;

            btnSign.Text = "修改密码";
            btnSign.LeftClick -= 修改信息事件;
            btnSign.LeftClick += 修改密码事件;

            btnLogin.LeftClick -= 修改密码;
            btnLogin.LeftClick += 修改信息;

        }

        private void 修改密码(object sender, EventArgs e)
        {
            var user = UserINISettings.Instance.User;

            if (tbPassword.Password != tbPassword2.Password)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "The password entered twice is inconsistent".L10N("UI:DTAConfig:PasswordTwiceInconsistent"));
                return;
            }

            var messageBox = new XNAMessageBox(WindowManager, "Confirm".L10N("UI:Main:Yes"), "Are you sure you want to change your password?".L10N("UI:DTAConfig:ChangePassword"), XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) =>
            {
                btnLogin.Enabled = false;
                Task.Run(async () =>
                {
                    var r = (await NetWorkINISettings.Post<bool?>("user/changePassword",new ChangePwdDto(){
                        id = user.id,
                        oldPwd = tbPlayerName.Text,
                        newPwd = tbPassword.Password
                }));

                    if (r.Item1 == true)
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Modification successful!".L10N("UI:DTAConfig:ModificationSuccessful"));
                        退出登录?.Invoke();
                    }
                    else
                    {
                        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), $"The original password is wrong!".L10N("UI:DTAConfig:OriginalPasswordWrong"));
                    }
                    btnLogin.Enabled = true;
                });
            };
            messageBox.Show();
        }

        private void 修改信息(object sender, EventArgs e)
        {
            var messageBox = new XNAMessageBox(WindowManager, "Confirm".L10N("UI:Main:Yes"), "Are you sure you want to modify?".L10N("UI:DTAConfig:ModifyConfirm"),XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) => {
                var user = UserINISettings.Instance.User;
                user.side = ddSide.SelectedIndex;
                user.username = tbPlayerName.Text;
                btnLogin.Enabled = false;
                Task.Run(async () =>
                {
                   var r = (await NetWorkINISettings.Post<bool?>("user/editUser", user));

                    if(r.Item1 == true)
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Modification successful!".L10N("UI:DTAConfig:ModificationSuccessful"));
                        重新登录?.Invoke();
                        Disable();
                    }
                    else
                    {
                        XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), $"修改失败! 原因: {r.Item2}");
                    }
                    btnLogin.Enabled = true;
                });
                
            };
            messageBox.Show();


        }


        private void 退出登录事件(object sender, EventArgs e)
        {
            var messageBox = new XNAMessageBox(WindowManager, "Logout".L10N("UI:DTAConfig:Logout"), "Are you sure you want to logout？".L10N("UI:DTAConfig:LogoutConfirm"), XNAMessageBoxButtons.YesNo);

            messageBox.YesClickedAction += (_) =>
            {
                退出登录?.Invoke();
                Disable();
            };
            messageBox.Show();
        }
    }

    public class 出题窗口(WindowManager windowManager, string[] options,QuestionBank questionBank = null) : XNAWindow(windowManager)
    {
        private XNATextBox 题目框, 选项框;
        public Action 需要更新;
        private XNAClientDropDown 答案框, 难度框;
        private XNAClientButton 提交按钮;
        private readonly List<XNAClientCheckBox> chkOptions = [];
        private readonly string[] options = options;
        const int maxCheckedCount = 4;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 460, 350);
            BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");

            var 标题 = new XNALabel(WindowManager)
            {
                Text = "出题",
                ClientRectangle = new Rectangle(193, 10, 0, 0)
            };

            var 题目标签 = new XNALabel(WindowManager)
            {
                Text = "题目: ",
                ClientRectangle = new Rectangle(20, 40, 0, 0)
            };

            题目框 = new XNATextBox(WindowManager)
            {
                ClientRectangle = new Rectangle(题目标签.Right + 60, 题目标签.Y, 360, 25)
            };


            var 选项标签 = new XNALabel(WindowManager)
            { 
                Text = "选项(空格隔开): ",
                ClientRectangle = new Rectangle(题目标签.X - 20 , 题目标签.Y + 40, 0, 0)
            };


            选项框 = new XNATextBox(WindowManager)
            {
                
                ClientRectangle = new Rectangle(选项标签.Right + 120, 选项标签.Y, 320, 25)
            };
            选项框.SelectedChanged += 选项框_SelectedChanged;

            var 答案标签 = new XNALabel(WindowManager)
            {
                Text = "答案: ",
                ClientRectangle = new Rectangle(题目标签.X, 选项标签.Y + 40, 0, 0)
            };

            答案框 = new XNAClientDropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(答案标签.Right + 60, 答案标签.Y, 130, 25)
            };

            var 难度标签 = new XNALabel(WindowManager)
            {
                Text = "难度: ",
                ClientRectangle = new Rectangle(答案框.Right + 50, 答案框.Y, 0, 0)
            };

            难度框 = new XNAClientDropDown(WindowManager)
            {
                ClientRectangle = new Rectangle(难度标签.Right + 60, 难度标签.Y, 120, 25)
            };
            难度框.AddItem("简单");
            难度框.AddItem("中等");
            难度框.AddItem("困难");
            难度框.SelectedIndex = 0;

            var 类型标签 = new XNALabel(WindowManager)
            {
                Text = "类型: ",
                ClientRectangle = new Rectangle(答案标签.X, 答案标签.Y + 40, 0, 0)
            };

            var 返回按钮 = new XNAClientButton(WindowManager)
            {
                Text = "返回",
                ClientRectangle = new Rectangle(类型标签.X, Bottom - 40, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
            };

            返回按钮.LeftClick += 返回按钮_LeftClick;

            提交按钮 = new XNAClientButton(WindowManager)
            {
                Text = "提交",
                ClientRectangle = new Rectangle(难度框.X + 20, 返回按钮.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
            };
            
            base.Initialize();
            CenterOnParent();
            添加答题类型选项组();
            if (questionBank != null)
            {
                题目框.Text = questionBank.problem;
                选项框.Text = questionBank.options;
                
                答案框.SelectedIndex = questionBank.answer;
                难度框.SelectedIndex = questionBank.difficulty;
                foreach (var index in questionBank.type.Split(',').Select(int.Parse))
                {
                    chkOptions[index].Checked = true;
                }
                提交按钮.Text = "修改";
                提交按钮.LeftClick += 修改;
            }
            else
            {
                选项框.Text = "A:选项1 B:选项2 C:选项3 D:选项4";
                提交按钮.Text = "提交";
                提交按钮.LeftClick += 提交按钮_LeftClick;
            }
            选项框_SelectedChanged(null, null);
            AddChild([标题, 题目标签, 题目框, 选项标签, 选项框, 答案标签, 答案框, 难度标签, 难度框, 类型标签, 返回按钮, 提交按钮]);
            

            void 添加答题类型选项组()
            {
                for (int i = 0, j = 0; i < options.Length; i++)
                {
                    chkOptions.Add(
                        new XNAClientCheckBox(WindowManager)
                        {
                            Text = options[i],
                            ClientRectangle = new Rectangle(20 + 150 * j, 类型标签.Bottom + 30 * (1 + i % 3), 0, 0)
                        }
                    );
                    if (i % 3 == 2) j++;
                }


                foreach (var chk in chkOptions)
                {
                    chk.CheckedChanged += (_, _) => {
                        if (chk.Checked && chkOptions.FindAll(x => x.Checked).Count > maxCheckedCount)
                        {
                            XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"Select up to {maxCheckedCount} items".L10N("UI:DTAConfig:MaxSelectItem"));
                            chk.Checked = false;
                        }
                    };

                    AddChild(chk);
                }
            }
        }

        private void 修改(object sender, EventArgs e)
        {

            if ((题目框.Text + 选项框.Text + 答案框.Text).Length == 0 || !chkOptions.Exists(c => c.Checked))
            {
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "There are unfilled fields.".L10N("UI:DTAConfig:UnfilledFields"));
                return;
            }

            var messageBox = new XNAMessageBox(windowManager, "Tips".L10N("UI:Main:Tips"), "Are you sure you want to modify?".L10N("UI:DTAConfig:ModifyConfirm"), XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) => {
                Task.Run(async () =>
                {
                    提交按钮.Enabled = false;
                    var q = new QuestionBank()
                    {
                        id = questionBank.id,
                        name = UserINISettings.Instance.User.id.ToString(),
                        problem = 题目框.Text,
                        options = 选项框.Text,
                        answer = 答案框.SelectedIndex,
                        difficulty = 难度框.SelectedIndex,
                        type = string.Join(",",
                        chkOptions.Select((option, index) => new { option, index })  // 将选项和索引组合
                                            .Where(x => x.option.Checked)                   // 筛选被选中的项
                                            .Select(x => x.index.ToString())                               // 提取索引
                                            .ToArray())
                    };
                    var r = await NetWorkINISettings.Post<bool>("questionBank/updQuestionBank", q);
                    if (r.Item1)
                    {
                        XNAMessageBox.Show(windowManager, "Info".L10N("UI:Main:Info"), "Submitted successfully!".L10N("UI:DTAConfig:SubmittedSuccessfully"));
                    }
                    else
                    {
                        XNAMessageBox.Show(windowManager, "Info".L10N("UI:Main:Info"), $"提交失败! 原因: {r.Item2}");
                    }
                    提交按钮.Enabled = true;
                    需要更新?.Invoke();
                });
            };
            messageBox.Show();

        }

        private void 选项框_SelectedChanged(object sender, EventArgs e)
        {
            if (选项框.Focused) return;
            答案框.Items.Clear();
            foreach(var item in 选项框.Text.TrimStart(' ').Split(" "))
            {
                答案框.AddItem(item);
            }
        }

        private void 提交按钮_LeftClick(object sender, EventArgs e)
        {
            if((题目框.Text + 选项框.Text + 答案框.Text).Length == 0 || !chkOptions.Exists(c => c.Checked))
            {
                XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "There are unfilled fields.".L10N("UI:DTAConfig:UnfilledFields"));
                return;
            }

            var messageBox = new XNAMessageBox(windowManager, "Tips".L10N("UI:Main:Tips"), "Are you sure you want to submit?".L10N("UI:DTAConfig:SubmitConfirm"), XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) => {
                Task.Run(async () =>
                {
                    提交按钮.Enabled = false;
                    var q = new QuestionBank()
                    {
                        name = UserINISettings.Instance.User.id.ToString(),
                        problem = 题目框.Text,
                        options = 选项框.Text,
                        answer = 答案框.SelectedIndex,
                        difficulty = 难度框.SelectedIndex,
                        type = string.Join(",",
                        chkOptions.Select((option, index) => new { option, index })  // 将选项和索引组合
                                            .Where(x => x.option.Checked)                   // 筛选被选中的项
                                            .Select(x => x.index.ToString())                               // 提取索引
                                            .ToArray())
                    };
                    var r = await NetWorkINISettings.Post<bool>("questionBank/addQuestionBank", q);
                    if (r.Item1)
                    {
                        XNAMessageBox.Show(windowManager, "Info".L10N("UI:Main:Info"), "Submitted successfully!".L10N("UI:DTAConfig:SubmittedSuccessfully"));
                        题目框.Text = string.Empty;
                        选项框.Text = "A:选项1 B:选项2 C:选项3 D:选项4";
                        答案框.SelectedIndex = 0;
                        难度框.SelectedIndex = 0;
                        chkOptions.ForEach(c => c.Checked = false);
                    }
                    else
                    {
                        XNAMessageBox.Show(windowManager, "Info".L10N("UI:Main:Info"), $"提交失败! 原因: {r.Item2}");
                    }
                    提交按钮.Enabled = true;

                });
            };
            messageBox.Show();
        }

        private void 返回按钮_LeftClick(object sender, EventArgs e)
        {
            var messageBox = new XNAMessageBox(windowManager, "Tips".L10N("UI:Main:Tips"), "Are you sure you want to exit？".L10N("UI:DTAConfig:ExitConfirm"), XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) => { Disable(); };
            messageBox.Show();
        }

        
    }

    public class 出题记录窗口(WindowManager windowManager) : XNAWindow(windowManager)
    {
        private XNAMultiColumnListBox 题目列表;
        private static readonly string[] 难度列表 = ["简单", "中等", "困难"];

        private List<QuestionBank> questionBanks = [];

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 500, 300);
             BackgroundTexture = AssetLoader.LoadTextureUncached("logindialogbg.png");


            题目列表 = new XNAMultiColumnListBox(WindowManager)
            {
                ClientRectangle = new Rectangle(0, 0, 500, 260),
            };

            题目列表.AddColumn("题目", 160).AddColumn("选项", 160).AddColumn("答案", 80).AddColumn("难度", 50).AddColumn("通过",50);

            AddChild(题目列表);

            var _menu = new XNAContextMenu(WindowManager);
            _menu.Name = nameof(_menu);
            _menu.Width = 100;

            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "刷新",
                SelectAction = 刷新
            });
            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "编辑",
                SelectAction = 编辑
            });
            _menu.AddItem(new XNAContextMenuItem
            {
                Text = "删除",
                SelectAction = 删除
            });

            AddChild(_menu);

            题目列表.RightClick += (_, _) => {
                题目列表.SelectedIndex = 题目列表.HoveredIndex;
                _menu.Open(GetCursorPoint());
            };

            var 关闭按钮 = new XNAClientButton(WindowManager)
            {
                Text = "关闭",
                X = 170,
                Y = Bottom - 30
            };
            关闭按钮.LeftClick += (_, _) => { Disable(); };
            AddChild(关闭按钮);

            刷新();
            CenterOnParent();
            base.Initialize();

        }

        private async Task 获取该用户题目列表() => questionBanks = (await NetWorkINISettings.Get<List<QuestionBank>>($"questionBank/getQuestionBankByUserID?id={UserINISettings.Instance.User.id}")).Item1 ?? [];

        private void 刷新()
        {
            Task.Run(async () =>
            {
                await 获取该用户题目列表();
                题目列表.ClearItems();

                questionBanks.ForEach(c => 题目列表.AddItem([c.problem, c.options, c.options.Split(" ")[c.answer], 难度列表[c.difficulty],c.enable == 0 ? "否":"是"], true));
            });

        }

        private void 编辑()
        {
            if (题目列表.SelectedIndex == -1) return;
            var questionTypes = NetWorkINISettings.Get<string>("dict/getValue?section=question&key=type").GetAwaiter().GetResult().Item1?.Split(",") ?? [];
            if (questionTypes.Length == 0)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), "Failed to get question bank".L10N("UI:DTAConfig:FailedGetQuestion"));
                return;
            }
            var w = new 出题窗口(WindowManager, questionTypes, questionBanks[题目列表.SelectedIndex]);
            w.需要更新 += 刷新;
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, w);
        }

        private void 删除()
        {
            if (题目列表.SelectedIndex == -1) return;

            var q = questionBanks[题目列表.SelectedIndex];
            var messageBox = new XNAMessageBox(WindowManager, "Tips".L10N("UI:Main:Tips"), $"您确定要删除此问题吗:\n{q.problem}".L10N("UI:DTAConfig:RegistrationSuccessful"),XNAMessageBoxButtons.YesNo);
            messageBox.YesClickedAction += (_) =>
            {
                
                Task.Run(async () =>
                {
                    var r = await NetWorkINISettings.Post<bool>($"questionBank/delQuestionBank", q);
                    if (r.Item1)
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), "Deleted successfully!".L10N("UI:DTAConfig:DeletedSuccessfully"));
                    }
                    else
                    {
                        XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), $"删除失败!原因: {r.Item2}");
                    }
                    刷新();
                });
            };
            messageBox.Show();
        }

    }

    public record class LoginDto
    {
        public string name { get; set; }
        public string pwd { get; set; }
    }

    public record class ChangePwdDto
    {
        public string id { get; set; }
        public string oldPwd { get; set; }
        public string newPwd { get; set; }
    }

    public record class TokenVo
    {
        public string access_token { get; set; }
        
        public User user { get; set; }
    }

}

