namespace Aramis.Loader
    {
    partial class SplashForm
        {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
            {
            if ( disposing && ( components != null ) )
                {
                components.Dispose();
                }
            base.Dispose(disposing);
            }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
            {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.ChouseSolutionPage = new System.Windows.Forms.TabPage();
            this.ExitButton = new System.Windows.Forms.Button();
            this.RunSolutionButton = new System.Windows.Forms.Button();
            this.SolutionsListControl = new System.Windows.Forms.ListBox();
            this.WaitingWhileUpdatePage = new System.Windows.Forms.TabPage();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.LoadUpdateFilesPage = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.TimeLeftLabel = new System.Windows.Forms.Label();
            this.TimeLeft = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.Downloaded = new System.Windows.Forms.Label();
            this.DownloadedLabel = new System.Windows.Forms.Label();
            this.CurrentFile = new System.Windows.Forms.Label();
            this.CurrentFileLabel = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.ChouseSolutionPage.SuspendLayout();
            this.WaitingWhileUpdatePage.SuspendLayout();
            this.LoadUpdateFilesPage.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.ChouseSolutionPage);
            this.tabControl1.Controls.Add(this.WaitingWhileUpdatePage);
            this.tabControl1.Controls.Add(this.LoadUpdateFilesPage);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.ItemSize = new System.Drawing.Size(0, 1);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(331, 81);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControl1.TabIndex = 3;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 5);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(323, 72);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // ChouseSolutionPage
            // 
            this.ChouseSolutionPage.Controls.Add(this.ExitButton);
            this.ChouseSolutionPage.Controls.Add(this.RunSolutionButton);
            this.ChouseSolutionPage.Controls.Add(this.SolutionsListControl);
            this.ChouseSolutionPage.Location = new System.Drawing.Point(4, 5);
            this.ChouseSolutionPage.Name = "ChouseSolutionPage";
            this.ChouseSolutionPage.Padding = new System.Windows.Forms.Padding(3);
            this.ChouseSolutionPage.Size = new System.Drawing.Size(323, 72);
            this.ChouseSolutionPage.TabIndex = 0;
            this.ChouseSolutionPage.Text = "tabPage1";
            this.ChouseSolutionPage.UseVisualStyleBackColor = true;
            // 
            // ExitButton
            // 
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(284, 43);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(133, 31);
            this.ExitButton.TabIndex = 4;
            this.ExitButton.Text = "Выход";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // RunSolutionButton
            // 
            this.RunSolutionButton.Location = new System.Drawing.Point(284, 6);
            this.RunSolutionButton.Name = "RunSolutionButton";
            this.RunSolutionButton.Size = new System.Drawing.Size(133, 31);
            this.RunSolutionButton.TabIndex = 3;
            this.RunSolutionButton.Text = "Запуск";
            this.RunSolutionButton.UseVisualStyleBackColor = true;
            this.RunSolutionButton.Click += new System.EventHandler(this.RunSolutionButton_Click);
            // 
            // SolutionsListControl
            // 
            this.SolutionsListControl.Dock = System.Windows.Forms.DockStyle.Left;
            this.SolutionsListControl.FormattingEnabled = true;
            this.SolutionsListControl.Location = new System.Drawing.Point(3, 3);
            this.SolutionsListControl.Name = "SolutionsListControl";
            this.SolutionsListControl.Size = new System.Drawing.Size(275, 66);
            this.SolutionsListControl.TabIndex = 1;
            this.SolutionsListControl.DoubleClick += new System.EventHandler(this.RunSolutionButton_Click);
            // 
            // WaitingWhileUpdatePage
            // 
            this.WaitingWhileUpdatePage.Controls.Add(this.progressBar1);
            this.WaitingWhileUpdatePage.Controls.Add(this.label1);
            this.WaitingWhileUpdatePage.Location = new System.Drawing.Point(4, 5);
            this.WaitingWhileUpdatePage.Name = "WaitingWhileUpdatePage";
            this.WaitingWhileUpdatePage.Padding = new System.Windows.Forms.Padding(3);
            this.WaitingWhileUpdatePage.Size = new System.Drawing.Size(323, 72);
            this.WaitingWhileUpdatePage.TabIndex = 1;
            this.WaitingWhileUpdatePage.Text = "tabPage2";
            this.WaitingWhileUpdatePage.UseVisualStyleBackColor = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar1.Location = new System.Drawing.Point(3, 48);
            this.progressBar1.Maximum = 1000;
            this.progressBar1.Minimum = 1;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(317, 25);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 2;
            this.progressBar1.Value = 1;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( ( byte ) ( 204 ) ));
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(317, 45);
            this.label1.TabIndex = 1;
            this.label1.Text = "Выполняется обновление системы. Подождите несколько секунд.";
            // 
            // LoadUpdateFilesPage
            // 
            this.LoadUpdateFilesPage.Controls.Add(this.tableLayoutPanel1);
            this.LoadUpdateFilesPage.Controls.Add(this.progressBar2);
            this.LoadUpdateFilesPage.Controls.Add(this.label2);
            this.LoadUpdateFilesPage.Location = new System.Drawing.Point(4, 5);
            this.LoadUpdateFilesPage.Name = "LoadUpdateFilesPage";
            this.LoadUpdateFilesPage.Size = new System.Drawing.Size(323, 72);
            this.LoadUpdateFilesPage.TabIndex = 3;
            this.LoadUpdateFilesPage.Text = "tabPage2";
            this.LoadUpdateFilesPage.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30.03096F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 69.96904F));
            this.tableLayoutPanel1.Controls.Add(this.TimeLeftLabel, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.TimeLeft, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.button1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 50);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(323, 22);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // TimeLeftLabel
            // 
            this.TimeLeftLabel.AutoSize = true;
            this.TimeLeftLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TimeLeftLabel.Location = new System.Drawing.Point(23, 0);
            this.TimeLeftLabel.Name = "TimeLeftLabel";
            this.TimeLeftLabel.Size = new System.Drawing.Size(84, 22);
            this.TimeLeftLabel.TabIndex = 4;
            this.TimeLeftLabel.Text = "Осталось (сек)";
            this.TimeLeftLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TimeLeft
            // 
            this.TimeLeft.AutoSize = true;
            this.TimeLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TimeLeft.Location = new System.Drawing.Point(113, 0);
            this.TimeLeft.Name = "TimeLeft";
            this.TimeLeft.Size = new System.Drawing.Size(207, 22);
            this.TimeLeft.TabIndex = 5;
            this.TimeLeft.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Margin = new System.Windows.Forms.Padding(0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(20, 22);
            this.button1.TabIndex = 6;
            this.button1.Text = "+";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // progressBar2
            // 
            this.progressBar2.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressBar2.Location = new System.Drawing.Point(0, 27);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(323, 23);
            this.progressBar2.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ( ( byte ) ( 204 ) ));
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(323, 27);
            this.label2.TabIndex = 1;
            this.label2.Text = "Загрузка обновлений";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Downloaded
            // 
            this.Downloaded.AutoSize = true;
            this.Downloaded.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Downloaded.Location = new System.Drawing.Point(113, 22);
            this.Downloaded.Name = "Downloaded";
            this.Downloaded.Size = new System.Drawing.Size(207, 22);
            this.Downloaded.TabIndex = 3;
            this.Downloaded.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // DownloadedLabel
            // 
            this.DownloadedLabel.AutoSize = true;
            this.DownloadedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DownloadedLabel.Location = new System.Drawing.Point(23, 22);
            this.DownloadedLabel.Name = "DownloadedLabel";
            this.DownloadedLabel.Size = new System.Drawing.Size(84, 22);
            this.DownloadedLabel.TabIndex = 2;
            this.DownloadedLabel.Text = "Загружается";
            this.DownloadedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // CurrentFile
            // 
            this.CurrentFile.AutoSize = true;
            this.CurrentFile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CurrentFile.Location = new System.Drawing.Point(113, 0);
            this.CurrentFile.Name = "CurrentFile";
            this.CurrentFile.Size = new System.Drawing.Size(207, 22);
            this.CurrentFile.TabIndex = 1;
            this.CurrentFile.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // CurrentFileLabel
            // 
            this.CurrentFileLabel.AutoSize = true;
            this.CurrentFileLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CurrentFileLabel.Location = new System.Drawing.Point(23, 0);
            this.CurrentFileLabel.Name = "CurrentFileLabel";
            this.CurrentFileLabel.Size = new System.Drawing.Size(84, 22);
            this.CurrentFileLabel.TabIndex = 0;
            this.CurrentFileLabel.Text = "Текущий файл:";
            this.CurrentFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 81);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SplashForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SplashForm";
            this.Load += new System.EventHandler(this.SplashForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.ChouseSolutionPage.ResumeLayout(false);
            this.WaitingWhileUpdatePage.ResumeLayout(false);
            this.LoadUpdateFilesPage.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

            }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage ChouseSolutionPage;
        private System.Windows.Forms.TabPage WaitingWhileUpdatePage;
        private System.Windows.Forms.ListBox SolutionsListControl;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button RunSolutionButton;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage LoadUpdateFilesPage;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label TimeLeftLabel;
        private System.Windows.Forms.Label TimeLeft;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label CurrentFileLabel;
        private System.Windows.Forms.Label CurrentFile;
        private System.Windows.Forms.Label DownloadedLabel;
        private System.Windows.Forms.Label Downloaded;
        private System.Windows.Forms.Button button1;
        }
    }