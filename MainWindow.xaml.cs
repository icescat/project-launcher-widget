using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


namespace ProjectLauncherWidget;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private List<Project> projects = new List<Project>();
    private Point dragStartPoint;

    public MainWindow()
    {
        InitializeComponent();
        LoadProjects();
        UpdateProjectGrid();
    }

    // 窗口拖拽移动
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is Window || e.Source is Grid) {
            DragMove();
        }
    }

    // 关闭按钮
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // 拖拽进入
    private void Window_PreviewDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            e.Effects = DragDropEffects.Copy;
            DragDropHint.Visibility = Visibility.Visible;
        } else {
            e.Effects = DragDropEffects.None;
        }
    }

    // 拖拽移动
    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            e.Effects = DragDropEffects.Copy;
        } else {
            e.Effects = DragDropEffects.None;
        }
    }

    // 拖拽释放
    private void Window_PreviewDrop(object sender, DragEventArgs e)
    {
        DragDropHint.Visibility = Visibility.Collapsed;
        if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) {
                if (Directory.Exists(file)) {
                    AddProjectFromPath(file);
                } else if (File.Exists(file)) {
                    // 也支持拖拽文件
                    string directoryPath = Path.GetDirectoryName(file);
                    AddProjectFromPath(directoryPath);
                }
            }
        }
    }

    // 项目图标点击
    private void ProjectIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        var projectId = border.Tag.ToString();
        var project = projects.Find(p => p.Id == projectId);
        if (project != null) {
            LaunchProject(project);
        }
    }

    // 项目图标右键菜单
    private void ProjectIcon_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var border = sender as Border;
        var projectId = border.Tag.ToString();
        var project = projects.Find(p => p.Id == projectId);
        if (project != null) {
            ShowContextMenu(project, e.GetPosition(this));
        }
    }

    // 显示上下文菜单
        private void ShowContextMenu(Project project, Point position)
        {
            var contextMenu = new ContextMenu();

            // 设置菜单项
            var settingsMenuItem = new MenuItem { Header = "设置" };
            settingsMenuItem.Click += (s, args) => ShowProjectSettings(project);
            contextMenu.Items.Add(settingsMenuItem);

            // 添加分隔线
            contextMenu.Items.Add(new Separator());

            // 打开项目目录菜单项
            var openDirMenuItem = new MenuItem { Header = "打开项目目录" };
            openDirMenuItem.Click += (s, args) => {
                try {
                    if (Directory.Exists(project.Path)) {
                        System.Diagnostics.Process.Start("explorer.exe", project.Path);
                    } else {
                        MessageBox.Show($"项目目录不存在：{project.Path}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"打开目录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            contextMenu.Items.Add(openDirMenuItem);

            // 复制项目路径菜单项
            var copyPathMenuItem = new MenuItem { Header = "复制项目路径" };
            copyPathMenuItem.Click += (s, args) => {
                try {
                    Clipboard.SetText(project.Path);
                    MessageBox.Show("项目路径已复制到剪贴板。", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (Exception ex) {
                    MessageBox.Show($"复制路径失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            contextMenu.Items.Add(copyPathMenuItem);

            // 打开README菜单项
            var readmeMenuItem = new MenuItem { Header = "打开README文件" };
            readmeMenuItem.Click += (s, args) => OpenReadmeFile(project);
            contextMenu.Items.Add(readmeMenuItem);

            // 添加分隔线
            contextMenu.Items.Add(new Separator());

            // 删除菜单项
            var deleteMenuItem = new MenuItem { Header = "删除快捷方式" };
            deleteMenuItem.Click += (s, args) => {
                if (MessageBox.Show("确定要删除此快捷方式吗？", "确认删除", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    projects.Remove(project);
                    SaveProjects();
                    UpdateProjectGrid();
                }
            };
            contextMenu.Items.Add(deleteMenuItem);

            // 显示菜单
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            contextMenu.IsOpen = true;
        }

    // 打开README文件
    private void OpenReadmeFile(Project project)
    {
        try {
            // 查找README文件
            string[] readmeFiles = Directory.GetFiles(project.Path, "README*", SearchOption.TopDirectoryOnly);
            if (readmeFiles.Length > 0) {
                // 用记事本打开README文件
                System.Diagnostics.Process.Start("notepad.exe", readmeFiles[0]);
            } else {
                MessageBox.Show("项目中未找到README文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        } catch (Exception ex) {
            MessageBox.Show($"打开README文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 项目网格鼠标按下
    private void ProjectGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(null);
    }

    // 项目网格鼠标移动
    private void ProjectGrid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        // 暂时不实现拖拽排序功能
    }

    // 项目网格鼠标释放
    private void ProjectGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 暂时不实现拖拽排序功能
    }

    // 从路径添加项目
    private void AddProjectFromPath(string path)
    {
        string projectName = Path.GetFileName(path);
        string iconPath = GetDefaultIconPath();

        // 尝试提取项目图标
        string[] iconFiles = Directory.GetFiles(path, "*.ico", SearchOption.AllDirectories);
        if (iconFiles.Length > 0) {
            iconPath = iconFiles[0];
        }

        // 创建项目对象
        var project = new Project {
            Id = Guid.NewGuid().ToString(),
            Name = projectName,
            Path = path,
            Command = GetDefaultCommand(path),
            IconPath = iconPath,
            Icon = GetIcon(iconPath)
        };

        projects.Add(project);
        SaveProjects();
        UpdateProjectGrid();
    }

    // 获取默认命令
    private string GetDefaultCommand(string path)
    {
        // 检查项目类型并返回默认命令
        if (Directory.Exists(Path.Combine(path, "node_modules")) || File.Exists(Path.Combine(path, "package.json"))) {
            return "npm start";
        } else if (File.Exists(Path.Combine(path, "requirements.txt"))) {
            // 查找主Python文件
            string[] pyFiles = Directory.GetFiles(path, "*.py", SearchOption.TopDirectoryOnly);
            if (pyFiles.Length > 0) {
                return $"python {Path.GetFileName(pyFiles[0])}";
            }
        } else if (Directory.GetFiles(path, "*.sln", SearchOption.TopDirectoryOnly).Length > 0) {
            return "dotnet run";
        }
        return "";
    }

    // 获取默认图标路径
    private string GetDefaultIconPath()
    {
        // 返回默认图标路径，使用系统默认图标
        return "";
    }

    // 获取图标
    private ImageSource GetIcon(string iconPath)
    {
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath)) {
            try {
                return new BitmapImage(new Uri(iconPath));
            } catch {
                // 如果图标加载失败，使用默认图标
            }
        }
        // 使用默认图标
        return new BitmapImage();
    }

    // 启动项目
        private void LaunchProject(Project project)
        {
            try {
                // 检查命令是否为空
                if (string.IsNullOrEmpty(project.Command)) {
                    MessageBox.Show("启动命令为空，请在设置中配置启动命令。", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 检查工作目录是否存在
                if (!Directory.Exists(project.Path)) {
                    MessageBox.Show($"工作目录不存在：{project.Path}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 执行命令
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{project.Command}\"",  // 使用 /k 保持窗口打开
                    WorkingDirectory = project.Path,
                    UseShellExecute = true,  // 使用ShellExecute以便正确处理环境变量
                    CreateNoWindow = false
                };

                // 设置启动选项
                if (project.RunAsAdmin) {
                    startInfo.Verb = "runas";  // 以管理员身份运行
                }

                if (project.MinimizeWindow) {
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;  // 最小化窗口
                }

                // 启动进程
                System.Diagnostics.Process.Start(startInfo);
            } catch (Exception ex) {
                MessageBox.Show($"启动失败：{ex.Message}\n\n请检查环境配置。", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    // 显示项目设置
    private void ShowProjectSettings(Project project)
    {
        // 创建设置对话框
        Window settingsWindow = new Window {
            Title = "项目设置",
            Width = 600,
            Height = 350,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        // 创建布局
        Grid grid = new Grid {
            Margin = new Thickness(10)
        };
        
        // 添加行定义
        for (int i = 0; i < 8; i++) {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
        
        // 添加列定义
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 1. 项目名称 - 第一行
        StackPanel namePanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 10)
        };
        Label nameLabel = new Label {
            Content = "项目名称:",
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox nameTextBox = new TextBox {
            Text = project.Name,
            Width = 450
        };
        namePanel.Children.Add(nameLabel);
        namePanel.Children.Add(nameTextBox);
        Grid.SetRow(namePanel, 0);
        Grid.SetColumnSpan(namePanel, 2);

        // 2. 启动命令 - 第二行
        StackPanel commandPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Label commandLabel = new Label {
            Content = "启动命令:",
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox commandTextBox = new TextBox {
            Text = project.Command,
            Width = 450
        };
        commandPanel.Children.Add(commandLabel);
        commandPanel.Children.Add(commandTextBox);
        Grid.SetRow(commandPanel, 1);
        Grid.SetColumnSpan(commandPanel, 2);

        // 3. 项目目录 - 第三行
        StackPanel pathPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Label pathLabel = new Label {
            Content = "项目目录:",
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox pathTextBox = new TextBox {
            Text = project.Path,
            Width = 420,
            Margin = new Thickness(0, 0, 5, 0)
        };
        Button browseButton = new Button {
            Content = "📂",
            Width = 30,
            Height = 23
        };
        pathPanel.Children.Add(pathLabel);
        pathPanel.Children.Add(pathTextBox);
        pathPanel.Children.Add(browseButton);
        Grid.SetRow(pathPanel, 2);
        Grid.SetColumnSpan(pathPanel, 2);

        // 4. 项目图标 - 第四行
        StackPanel iconPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Label iconLabel = new Label {
            Content = "项目图标:",
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        // 创建带换行的按钮内容
        StackPanel buttonContent = new StackPanel {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBlock selectText = new TextBlock {
            Text = "选择",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        TextBlock iconText = new TextBlock {
            Text = "图标",
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        buttonContent.Children.Add(selectText);
        buttonContent.Children.Add(iconText);
        
        Button iconButton = new Button {
            Content = buttonContent,
            Width = 50,
            Height = 50,
            Margin = new Thickness(0, 0, 10, 0)
        };
        
        // 图标预览
        StackPanel iconPreview = new StackPanel {
            Width = 50,
            Height = 50,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        if (project.Icon != null) {
            Image iconImage = new Image {
                Source = project.Icon,
                Stretch = Stretch.Uniform,
                Width = 50,
                Height = 50
            };
            iconPreview.Children.Add(iconImage);
        } else {
            TextBlock noIconText = new TextBlock {
                Text = "无图标",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10
            };
            iconPreview.Children.Add(noIconText);
        }
        iconPanel.Children.Add(iconLabel);
        iconPanel.Children.Add(iconButton);
        iconPanel.Children.Add(iconPreview);
        Grid.SetRow(iconPanel, 3);
        Grid.SetColumnSpan(iconPanel, 2);

        // 5. 启动选项 - 第五行
        StackPanel launchPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Label launchLabel = new Label {
            Content = "启动选项:",
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        CheckBox adminCheckBox = new CheckBox {
            Content = "以管理员身份运行",
            IsChecked = project.RunAsAdmin,
            Margin = new Thickness(0, 0, 20, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        CheckBox minimizeCheckBox = new CheckBox {
            Content = "最小化窗口运行",
            IsChecked = project.MinimizeWindow,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        launchPanel.Children.Add(launchLabel);
        launchPanel.Children.Add(adminCheckBox);
        launchPanel.Children.Add(minimizeCheckBox);
        Grid.SetRow(launchPanel, 4);
        Grid.SetColumnSpan(launchPanel, 2);

        // 6. 按钮 - 第六行
        StackPanel buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10)
        };
        Button testButton = new Button {
            Content = "测试启动",
            Width = 100,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Button saveButton = new Button {
            Content = "保存",
            Width = 100,
            Height = 30,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Button cancelButton = new Button {
            Content = "取消",
            Width = 100,
            Height = 30
        };
        buttonPanel.Children.Add(testButton);
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 5);
        Grid.SetColumnSpan(buttonPanel, 2);

        // 添加所有控件到网格
        grid.Children.Add(namePanel);
        grid.Children.Add(commandPanel);
        grid.Children.Add(pathPanel);
        grid.Children.Add(iconPanel);
        grid.Children.Add(launchPanel);
        grid.Children.Add(buttonPanel);

        // 设置窗口内容
        settingsWindow.Content = grid;

        // 浏览按钮点击事件
        browseButton.Click += (s, args) => {
            var dialog = new Microsoft.Win32.OpenFolderDialog {
                Title = "选择工作目录"
            };
            if (dialog.ShowDialog() == true) {
                pathTextBox.Text = dialog.FolderName;
            }
        };

        // 选择图标按钮点击事件
        iconButton.Click += (s, args) => {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = "图标文件 (*.ico)|*.ico|所有文件 (*.*)|*.*",
                Title = "选择项目图标"
            };
            if (dialog.ShowDialog() == true) {
                project.IconPath = dialog.FileName;
                project.Icon = new BitmapImage(new Uri(dialog.FileName));
                
                // 更新图标预览
                iconPreview.Children.Clear();
                Image iconImage = new Image {
                    Source = project.Icon,
                    Stretch = Stretch.Uniform,
                    Width = 40,
                    Height = 40
                };
                iconPreview.Children.Add(iconImage);
            }
        };

        // 保存按钮点击事件
        saveButton.Click += (s, args) => {
            // 保存设置
            project.Name = nameTextBox.Text;
            project.Command = commandTextBox.Text;
            project.Path = pathTextBox.Text;
            project.RunAsAdmin = adminCheckBox.IsChecked ?? false;
            project.MinimizeWindow = minimizeCheckBox.IsChecked ?? false;
            SaveProjects();
            UpdateProjectGrid();
            settingsWindow.Close();
        };

        // 取消按钮点击事件
        cancelButton.Click += (s, args) => {
            settingsWindow.Close();
        };

        // 测试按钮点击事件
        testButton.Click += (s, args) => {
            try {
                // 获取当前设置的命令和路径
                string testCommand = commandTextBox.Text;
                string testPath = pathTextBox.Text;

                // 检查命令是否为空
                if (string.IsNullOrEmpty(testCommand)) {
                    MessageBox.Show("启动命令为空，请先配置启动命令。", "测试错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 检查工作目录是否存在
                if (!Directory.Exists(testPath)) {
                    MessageBox.Show($"工作目录不存在：{testPath}", "测试错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 执行测试命令
                var startInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{testCommand}\"",  // 使用 /k 保持窗口打开
                    WorkingDirectory = testPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                // 启动进程
                System.Diagnostics.Process.Start(startInfo);

                // 显示测试成功消息
                MessageBox.Show("测试命令已执行，请查看命令窗口输出。", "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show($"测试失败：{ex.Message}", "测试错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        // 显示窗口
        settingsWindow.ShowDialog();
    }

    // 加载项目
    private void LoadProjects()
    {
        string configPath = GetConfigPath();
        if (File.Exists(configPath)) {
            try {
                string json = File.ReadAllText(configPath);
                // 这里可以使用JSON库解析配置文件
                // 暂时使用模拟数据
                projects = new List<Project>();
            } catch (Exception ex) {
                MessageBox.Show($"加载配置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                projects = new List<Project>();
            }
        } else {
            // 添加示例项目
            projects.Add(new Project {
                Id = Guid.NewGuid().ToString(),
                Name = "Sample Project",
                Path = Environment.CurrentDirectory,
                Command = "echo Hello World",
                IconPath = GetDefaultIconPath(),
                Icon = GetIcon(GetDefaultIconPath())
            });
        }
    }

    // 保存项目
    private void SaveProjects()
    {
        string configPath = GetConfigPath();
        string configDir = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(configDir)) {
            Directory.CreateDirectory(configDir);
        }
        // 这里可以使用JSON库保存配置文件
        // 暂时仅创建空文件
        File.WriteAllText(configPath, "");
    }

    // 获取配置文件路径
    private string GetConfigPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjectLauncherWidget", "config.json");
    }

    // 更新项目网格
    private void UpdateProjectGrid()
    {
        ProjectGrid.ItemsSource = null;
        ProjectGrid.ItemsSource = projects;
    }
}

// 项目模型
public class Project
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public ImageSource Icon { get; set; } = null;
    public bool RunAsAdmin { get; set; } = false;
    public bool MinimizeWindow { get; set; } = false;
}