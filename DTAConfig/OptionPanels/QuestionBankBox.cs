using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ClientCore;
using ClientCore.Entity;
using ClientCore.Settings;
using ClientGUI;
using DTAConfig.Entity;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Localization;

namespace DTAConfig.OptionPanels
{
    public class QuestionBankBox(WindowManager windowManager, List<QuestionBank> questionBanks,Action pass) : XNAWindow(windowManager)
    {
        private const int 答题时间 = 3600;

        private int 倒计时 = 答题时间;
        /// <summary>
        /// 当前题目索引
        /// </summary>
        private int index = 0;

        /// <summary>
        /// 当前分数
        /// </summary>
        private int Score
        {
            get => lblScore.Text != null ? int.Parse(lblScore.Text) : 0;
            set => lblScore.Text = value.ToString();
        }

        /// <summary>
        /// 出题人
        /// </summary>
        private XNALabel lblName;

        /// <summary>
        /// 当前分数
        /// </summary>
        private XNALabel lblScore;

        /// <summary>
        /// 当前题目
        /// </summary>
        private XNALabel lblProblem;

        private XNALabel 倒计时标签;

        /// <summary>
        /// 四个选项
        /// </summary>
        private readonly List<XNAClientCheckBox> chkOptions = [];

        /// <summary>
        /// 下一题
        /// </summary>
        private XNAClientButton 下一题按钮,取消按钮;

        public override void Initialize()
        {
            
            ClientRectangle = new Rectangle(0, 0, 600, 260);

            base.Initialize();
            CenterOnParent();

            lblName = new XNALabel(WindowManager)
            {
                Text = $"出题人: {questionBanks[index].name}",
                ClientRectangle = new Rectangle(20, 10, 0, 0)
            };
            倒计时标签 = new XNALabel(WindowManager)
            {
                ClientRectangle = new Rectangle(280, 10, 0, 0)
            };
            lblScore = new XNALabel(WindowManager) {
                Text = "0",
                ClientRectangle = new Rectangle(530,10,0,0)
            };
            lblProblem = new XNALabel(WindowManager)
            {
                Text = questionBanks[index].problem,
                ClientRectangle = new Rectangle(40,50,0,0)
            };

            
            下一题按钮 = new XNAClientButton(WindowManager)
            {
                Text = "Next question".L10N("UI:DTAConfig:NextQuestion"),
                X = lblScore.X - 110,
                Y = 225
            };
            下一题按钮.LeftClick += BtnNext_LeftClick;

            取消按钮 = new XNAClientButton(WindowManager)
            {
                Text = "I don't want to do it".L10N("UI:DTAConfig:DonotWantDoQuestion"),
                X = lblName.X,
                Y = 225
            };

            取消按钮.LeftClick += (_, _) => {
                var messageBox = new XNAMessageBox(WindowManager, "Tips".L10N("UI:Main:Tips"), "Are you sure you want to give up?".L10N("UI:DTAConfig:GiveUp"), XNAMessageBoxButtons.YesNo);
                messageBox.YesClickedAction += (_) => {
                    index = questionBanks.Count;
                    判断答题结束(); 
                };
                messageBox.Show();
            };

            AddChild([lblName, 倒计时标签, lblScore, lblProblem]);

            添加答题选项();

            AddChild([下一题按钮, 取消按钮]);

            开始计时();

            更新题目(questionBanks[index]);

            #region 函数
            void 添加答题选项()
            {
                for (int i = 0; i < 4; i++)
                {
                    var chk = new XNAClientCheckBox(WindowManager)
                    {   Name = $"chk{i + 1}",
                        ClientRectangle = new Rectangle(lblProblem.X, lblProblem.Bottom + 30 * (i + 1), 0, 0),
                    };
                    chk.CheckedChanged += (_,_) => { 
                        if (chk.Checked)
                            chkOptions.ForEach(x => { 
                                if (x.Name != chk.Name) 
                                    x.Checked = false; 
                            }); 
                    };
                    chkOptions.Add(chk);
                    AddChild(chk);
                }
 
            }

            void 开始计时()
            {
                倒计时 = 答题时间;
                var timer = new Timer
                {
                    Interval = 1000
                };

                timer.Tick += (_, _) => {
                    倒计时--;
                    if (倒计时 == 0)
                    {
                        index = questionBanks.Count;
                        判断答题结束();
                    }

                    倒计时标签.Text = $"倒计时: {倒计时} 秒";
                };

                // 启动计时器
                timer.Start();
            }
            #endregion
        }

