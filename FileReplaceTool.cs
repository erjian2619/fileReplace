using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FileReplaceTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try { NativeDpi.SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch { try { NativeDpi.SetProcessDPIAware(); } catch { } }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ModernMainForm());
        }
    }

    internal static class NativeDpi
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    }

    internal enum ReplaceMode { Files, WholeDirectory }

    internal sealed class MainForm : Form
    {
        private readonly RadioButton filesMode = new RadioButton();
        private readonly RadioButton wholeMode = new RadioButton();
        private readonly Label sourceLabel = new Label();
        private readonly Label targetLabel = new Label();
        private readonly Label directoryNameLabel = new Label();
        private readonly TextBox sourceBox = new TextBox();
        private readonly TextBox targetBox = new TextBox();
        private readonly TextBox directoryNameBox = new TextBox();
        private readonly Button sourceButton = new Button();
        private readonly Button sourceFolderButton = new Button();
        private readonly Label explanation = new Label();
        private readonly Label statusLabel = new Label();
        private readonly List<string> selectedFiles = new List<string>();
        private readonly string settingsPath;

        public MainForm()
        {
            settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileReplaceTool.ini");
            Text = "轻量文件替换工具";
            ClientSize = new Size(760, 500);
            MinimumSize = new Size(776, 539);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(244, 247, 251);
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            sourceBox.AllowDrop = true;
            sourceBox.DragEnter += OnDragEnter;
            sourceBox.DragDrop += OnDragDrop;

            var header = new Panel { Location = new Point(0, 0), Size = new Size(760, 82), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(30, 64, 175) };
            header.Controls.Add(new Label { Text = "轻量文件替换工具", Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(26, 15) });
            header.Controls.Add(new Label { Text = "拖入文件或目录，确认范围后安全替换", ForeColor = Color.FromArgb(219, 234, 254), AutoSize = true, Location = new Point(29, 53) });
            Controls.Add(header);

            var modePanel = new GroupBox { Text = " 替换方式 ", Location = new Point(24, 99), Size = new Size(712, 72), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.White, ForeColor = Color.FromArgb(51, 65, 85) };
            ConfigureRadio(filesMode, "文件替换（单个或多个）", 78);
            ConfigureRadio(wholeMode, "整个目录替换", 404);
            modePanel.Controls.Add(filesMode);
            modePanel.Controls.Add(wholeMode);
            Controls.Add(modePanel);

            AddPathRow(sourceLabel, sourceBox, sourceButton, 193);
            sourceButton.Click += ChooseSource;
            sourceFolderButton.Text = "选择目录…";
            sourceFolderButton.Location = new Point(516, 192);
            sourceFolderButton.Size = new Size(100, 30);
            sourceFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sourceFolderButton.Click += ChooseSourceFolder;
            Controls.Add(sourceFolderButton);
            var targetButton = new Button();
            AddPathRow(targetLabel, targetBox, targetButton, 243);
            targetButton.Text = "选择目录…";
            targetButton.Click += ChooseTarget;

            directoryNameLabel.Text = "新目录名称";
            directoryNameLabel.Location = new Point(28, 300);
            directoryNameLabel.AutoSize = true;
            directoryNameBox.Location = new Point(135, 293);
            directoryNameBox.Size = new Size(601, 28);
            directoryNameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(directoryNameLabel);
            Controls.Add(directoryNameBox);

            explanation.Location = new Point(28, 342);
            explanation.Size = new Size(708, 48);
            explanation.BackColor = Color.FromArgb(239, 246, 255);
            explanation.ForeColor = Color.FromArgb(30, 64, 175);
            explanation.Padding = new Padding(10, 8, 10, 8);
            explanation.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(explanation);

            var savePreset = new Button { Text = "保存目标目录", Location = new Point(566, 401), Size = new Size(170, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            StyleSecondaryButton(savePreset);
            savePreset.Click += delegate { SaveSettings(true); };
            Controls.Add(savePreset);

            statusLabel.Text = "等待操作";
            statusLabel.Location = new Point(28, 457);
            statusLabel.Size = new Size(470, 30);
            statusLabel.ForeColor = Color.DimGray;
            Controls.Add(statusLabel);

            var replaceButton = new Button
            {
                Text = "检查并开始替换",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Location = new Point(526, 446),
                Size = new Size(210, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            replaceButton.FlatAppearance.BorderSize = 0;
            replaceButton.Click += Replace;
            Controls.Add(replaceButton);

            filesMode.CheckedChanged += ModeChanged;
            wholeMode.CheckedChanged += ModeChanged;
            filesMode.Checked = true;
            StyleSecondaryButton(sourceButton);
            StyleSecondaryButton(sourceFolderButton);
            StyleSecondaryButton(targetButton);
            LoadSettings();
            ApplyMode();
        }

        private void ConfigureRadio(RadioButton radio, string text, int left)
        {
            radio.Text = text;
            radio.Location = new Point(left, 23);
            radio.Size = new Size(230, 34);
            radio.Appearance = Appearance.Button;
            radio.TextAlign = ContentAlignment.MiddleCenter;
            radio.FlatStyle = FlatStyle.Flat;
            radio.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            radio.BackColor = Color.FromArgb(248, 250, 252);
        }

        private void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(148, 163, 184);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(51, 65, 85);
        }

        private void AddPathRow(Label label, TextBox box, Button button, int top)
        {
            label.Location = new Point(28, top + 7);
            label.AutoSize = true;
            box.Location = new Point(135, top);
            box.Size = new Size(481, 28);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button.Location = new Point(626, top - 1);
            button.Size = new Size(110, 30);
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(label);
            Controls.Add(box);
            Controls.Add(button);
        }

        private ReplaceMode CurrentMode
        {
            get { return wholeMode.Checked ? ReplaceMode.WholeDirectory : ReplaceMode.Files; }
        }

        private void ModeChanged(object sender, EventArgs e) { if (((RadioButton)sender).Checked) ApplyMode(); }

        private void ApplyMode()
        {
            bool files = CurrentMode == ReplaceMode.Files;
            if (!files && selectedFiles.Count > 0)
            {
                selectedFiles.Clear();
                sourceBox.ReadOnly = false;
                sourceBox.Text = "";
            }
            sourceLabel.Text = files ? "来源" : "源目录";
            targetLabel.Text = "目标目录";
            sourceButton.Text = "选择文件…";
            sourceButton.Visible = files;
            sourceFolderButton.Visible = true;
            sourceFolderButton.Location = new Point(files ? 516 : 626, 192);
            sourceBox.Size = files ? new Size(371, 28) : new Size(481, 28);
            directoryNameLabel.Visible = !files;
            directoryNameBox.Visible = !files;
            if (!files && String.IsNullOrWhiteSpace(directoryNameBox.Text) && Directory.Exists(sourceBox.Text.Trim()))
                directoryNameBox.Text = new DirectoryInfo(sourceBox.Text.Trim()).Name;
            filesMode.BackColor = files ? Color.FromArgb(219, 234, 254) : Color.FromArgb(248, 250, 252);
            wholeMode.BackColor = files ? Color.FromArgb(248, 250, 252) : Color.FromArgb(219, 234, 254);
            filesMode.ForeColor = files ? Color.FromArgb(30, 64, 175) : Color.FromArgb(51, 65, 85);
            wholeMode.ForeColor = files ? Color.FromArgb(51, 65, 85) : Color.FromArgb(30, 64, 175);

            if (files)
                explanation.Text = "可多选或拖入文件（复制到目标目录根部），也可选择源目录并保持相对路径；其他文件保留。";
            else
                explanation.Text = "源目录将按“新目录名称”复制到目标位置；若已有同名目录，旧目录会自动改名并保留。";
        }

        private void ChooseSource(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Title = "选择一个或多个源文件", CheckFileExists = true, Multiselect = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                SetSelectedFiles(dialog.FileNames);
            }
        }

        private void ChooseSourceFolder(object sender, EventArgs e)
        {
            var selected = ChooseFolder("选择包含替换内容的源目录", sourceBox.ReadOnly ? "" : sourceBox.Text, false);
            if (selected != null)
            {
                selectedFiles.Clear();
                sourceBox.ReadOnly = false;
                sourceBox.Text = selected;
                if (CurrentMode == ReplaceMode.WholeDirectory)
                    directoryNameBox.Text = new DirectoryInfo(selected).Name;
            }
        }

        private void SetSelectedFiles(string[] paths)
        {
            selectedFiles.Clear();
            foreach (string path in paths)
                if (File.Exists(path) && !selectedFiles.Contains(path)) selectedFiles.Add(path);
            sourceBox.ReadOnly = selectedFiles.Count > 0;
            sourceBox.Text = selectedFiles.Count == 0 ? "" : "已选择 " + selectedFiles.Count + " 个文件（可重新选择或拖入）";
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
                MessageBox.Show(this, "请不要同时拖入文件和目录。", "无法添加", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (directories.Count > 0)
            {
                if (directories.Count != 1)
                {
                    MessageBox.Show(this, "一次只能拖入一个源目录。", "无法添加", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                selectedFiles.Clear();
                sourceBox.ReadOnly = false;
                sourceBox.Text = directories[0];
                if (CurrentMode == ReplaceMode.WholeDirectory)
                    directoryNameBox.Text = new DirectoryInfo(directories[0]).Name;
            }
            else if (files.Count > 0)
            {
                filesMode.Checked = true;
                SetSelectedFiles(files.ToArray());
            }
        }

        private void ChooseTarget(object sender, EventArgs e)
        {
            var selected = ChooseFolder(CurrentMode == ReplaceMode.Files ? "选择或新建目标目录" : "选择新目录所在的目标位置", targetBox.Text, true);
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
                string message;
                string effectiveTarget = CurrentMode == ReplaceMode.Files ? targetBox.Text.Trim() : GetWholeDestination();
                var sourceFiles = CurrentMode == ReplaceMode.Files ? GetFilesForCopy() : FileOperations.ListFiles(sourceBox.Text.Trim());
                int overwrite = 0;
                foreach (var sourceFile in sourceFiles)
                {
                    string relative = CurrentMode == ReplaceMode.Files && selectedFiles.Count > 0
                        ? Path.GetFileName(sourceFile)
                        : FileOperations.RelativePath(sourceBox.Text.Trim(), sourceFile);
                    if (File.Exists(Path.Combine(effectiveTarget, relative))) overwrite++;
                }
                int add = sourceFiles.Count - overwrite;
                if (CurrentMode == ReplaceMode.Files)
                    message = "将处理 " + sourceFiles.Count + " 个文件：覆盖 " + overwrite + " 个，新增 " + add + " 个。\r\n目标目录中的其他文件会保留。\r\n\r\n不会创建备份，是否继续？";
                else
                {
                    int oldCount = Directory.Exists(effectiveTarget) ? FileOperations.ListFiles(effectiveTarget).Count : 0;
                    message = !Directory.Exists(effectiveTarget)
                        ? "将创建目录：\r\n" + effectiveTarget + "\r\n并复制 " + sourceFiles.Count + " 个文件。\r\n\r\n是否继续？"
                        : "目标位置已有同名目录（" + oldCount + " 个文件）。\r\n旧目录会先改名为带 backup 时间戳的名称，再放入新目录：\r\n" + effectiveTarget + "\r\n\r\n是否继续？";
                }

                if (MessageBox.Show(this, message, "确认替换", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

                Cursor = Cursors.WaitCursor;
                string backupPath = null;
                if (CurrentMode == ReplaceMode.Files)
                {
                    if (selectedFiles.Count > 0) FileOperations.CopyFilesToRoot(selectedFiles, targetBox.Text.Trim());
                    else FileOperations.CopyPartial(sourceBox.Text.Trim(), targetBox.Text.Trim());
                }
                else
                    backupPath = FileOperations.ReplaceWholeDirectory(sourceBox.Text.Trim(), effectiveTarget);

                SaveSettings(false);
                statusLabel.Text = "替换成功  " + DateTime.Now.ToString("HH:mm:ss");
                statusLabel.ForeColor = Color.SeaGreen;
                string completion = backupPath == null ? "替换成功。" : "替换成功。\r\n\r\n旧目录已保留为：\r\n" + backupPath;
                MessageBox.Show(this, completion, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                statusLabel.Text = "替换失败";
                statusLabel.ForeColor = Color.Firebrick;
                MessageBox.Show(this, "操作未完成：\r\n" + ex.Message, "文件与目录替换工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private void ValidateInputs()
        {
            string source = sourceBox.Text.Trim();
            string target = targetBox.Text.Trim();
            if (target.Length == 0) throw new InvalidOperationException("请输入或选择目标目录。");
            if (File.Exists(target)) throw new InvalidOperationException("目标目录路径当前是一个文件。");
            if (Directory.Exists(target)) FileOperations.EnsureNormalDirectory(target);
            if (CurrentMode == ReplaceMode.Files && selectedFiles.Count > 0)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (string file in selectedFiles)
                {
                    if (!File.Exists(file)) throw new InvalidOperationException("源文件不存在：\r\n" + file);
                    if (!names.Add(Path.GetFileName(file))) throw new InvalidOperationException("多个源文件具有相同文件名，无法复制到同一目标目录。");
                    string destination = Path.Combine(target, Path.GetFileName(file));
                    if (FileOperations.SamePath(file, destination)) throw new InvalidOperationException("源文件和目标文件相同：\r\n" + file);
                }
            }
            else
            {
                if (CurrentMode == ReplaceMode.WholeDirectory)
                {
                    string name = directoryNameBox.Text.Trim();
                    if (name.Length == 0 || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
                        throw new InvalidOperationException("新目录名称无效。");
                    target = GetWholeDestination();
                    if (File.Exists(target)) throw new InvalidOperationException("新目录的目标路径当前是一个文件。");
                    if (Directory.Exists(target)) FileOperations.EnsureNormalDirectory(target);
                }
                if (!Directory.Exists(source)) throw new InvalidOperationException("请选择有效的源目录。");
                if (FileOperations.PathsOverlap(source, target)) throw new InvalidOperationException("源目录与目标目录不能相同，也不能互相包含。");
                FileOperations.EnsureNormalDirectory(source);
                if (FileOperations.ListFiles(source).Count == 0) throw new InvalidOperationException("源目录中没有可替换的文件。");
            }
            if (CurrentMode == ReplaceMode.WholeDirectory && Directory.GetParent(Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar)) == null)
                throw new InvalidOperationException("不能完整替换磁盘根目录。");
        }

        private string GetWholeDestination()
        {
            return Path.Combine(targetBox.Text.Trim(), directoryNameBox.Text.Trim());
        }

        private List<string> GetFilesForCopy()
        {
            return selectedFiles.Count > 0 ? new List<string>(selectedFiles) : FileOperations.ListFiles(sourceBox.Text.Trim());
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return;
                foreach (var line in File.ReadAllLines(settingsPath, Encoding.UTF8))
                {
                    int separator = line.IndexOf('=');
                    if (separator < 0) continue;
                    string key = line.Substring(0, separator);
                    string value = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(separator + 1)));
                    if (key == "Target") targetBox.Text = value;
                }
            }
            catch { }
        }

        private void SaveSettings(bool showConfirmation)
        {
            try
            {
                Func<string, string> encoded = value => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));
                string content = "Target=" + encoded(targetBox.Text.Trim()) + Environment.NewLine;
                File.WriteAllText(settingsPath, content, new UTF8Encoding(false));
                if (showConfirmation) MessageBox.Show(this, "目标目录已保存为预设。", "已保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (showConfirmation) MessageBox.Show(this, "无法保存预设：\r\n" + ex.Message, "文件与目录替换工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public static class FileOperations
    {
        public static bool SamePath(string first, string second)
        {
            return String.Equals(Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        public static bool PathsOverlap(string first, string second)
        {
            string a = Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string b = Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase) || b.StartsWith(a, StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureNormalDirectory(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("不支持目录链接或重解析点：\r\n" + path);
        }

        public static List<string> ListFiles(string root)
        {
            var result = new List<string>();
            CollectFiles(Path.GetFullPath(root), result);
            return result;
        }

        private static List<string> ListDirectories(string root)
        {
            var result = new List<string>();
            CollectDirectories(Path.GetFullPath(root), result);
            return result;
        }

        private static void CollectDirectories(string directory, List<string> result)
        {
            EnsureNormalDirectory(directory);
            foreach (string child in Directory.GetDirectories(directory))
            {
                EnsureNormalDirectory(child);
                result.Add(child);
                CollectDirectories(child, result);
            }
        }

        private static void CollectFiles(string directory, List<string> result)
        {
            EnsureNormalDirectory(directory);
            foreach (string file in Directory.GetFiles(directory))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("不支持文件链接或重解析点：\r\n" + file);
                result.Add(file);
            }
            foreach (string child in Directory.GetDirectories(directory)) CollectFiles(child, result);
        }

        public static string RelativePath(string root, string fullPath)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string normalizedFile = Path.GetFullPath(fullPath);
            if (!normalizedFile.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("文件不在源目录内。");
            return normalizedFile.Substring(normalizedRoot.Length);
        }

        public static void ReplaceSingle(string source, string destination)
        {
            string parent = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(parent);
            string temporary = Path.Combine(parent, ".replace-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.Copy(source, temporary, true);
                if (File.Exists(destination)) File.Replace(temporary, destination, null, true);
                else File.Move(temporary, destination);
                temporary = null;
            }
            finally { if (temporary != null && File.Exists(temporary)) File.Delete(temporary); }
        }

        public static void CopyPartial(string sourceRoot, string targetRoot)
        {
            foreach (string sourceDirectory in ListDirectories(sourceRoot))
            {
                string destinationDirectory = Path.Combine(targetRoot, RelativePath(sourceRoot, sourceDirectory));
                EnsureSafeTargetPath(targetRoot, destinationDirectory);
                Directory.CreateDirectory(destinationDirectory);
            }
            foreach (string sourceFile in ListFiles(sourceRoot))
            {
                string destination = Path.Combine(targetRoot, RelativePath(sourceRoot, sourceFile));
                EnsureSafeTargetPath(targetRoot, Path.GetDirectoryName(destination));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(sourceFile, destination, true);
            }
        }

        public static void CopyFilesToRoot(IList<string> sourceFiles, string targetRoot)
        {
            Directory.CreateDirectory(targetRoot);
            EnsureNormalDirectory(targetRoot);
            foreach (string sourceFile in sourceFiles)
            {
                if ((File.GetAttributes(sourceFile) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("不支持文件链接或重解析点：\r\n" + sourceFile);
                File.Copy(sourceFile, Path.Combine(targetRoot, Path.GetFileName(sourceFile)), true);
            }
        }

        private static void EnsureSafeTargetPath(string targetRoot, string directory)
        {
            string root = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar);
            string current = Path.GetFullPath(directory);
            while (current.Length >= root.Length)
            {
                if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("目标路径包含目录链接，已停止操作：\r\n" + current);
                if (SamePath(current, root)) break;
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
        }

        public static string ReplaceWholeDirectory(string sourceRoot, string targetRoot)
        {
            string target = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar);
            string parent = Directory.GetParent(target).FullName;
            Directory.CreateDirectory(parent);
            string name = Path.GetFileName(target);
            string staging = Path.Combine(parent, "." + name + "-new-" + Guid.NewGuid().ToString("N"));
            string backup = null;
            bool targetMoved = false;
            try
            {
                Directory.CreateDirectory(staging);
                CopyPartial(sourceRoot, staging);
                if (!Directory.Exists(target))
                {
                    Directory.Move(staging, target);
                    return null;
                }
                backup = GetBackupPath(parent, name);
                Directory.Move(target, backup);
                targetMoved = true;
                try
                {
                    Directory.Move(staging, target);
                }
                catch
                {
                    Directory.Move(backup, target);
                    targetMoved = false;
                    throw;
                }
                targetMoved = false;
                return backup;
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                if (targetMoved && backup != null && Directory.Exists(backup) && !Directory.Exists(target)) Directory.Move(backup, target);
            }
        }

        private static string GetBackupPath(string parent, string name)
        {
            string baseName = name + "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string candidate = Path.Combine(parent, baseName);
            int counter = 2;
            while (Directory.Exists(candidate) || File.Exists(candidate))
            {
                candidate = Path.Combine(parent, baseName + "_" + counter);
                counter++;
            }
            return candidate;
        }
    }
}
