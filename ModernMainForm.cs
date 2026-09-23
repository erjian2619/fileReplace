using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FileReplaceTool
{
    internal sealed class ModernMainForm : Form
    {
        private static readonly Color Canvas = Color.FromArgb(246, 247, 251);
        private static readonly Color Surface = Color.White;
        private static readonly Color TextMain = Color.FromArgb(24, 31, 46);
        private static readonly Color TextMuted = Color.FromArgb(106, 116, 137);
        private static readonly Color Border = Color.FromArgb(224, 228, 237);
        private static readonly Color Accent = Color.FromArgb(91, 76, 226);
        private static readonly Color AccentSoft = Color.FromArgb(241, 239, 255);
        private static readonly Color Success = Color.FromArgb(23, 145, 100);

        private readonly ModeCard filesCard;
        private readonly ModeCard wholeCard;
        private readonly RoundedPanel sourceCard;
        private readonly RoundedPanel targetCard;
        private readonly DropPanel dropPanel;
        private readonly Label sourceDisplay;
        private readonly RoundedButton chooseFilesButton;
        private readonly RoundedButton chooseFolderButton;
        private readonly TextBox targetBox;
        private readonly ComboBox presetBox;
        private readonly RoundedPanel nameInputPanel;
        private readonly TextBox directoryNameBox;
        private readonly Label infoLabel;
        private readonly Label statusLabel;
        private readonly List<string> selectedFiles = new List<string>();
        private readonly string settingsPath;
        private ReplaceMode currentMode = ReplaceMode.Files;
        private string sourcePath = "";

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public ModernMainForm()
        {
            settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileReplaceTool.ini");
            Text = "轻量文件替换工具";
            ClientSize = new Size(860, 680);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            BackColor = Canvas;
            Padding = new Padding(0);
            Font = new Font("Microsoft YaHei UI", 9F);
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            var canvas = new Panel { Dock = DockStyle.Fill, BackColor = Canvas };
            Controls.Add(canvas);

            var titleBar = new Panel { Location = new Point(0, 0), Size = new Size(858, 66), BackColor = Surface, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            titleBar.MouseDown += DragWindow;
            canvas.Controls.Add(titleBar);

            var logo = new LogoMark { Location = new Point(25, 16), Size = new Size(34, 34) };
            titleBar.Controls.Add(logo);
            var title = new Label { Text = "文件替换", Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = TextMain, AutoSize = true, Location = new Point(72, 12) };
            var subtitle = new Label { Text = "快速、清晰地更新文件与目录", Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = TextMuted, AutoSize = true, Location = new Point(73, 38) };
            title.MouseDown += DragWindow;
            subtitle.MouseDown += DragWindow;
            titleBar.Controls.Add(title);
            titleBar.Controls.Add(subtitle);

            var closeButton = CreateWindowButton("×", new Point(810, 12));
            closeButton.Click += delegate { Close(); };
            closeButton.HoverBackColor = Color.FromArgb(254, 226, 226);
            closeButton.HoverForeColor = Color.FromArgb(185, 28, 28);
            var minimizeButton = CreateWindowButton("—", new Point(765, 12));
            minimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            titleBar.Controls.Add(minimizeButton);
            titleBar.Controls.Add(closeButton);

            var content = new Panel { Location = new Point(24, 82), Size = new Size(810, 578), BackColor = Canvas };
            canvas.Controls.Add(content);

            filesCard = new ModeCard("文件替换", "单个、多选或拖入", "F") { Location = new Point(0, 0), Size = new Size(394, 74) };
            wholeCard = new ModeCard("整个目录", "支持改名并保留旧目录", "D") { Location = new Point(416, 0), Size = new Size(394, 74) };
            filesCard.Click += delegate { SetMode(ReplaceMode.Files); };
            wholeCard.Click += delegate { SetMode(ReplaceMode.WholeDirectory); };
            content.Controls.Add(filesCard);
            content.Controls.Add(wholeCard);

            sourceCard = new RoundedPanel { Location = new Point(0, 92), Size = new Size(810, 128), Radius = 14, FillColor = Surface, BorderColor = Border };
            content.Controls.Add(sourceCard);
            sourceCard.Controls.Add(SectionLabel("01", "选择来源", new Point(20, 15)));

            dropPanel = new DropPanel { Location = new Point(20, 48), Size = new Size(530, 60), BackColor = Surface };
            dropPanel.AllowDrop = true;
            dropPanel.DragEnter += OnDragEnter;
            dropPanel.DragDrop += OnDragDrop;
            dropPanel.Click += delegate { if (currentMode == ReplaceMode.Files) ChooseFiles(); else ChooseSourceFolder(); };
            sourceDisplay = new Label
            {
                Text = "拖入文件或目录，或点击右侧按钮选择",
                ForeColor = TextMuted,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(48, 4),
                Size = new Size(466, 52),
                Cursor = Cursors.Hand
            };
            sourceDisplay.Click += delegate { if (currentMode == ReplaceMode.Files) ChooseFiles(); else ChooseSourceFolder(); };
            dropPanel.Controls.Add(new Label { Text = "＋", Font = new Font("Microsoft YaHei UI", 18F), ForeColor = Accent, TextAlign = ContentAlignment.MiddleCenter, Location = new Point(8, 4), Size = new Size(38, 52), Cursor = Cursors.Hand });
            dropPanel.Controls.Add(sourceDisplay);
            sourceCard.Controls.Add(dropPanel);

            chooseFolderButton = new RoundedButton { Text = "选择目录", Location = new Point(670, 58), Size = new Size(120, 40), Radius = 9, FillColor = Surface, BorderColor = Border, TextColor = TextMain };
            chooseFolderButton.Click += delegate { ChooseSourceFolder(); };
            chooseFilesButton = new RoundedButton { Text = "选择文件", Location = new Point(562, 58), Size = new Size(100, 40), Radius = 9, FillColor = AccentSoft, BorderColor = AccentSoft, TextColor = Accent };
            chooseFilesButton.Click += delegate { ChooseFiles(); };
            sourceCard.Controls.Add(chooseFilesButton);
            sourceCard.Controls.Add(chooseFolderButton);

            targetCard = new RoundedPanel { Location = new Point(0, 238), Size = new Size(810, 112), Radius = 14, FillColor = Surface, BorderColor = Border };
            content.Controls.Add(targetCard);
            targetCard.Controls.Add(SectionLabel("02", "目标设置", new Point(20, 15)));
            targetCard.Controls.Add(new Label { Text = "目标目录", AutoSize = true, ForeColor = TextMuted, Location = new Point(20, 58) });
            var targetInputPanel = new RoundedPanel { Location = new Point(106, 45), Size = new Size(552, 42), Radius = 9, FillColor = Color.FromArgb(250, 251, 253), BorderColor = Border };
            targetBox = new TextBox { BorderStyle = BorderStyle.None, BackColor = targetInputPanel.FillColor, ForeColor = TextMain, Font = new Font("Microsoft YaHei UI", 9.5F), Location = new Point(13, 11), Size = new Size(526, 24) };
            targetInputPanel.Controls.Add(targetBox);
            targetCard.Controls.Add(targetInputPanel);
            var chooseTargetButton = new RoundedButton { Text = "浏览…", Location = new Point(674, 45), Size = new Size(116, 42), Radius = 9, FillColor = AccentSoft, BorderColor = AccentSoft, TextColor = Accent };
            chooseTargetButton.Click += delegate { ChooseTarget(); };
            targetCard.Controls.Add(chooseTargetButton);

            targetCard.Controls.Add(new Label { Text = "游戏预设", AutoSize = true, ForeColor = TextMuted, Location = new Point(20, 109) });
            var presetInputPanel = new RoundedPanel { Location = new Point(106, 96), Size = new Size(552, 42), Radius = 9, FillColor = Color.FromArgb(250, 251, 253), BorderColor = Border };
            presetBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = presetInputPanel.FillColor, ForeColor = TextMain, Font = new Font("Microsoft YaHei UI", 9F), Location = new Point(10, 8), Size = new Size(530, 26) };
            presetBox.SelectedIndexChanged += PresetChanged;
            presetInputPanel.Controls.Add(presetBox);
            targetCard.Controls.Add(presetInputPanel);
            var scanButton = new RoundedButton { Text = "重新扫描", Location = new Point(674, 96), Size = new Size(116, 42), Radius = 9, FillColor = Surface, BorderColor = Border, TextColor = TextMain };
            scanButton.Click += delegate { RefreshGamePresets(true); };
            targetCard.Controls.Add(scanButton);

            nameInputPanel = new RoundedPanel { Location = new Point(106, 147), Size = new Size(684, 42), Radius = 9, FillColor = Color.FromArgb(250, 251, 253), BorderColor = Border };
            directoryNameBox = new TextBox { BorderStyle = BorderStyle.None, BackColor = nameInputPanel.FillColor, ForeColor = TextMain, Font = new Font("Microsoft YaHei UI", 9.5F), Location = new Point(13, 11), Size = new Size(656, 24) };
            nameInputPanel.Controls.Add(directoryNameBox);
            targetCard.Controls.Add(nameInputPanel);
            var nameLabel = new Label { Name = "DirectoryNameLabel", Text = "新目录名", AutoSize = true, ForeColor = TextMuted, Location = new Point(20, 160) };
            targetCard.Controls.Add(nameLabel);

            infoLabel = new Label { Location = new Point(2, 370), Size = new Size(806, 44), ForeColor = TextMuted, Font = new Font("Microsoft YaHei UI", 8.5F), TextAlign = ContentAlignment.MiddleLeft };
            content.Controls.Add(infoLabel);

            statusLabel = new Label { Text = "●  等待操作", ForeColor = TextMuted, AutoSize = true, Location = new Point(4, 538) };
            content.Controls.Add(statusLabel);
            var saveButton = new RoundedButton { Text = "保存目标目录", Location = new Point(486, 520), Size = new Size(142, 44), Radius = 10, FillColor = Surface, BorderColor = Border, TextColor = TextMain };
            saveButton.Click += delegate { SaveSettings(true); };
            content.Controls.Add(saveButton);
            var executeButton = new RoundedButton { Text = "检查并开始替换  →", Location = new Point(640, 520), Size = new Size(170, 44), Radius = 10, FillColor = Accent, BorderColor = Accent, TextColor = Color.White, Bold = true };
            executeButton.Click += Replace;
            content.Controls.Add(executeButton);

            LoadSettings();
            RefreshGamePresets(false);
            SetMode(ReplaceMode.Files);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            int roundPreference = 2;
            int noBorder = unchecked((int)0xFFFFFFFE);
            try { DwmSetWindowAttribute(Handle, 33, ref roundPreference, sizeof(int)); } catch { }
            try { DwmSetWindowAttribute(Handle, 34, ref noBorder, sizeof(int)); } catch { }
            ApplyWindowRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyWindowRegion();
        }

        private void ApplyWindowRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            int radius = Math.Max(12, (int)Math.Round(16F * Width / 860F));
            using (var path = UiShapes.RoundRect(new Rectangle(0, 0, Width, Height), radius))
            {
                var previous = Region;
                Region = new Region(path);
                if (previous != null) previous.Dispose();
            }
        }

        private RoundedButton CreateWindowButton(string text, Point location)
        {
            return new RoundedButton { Text = text, Location = location, Size = new Size(36, 36), Radius = 9, FillColor = Surface, BorderColor = Surface, TextColor = TextMuted, Font = new Font("Segoe UI", 12F) };
        }

        private Label SectionLabel(string number, string text, Point location)
        {
            return new Label { Text = number + "   " + text, Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold), ForeColor = TextMain, AutoSize = true, Location = location };
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, 0x2, 0);
        }

        private void SetMode(ReplaceMode mode)
        {
            currentMode = mode;
            filesCard.Selected = mode == ReplaceMode.Files;
            wholeCard.Selected = mode == ReplaceMode.WholeDirectory;
            if (mode == ReplaceMode.WholeDirectory && selectedFiles.Count > 0)
            {
                selectedFiles.Clear();
                sourcePath = "";
                UpdateSourceDisplay();
            }
            chooseFilesButton.Visible = mode == ReplaceMode.Files;
            chooseFolderButton.Location = new Point(Dip(mode == ReplaceMode.Files ? 670 : 658), Dip(58));
            chooseFolderButton.Size = new Size(Dip(mode == ReplaceMode.Files ? 120 : 132), Dip(40));
            nameInputPanel.Visible = mode == ReplaceMode.WholeDirectory;
            var nameLabel = targetCard.Controls["DirectoryNameLabel"];
            nameLabel.Visible = mode == ReplaceMode.WholeDirectory;
            targetCard.Height = Dip(mode == ReplaceMode.Files ? 158 : 209);
            infoLabel.Top = Dip(mode == ReplaceMode.Files ? 416 : 467);
            if (mode == ReplaceMode.WholeDirectory && String.IsNullOrWhiteSpace(directoryNameBox.Text) && Directory.Exists(sourcePath))
                directoryNameBox.Text = new DirectoryInfo(sourcePath).Name;
            infoLabel.Text = mode == ReplaceMode.Files
                ? "文件模式会覆盖同名文件并创建缺少的文件，目标目录中的其他内容保持不变。"
                : "若目标位置已有同名目录，旧目录会改名为“原名_backup_时间戳”并完整保留。";
            Invalidate(true);
        }

        private int Dip(int value)
        {
            if (filesCard == null || filesCard.Width <= 0) return value;
            return (int)Math.Round(value * filesCard.Width / 394F);
        }

        private void ChooseFiles()
        {
            using (var dialog = new OpenFileDialog { Title = "选择一个或多个源文件", CheckFileExists = true, Multiselect = true })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) SetSelectedFiles(dialog.FileNames);
            }
        }

        private void ChooseSourceFolder()
        {
            string selected = ChooseFolder("选择源目录", sourcePath, false);
            if (selected == null) return;
            selectedFiles.Clear();
            sourcePath = selected;
            if (currentMode == ReplaceMode.WholeDirectory) directoryNameBox.Text = new DirectoryInfo(selected).Name;
            UpdateSourceDisplay();
        }

        private void SetSelectedFiles(string[] paths)
        {
            selectedFiles.Clear();
            foreach (string path in paths)
                if (File.Exists(path) && !selectedFiles.Contains(path)) selectedFiles.Add(path);
            sourcePath = "";
            UpdateSourceDisplay();
        }

        private void UpdateSourceDisplay()
        {
            if (selectedFiles.Count == 1) sourceDisplay.Text = Path.GetFileName(selectedFiles[0]);
            else if (selectedFiles.Count > 1) sourceDisplay.Text = "已选择 " + selectedFiles.Count + " 个文件";
            else if (Directory.Exists(sourcePath)) sourceDisplay.Text = sourcePath;
            else sourceDisplay.Text = currentMode == ReplaceMode.Files ? "拖入一个或多个文件，也可以拖入目录" : "拖入一个源目录，或点击右侧按钮选择";
            sourceDisplay.ForeColor = selectedFiles.Count > 0 || Directory.Exists(sourcePath) ? TextMain : TextMuted;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length == 0) return;
            var files = new List<string>();
            var directories = new List<string>();
            foreach (string path in paths)
            {
                if (File.Exists(path)) files.Add(path);
                else if (Directory.Exists(path)) directories.Add(path);
            }
            if (files.Count > 0 && directories.Count > 0)
            {
                ShowWarning("请不要同时拖入文件和目录。");
                return;
            }
            if (directories.Count > 1)
            {
                ShowWarning("一次只能拖入一个源目录。");
                return;
            }
            if (directories.Count == 1)
            {
                selectedFiles.Clear();
                sourcePath = directories[0];
                if (currentMode == ReplaceMode.WholeDirectory) directoryNameBox.Text = new DirectoryInfo(sourcePath).Name;
                UpdateSourceDisplay();
            }
            else if (files.Count > 0)
            {
                SetMode(ReplaceMode.Files);
                SetSelectedFiles(files.ToArray());
            }
        }

        private void PresetChanged(object sender, EventArgs e)
        {
            var install = presetBox.SelectedItem as GameInstall;
            if (install != null && install.Path.Length > 0) targetBox.Text = install.Path;
        }

        private void RefreshGamePresets(bool showStatus)
        {
            var installs = GameInstallScanner.FindPathOfExile2();
            presetBox.BeginUpdate();
            try
            {
                presetBox.Items.Clear();
                presetBox.Items.Add(new GameInstall(installs.Count == 0 ? "未检测到流放之路2，可手动浏览" : "选择检测到的流放之路2目录", ""));
                foreach (var install in installs) presetBox.Items.Add(install);
                presetBox.SelectedIndex = 0;
            }
            finally { presetBox.EndUpdate(); }
            if (showStatus)
            {
                statusLabel.Text = installs.Count == 0 ? "●  未检测到游戏安装目录" : "●  已找到 " + installs.Count + " 个游戏安装位置";
                statusLabel.ForeColor = installs.Count == 0 ? TextMuted : Success;
            }
        }

        private void ChooseTarget()
        {
            string description = currentMode == ReplaceMode.Files ? "选择或新建目标目录" : "选择新目录所在的目标位置";
            string selected = ChooseFolder(description, targetBox.Text, true);
            if (selected != null) targetBox.Text = selected;
        }

        private string ChooseFolder(string description, string current, bool allowNew)
        {
            using (var dialog = new FolderBrowserDialog { Description = description, ShowNewFolderButton = allowNew })
            {
                if (Directory.Exists(current)) dialog.SelectedPath = current;
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        private void Replace(object sender, EventArgs e)
        {
            try
            {
                ValidateInputs();
                string effectiveTarget = currentMode == ReplaceMode.Files ? targetBox.Text.Trim() : GetWholeDestination();
                var sourceFiles = currentMode == ReplaceMode.Files ? GetFilesForCopy() : FileOperations.ListFiles(sourcePath);
                int overwrite = 0;
                foreach (string sourceFile in sourceFiles)
                {
                    string relative = currentMode == ReplaceMode.Files && selectedFiles.Count > 0 ? Path.GetFileName(sourceFile) : FileOperations.RelativePath(sourcePath, sourceFile);
                    if (File.Exists(Path.Combine(effectiveTarget, relative))) overwrite++;
                }
                int add = sourceFiles.Count - overwrite;
                string message;
                if (currentMode == ReplaceMode.Files)
                    message = "将处理 " + sourceFiles.Count + " 个文件：覆盖 " + overwrite + " 个，新增 " + add + " 个。\r\n目标目录中的其他内容会保留。\r\n\r\n是否继续？";
                else if (Directory.Exists(effectiveTarget))
                    message = "目标位置已有同名目录。\r\n旧目录会先改名为带 backup 时间戳的名称，再放入新目录：\r\n" + effectiveTarget + "\r\n\r\n是否继续？";
                else
                    message = "将创建新目录并复制 " + sourceFiles.Count + " 个文件：\r\n" + effectiveTarget + "\r\n\r\n是否继续？";

                if (MessageBox.Show(this, message, "确认替换", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                Cursor = Cursors.WaitCursor;
                string backupPath = null;
                if (currentMode == ReplaceMode.Files)
                {
                    if (selectedFiles.Count > 0) FileOperations.CopyFilesToRoot(selectedFiles, targetBox.Text.Trim());
                    else FileOperations.CopyPartial(sourcePath, targetBox.Text.Trim());
                }
                else backupPath = FileOperations.ReplaceWholeDirectory(sourcePath, effectiveTarget);
                SaveSettings(false);
                statusLabel.Text = "●  替换成功  " + DateTime.Now.ToString("HH:mm:ss");
                statusLabel.ForeColor = Success;
                string done = backupPath == null ? "替换成功。" : "替换成功。\r\n\r\n旧目录已保留为：\r\n" + backupPath;
                MessageBox.Show(this, done, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "●  操作未完成";
                statusLabel.ForeColor = Color.FromArgb(190, 52, 52);
                MessageBox.Show(this, "操作未完成：\r\n" + ex.Message, "文件替换", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void ValidateInputs()
        {
            string target = targetBox.Text.Trim();
            if (target.Length == 0) throw new InvalidOperationException("请输入或选择目标目录。");
            if (File.Exists(target)) throw new InvalidOperationException("目标目录路径当前是一个文件。");
            if (Directory.Exists(target)) FileOperations.EnsureNormalDirectory(target);
            if (currentMode == ReplaceMode.Files && selectedFiles.Count > 0)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in selectedFiles)
                {
                    if (!File.Exists(file)) throw new InvalidOperationException("源文件不存在：\r\n" + file);
                    if (!names.Add(Path.GetFileName(file))) throw new InvalidOperationException("多个源文件具有相同文件名，无法复制到同一目标目录。");
                    if (FileOperations.SamePath(file, Path.Combine(target, Path.GetFileName(file)))) throw new InvalidOperationException("源文件和目标文件相同：\r\n" + file);
                }
            }
            else
            {
                if (!Directory.Exists(sourcePath)) throw new InvalidOperationException("请选择有效的源目录。");
                string destination = target;
                if (currentMode == ReplaceMode.WholeDirectory)
                {
                    string name = directoryNameBox.Text.Trim();
                    if (name.Length == 0 || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
                        throw new InvalidOperationException("新目录名称无效。");
                    destination = GetWholeDestination();
                    if (File.Exists(destination)) throw new InvalidOperationException("新目录的目标路径当前是一个文件。");
                }
                if (FileOperations.PathsOverlap(sourcePath, destination)) throw new InvalidOperationException("源目录与目标目录不能相同，也不能互相包含。");
                FileOperations.EnsureNormalDirectory(sourcePath);
                if (FileOperations.ListFiles(sourcePath).Count == 0) throw new InvalidOperationException("源目录中没有可替换的文件。");
            }
        }

        private string GetWholeDestination()
        {
            return Path.Combine(targetBox.Text.Trim(), directoryNameBox.Text.Trim());
        }

        private List<string> GetFilesForCopy()
        {
            return selectedFiles.Count > 0 ? new List<string>(selectedFiles) : FileOperations.ListFiles(sourcePath);
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return;
                foreach (string line in File.ReadAllLines(settingsPath, Encoding.UTF8))
                {
                    int separator = line.IndexOf('=');
                    if (separator < 0 || line.Substring(0, separator) != "Target") continue;
                    targetBox.Text = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(separator + 1)));
                }
            }
            catch { }
        }

        private void SaveSettings(bool showConfirmation)
        {
            try
            {
                string value = Convert.ToBase64String(Encoding.UTF8.GetBytes(targetBox.Text.Trim()));
                File.WriteAllText(settingsPath, "Target=" + value + Environment.NewLine, new UTF8Encoding(false));
                if (showConfirmation) MessageBox.Show(this, "目标目录已保存。", "已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (showConfirmation) MessageBox.Show(this, "无法保存目标目录：\r\n" + ex.Message, "文件替换", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(this, message, "无法添加", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    internal sealed class GameInstall
    {
        public string Label { get; private set; }
        public string Path { get; private set; }

        public GameInstall(string label, string path)
        {
            Label = label;
            Path = path ?? "";
        }

        public override string ToString()
        {
            return Path.Length == 0 ? Label : Label + "  ·  " + Path;
        }
    }

    internal static class GameInstallScanner
    {
        private const string SteamAppId = "2694490";

        public static List<GameInstall> FindPathOfExile2()
        {
            var results = new List<GameInstall>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ScanUninstallRegistry(results, seen, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            ScanUninstallRegistry(results, seen, Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            ScanUninstallRegistry(results, seen, Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            ScanSteam(results, seen);
            ScanEpic(results, seen);
            ScanCommonLocations(results, seen);
            return results;
        }

        private static void Add(List<GameInstall> results, HashSet<string> seen, string label, string path)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path)) return;
                path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')).TrimEnd(Path.DirectorySeparatorChar);
                if (!Directory.Exists(path)) return;
                path = Path.GetFullPath(path);
                if (seen.Add(path)) results.Add(new GameInstall(label, path));
            }
            catch { }
        }

        private static void ScanUninstallRegistry(List<GameInstall> results, HashSet<string> seen, RegistryKey hive, string keyPath)
        {
            try
            {
                using (var root = hive.OpenSubKey(keyPath))
                {
                    if (root == null) return;
                    foreach (string subKeyName in root.GetSubKeyNames())
                    {
                        using (var app = root.OpenSubKey(subKeyName))
                        {
                            if (app == null) continue;
                            string displayName = Convert.ToString(app.GetValue("DisplayName"));
                            bool isPoe2 = subKeyName.IndexOf("Steam App " + SteamAppId, StringComparison.OrdinalIgnoreCase) >= 0
                                || displayName.IndexOf("Path of Exile 2", StringComparison.OrdinalIgnoreCase) >= 0
                                || displayName.IndexOf("流放之路2", StringComparison.OrdinalIgnoreCase) >= 0;
                            if (!isPoe2) continue;
                            string location = Convert.ToString(app.GetValue("InstallLocation"));
                            if (String.IsNullOrWhiteSpace(location)) location = DirectoryFromDisplayIcon(Convert.ToString(app.GetValue("DisplayIcon")));
                            Add(results, seen, subKeyName.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0 ? "Steam" : "已安装客户端", location);
                        }
                    }
                }
            }
            catch { }
        }

        private static string DirectoryFromDisplayIcon(string value)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(value)) return "";
                value = value.Trim();
                if (value.StartsWith("\""))
                {
                    int closing = value.IndexOf('"', 1);
                    if (closing > 1) value = value.Substring(1, closing - 1);
                }
                else
                {
                    int comma = value.IndexOf(',');
                    if (comma > 0) value = value.Substring(0, comma);
                }
                return Path.GetDirectoryName(value);
            }
            catch { return ""; }
        }

        private static void ScanSteam(List<GameInstall> results, HashSet<string> seen)
        {
            var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string value = Convert.ToString(key.GetValue("SteamPath"));
                        if (!String.IsNullOrWhiteSpace(value)) steamRoots.Add(value.Replace('/', '\\'));
                    }
                }
            }
            catch { }
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!String.IsNullOrWhiteSpace(programFilesX86)) steamRoots.Add(Path.Combine(programFilesX86, "Steam"));

            var libraries = new HashSet<string>(steamRoots, StringComparer.OrdinalIgnoreCase);
            foreach (string steamRoot in steamRoots)
            {
                try
                {
                    string libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                    if (!File.Exists(libraryFile)) continue;
                    string text = File.ReadAllText(libraryFile);
                    foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                        libraries.Add(match.Groups[1].Value.Replace("\\\\", "\\"));
                }
                catch { }
            }

            foreach (string library in libraries)
            {
                try
                {
                    string steamApps = Path.Combine(library, "steamapps");
                    string installDir = "Path of Exile 2";
                    string manifest = Path.Combine(steamApps, "appmanifest_" + SteamAppId + ".acf");
                    if (File.Exists(manifest))
                    {
                        Match match = Regex.Match(File.ReadAllText(manifest), "\\\"installdir\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                        if (match.Success) installDir = match.Groups[1].Value;
                    }
                    Add(results, seen, "Steam", Path.Combine(steamApps, "common", installDir));
                }
                catch { }
            }
        }

        private static void ScanEpic(List<GameInstall> results, HashSet<string> seen)
        {
            try
            {
                string manifests = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Epic", "EpicGamesLauncher", "Data", "Manifests");
                if (!Directory.Exists(manifests)) return;
                foreach (string file in Directory.GetFiles(manifests, "*.item"))
                {
                    string text = File.ReadAllText(file);
                    if (text.IndexOf("Path of Exile 2", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    Match match = Regex.Match(text, "\\\"InstallLocation\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                    if (match.Success) Add(results, seen, "Epic Games", match.Groups[1].Value.Replace("\\\\", "\\").Replace("\\/", "/"));
                }
            }
            catch { }
        }

        private static void ScanCommonLocations(List<GameInstall> results, HashSet<string> seen)
        {
            string[] relativePaths =
            {
                @"Program Files\Grinding Gear Games\Path of Exile 2",
                @"Program Files (x86)\Grinding Gear Games\Path of Exile 2",
                @"Games\Path of Exile 2",
                @"WeGameApps\rail_apps\Path of Exile 2",
                @"WeGameApps\rail_apps\流放之路2",
                @"WeGameApps\rail_apps\流放之路：降临",
                @"SteamLibrary\steamapps\common\Path of Exile 2",
                @"Steam\steamapps\common\Path of Exile 2"
            };
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady || drive.DriveType == DriveType.CDRom) continue;
                    foreach (string relative in relativePaths)
                    {
                        string label = relative.IndexOf("Steam", StringComparison.OrdinalIgnoreCase) >= 0 ? "Steam（常见位置）" : "本地安装";
                        Add(results, seen, label, Path.Combine(drive.RootDirectory.FullName, relative));
                    }
                }
            }
            catch { }
        }
    }

    internal sealed class RoundedPanel : Panel
    {
        public int Radius { get; set; }
        private Color fillColor;
        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; BackColor = value; Invalidate(); }
        }
        public Color BorderColor { get; set; }

        public RoundedPanel()
        {
            Radius = 12;
            FillColor = Color.White;
            BorderColor = Color.Gainsboro;
            DoubleBuffered = true;
            BackColor = FillColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DeviceDpi / 96F;
            using (var path = UiShapes.RoundRect(ClientRectangle, (int)(Radius * scale)))
            using (var fill = new SolidBrush(FillColor))
            using (var pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(fill, path);
                var borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var borderPath = UiShapes.RoundRect(borderRect, (int)(Radius * scale))) e.Graphics.DrawPath(pen, borderPath);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class DropPanel : Panel
    {
        public DropPanel() { DoubleBuffered = true; Cursor = Cursors.Hand; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DeviceDpi / 96F;
            int inset = Math.Max(1, (int)scale);
            using (var path = UiShapes.RoundRect(new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1), (int)(10 * scale)))
            using (var pen = new Pen(Color.FromArgb(205, 199, 255), 1F * scale))
            {
                e.Graphics.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }

    internal sealed class RoundedButton : Control
    {
        public int Radius { get; set; }
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public Color TextColor { get; set; }
        public Color HoverBackColor { get; set; }
        public Color HoverForeColor { get; set; }
        public bool Bold { get; set; }
        private bool hovering;

        public RoundedButton()
        {
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            Radius = 10;
            FillColor = Color.White;
            BorderColor = Color.Gainsboro;
            TextColor = Color.Black;
            HoverBackColor = Color.Empty;
            HoverForeColor = Color.Empty;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
            float scale = DeviceDpi / 96F;
            Color fillColor = hovering && HoverBackColor != Color.Empty ? HoverBackColor : FillColor;
            Color textColor = hovering && HoverForeColor != Color.Empty ? HoverForeColor : TextColor;
            using (var path = UiShapes.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), (int)(Radius * scale)))
            using (var brush = new SolidBrush(fillColor))
            using (var pen = new Pen(BorderColor))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            using (var font = Bold ? new Font(Font, FontStyle.Bold) : new Font(Font, FontStyle.Regular))
            {
                TextRenderer.DrawText(e.Graphics, Text, font, ClientRectangle, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
    }

    internal sealed class ModeCard : Control
    {
        private readonly string title;
        private readonly string subtitle;
        private readonly string mark;
        private bool selected;
        public bool Selected { get { return selected; } set { selected = value; Invalidate(); } }

        public ModeCard(string title, string subtitle, string mark)
        {
            this.title = title;
            this.subtitle = subtitle;
            this.mark = mark;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
            float scale = DeviceDpi / 96F;
            Func<int, int> px = value => (int)Math.Round(value * scale);
            Color accent = Color.FromArgb(91, 76, 226);
            Color border = selected ? accent : Color.FromArgb(224, 228, 237);
            Color fill = selected ? Color.FromArgb(248, 247, 255) : Color.White;
            using (var path = UiShapes.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), px(14)))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border, selected ? 1.6F : 1F))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            using (var circleBrush = new SolidBrush(selected ? accent : Color.FromArgb(241, 243, 248))) e.Graphics.FillEllipse(circleBrush, px(18), px(17), px(40), px(40));
            using (var markFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, mark, markFont, new Rectangle(px(18), px(17), px(40), px(40)), selected ? Color.White : Color.FromArgb(106, 116, 137),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            using (var titleFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, title, titleFont, new Point(px(72), px(15)), Color.FromArgb(24, 31, 46), TextFormatFlags.NoPadding);
            using (var subFont = new Font("Microsoft YaHei UI", 8F))
                TextRenderer.DrawText(e.Graphics, subtitle, subFont, new Point(px(72), px(41)), Color.FromArgb(106, 116, 137), TextFormatFlags.NoPadding);
            if (selected)
            {
                using (var checkBrush = new SolidBrush(accent)) e.Graphics.FillEllipse(checkBrush, Width - px(34), px(25), px(20), px(20));
                using (var checkFont = new Font("Segoe UI Symbol", 8F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, "✓", checkFont, new Rectangle(Width - px(34), px(25), px(20), px(20)), Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
        }
    }

    internal sealed class LogoMark : Control
    {
        public LogoMark() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = DeviceDpi / 96F;
            Func<int, int> px = value => (int)Math.Round(value * scale);
            using (var brush = new SolidBrush(Color.FromArgb(91, 76, 226))) e.Graphics.FillEllipse(brush, 0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(Color.White, 2F * scale))
            {
                e.Graphics.DrawRectangle(pen, px(9), px(8), px(13), px(16));
                e.Graphics.DrawLine(pen, px(13), px(12), px(25), px(12));
                e.Graphics.DrawLine(pen, px(13), px(17), px(25), px(17));
            }
        }
    }

    internal static class UiShapes
    {
        public static GraphicsPath RoundRect(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