        private bool 判断答题结束()
        {
           
            if (questionBanks.Count - index < 60 - Score)
            {
                XNAMessageBox.Show(WindowManager, "Failed".L10N("UI:Main:Failed"), "If you fail the exam, please keep up the good work.".L10N("UI:DTAConfig:FailedExam"));
                //不通过
                Disable();
                return true;
            }

            if (index == questionBanks.Count)
            {
                if (Score >= 60)
                {
                    Task.Run(通过认证);
                }
                else
                    XNAMessageBox.Show(WindowManager, "Failed".L10N("UI:Main:Failed"), "If you fail the exam, please keep up the good work.".L10N("UI:DTAConfig:FailedExam"));
                Disable();
                return true;
            }

            if (lblScore.Text == "60" && 取消按钮.Text == "我不想做了")
            {
                取消按钮.Text = "Submit".L10N("UI:DTAConfig:Submit");
                XNAMessageBox.Show(WindowManager, "Passed".L10N("UI:Main:Passed"), "Now that you've passed the exam, you can submit your results at any time or continue to answer the questions for a higher score.".L10N("UI:DTAConfig:PassedExam"));
                return false;
            }
            return false;
        }

        private void BtnNext_LeftClick(object sender, EventArgs e)
        {
            #region 处理当前页面信息
            var chkIndex = chkOptions.FindIndex(chk => chk.Checked);
            if (chkIndex == -1)
            {
                XNAMessageBox.Show(WindowManager, "Passed".L10N("UI:Main:Passed"), "Please select an option".L10N("UI:DTAConfig:SelectOption"));
                return;
            }

            if (chkIndex == questionBanks[index].answer)
                Score++;

            index++;

            #endregion
            if(!判断答题结束())
                // 更新下一个页面信息
                更新题目(questionBanks[index]);
        }

        /// <summary>
        /// 更新题目信息
        /// </summary>
        /// <param name="questionBank"></param>
        private void 更新题目(in QuestionBank questionBank)
        {
            lblName.Text = $"出题人: {questionBanks[index].name}";
            lblProblem.Text = questionBank.problem;

            foreach (var chk in chkOptions)
            {
                chk.Visible = chk.Checked = false;
            }

            var options = questionBank.options.Split(" ");

            for (int i = 0; i < options.Length; i++)
            {
                chkOptions[i].Text = options[i];
                chkOptions[i].Visible = true;
            }

            if (index == questionBanks.Count - 1)
            {
                下一题按钮.Text = "Concludes the answer".L10N("UI:DTAConfig:ConcludesAnswer");
            }
        }

        async void 通过认证()
        {

            var r = (await NetWorkINISettings.Post<bool?>($"user/pass", new PassDto()
            {
                userId = UserINISettings.Instance.User.id,
                score = Score,
            }));
            string s;
            if (true == r.Item1)
            {
                s = "认证成功！";
            }
            else
            {
                s = r.Item2;
            }
            XNAMessageBox.Show(WindowManager, "Info".L10N("UI:Main:Info"), s);
            pass?.Invoke();
        }
    }

    public class 创作者认证答题须知窗口(WindowManager windowManager, string[] options) : XNAWindow(windowManager)
    {
        private readonly List<XNAClientCheckBox> chkOptions = [];
        private readonly string[] options = options;
        const int maxCheckedCount = 5;
        const int minCheckedCount = 2;

        public Action pass;

