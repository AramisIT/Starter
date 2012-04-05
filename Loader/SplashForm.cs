using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Aramis.Loader.SolutionsInfoLoader;
using Aramis.Loader.SolutionUpdate;
using System.IO;
using System.Threading;

namespace Aramis.Loader
    {
    public partial class SplashForm : Form
        {
        private delegate void VoidDelegate();
        /// <summary>
        /// Размер формы когда активна первая страница
        /// </summary>
        private static readonly Size FIRST_PAGE_SELECTED_SIZE = new Size(433, 264);
        /// <summary>
        /// размер формы когда активна вторая страница
        /// </summary>
        private static readonly Size SECOND_PAGE_SELECTED_SIZE = new Size(331, 85);
        /// <summary>
        /// Размер формы когда активна третья страница(краткий список)
        /// </summary>
        private static readonly Size THIRD_PAGE_SELECTED_SIZE = new Size(331, 81);
        /// <summary>
        /// Размер формы когда активна третья страница(расширеный список инфо)
        /// </summary>
        private static readonly Size THIRD_DETAILED_PAGE_SELECTED_SIZE = new Size(331, 127);

        private bool skipSolutionСhouse = true;
        private bool error = false;
        private int filesCount = 0;
        private int currentNumber = 0;
        private long updateSize = 0;
        private long ticksStart = 0;

        public SplashForm()
            {
            InitializeComponent();
            }

        private void SplashForm_Load(object sender, EventArgs e)
            {
            if ( Program.IsAutoStart )
                {
                HideForm();
                RunSolutionChecker();
                }
            else
                {
                if ( Program.SolutionName == null )
                    {
                    LoadSolutionsList();
                    //skipSolutionСhouse = false;
                    }

                if ( !error )
                    {
                    this.Shown += SplashForm_Shown;
                    }
                else
                    {
                    HideForm();
                    }
                }
            }

        /// <summary>
        /// Прячет форму.
        /// Форма не закрывается, но убирается с панели задач и перемещается в невидимую зону
        /// </summary>
        private void HideForm()
            {
            this.StartPosition = FormStartPosition.Manual;
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Location = new Point(-32000, -32000);
            }

        /// <summary>
        /// Загружает список решений в контрол выбора решений
        /// </summary>
        private void LoadSolutionsList()
            {
            List<SolutionInfo> solutionsList = SolutionListLoader.LoadSolutionsList();
            switch ( solutionsList.Count )
                {
                case 0:
                    // Если количество решений в списке нулевое - это значит отсутствие файла или не правильное его построение.
                    "Невозможно построить список решений. Свяжитесь с разработчиком.".Error();
                    error = true;
                    Program.ExitWithResult(LoaderResult.Error);
                    break;
                case 1:
                    // Если в списке содержится всего одно решение - нет смысла отображать форму выбора решений. Пропускаем ее.
                    SolutionsListControl.Items.Add(solutionsList[0]);
                    SolutionsListControl.SelectedIndex = 0;
                    SetSolutionName();
                    skipSolutionСhouse = true;
                    break;
                default:
                    // Во всех остальных случаях - заполняем список решений полученными значениями.
                    solutionsList.ForEach(solution => SolutionsListControl.Items.Add(solution));
                    SolutionsListControl.SelectedIndex = 0;
                    break;
                }
            }

        private void RunSolutionButton_Click(object sender, EventArgs e)
            {
            tabControl1.SelectedTab = WaitingWhileUpdatePage;
            SetSolutionName();
            new Thread(RunSolutionChecker) { IsBackground = true }.Start();
            }

        /// <summary>
        /// Задает имя выбраного решения.
        /// </summary>
        private void SetSolutionName()
            {
            Program.SolutionName = ( ( SolutionInfo ) SolutionsListControl.SelectedItem ).ApplicationDirectory;
            }

        /// <summary>
        /// Запускает проверку файлов решения.
        /// </summary>
        private void RunSolutionChecker()
            {
            filesCount = 0;
            currentNumber = 0;
            // Назначаем обработчик статического события, которое происходит при окончании ожидания(в данном случае ожидания окончания загрузки файлов в БД)
            // Отписываться от события не нужно. Все подписи на это событие автоматически снимаются после вызова.
            Waiting.OnWaitingEnd += OnWaitingDownloadFilesToDBEnd;
            // Назначаем обработчик статического события, которое происходит при окончании загрузки файла.
            // По этому собитию будут обновляться надписи на формах и т.д.
            // Подписываться на событие имеет смысл лишь в случае когда запуск проходит не в автоматическом режиме
            if ( !Program.IsAutoStart )
                {
                SolutionUpdate.Update.OnFileDownloading += Update_OnFileDownloading;
                SolutionUpdate.Update.OnFileDownloadingStart += Update_OnFileDownloadingStart;
                filesCount = SolutionUpdate.Update.FilesCount;
                updateSize = 0;
                ticksStart = DateTime.Now.Ticks;
                }
            // Проверяем и загружаем обновления
            SolutionRuner.CheckSolutionUpdates();
            // если доступны обновления - применяем их
            if ( SolutionRuner.SolutionUpdatesExists() )
                {
                SolutionRuner.AcceptSolutionUpdates();
                }
            // Отписываемся от событий, на которые нам уже не нужно реагировать
            if ( !Program.IsAutoStart )
                {
                SolutionUpdate.Update.OnFileDownloading -= Update_OnFileDownloading;
                SolutionUpdate.Update.OnFileDownloadingStart -= Update_OnFileDownloadingStart;
                }
            // Меняем страницу на вторую(страница ожидания), ожидаем окончания обновления БД и запускаем решение
            WaitingWhileDBUpdating();
            }

        /// <summary>
        /// Меняет страницу интерфейса на вторую, ожидает окончания обновления БД и рапускает решение
        /// </summary>
        private void WaitingWhileDBUpdating()
            {
            if ( InvokeRequired )
                {
                // Инвокаем форму и меняем страницу интерфейса
                Invoke(new VoidDelegate(WaitingWhileDBUpdating));
                }
            else
                {
                tabControl1.SelectedTab = WaitingWhileUpdatePage;
                // Подписываемся на событие окончания ожидания(по окончанию ожидания мы должны запустить решение)
                Waiting.OnWaitingEnd += Waiting_OnWaitingEnd;
                // Запускаем ожидание
                Waiting.Wait(WaitingCodes.UPDATING_DB_STRUCTURE_CODE);
                }
            }

        void Waiting_OnWaitingEnd()
            {
            Waiting.OnWaitingEnd -= Waiting_OnWaitingEnd;
            // Запускаем решение в основном потоке
            RunSolution();
            }

        void Update_OnFileDownloadingStart(UpdatingInfo info)
            {
            if ( InvokeRequired )
                {
                Invoke(new OnFileDownloadingStartDelegate(Update_OnFileDownloadingStart), info);
                }
            else
                {
                currentNumber++;
                CurrentFile.Text = Path.GetFileName(info.FileName);
                Downloaded.Text = String.Format("{0} из {1}", currentNumber, filesCount);
                }
            }

        void Update_OnFileDownloading(UpdatingInfo info)
            {
            if ( InvokeRequired )
                {
                Invoke(new OnFileDownloadingDelegate(Update_OnFileDownloading), info);
                }
            else
                {
                updateSize += info.FileSize;
                long ticks = DateTime.Now.Ticks - ticksStart;
                if ( updateSize != 0 )
                    {
                    TimeSpan span = new TimeSpan(( SolutionUpdate.Update.TotalUpdateSize * ticks / updateSize ) - ticks);
                    TimeLeft.Text = String.Format("{0:00}:{1:00}:{2:00}", Math.Round(span.TotalHours, 0), span.Minutes, span.Seconds);
                    }
                }
            }

        void OnWaitingDownloadFilesToDBEnd()
            {
            if ( InvokeRequired )
                {
                Invoke(new Waiting.OnWaitingEndDelegate(OnWaitingDownloadFilesToDBEnd));
                }
            else
                {
                tabControl1.SelectedTab = LoadUpdateFilesPage;
                }
            }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
            {
            if ( !Program.IsAutoStart )
                {
                if ( tabControl1.SelectedTab == ChouseSolutionPage )
                    {
                    this.ShowInTaskbar = true;
                    this.FormBorderStyle = FormBorderStyle.FixedDialog;
                    this.WindowState = FormWindowState.Normal;
                    this.Size = SplashForm.FIRST_PAGE_SELECTED_SIZE;
                    this.ControlBox = true;
                    this.Location = GetCenterScreenWindowLocation();
                    }
                else if ( tabControl1.SelectedTab == WaitingWhileUpdatePage )
                    {
                    this.ShowInTaskbar = true;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Normal;
                    this.Size = SplashForm.SECOND_PAGE_SELECTED_SIZE;
                    this.ControlBox = false;
                    this.Location = GetCenterScreenWindowLocation();
                    }
                else if ( tabControl1.SelectedTab == LoadUpdateFilesPage )
                    {
                    this.ShowInTaskbar = true;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Normal;
                    this.Size = SplashForm.THIRD_PAGE_SELECTED_SIZE;
                    this.ControlBox = false;
                    this.Location = GetCenterScreenWindowLocation();
                    }
                }
            }

        private Point GetCenterScreenWindowLocation()
            {
            Size screenSize = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Size;
            return new Point(( screenSize.Width - this.Width ) / 2, ( screenSize.Height - this.Height ) / 2);
            }

        private void SplashForm_Shown(object sender, EventArgs e)
            {
            if ( skipSolutionСhouse )
                {
                tabControl1.SelectedTab = WaitingWhileUpdatePage;
                RunSolutionChecker();
                }
            else
                {
                tabControl1.SelectedTab = ChouseSolutionPage;
                }
            }

        private void ExitButton_Click(object sender, EventArgs e)
            {
            Program.ExitWithResult(LoaderResult.Exit);
            }

        /// <summary>
        /// Запускает решение в основном потоке
        /// </summary>
        private void RunSolution()
            {
            if ( InvokeRequired )
                {
                Invoke(new VoidDelegate(RunSolution));
                }
            else
                {
                Hide();
                SolutionRuner.Start();
                }
            }

        private void button1_Click(object sender, EventArgs e)
            {
            if ( ( ( Button ) sender ).Tag != null && Convert.ToBoolean(( ( Button ) sender ).Tag) == false )
                {
                ( ( Button ) sender ).Text = "+";
                ( ( Button ) sender ).Tag = true;
                this.Size = THIRD_PAGE_SELECTED_SIZE;
                this.tableLayoutPanel1.Controls.Clear();
                this.tableLayoutPanel1.RowStyles.Clear();
                this.tableLayoutPanel1.Controls.Add(this.TimeLeftLabel, 1, 0);
                this.tableLayoutPanel1.Controls.Add(this.TimeLeft, 2, 0);
                this.tableLayoutPanel1.Controls.Add(this.button1, 0, 0);
                this.tableLayoutPanel1.RowCount = 1;
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 68F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 68F));
                this.tableLayoutPanel1.SetRowSpan(this.button1, 1);
                this.tableLayoutPanel1.ResumeLayout(false);
                this.tableLayoutPanel1.PerformLayout();
                }
            else
                {
                ( ( Button ) sender ).Text = "-";
                ( ( Button ) sender ).Tag = false;
                this.Size = THIRD_DETAILED_PAGE_SELECTED_SIZE;
                this.tableLayoutPanel1.Controls.Clear();
                this.tableLayoutPanel1.RowStyles.Clear();
                this.tableLayoutPanel1.Controls.Add(this.CurrentFileLabel, 1, 0);
                this.tableLayoutPanel1.Controls.Add(this.CurrentFile, 2, 0);
                this.tableLayoutPanel1.Controls.Add(this.DownloadedLabel, 1, 1);
                this.tableLayoutPanel1.Controls.Add(this.Downloaded, 2, 1);
                this.tableLayoutPanel1.Controls.Add(this.TimeLeftLabel, 1, 2);
                this.tableLayoutPanel1.Controls.Add(this.TimeLeft, 2, 2);
                this.tableLayoutPanel1.Controls.Add(this.button1, 0, 0);
                this.tableLayoutPanel1.RowCount = 3;
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
                this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
                this.tableLayoutPanel1.Size = new System.Drawing.Size(323, 68);
                this.tableLayoutPanel1.TabIndex = 2;
                this.tableLayoutPanel1.SetRowSpan(this.button1, 1);
                this.tableLayoutPanel1.ResumeLayout(false);
                this.tableLayoutPanel1.PerformLayout();
                }
            }
        }
    }
