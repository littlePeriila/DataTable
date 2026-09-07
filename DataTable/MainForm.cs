using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PerillaTable
{
    class MainForm : Form
    {
        private readonly TextBox txtExcelPath = new TextBox();
        private readonly TextBox txtDataPath = new TextBox();
        private readonly TextBox txtClassPath = new TextBox();
        private readonly TableLayoutPanel fileTable = new TableLayoutPanel();
        private readonly TextBox txtLog = new TextBox();
        private readonly TextBox txtFilter = new TextBox();
        private readonly List<(string Path, List<SheetInfo> Sheets)> allFiles = new();
        private bool suppressAutoSave = false;
        private readonly GroupBox fileGroup = new GroupBox();
        private readonly Panel scrollPanel = new Panel();
        private readonly Panel filterPanel = new Panel();
        private readonly HashSet<string> checkedFiles = new();

        public MainForm()
        {
            SetupUI();
            Logger.OnLog += OnLogReceived;
            LoadConfigToUI();
            RefreshFileList();
        }

        private void SetupUI()
        {
            Text = "Excel导表工具";
            Width = 950;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 500);

            // ── MenuStrip ──
            var menuStrip = new MenuStrip();
            var miFile = new ToolStripMenuItem("文件(&F)");
            var miFileExit = new ToolStripMenuItem("退出");
            miFileExit.Click += (s, e) => Close();
            miFile.DropDownItems.Add(miFileExit);

            var miAction = new ToolStripMenuItem("操作(&A)");
            var miRefresh = new ToolStripMenuItem("刷新文件列表");
            miRefresh.Click += (s, e) => RefreshFileList();
            var miExportAll = new ToolStripMenuItem("导出全部");
            miExportAll.Click += (s, e) => ExportAll();
            miAction.DropDownItems.Add(miRefresh);
            miAction.DropDownItems.Add(miExportAll);

            menuStrip.Items.Add(miFile);
            menuStrip.Items.Add(miAction);
            MainMenuStrip = menuStrip;

            // ── Config panel (collapsible) ──
            var configPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 124
            };

            // 标题栏：点击切换折叠
            var configHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = SystemColors.Control,
                Cursor = Cursors.Hand
            };
            var lblConfigTitle = new Label
            {
                Text = "配置",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            configHeader.Controls.Add(lblConfigTitle);

            var configContent = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 96,
                Padding = new Padding(0)
            };

            var configTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(10, 0, 10, 0)
            };
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
            configTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
            for (int i = 0; i < 3; i++)
                configTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            // Row 0: Excel path
            configTable.Controls.Add(MakeLabel("Excel路径:"), 0, 0);
            txtExcelPath.Dock = DockStyle.Fill;
            configTable.Controls.Add(txtExcelPath, 1, 0);
            var btnBrowseExcel = MakeConfigButton("浏览...");
            btnBrowseExcel.Click += (s, e) => BrowseFolder(txtExcelPath, RefreshFileList);
            configTable.Controls.Add(btnBrowseExcel, 2, 0);
            var btnOpenExcelDir = MakeConfigButton("打开文件夹");
            btnOpenExcelDir.Click += (s, e) => OpenFolder(txtExcelPath.Text);
            configTable.Controls.Add(btnOpenExcelDir, 3, 0);

            // Row 1: Data path
            configTable.Controls.Add(MakeLabel("数据保存路径:"), 0, 1);
            txtDataPath.Dock = DockStyle.Fill;
            configTable.Controls.Add(txtDataPath, 1, 1);
            var btnBrowseData = MakeConfigButton("浏览...");
            btnBrowseData.Click += (s, e) => BrowseFolder(txtDataPath, null);
            configTable.Controls.Add(btnBrowseData, 2, 1);
            var btnOpenDataDir = MakeConfigButton("打开文件夹");
            btnOpenDataDir.Click += (s, e) => OpenFolder(txtDataPath.Text);
            configTable.Controls.Add(btnOpenDataDir, 3, 1);

            // Row 2: Class path
            configTable.Controls.Add(MakeLabel("C#类保存路径:"), 0, 2);
            txtClassPath.Dock = DockStyle.Fill;
            configTable.Controls.Add(txtClassPath, 1, 2);
            var btnBrowseClass = MakeConfigButton("浏览...");
            btnBrowseClass.Click += (s, e) => BrowseFolder(txtClassPath, null);
            configTable.Controls.Add(btnBrowseClass, 2, 2);
            var btnOpenClassDir = MakeConfigButton("打开文件夹");
            btnOpenClassDir.Click += (s, e) => OpenFolder(txtClassPath.Text);
            configTable.Controls.Add(btnOpenClassDir, 3, 2);

            // Auto-save on any path change
            txtExcelPath.TextChanged += (s, e) => AutoSaveConfig();
            txtDataPath.TextChanged += (s, e) => AutoSaveConfig();
            txtClassPath.TextChanged += (s, e) => AutoSaveConfig();

            configContent.Controls.Add(configTable);
            configPanel.Controls.Add(configContent);
            configPanel.Controls.Add(configHeader);

            // 折叠/展开配置面板（点击配置标题行切换）
            bool configCollapsed = false;
            void ToggleConfig()
            {
                configCollapsed = !configCollapsed;
                configContent.Visible = !configCollapsed;
                configPanel.Height = configCollapsed ? 26 : 124;
            }
            lblConfigTitle.Click += (s, e) => ToggleConfig();

            // ── SplitContainer (file list + log) ──
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 280
            };

            // File list（标题栏与配置面板同样式，可折叠结构一致）
            fileGroup.Text = "Excel文件列表";
            fileGroup.Dock = DockStyle.Fill;
            fileGroup.Padding = new Padding(5, 9, 5, 5);

            //Panel1 顶部留白：与上方配置面板拉开间隔（Dock布局下 Margin 无效，用 Padding）
            split.Panel1.Padding = new Padding(0, 14, 0, 0);

            filterPanel.Dock = DockStyle.Top;
            filterPanel.Height = 32;
            filterPanel.Padding = new Padding(0, 1, 0, 4);
            var lblFilter = new Label
            {
                Text = "筛选:",
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Width = 45
            };
            txtFilter.Dock = DockStyle.Fill;
            txtFilter.PlaceholderText = "输入文件名或子表名筛选...";
            txtFilter.TextChanged += (s, e) => RenderFileList();

            // 间隔垫片：输入框与按钮之间留空隙
            var gap1 = new Panel { Dock = DockStyle.Right, Width = 8 };
            var gap2 = new Panel { Dock = DockStyle.Right, Width = 8 };
            var gap3 = new Panel { Dock = DockStyle.Right, Width = 8 };

            var btnSelectAll = new Button
            {
                Text = "全选",
                Dock = DockStyle.Right,
                Width = 55
            };
            btnSelectAll.Click += (s, e) => ToggleSelectAll();
            var btnExportChecked = new Button
            {
                Text = "导出勾选",
                Dock = DockStyle.Right,
                Width = 80
            };
            btnExportChecked.Click += (s, e) => ExportCheckedFiles();
            var btnRefresh = new Button
            {
                Text = "刷新",
                Dock = DockStyle.Right,
                Width = 55
            };
            btnRefresh.Click += (s, e) => RefreshFileList();

            // Dock=Right 的控件按添加顺序从右往左排列
            filterPanel.Controls.Add(txtFilter);
            filterPanel.Controls.Add(lblFilter);
            filterPanel.Controls.Add(gap3);
            filterPanel.Controls.Add(btnRefresh);
            filterPanel.Controls.Add(gap2);
            filterPanel.Controls.Add(btnExportChecked);
            filterPanel.Controls.Add(gap1);
            filterPanel.Controls.Add(btnSelectAll);

            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            fileTable.Dock = DockStyle.Top;
            fileTable.ColumnCount = 7;
            fileTable.GrowStyle = TableLayoutPanelGrowStyle.AddRows;
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            fileTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            scrollPanel.Controls.Add(fileTable);
            fileGroup.Controls.Add(scrollPanel);
            fileGroup.Controls.Add(filterPanel);
            split.Panel1.Controls.Add(fileGroup);

            // Log panel
            var logPanel = new GroupBox
            {
                Text = "日志",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = new Font("Consolas", 9F);
            logPanel.Controls.Add(txtLog);
            split.Panel2.Controls.Add(logPanel);

            Controls.Add(split);

            // Dock layout: controls added later dock first (front of z-order).
            // Order: split(Fill) -> configPanel(Top) -> menuStrip(Top)
            // menuStrip ends up top-most, configPanel below it, split fills the rest.
            Controls.Add(configPanel);
            Controls.Add(menuStrip);
            PerformLayout();
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
        }

        private static Label MakeHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
            };
        }

        //配置面板按钮：固定高度26px、水平拉伸，三行渲染完全一致
        private static Button MakeConfigButton(string text)
        {
            return new Button
            {
                Text = text,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                Height = 26,
                Margin = new Padding(3, 3, 3, 3)
            };
        }

        private void OpenFile(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Error($"打开文件失败: {ex.Message}");
            }
        }

        private static void OpenFolder(string path)
        {
            string trimmed = path?.Trim() ?? "";
            if (string.IsNullOrEmpty(trimmed))
            {
                MessageBox.Show("路径为空，请先设置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string full = Path.GetFullPath(trimmed);
                if (!Directory.Exists(full))
                {
                    MessageBox.Show($"路径不存在: {full}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Process.Start(new ProcessStartInfo(full) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Error($"打开文件夹失败: {ex.Message}");
            }
        }

        private void CheckFile(string filePath)
        {
            var excelTool = new ExcelTool();

            var issues = excelTool.ValidateFile(filePath);
            foreach (var issue in issues)
                Logger.Log(issue);

            if (issues.Count == 0)
                MessageBox.Show($"文件检查通过: {Path.GetFileName(filePath)}", "检查结果",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"发现 {issues.Count} 个问题，请查看日志", "检查结果",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowSubSheets(string filePath)
        {
            using var dialog = new SubSheetsDialog(filePath);
            dialog.ShowDialog(this);
        }

        private void ExportFile(string filePath)
        {
            Config.I.ApplyConfig(txtExcelPath.Text, txtDataPath.Text, txtClassPath.Text, Config.I.exportList);
            using var dialog = new ExportDialog(filePath);
            dialog.ShowDialog(this);
        }

        private void LoadConfigToUI()
        {
            suppressAutoSave = true;
            txtExcelPath.Text = Config.I.excelPath ?? "";
            txtDataPath.Text = Config.I.dataPath ?? "";
            txtClassPath.Text = Config.I.classPath ?? "";
            suppressAutoSave = false;
        }

        private static void BrowseFolder(TextBox target, Action? onApplied)
        {
            using var dialog = new FolderBrowserDialog();
            string path = target.Text.Trim();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                dialog.SelectedPath = Path.GetFullPath(path);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                target.Text = dialog.SelectedPath;
                onApplied?.Invoke();
            }
        }

        private void AutoSaveConfig()
        {
            if (suppressAutoSave) return;
            Config.I.ApplyConfig(txtExcelPath.Text, txtDataPath.Text, txtClassPath.Text, Config.I.exportList);
            Config.I.SaveConfig();
        }

        private void RefreshFileList()
        {
            allFiles.Clear();

            Logger.Log($"工作目录: {Environment.CurrentDirectory}");

            string excelPath = Config.I.excelPath;
            Logger.Log($"Config.I.excelPath 原始值: {excelPath ?? "(null)"}");

            if (string.IsNullOrEmpty(excelPath))
            {
                Logger.Warning("Excel路径为空，请在配置面板中设置");
                RenderFileList();
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(excelPath);
                Logger.Log($"完整路径: {fullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"路径解析失败: {ex.Message}");
                RenderFileList();
                return;
            }

            if (!Directory.Exists(excelPath))
            {
                Logger.Warning($"Excel路径不存在: {excelPath}");
                RenderFileList();
                return;
            }

            try
            {
                var files = new List<string>();
                GetExcelFiles(excelPath, files);
                Logger.Log($"找到 {files.Count} 个文件");

                foreach (string file in files)
                {
                    if (file.Contains("Enum.xlsx") || file.Contains("~$"))
                        continue;

                    List<SheetInfo> sheets = new();
                    try
                    {
                        var excelTool = new ExcelTool();
                        sheets = excelTool.GetSheetInfos(file);
                    }
                    catch { }

                    allFiles.Add((file, sheets));
                }

                Logger.Log($"已加载 {allFiles.Count} 个Excel文件到列表");
            }
            catch (Exception ex)
            {
                Logger.Error($"扫描文件时出错: {ex.Message}");
            }

            RenderFileList();
        }

        private void RenderFileList()
        {
            fileTable.SuspendLayout();
            fileTable.Controls.Clear();
            fileTable.RowStyles.Clear();
            fileTable.RowCount = 0;

            // Header row
            fileTable.RowCount = 1;
            fileTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            fileTable.Controls.Add(MakeHeaderLabel(""), 0, 0);
            fileTable.Controls.Add(MakeHeaderLabel("文件名"), 1, 0);
            fileTable.Controls.Add(MakeHeaderLabel("大小"), 2, 0);
            fileTable.Controls.Add(MakeHeaderLabel("修改时间"), 3, 0);
            fileTable.Controls.Add(MakeHeaderLabel("检查"), 4, 0);
            fileTable.Controls.Add(MakeHeaderLabel("子表"), 5, 0);
            fileTable.Controls.Add(MakeHeaderLabel("导出"), 6, 0);

            string filter = txtFilter.Text.Trim().ToLower();
            int row = 1;

            //默认按修改时间倒序（最新在上）
            var visible = allFiles
                .Where(f => MatchesFilter(f.Path, f.Sheets, filter))
                .OrderByDescending(f => File.GetLastWriteTime(f.Path))
                .ToList();

            foreach (var (file, sheets) in visible)
            {
                FileInfo fi = new FileInfo(file);
                fileTable.RowCount++;
                fileTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

                var chk = new CheckBox
                {
                    Dock = DockStyle.Fill,
                    Checked = checkedFiles.Contains(file),
                    Tag = file
                };
                chk.CheckedChanged += (s, e) =>
                {
                    var cb = (CheckBox)s!;
                    if (cb.Checked) checkedFiles.Add((string)cb.Tag!);
                    else checkedFiles.Remove((string)cb.Tag!);
                };
                fileTable.Controls.Add(chk, 0, row);

                var lblName = new Label
                {
                    Text = Path.GetFileName(file),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Tag = file
                };
                lblName.DoubleClick += (s, e) => OpenFile(file);
                fileTable.Controls.Add(lblName, 1, row);

                var lblSize = new Label
                {
                    Text = FormatFileSize(fi.Length),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                fileTable.Controls.Add(lblSize, 2, row);

                var lblTime = new Label
                {
                    Text = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                fileTable.Controls.Add(lblTime, 3, row);

                var btnCheck = new Button { Text = "检查", Dock = DockStyle.Fill };
                btnCheck.Click += (s, e) => CheckFile(file);
                fileTable.Controls.Add(btnCheck, 4, row);

                var btnSheets = new Button { Text = "查看子表", Dock = DockStyle.Fill };
                btnSheets.Click += (s, e) => ShowSubSheets(file);
                fileTable.Controls.Add(btnSheets, 5, row);

                var btnExport = new Button { Text = "导出", Dock = DockStyle.Fill };
                btnExport.Click += (s, e) => ExportFile(file);
                fileTable.Controls.Add(btnExport, 6, row);

                row++;
            }

            fileTable.ResumeLayout(true);
            fileTable.Height = fileTable.RowCount * 28;
        }

        private static bool MatchesFilter(string file, List<SheetInfo> sheets, string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            if (Path.GetFileName(file).ToLower().Contains(filter))
                return true;

            foreach (var si in sheets)
            {
                if (si.SheetName.ToLower().Contains(filter) ||
                    si.ClassName.ToLower().Contains(filter))
                    return true;
            }
            return false;
        }

        private List<string> GetVisibleFiles()
        {
            string filter = txtFilter.Text.Trim().ToLower();
            return allFiles
                .Where(f => MatchesFilter(f.Path, f.Sheets, filter))
                .Select(f => f.Path)
                .ToList();
        }

        private void ToggleSelectAll()
        {
            var visible = GetVisibleFiles();
            if (visible.Count == 0) return;

            bool allChecked = visible.All(checkedFiles.Contains);
            if (allChecked)
                foreach (var f in visible) checkedFiles.Remove(f);
            else
                foreach (var f in visible) checkedFiles.Add(f);

            RenderFileList();
        }

        private void ExportCheckedFiles()
        {
            if (checkedFiles.Count == 0)
            {
                MessageBox.Show("请先勾选要导出的文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Config.I.ApplyConfig(txtExcelPath.Text, txtDataPath.Text, txtClassPath.Text, Config.I.exportList);
            var files = allFiles.Select(f => f.Path).Where(checkedFiles.Contains).ToList();
            using var dialog = new ExportDialog(files);
            dialog.ShowDialog(this);
        }

        private static void GetExcelFiles(string path, List<string> files)
        {
            foreach (string file in Directory.GetFiles(path, "*.xls"))
                files.Add(file);
            foreach (string file in Directory.GetFiles(path, "*.xlsx"))
                if (!files.Contains(file))
                    files.Add(file);
            foreach (string dir in Directory.GetDirectories(path))
                GetExcelFiles(dir, files);
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F1} MB";
        }

        private void OnLogReceived(string msg, LogLevel level)
        {
            if (txtLog.IsDisposed) return;
            if (txtLog.InvokeRequired)
            {
                try { txtLog.Invoke(new Action<string, LogLevel>(OnLogReceived), msg, level); } catch { }
                return;
            }

            string prefix = level switch
            {
                LogLevel.Error => "[ERROR] ",
                LogLevel.Warning => "[WARN]  ",
                _ => "[INFO]  "
            };
            txtLog.AppendText($"{prefix}{msg}{Environment.NewLine}");
        }

        private void ExportAll()
        {
            Config.I.ApplyConfig(txtExcelPath.Text, txtDataPath.Text, txtClassPath.Text, Config.I.exportList);
            using var dialog = new ExportDialog(null);
            dialog.ShowDialog(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Logger.OnLog -= OnLogReceived;
            base.OnFormClosed(e);
        }
    }

    // ──────────────────────────────────────────────

    class SubSheetsDialog : Form
    {
        private readonly string _filePath;

        public SubSheetsDialog(string filePath)
        {
            _filePath = filePath;
            SetupUI();
        }

        private void SetupUI()
        {
            Text = $"查看子表 - {Path.GetFileName(_filePath)}";
            Width = 720;
            Height = 400;
            StartPosition = FormStartPosition.CenterParent;

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 5,
                GrowStyle = TableLayoutPanelGrowStyle.AddRows
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));

            // Header row
            table.RowCount = 1;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            table.Controls.Add(MakeLabel("表名"), 0, 0);
            table.Controls.Add(MakeLabel("行x列"), 1, 0);
            table.Controls.Add(MakeLabel("修改时间"), 2, 0);
            table.Controls.Add(MakeLabel("检查"), 3, 0);
            table.Controls.Add(MakeLabel("导出"), 4, 0);

            // Load sheet infos
            var excelTool = new ExcelTool();
            List<SheetInfo> sheets;
            try
            {
                sheets = excelTool.GetSheetInfos(_filePath);
            }
            catch (Exception ex)
            {
                Logger.Error($"读取Sheet信息失败: {ex.Message}");
                sheets = new List<SheetInfo>();
            }

            //按修改时间倒序（最新在上）
            sheets = sheets.OrderByDescending(s => s.ModifyTime).ToList();

            int row = 1;
            foreach (var info in sheets)
            {
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

                var lblSheet = new Label
                {
                    Text = info.SheetName,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                table.Controls.Add(lblSheet, 0, row);

                var lblSize = new Label
                {
                    Text = $"{info.RowCount}x{info.ColumnCount}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                table.Controls.Add(lblSize, 1, row);

                var lblTime = new Label
                {
                    Text = info.ModifyTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                table.Controls.Add(lblTime, 2, row);

                var btnCheck = new Button { Text = "检查", Dock = DockStyle.Fill };
                int sheetIdx = info.SheetIndex;
                string sheetName = info.SheetName;
                btnCheck.Click += (s, e) => CheckSheet(sheetName, sheetIdx);
                table.Controls.Add(btnCheck, 3, row);

                var btnExport = new Button { Text = "导出", Dock = DockStyle.Fill };
                btnExport.Click += (s, e) => ExportSheet(sheetIdx);
                table.Controls.Add(btnExport, 4, row);

                row++;
            }

            if (sheets.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "未找到可用的Sheet",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                table.RowCount++;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
                table.Controls.Add(lblEmpty, 0, row);
                table.SetColumnSpan(lblEmpty, 5);
            }

            scrollPanel.Controls.Add(table);
            Controls.Add(scrollPanel);
        }

        private void CheckSheet(string sheetName, int sheetIndex)
        {
            var excelTool = new ExcelTool();

            var sheets = ExcelTool.ExcelToDataTable(_filePath);
            if (sheets == null || sheetIndex >= sheets.Count)
            {
                Logger.Error($"无法读取Sheet: index {sheetIndex}");
                return;
            }

            var issues = excelTool.ValidateSheet(sheets[sheetIndex], sheetName);
            foreach (var issue in issues)
                Logger.Log($"[{sheetName}] {issue}");

            if (issues.Count == 0)
                MessageBox.Show($"Sheet检查通过: {sheetName}", "检查结果",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"[{sheetName}] 发现 {issues.Count} 个问题，请查看日志", "检查结果",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ExportSheet(int sheetIndex)
        {
            using var dialog = new ExportDialog(_filePath, sheetIndex);
            dialog.ShowDialog(this);
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
        }
    }

    // ──────────────────────────────────────────────

    class ExportDialog : Form
    {
        private readonly CheckBox chkJson = new CheckBox();
        private readonly CheckBox chkBytes = new CheckBox();
        private readonly TextBox txtDataPath = new TextBox();
        private readonly TextBox txtClassPath = new TextBox();
        private readonly Button btnExport = new Button();
        private readonly string? _filePath;
        private readonly int? _sheetIndex;
        private readonly List<string>? _fileList;

        public ExportDialog(string? filePath, int? sheetIndex = null)
        {
            _filePath = filePath;
            _sheetIndex = sheetIndex;
            SetupUI();
        }

        public ExportDialog(List<string> files)
        {
            _fileList = files;
            SetupUI();
        }

        private void SetupUI()
        {
            Text = _fileList != null
                ? $"导出勾选 - {_fileList.Count} 个文件"
                : _filePath != null
                    ? $"导出 - {Path.GetFileName(_filePath)}"
                    : "导出全部";
            Width = 520;
            Height = 280;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(10)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            for (int i = 0; i < 5; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

            // Row 0: Export format checkboxes
            table.Controls.Add(MakeLabel("导出格式:"), 0, 0);
            var fmtPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight
            };
            chkJson.Text = "JSON";
            chkJson.Checked = Config.I.exportList.Contains(Config.ExportType.Json);
            chkBytes.Text = "Bytes";
            chkBytes.Checked = Config.I.exportList.Contains(Config.ExportType.Bytes);
            fmtPanel.Controls.Add(chkJson);
            fmtPanel.Controls.Add(chkBytes);
            table.Controls.Add(fmtPanel, 1, 0);
            table.SetColumnSpan(fmtPanel, 2);

            // Row 1: Data path
            table.Controls.Add(MakeLabel("数据输出路径:"), 0, 1);
            txtDataPath.Dock = DockStyle.Fill;
            txtDataPath.Text = Config.I.dataPath ?? "";
            table.Controls.Add(txtDataPath, 1, 1);
            var btnBrowseData = new Button { Text = "浏览...", Dock = DockStyle.Fill };
            btnBrowseData.Click += (s, e) => BrowseFolder(txtDataPath);
            table.Controls.Add(btnBrowseData, 2, 1);

            // Row 2: Class path
            table.Controls.Add(MakeLabel("C#类输出路径:"), 0, 2);
            txtClassPath.Dock = DockStyle.Fill;
            txtClassPath.Text = Config.I.classPath ?? "";
            table.Controls.Add(txtClassPath, 1, 2);
            var btnBrowseClass = new Button { Text = "浏览...", Dock = DockStyle.Fill };
            btnBrowseClass.Click += (s, e) => BrowseFolder(txtClassPath);
            table.Controls.Add(btnBrowseClass, 2, 2);

            // Row 3: spacer
            // Row 4: Export button
            btnExport.Text = "导出";
            btnExport.Dock = DockStyle.Fill;
            btnExport.Click += async (s, e) => await DoExport();
            table.Controls.Add(btnExport, 1, 4);
            table.SetColumnSpan(btnExport, 2);

            Controls.Add(table);
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
        }

        private static void BrowseFolder(TextBox target)
        {
            using var dialog = new FolderBrowserDialog();
            string path = target.Text.Trim();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                dialog.SelectedPath = Path.GetFullPath(path);
            if (dialog.ShowDialog() == DialogResult.OK)
                target.Text = dialog.SelectedPath;
        }

        private async Task DoExport()
        {
            var exportTypes = new List<Config.ExportType>();
            if (chkJson.Checked) exportTypes.Add(Config.ExportType.Json);
            if (chkBytes.Checked) exportTypes.Add(Config.ExportType.Bytes);

            if (exportTypes.Count == 0)
            {
                MessageBox.Show("请至少选择一种导出格式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDataPath.Text) || string.IsNullOrWhiteSpace(txtClassPath.Text))
            {
                MessageBox.Show("请设置输出路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Config.I.ApplyConfig(Config.I.excelPath, txtDataPath.Text, txtClassPath.Text, exportTypes);

            btnExport.Enabled = false;
            btnExport.Text = "导出中...";

            try
            {
                await Task.Run(() =>
                {
                    var excelTool = new ExcelTool();

                    if (_fileList != null)
                    {
                        Logger.Log($"开始导出勾选的 {_fileList.Count} 个文件...");
                        foreach (string file in _fileList)
                        {
                            if (file.Contains("~$"))
                                continue;
                            try { excelTool.CreateDataTable(file); }
                            catch (Exception ex) { Logger.Error(ex); }
                        }
                    }
                    else if (_filePath != null)
                    {
                        Logger.Log($"开始导出: {Path.GetFileName(_filePath)}");
                        if (_sheetIndex.HasValue)
                            excelTool.CreateDataTable(_filePath, _sheetIndex.Value);
                        else
                            excelTool.CreateDataTable(_filePath);
                    }
                    else
                    {
                        Logger.Log("开始导出全部文件...");
                        var fileTool = new FileTool();
                        fileTool.GetAllFiles(Config.I.excelPath);
                        foreach (string file in fileTool.fileList)
                        {
                            if (file.Contains("Enum.xlsx") || file.Contains("~$"))
                                continue;
                            if (file.Contains(".xls"))
                            {
                                try { excelTool.CreateDataTable(file); }
                                catch (Exception ex) { Logger.Error(ex); }
                            }
                        }
                    }
                });

                Logger.Log("导出完成!");
                MessageBox.Show("导出完成!", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"导出失败: {ex.Message}");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed && !btnExport.IsDisposed)
                {
                    btnExport.Enabled = true;
                    btnExport.Text = "导出";
                }
            }
        }
    }
}