        public override void Initialize()
        {
            #region 初始化控件
            ClientRectangle = new Rectangle(0, 0, 600, 460);

            base.Initialize();

            var lbltitle = new XNALabel(WindowManager)
            {
                Text = "Reunion * Workshop - Instructions for Creator Certification".L10N("UI:DTAConfig:ReunionWorkshop"),
                ClientRectangle = new Rectangle(160, 20, 0, 0)
            };

            var lbltext = new XNALabel(WindowManager)
            {
                Text =
                "欢迎参加重聚未来创意工坊创作者认证考试！为了确保创作者拥有足够的知识储备，我们\n" +
                "设置了一套关于红警2的认证答题规则，请仔细阅读以下须知：\n\n" +
                "题目总数：100道题 单选题：100道\n" +
                "评分标准：每题1分，总分100分 及格线：60分\n" +
                "答题要求：选择您擅长的领域进行答题，最多可以选择5个领域。请在考试前确认您的选择。\n" +
                "答题时间为60分钟，请合理安排时间。\n",
                FontIndex = 1,
                ClientRectangle = new Rectangle( 20,lbltitle.Y + 30,0,0)
            };

            AddChild([lbltitle, lbltext]);

            添加答题类型选项组();
            var 返回按钮 = new XNAClientButton(WindowManager)
            {
                Name = "btnReturn",
                Text = "Back".L10N("UI:Main:Back"),
                X = 20,
                Y = Bottom - 40,
            };

            返回按钮.LeftClick += (_, _) =>{Disable();};

            var 进入答题按钮 = new XNAClientButton(WindowManager)
            {
                Name = "btnStart",
                Text = "Start answering".L10N("UI:DTAConfig:StartAnswer"),
                X = Right - 170,
                Y = Bottom - 40,
            };

            进入答题按钮.LeftClick += 进入答题按钮_点击事件;

            AddChild(进入答题按钮);
            AddChild(返回按钮);

            CenterOnParent();
            #endregion

            #region 函数

            void 添加答题类型选项组()
            {
                for (int i = 0, j = -1; i < options.Length; i++)
                {
                    if (i % 4 == 0)
                        j++;

                    chkOptions.Add(
                        new XNAClientCheckBox(WindowManager)
                        {
                            Text = options[i],
                            ClientRectangle = new Rectangle(20 + 230 * j, lbltext.Bottom + 100 + 30 * (1 + i % 4), 0, 0)
                        }
                    );
                    
                }


                foreach (var chk in chkOptions)
                {
                    chk.CheckedChanged += (_, _) => {
                        if (chk.Checked && chkOptions.FindAll(x => x.Checked).Count > maxCheckedCount)
                        {
                            XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"最多选择 {maxCheckedCount} 项");
                            chk.Checked = false;
                        }
                    };

                    AddChild(chk);
                }
            }

            #endregion
        }

        private void 进入答题按钮_点击事件(object sender, EventArgs e)
        {
            if (chkOptions.FindAll(x => x.Checked).Count < minCheckedCount)
            {
                XNAMessageBox.Show(WindowManager, "Tips".L10N("UI:Main:Tips"), $"至少选择 {minCheckedCount} 项");
                return;
            }

            var selectedIndexes = chkOptions
                                    .Select((option, index) => new { option, index })  // 将选项和索引组合
                                    .Where(x => x.option.Checked)                      // 筛选被选中的项
                                    .Select(x => x.index)                              // 提取索引
                                    .ToList();

            var (questions, msg) = NetWorkINISettings.Get<List<QuestionBank>>($"questionBank/getQuestionBank?types={string.Join(",", selectedIndexes)}").GetAwaiter().GetResult();
            if (questions == null)
            {
                XNAMessageBox.Show(WindowManager, "Error".L10N("UI:Main:Error"), $"获取题库失败, 请稍后重试: {msg}");
                return;
            }
            Disable();
            var questionBox = new QuestionBankBox(WindowManager, questions,pass);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, questionBox);
        }
    }
}
