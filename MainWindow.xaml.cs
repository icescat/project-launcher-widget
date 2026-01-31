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
        contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
        contextMenu.HorizontalOffset = position.X;
        contextMenu.VerticalOffset = position.Y;
        contextMenu.IsOpen = true;
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
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };

        // 创建布局
        Grid grid = new Grid {
            Margin = new Thickness(10)
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 项目名称
        Label nameLabel = new Label {
            Content = "项目名称:",
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(nameLabel, 0);
        TextBox nameTextBox = new TextBox {
            Text = project.Name,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(nameTextBox, 1);

        // 启动命令
        Label commandLabel = new Label {
            Content = "启动命令:",
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(commandLabel, 2);
        TextBox commandTextBox = new TextBox {
            Text = project.Command,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(commandTextBox, 3);

        // 工作目录
        Label pathLabel = new Label {
            Content = "工作目录:",
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(pathLabel, 4);
        TextBox pathTextBox = new TextBox {
            Text = project.Path,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(pathTextBox, 5);

        // 按钮
        StackPanel buttonPanel = new StackPanel {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        Button saveButton = new Button {
            Content = "保存",
            Width = 75,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Button cancelButton = new Button {
            Content = "取消",
            Width = 75
        };
        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        Grid.SetRow(buttonPanel, 6);

        // 添加到网格
        grid.Children.Add(nameLabel);
        grid.Children.Add(nameTextBox);
        grid.Children.Add(commandLabel);
        grid.Children.Add(commandTextBox);
        grid.Children.Add(pathLabel);
        grid.Children.Add(pathTextBox);
        grid.Children.Add(buttonPanel);

        // 设置窗口内容
        settingsWindow.Content = grid;

        // 按钮事件
        saveButton.Click += (s, args) => {
            // 保存设置
            project.Name = nameTextBox.Text;
            project.Command = commandTextBox.Text;
            project.Path = pathTextBox.Text;
            SaveProjects();
            UpdateProjectGrid();
            settingsWindow.Close();
        };

        cancelButton.Click += (s, args) => {
            settingsWindow.Close();
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
}