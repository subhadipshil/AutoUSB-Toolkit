using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AutoUSBToolkit.Core;
using AutoUSBToolkit.Models;

namespace AutoUSBToolkit.UI
{
    /// <summary>
    /// Linear-inspired Minimalist Dashboard.
    /// Features clean dark surfaces, subtle 1px borders, precision spacing,
    /// and a streamlined two-column sidebar navigation.
    /// </summary>
    public class MainWindow : Window
    {
        // Core State
        private List<UsbDriveInfo> _availableDrives = new List<UsbDriveInfo>();
        private UsbDriveInfo _selectedDrive = null;
        private List<CarProfile> _profiles = new List<CarProfile>();
        private CarProfile _selectedProfile = null;
        private AuditResult _currentAudit = null;
        private bool _isBusy = false;
        private int _currentViewIndex = 0; // 0=Format, 1=Music, 2=Doctor, 3=FatSort, 4=Profiles

        // Sidebar Controls
        private Border[] _sidebarButtons;
        private TextBlock[] _sidebarTexts;
        private TextBlock _tbSidebarDriveTitle;
        private TextBlock _tbSidebarDriveSpecs;
        private ProgressBar _pbSidebarDriveUsage;
        private TextBlock _tbSidebarStatusDot;
        private CheckBox _chkShowFixed;

        // Header Controls
        private TextBlock _tbHeaderBreadcrumb;
        private TextBlock _tbHeaderSub;
        private Border _borderAdminPill;

        // Content Views (Switched via sidebar)
        private UIElement _viewFormat;
        private UIElement _viewMusic;
        private UIElement _viewDoctor;
        private UIElement _viewFatSort;
        private UIElement _viewProfiles;

        // View 1: Format Controls
        private ComboBox _cbCarPresets;
        private TextBlock _tbPresetDescription;
        private TextBlock _tbPresetSpecs;
        private ComboBox _cbFileSystem;
        private ComboBox _cbClusterSize;
        private TextBox _tbVolumeLabel;
        private CheckBox _chkEnforceMbr;
        private CheckBox _chkDirectFat32;
        private CheckBox _chkQuickFormat;
        private Button _btnFormat;

        // View 2: Music Sync Controls
        private TextBox _tbSourcePath;
        private Button _btnBrowseSource;
        private CheckBox _chkSanitizeNames;
        private CheckBox _chkPurgeJunk;
        private CheckBox _chkNormalizeTags;
        private CheckBox _chkResizeArt;
        private CheckBox _chkAutoFatSort;
        private RadioButton _rbOrgKeep;
        private RadioButton _rbOrgArtistAlbum;
        private RadioButton _rbOrgBatches;
        private Button _btnSync;

        // View 3: Drive Doctor Controls
        private Button _btnScanAudit;
        private Button _btnCleanFix;
        private TextBlock _tbAuditSummary;
        private StackPanel _spIssueList;

        // View 4: FAT Sorter Controls
        private Button _btnFatSort;
        private TextBlock _tbFatSortSummary;

        // View 5: Profiles Controls
        private TextBox _tbProfileSearch;
        private ListBox _lbProfileList;
        private TextBlock _tbProfileDetailHeader;
        private TextBlock _tbProfileDetailBody;
        private Button _btnApplyProfile;

        // Footer / Status Controls
        private ProgressBar _pbProgress;
        private TextBlock _tbStatus;
        private Button _btnToggleLog;
        private Border _borderLogConsole;
        private TextBox _tbLogConsole;
        private Button _btnCopyLog;

        public MainWindow()
        {
            Theme.ApplyGlobalSystemTheme();
            InitializeWindowSettings();
            BuildUi();
            BindEvents();
            LoadInitialData();
        }

        private void InitializeWindowSettings()
        {
            Title = "AutoUSB Toolkit";
            Width = 1040;
            Height = 740;
            MinWidth = 920;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Theme.WindowBackground;
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, Inter, -apple-system, sans-serif");
            try { Icon = Theme.GetAppIconImage(); } catch { }
        }

        private void BuildUi()
        {
            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) }); // Left Sidebar
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });   // 1px Border
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Content

            // 1. Left Sidebar
            var sidebar = BuildSidebar();
            mainGrid.Children.Add(sidebar);
            Grid.SetColumn(sidebar, 0);

            // Divider Line
            var divider = new Border { Background = Theme.BorderSubtle };
            mainGrid.Children.Add(divider);
            Grid.SetColumn(divider, 1);

            // 2. Right Content Area
            var rightContent = BuildContentArea();
            mainGrid.Children.Add(rightContent);
            Grid.SetColumn(rightContent, 2);

            Content = mainGrid;
        }

        #region Sidebar Builder

        private UIElement BuildSidebar()
        {
            var sidebarBorder = new Border
            {
                Background = Theme.SidebarBackground,
                Padding = new Thickness(14, 16, 14, 16)
            };

            var dock = new DockPanel();

            // --- Top: Workspace Brand ---
            var topStack = new StackPanel();

            var brandGrid = new Grid { Margin = new Thickness(4, 0, 4, 14) };
            brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            brandGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Workspace Icon
            var iconBox = Theme.CreateAppIconElement(24, 5);
            brandGrid.Children.Add(iconBox);
            Grid.SetColumn(iconBox, 0);

            // Title
            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            var title = new TextBlock
            {
                Text = "AutoUSB",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextPrimary
            };
            titleStack.Children.Add(title);
            brandGrid.Children.Add(titleStack);
            Grid.SetColumn(titleStack, 1);

            // Live USB Status Dot
            _tbSidebarStatusDot = new TextBlock
            {
                Text = "●",
                FontSize = 12,
                Foreground = Theme.TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "USB Drive Connection Status"
            };
            brandGrid.Children.Add(_tbSidebarStatusDot);
            Grid.SetColumn(_tbSidebarStatusDot, 2);

            topStack.Children.Add(brandGrid);

            // --- Quick Action Button ---
            var btnQuickOpt = new Button
            {
                Height = 32,
                Background = Theme.CardBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                Foreground = Theme.TextPrimary,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 16)
            };
            var btnQuickStack = new StackPanel { Orientation = Orientation.Horizontal };
            btnQuickStack.Children.Add(new TextBlock { Text = "⚡", FontSize = 12, Foreground = Theme.LinearAccent, Margin = new Thickness(0, 0, 8, 0) });
            btnQuickStack.Children.Add(new TextBlock { Text = "Format Drive", VerticalAlignment = VerticalAlignment.Center });
            btnQuickOpt.Content = btnQuickStack;
            btnQuickOpt.Click += (s, e) => SwitchView(0);
            topStack.Children.Add(btnQuickOpt);

            // --- Navigation Group ---
            var lblNav = new TextBlock
            {
                Text = "WORKFLOW",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextMuted,
                Margin = new Thickness(6, 0, 6, 8)
            };
            topStack.Children.Add(lblNav);

            _sidebarButtons = new Border[5];
            _sidebarTexts = new TextBlock[5];

            string[] navIcons = { "⚡", "♫", "✦", "⇅", "≡" };
            string[] navTitles = { "Format & Prep", "Music Sync", "Drive Doctor", "FAT Sorter", "Car Profiles" };

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                var navBorder = new Border
                {
                    Height = 32,
                    CornerRadius = new CornerRadius(5),
                    Background = (i == 0) ? Theme.CardHover : Brushes.Transparent,
                    Padding = new Thickness(10, 0, 10, 0),
                    Margin = new Thickness(0, 1, 0, 1),
                    Cursor = Cursors.Hand
                };

                var navRow = new Grid();
                navRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                navRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var iconTb = new TextBlock
                {
                    Text = navIcons[i],
                    FontSize = 12,
                    Foreground = (i == 0) ? Theme.TextPrimary : Theme.TextSecondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                navRow.Children.Add(iconTb);
                Grid.SetColumn(iconTb, 0);

                var titleTb = new TextBlock
                {
                    Text = navTitles[i],
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Foreground = (i == 0) ? Theme.TextPrimary : Theme.TextSecondary,
                    VerticalAlignment = VerticalAlignment.Center
                };
                navRow.Children.Add(titleTb);
                Grid.SetColumn(titleTb, 1);

                navBorder.Child = navRow;
                navBorder.MouseEnter += (s, e) => { if (_currentViewIndex != index) navBorder.Background = Theme.InputBackground; };
                navBorder.MouseLeave += (s, e) => { if (_currentViewIndex != index) navBorder.Background = Brushes.Transparent; };
                navBorder.MouseDown += (s, e) => SwitchView(index);

                _sidebarButtons[i] = navBorder;
                _sidebarTexts[i] = titleTb;
                topStack.Children.Add(navBorder);
            }

            dock.Children.Add(topStack);
            DockPanel.SetDock(topStack, Dock.Top);

            // --- Bottom: Active Device Card & System Status ---
            var bottomStack = new StackPanel();

            var lblDevice = new TextBlock
            {
                Text = "ACTIVE USB DRIVE",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextMuted,
                Margin = new Thickness(6, 0, 6, 8)
            };
            bottomStack.Children.Add(lblDevice);

            var cardDevice = new Border
            {
                Background = Theme.CardBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(2, 0, 2, 12)
            };
            var devInner = new StackPanel();

            _tbSidebarDriveTitle = new TextBlock
            {
                Text = "No USB Drive",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextPrimary,
                Margin = new Thickness(0, 0, 0, 2)
            };
            devInner.Children.Add(_tbSidebarDriveTitle);

            _tbSidebarDriveSpecs = new TextBlock
            {
                Text = "Plug in a flash drive",
                FontSize = 11,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(0, 0, 0, 6)
            };
            devInner.Children.Add(_tbSidebarDriveSpecs);

            _pbSidebarDriveUsage = new ProgressBar
            {
                Height = 3,
                Background = Theme.BorderSubtle,
                Foreground = Theme.LinearAccent,
                BorderThickness = new Thickness(0),
                Value = 0,
                Maximum = 100
            };
            devInner.Children.Add(_pbSidebarDriveUsage);
            cardDevice.Child = devInner;
            bottomStack.Children.Add(cardDevice);

            // Refresh & Show Fixed toggle
            var btnRefresh = Theme.CreateSecondaryButton("⟳ Refresh Drives", 28);
            btnRefresh.FontSize = 11;
            btnRefresh.Click += (s, e) => RefreshDrives();
            bottomStack.Children.Add(btnRefresh);

            _chkShowFixed = new CheckBox
            {
                Content = "Show All Drives",
                Foreground = Theme.TextMuted,
                FontSize = 11,
                Margin = new Thickness(6, 8, 6, 0),
                Cursor = Cursors.Hand
            };
            bottomStack.Children.Add(_chkShowFixed);

            dock.Children.Add(bottomStack);
            DockPanel.SetDock(bottomStack, Dock.Bottom);

            sidebarBorder.Child = dock;
            return sidebarBorder;
        }

        #endregion

        #region Content Area Builder

        private UIElement BuildContentArea()
        {
            var contentGrid = new Grid { Background = Theme.ContentBackground };
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) }); // Top Bar
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // View Area
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Bottom Progress & Status
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Collapsible Log Console

            // 1. Top Header Bar
            var topBar = BuildTopBar();
            contentGrid.Children.Add(topBar);
            Grid.SetRow(topBar, 0);

            // 2. View Container
            var viewContainer = new Grid { Margin = new Thickness(24, 16, 24, 16) };

            _viewFormat = BuildFormatView();
            _viewMusic = BuildMusicSyncView();
            _viewDoctor = BuildDriveDoctorView();
            _viewFatSort = BuildFatSorterView();
            _viewProfiles = BuildCarProfilesView();

            viewContainer.Children.Add(_viewFormat);
            viewContainer.Children.Add(_viewMusic);
            viewContainer.Children.Add(_viewDoctor);
            viewContainer.Children.Add(_viewFatSort);
            viewContainer.Children.Add(_viewProfiles);

            // Set default view: Format
            _viewFormat.Visibility = Visibility.Visible;
            _viewMusic.Visibility = Visibility.Collapsed;
            _viewDoctor.Visibility = Visibility.Collapsed;
            _viewFatSort.Visibility = Visibility.Collapsed;
            _viewProfiles.Visibility = Visibility.Collapsed;

            contentGrid.Children.Add(viewContainer);
            Grid.SetRow(viewContainer, 1);

            // 3. Footer Bar
            var footer = BuildFooter();
            contentGrid.Children.Add(footer);
            Grid.SetRow(footer, 2);

            // 4. Log Console
            _borderLogConsole = BuildLogConsole();
            contentGrid.Children.Add(_borderLogConsole);
            Grid.SetRow(_borderLogConsole, 3);

            return contentGrid;
        }

        private UIElement BuildTopBar()
        {
            var border = new Border
            {
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 0, 24, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Breadcrumb
            var leftStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var breadcrumbRow = new StackPanel { Orientation = Orientation.Horizontal };

            _tbHeaderBreadcrumb = new TextBlock
            {
                Text = "Format & Prep",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextPrimary
            };
            breadcrumbRow.Children.Add(_tbHeaderBreadcrumb);

            var sep = new TextBlock
            {
                Text = " / ",
                FontSize = 13,
                Foreground = Theme.TextMuted,
                Margin = new Thickness(6, 0, 6, 0)
            };
            breadcrumbRow.Children.Add(sep);

            _tbHeaderSub = new TextBlock
            {
                Text = "Automotive Compatibility Setup",
                FontSize = 13,
                Foreground = Theme.TextSecondary
            };
            breadcrumbRow.Children.Add(_tbHeaderSub);
            leftStack.Children.Add(breadcrumbRow);

            grid.Children.Add(leftStack);
            Grid.SetColumn(leftStack, 0);

            // Right Actions
            var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // App Brand Pill
            var brandBadge = new Border
            {
                Background = Theme.CardBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            brandStack.Children.Add(Theme.CreateAppIconElement(14, 3));
            var tbBrand = new TextBlock
            {
                Text = "AutoUSB Toolkit",
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandStack.Children.Add(tbBrand);
            brandBadge.Child = brandStack;
            rightStack.Children.Add(brandBadge);

            bool isAdmin = DiskFormatter.IsAdministrator();
            _borderAdminPill = Theme.CreateBadge(
                isAdmin ? "✦ Admin" : "• User",
                isAdmin ? Theme.CardBackground : Theme.InputBackground,
                isAdmin ? Theme.Success : Theme.TextMuted);
            _borderAdminPill.BorderBrush = Theme.BorderSubtle;
            _borderAdminPill.BorderThickness = new Thickness(1);
            _borderAdminPill.Margin = new Thickness(0, 0, 8, 0);
            _borderAdminPill.ToolTip = isAdmin ? "Running with elevated disk partition privileges" : "Running in standard mode";
            rightStack.Children.Add(_borderAdminPill);

            var btnCopyLogTop = Theme.CreateSecondaryButton("≡ Logs", 28);
            btnCopyLogTop.FontSize = 11;
            btnCopyLogTop.Click += (s, e) => ToggleLogConsole();
            rightStack.Children.Add(btnCopyLogTop);

            grid.Children.Add(rightStack);
            Grid.SetColumn(rightStack, 1);

            border.Child = grid;
            return border;
        }

        #endregion

        #region View 1: Format & Prep (Linear Style)

        private UIElement BuildFormatView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();

            // Preset Overview Card
            var cardPreset = Theme.CreateCard();
            var presetStack = new StackPanel();

            presetStack.Children.Add(Theme.CreateSectionHeader("Vehicle Compatibility Preset", "Loads verified automotive criteria for your infotainment system."));

            var presetRow = new Grid { Margin = new Thickness(0, 4, 0, 10) };
            presetRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _cbCarPresets = new ComboBox
            {
                Height = 32,
                FontSize = 12
            };
            var cbDataTemplate = new DataTemplate();
            var cbTbFactory = new FrameworkElementFactory(typeof(TextBlock));
            cbTbFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("DisplayTitle"));
            cbTbFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            cbTbFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            cbTbFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ComboBoxItem), 1) });
            cbDataTemplate.VisualTree = cbTbFactory;
            _cbCarPresets.ItemTemplate = cbDataTemplate;
            Theme.StyleComboBox(_cbCarPresets);

            presetRow.Children.Add(_cbCarPresets);
            presetStack.Children.Add(presetRow);

            var specBox = new Border
            {
                Background = Theme.InputBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var specInner = new StackPanel();
            _tbPresetDescription = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Theme.LinearAccent, Margin = new Thickness(0, 0, 0, 4) };
            _tbPresetSpecs = new TextBlock { FontSize = 11, Foreground = Theme.TextSecondary, TextWrapping = TextWrapping.Wrap, LineHeight = 16 };
            specInner.Children.Add(_tbPresetDescription);
            specInner.Children.Add(_tbPresetSpecs);
            specBox.Child = specInner;
            presetStack.Children.Add(specBox);

            cardPreset.Child = presetStack;
            stack.Children.Add(cardPreset);

            // Storage Configuration Card (Linear Property List)
            var cardConfig = Theme.CreateCard();
            var configStack = new StackPanel();
            configStack.Children.Add(Theme.CreateSectionHeader("Volume & Sector Configuration"));

            // Property: File System
            _cbFileSystem = new ComboBox { Width = 260, Height = 28, FontSize = 12 };
            _cbFileSystem.Items.Add("FAT32 (Automotive Standard — Recommended)");
            _cbFileSystem.Items.Add("exFAT (Modern 2018+ Systems)");
            _cbFileSystem.Items.Add("NTFS (Specialized Systems)");
            _cbFileSystem.SelectedIndex = 0;
            Theme.StyleComboBox(_cbFileSystem);
            configStack.Children.Add(Theme.CreatePropertyRow("File System", "Target storage format required by your vehicle head unit.", _cbFileSystem));

            // Property: Cluster Size
            _cbClusterSize = new ComboBox { Width = 260, Height = 28, FontSize = 12 };
            _cbClusterSize.Items.Add("32 KB (Automotive Standard — Stutter-Free)");
            _cbClusterSize.Items.Add("16 KB (Standard for 4GB - 16GB)");
            _cbClusterSize.Items.Add("64 KB (High Bitrate Audio)");
            _cbClusterSize.Items.Add("Default Sector Size");
            _cbClusterSize.SelectedIndex = 0;
            Theme.StyleComboBox(_cbClusterSize);
            configStack.Children.Add(Theme.CreatePropertyRow("Allocation Unit (Cluster)", "32 KB prevents cache overflows and stutter in car stereos.", _cbClusterSize));

            // Property: Volume Label
            _tbVolumeLabel = new TextBox
            {
                Text = "CAR_USB",
                Width = 260,
                Height = 28,
                FontSize = 12,
                Background = Theme.InputBackground,
                Foreground = Theme.TextPrimary,
                BorderBrush = Theme.BorderSubtle,
                Padding = new Thickness(6, 4, 6, 4)
            };
            configStack.Children.Add(Theme.CreatePropertyRow("Volume Label", "Max 11 uppercase ASCII characters (e.g. CAR_USB).", _tbVolumeLabel));

            // Property: Enforce MBR
            _chkEnforceMbr = Theme.CreateCheckBox("", true);
            configStack.Children.Add(Theme.CreatePropertyRow("Enforce MBR Partition Table", "Converts modern GPT tables to MBR (fixes 'No Media' error on car stereos).", _chkEnforceMbr));

            // Property: Direct FAT32 Engine
            _chkDirectFat32 = Theme.CreateCheckBox("", true);
            configStack.Children.Add(Theme.CreatePropertyRow("Direct FAT32 Car Engine", "Breaks Windows 32 GB limit and formats up to 2 TB as FAT32.", _chkDirectFat32));

            // Property: Quick Format
            _chkQuickFormat = Theme.CreateCheckBox("", true);
            configStack.Children.Add(Theme.CreatePropertyRow("Quick Format", "Initializes file allocation tables in 1-2 seconds without full-surface zeroing.", _chkQuickFormat));

            cardConfig.Child = configStack;
            stack.Children.Add(cardConfig);

            // Action Row
            var actionGrid = new Grid { Margin = new Thickness(2, 4, 2, 8) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var warnText = new TextBlock
            {
                Text = "⚠️ All data on the target USB drive will be permanently erased.",
                Foreground = Theme.Danger,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            actionGrid.Children.Add(warnText);
            Grid.SetColumn(warnText, 0);

            _btnFormat = Theme.CreatePrimaryButton("⚡ Format & Optimize USB for Car", 36);
            _btnFormat.Padding = new Thickness(20, 0, 20, 0);
            actionGrid.Children.Add(_btnFormat);
            Grid.SetColumn(_btnFormat, 1);

            stack.Children.Add(actionGrid);

            scroll.Content = stack;
            return scroll;
        }

        #endregion

        #region View 2: Music Sync (Linear Style)

        private UIElement BuildMusicSyncView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();

            var cardSource = Theme.CreateCard();
            var srcStack = new StackPanel();
            srcStack.Children.Add(Theme.CreateSectionHeader("Source Music Folder", "Select the directory on your PC containing tracks or albums to transfer."));

            var srcRow = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            srcRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            srcRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            srcRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _tbSourcePath = new TextBox
            {
                Height = 30,
                FontSize = 12,
                Background = Theme.InputBackground,
                Foreground = Theme.TextPrimary,
                BorderBrush = Theme.BorderSubtle,
                Padding = new Thickness(8, 4, 8, 4)
            };
            srcRow.Children.Add(_tbSourcePath);
            Grid.SetColumn(_tbSourcePath, 0);

            _btnBrowseSource = Theme.CreateSecondaryButton("≡ Browse...", 30);
            srcRow.Children.Add(_btnBrowseSource);
            Grid.SetColumn(_btnBrowseSource, 2);

            srcStack.Children.Add(srcRow);
            cardSource.Child = srcStack;
            stack.Children.Add(cardSource);

            // Optimization Rules (Linear Property Rows)
            var cardRules = Theme.CreateCard();
            var rulesStack = new StackPanel();
            rulesStack.Children.Add(Theme.CreateSectionHeader("Automotive Optimization Rules"));

            _chkSanitizeNames = Theme.CreateCheckBox("", true);
            rulesStack.Children.Add(Theme.CreatePropertyRow("Sanitize Filenames", "Replaces illegal characters [ ], #, %, &, emojis with car-safe names.", _chkSanitizeNames));

            _chkPurgeJunk = Theme.CreateCheckBox("", true);
            rulesStack.Children.Add(Theme.CreatePropertyRow("Purge Mac & Windows Junk", "Removes ._*, .DS_Store, Thumbs.db that cause audio screeching and skips.", _chkPurgeJunk));

            _chkNormalizeTags = Theme.CreateCheckBox("", true);
            rulesStack.Children.Add(Theme.CreatePropertyRow("Normalize ID3 Tags to ID3v2.3", "Enforces UTF-16/ISO-8859-1 to prevent head unit freezes and 'Unknown Artist'.", _chkNormalizeTags));

            _chkResizeArt = Theme.CreateCheckBox("", true);
            rulesStack.Children.Add(Theme.CreatePropertyRow("Resize Album Art to 500×500", "Normalizes artwork to car-safe baseline JPEG (prevents buffer crash).", _chkResizeArt));

            _chkAutoFatSort = Theme.CreateCheckBox("", true);
            rulesStack.Children.Add(Theme.CreatePropertyRow("Sequential FAT Sorter", "Forces car stereos to play tracks in exact 01, 02, 03 sequential order.", _chkAutoFatSort));

            cardRules.Child = rulesStack;
            stack.Children.Add(cardRules);

            // Folder Organization Card
            var cardOrg = Theme.CreateCard();
            var orgStack = new StackPanel();
            orgStack.Children.Add(Theme.CreateSectionHeader("Directory Structure Organization"));

            _rbOrgKeep = new RadioButton { Content = "Preserve Original Hierarchy", Foreground = Theme.TextPrimary, IsChecked = true, Margin = new Thickness(0, 4, 0, 4) };
            _rbOrgArtistAlbum = new RadioButton { Content = "Auto-Organize: [Music] / [Artist] / [Album] / [Song.mp3]", Foreground = Theme.TextPrimary, Margin = new Thickness(0, 4, 0, 4) };
            _rbOrgBatches = new RadioButton { Content = "Batch Folders (Max 250 songs per folder for strict Toyota/Honda limits)", Foreground = Theme.TextPrimary, Margin = new Thickness(0, 4, 0, 4) };

            orgStack.Children.Add(_rbOrgKeep);
            orgStack.Children.Add(_rbOrgArtistAlbum);
            orgStack.Children.Add(_rbOrgBatches);

            cardOrg.Child = orgStack;
            stack.Children.Add(cardOrg);

            // Action Button
            _btnSync = Theme.CreatePrimaryButton("♫ Transfer & Optimize Music to USB", 36);
            stack.Children.Add(_btnSync);

            scroll.Content = stack;
            return scroll;
        }

        #endregion

        #region View 3: Drive Doctor (Linear Issue Tracker Style)

        private UIElement BuildDriveDoctorView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();

            // Header Action Card
            var topCard = Theme.CreateCard();
            var topStack = new StackPanel();
            topStack.Children.Add(Theme.CreateSectionHeader("Drive Diagnostics & Integrity Audit", "Inspect your USB drive for GPT partition bugs, hidden OS trash, and illegal characters."));

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 8) };
            _btnScanAudit = Theme.CreatePrimaryButton("✦ Scan & Audit USB Drive", 32);
            _btnCleanFix = Theme.CreateSecondaryButton("✓ Clean & Repair Issues", 32);
            _btnCleanFix.Margin = new Thickness(8, 0, 0, 0);
            _btnCleanFix.IsEnabled = false;

            btnRow.Children.Add(_btnScanAudit);
            btnRow.Children.Add(_btnCleanFix);
            topStack.Children.Add(btnRow);

            _tbAuditSummary = new TextBlock
            {
                Text = "Click 'Scan & Audit USB Drive' to begin diagnostics.",
                Foreground = Theme.TextSecondary,
                FontSize = 12
            };
            topStack.Children.Add(_tbAuditSummary);

            topCard.Child = topStack;
            stack.Children.Add(topCard);

            // Linear Issue List Container
            var cardIssues = Theme.CreateCard(14, 6);
            var issueCardStack = new StackPanel();

            var listHeaderGrid = new Grid { Margin = new Thickness(4, 0, 4, 10) };
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var listTitle = new TextBlock
            {
                Text = "Diagnostic Log & Detected Issues",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.TextPrimary
            };
            listHeaderGrid.Children.Add(listTitle);
            Grid.SetColumn(listTitle, 0);

            issueCardStack.Children.Add(listHeaderGrid);

            _spIssueList = new StackPanel();
            // Default placeholder row
            _spIssueList.Children.Add(CreateLinearIssueRow("SYS-0", "No active audit performed", "Run a scan to analyze compatibility criteria", "○ Idle", Theme.TextMuted));
            issueCardStack.Children.Add(_spIssueList);

            cardIssues.Child = issueCardStack;
            stack.Children.Add(cardIssues);

            scroll.Content = stack;
            return scroll;
        }

        private UIElement CreateLinearIssueRow(string key, string title, string description, string status, SolidColorBrush statusColor)
        {
            var border = new Border
            {
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 8, 4, 8),
                MinHeight = 38
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // Priority bars
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) }); // Key (e.g. ENG-54)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) }); // Status circle
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title & desc

            var priority = new TextBlock { Text = "ıll", FontSize = 11, Foreground = Theme.TextMuted, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(priority);
            Grid.SetColumn(priority, 0);

            var keyTb = new TextBlock { Text = key, FontSize = 11, FontWeight = FontWeights.Medium, Foreground = Theme.TextMuted, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(keyTb);
            Grid.SetColumn(keyTb, 1);

            var statusDot = new TextBlock { Text = "⊙", FontSize = 12, Foreground = statusColor, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(statusDot);
            Grid.SetColumn(statusDot, 2);

            var textStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var titleTb = new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.Medium, Foreground = Theme.TextPrimary, Margin = new Thickness(0, 0, 10, 0) };
            var descTb = new TextBlock { Text = description, FontSize = 11, Foreground = Theme.TextSecondary, VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(titleTb);
            textStack.Children.Add(descTb);
            grid.Children.Add(textStack);
            Grid.SetColumn(textStack, 3);

            border.Child = grid;
            return border;
        }

        #endregion

        #region View 4: FAT Sorter (Linear Style)

        private UIElement BuildFatSorterView()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel();

            var card = Theme.CreateCard();
            var inner = new StackPanel();
            inner.Children.Add(Theme.CreateSectionHeader("FAT Sequential Directory Sorter", "Eliminates the infamous bug where car stereos play songs in the wrong or arbitrary order."));

            var explBox = new Border
            {
                Background = Theme.InputBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 4, 0, 14)
            };
            var explStack = new StackPanel();
            explStack.Children.Add(new TextBlock
            {
                Text = "◆ Why do car stereos play tracks out of order?",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.Warning,
                Margin = new Thickness(0, 0, 0, 6)
            });
            explStack.Children.Add(new TextBlock
            {
                Text = "Unlike desktop operating systems which sort music alphabetically in RAM, automotive head units read tracks strictly in the physical order of directory entries on the FAT32 disk.\n\n" +
                       "When files are copied from Windows, they frequently land out of order (e.g. Track 05 plays before Track 01).\n\n" +
                       "FAT Sorter physically reorganizes disk directory entries into strict alphanumeric order so albums always play sequentially (01, 02, 03...).",
                FontSize = 12,
                Foreground = Theme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            });
            explBox.Child = explStack;
            inner.Children.Add(explBox);

            _btnFatSort = Theme.CreatePrimaryButton("⇅ Sort USB Drive for Sequential Car Playback", 36);
            inner.Children.Add(_btnFatSort);

            _tbFatSortSummary = new TextBlock
            {
                Text = "Ready to sort.",
                Foreground = Theme.TextMuted,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0)
            };
            inner.Children.Add(_tbFatSortSummary);

            card.Child = inner;
            stack.Children.Add(card);

            scroll.Content = stack;
            return scroll;
        }

        #endregion

        #region View 5: Car Profiles (Linear Style)

        private UIElement BuildCarProfilesView()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left: Search & Profiles List
            var leftCard = Theme.CreateCard();
            var leftStack = new StackPanel();
            leftStack.Children.Add(Theme.CreateSectionHeader("Vehicle Database", "65+ verified automotive models & aftermarket decks"));

            _tbProfileSearch = new TextBox
            {
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 8),
                Background = Theme.InputBackground,
                Foreground = Theme.TextPrimary,
                BorderBrush = Theme.BorderSubtle,
                Padding = new Thickness(6, 4, 6, 4)
            };
            leftStack.Children.Add(_tbProfileSearch);

            _lbProfileList = new ListBox
            {
                Height = 380,
                Background = Theme.InputBackground,
                BorderBrush = Theme.BorderSubtle,
                Foreground = Theme.TextPrimary,
                FontSize = 12
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(_lbProfileList, ScrollBarVisibility.Disabled);
            Theme.StyleListBox(_lbProfileList);

            var lbTemplate = new DataTemplate();
            var lbTbFactory = new FrameworkElementFactory(typeof(TextBlock));
            lbTbFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("DisplayTitle"));
            lbTbFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            lbTbFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.NoWrap);
            lbTbFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            lbTbFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("Foreground") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ListBoxItem), 1) });
            lbTemplate.VisualTree = lbTbFactory;
            _lbProfileList.ItemTemplate = lbTemplate;

            leftStack.Children.Add(_lbProfileList);
            leftCard.Child = leftStack;
            grid.Children.Add(leftCard);
            Grid.SetColumn(leftCard, 0);

            // Right: Detail Card
            var rightCard = Theme.CreateCard();
            var rightStack = new StackPanel();

            _tbProfileDetailHeader = new TextBlock
            {
                Text = "Select a vehicle from the list",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Theme.LinearAccent,
                Margin = new Thickness(0, 0, 0, 8)
            };
            rightStack.Children.Add(_tbProfileDetailHeader);

            _tbProfileDetailBody = new TextBlock
            {
                FontSize = 12,
                Foreground = Theme.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 16)
            };
            rightStack.Children.Add(_tbProfileDetailBody);

            _btnApplyProfile = Theme.CreatePrimaryButton("Apply This Preset to Formatter", 34);
            _btnApplyProfile.Click += (s, e) =>
            {
                if (_selectedProfile != null)
                {
                    _cbCarPresets.SelectedItem = _selectedProfile;
                    SwitchView(0); // Switch to Format tab
                    MessageBox.Show(string.Format("Preset '{0}' applied to Format tab!", _selectedProfile.DisplayTitle),
                        "Preset Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            rightStack.Children.Add(_btnApplyProfile);

            rightCard.Child = rightStack;
            grid.Children.Add(rightCard);
            Grid.SetColumn(rightCard, 2);

            return grid;
        }

        #endregion

        #region Footer & Logs

        private UIElement BuildFooter()
        {
            var footerBorder = new Border
            {
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 8, 24, 8)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _pbProgress = new ProgressBar
            {
                Height = 2,
                Background = Theme.BorderSubtle,
                Foreground = Theme.LinearAccent,
                BorderThickness = new Thickness(0),
                Value = 0,
                Maximum = 100,
                Margin = new Thickness(0, 0, 0, 6)
            };
            grid.Children.Add(_pbProgress);
            Grid.SetRow(_pbProgress, 0);

            var bottomDock = new DockPanel();
            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            statusStack.Children.Add(Theme.CreateAppIconElement(14, 3));
            _tbStatus = new TextBlock
            {
                Text = "Ready.",
                FontSize = 11,
                Foreground = Theme.TextSecondary,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusStack.Children.Add(_tbStatus);
            bottomDock.Children.Add(statusStack);
            DockPanel.SetDock(statusStack, Dock.Left);

            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            _btnCopyLog = Theme.CreateSecondaryButton("Copy Log", 24);
            _btnCopyLog.FontSize = 11;
            _btnCopyLog.Margin = new Thickness(0, 0, 6, 0);
            _btnCopyLog.Click += (s, e) =>
            {
                Clipboard.SetText(Logger.GetFullLog());
                MessageBox.Show("Log copied to clipboard!", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            rightPanel.Children.Add(_btnCopyLog);

            _btnToggleLog = Theme.CreateSecondaryButton("Console", 24);
            _btnToggleLog.FontSize = 11;
            _btnToggleLog.Click += (s, e) => ToggleLogConsole();
            rightPanel.Children.Add(_btnToggleLog);

            bottomDock.Children.Add(rightPanel);
            grid.Children.Add(bottomDock);
            Grid.SetRow(bottomDock, 1);

            footerBorder.Child = grid;
            return footerBorder;
        }

        private Border BuildLogConsole()
        {
            var border = new Border
            {
                Height = 130,
                Background = Theme.SidebarBackground,
                BorderBrush = Theme.BorderSubtle,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Visibility = Visibility.Collapsed
            };

            _tbLogConsole = new TextBox
            {
                Background = Theme.SidebarBackground,
                Foreground = Theme.TextSecondary,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(16, 6, 16, 6)
            };
            border.Child = _tbLogConsole;
            return border;
        }

        private void ToggleLogConsole()
        {
            bool isVisible = (_borderLogConsole.Visibility == Visibility.Visible);
            _borderLogConsole.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            _btnToggleLog.Content = isVisible ? "Console" : "Hide Console";
        }

        #endregion

        #region Navigation & Events

        private void SwitchView(int viewIndex)
        {
            _currentViewIndex = viewIndex;

            // Highlight selected sidebar item
            for (int i = 0; i < 5; i++)
            {
                bool active = (i == viewIndex);
                _sidebarButtons[i].Background = active ? Theme.CardHover : Brushes.Transparent;
                _sidebarTexts[i].Foreground = active ? Theme.TextPrimary : Theme.TextSecondary;
                _sidebarTexts[i].FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium;
            }

            // Toggle view visibility
            _viewFormat.Visibility   = (viewIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
            _viewMusic.Visibility    = (viewIndex == 1) ? Visibility.Visible : Visibility.Collapsed;
            _viewDoctor.Visibility   = (viewIndex == 2) ? Visibility.Visible : Visibility.Collapsed;
            _viewFatSort.Visibility  = (viewIndex == 3) ? Visibility.Visible : Visibility.Collapsed;
            _viewProfiles.Visibility = (viewIndex == 4) ? Visibility.Visible : Visibility.Collapsed;

            // Update top bar breadcrumb
            string[] titles = { "Format & Prep", "Music Sync", "Drive Doctor", "FAT Sorter", "Car Profiles" };
            string[] subs = {
                "Automotive Compatibility Setup",
                "Media Transfer & Cover Art Normalizer",
                "Diagnostics & Integrity Audit",
                "Physical Directory Order Sorter",
                "Database & Compatibility Explorer"
            };

            _tbHeaderBreadcrumb.Text = titles[viewIndex];
            _tbHeaderSub.Text = subs[viewIndex];
        }

        private void BindEvents()
        {
            _chkShowFixed.Checked += (s, e) => RefreshDrives();
            _chkShowFixed.Unchecked += (s, e) => RefreshDrives();

            _cbCarPresets.SelectionChanged += (s, e) =>
            {
                _selectedProfile = _cbCarPresets.SelectedItem as CarProfile;
                ApplySelectedProfile();
            };

            _btnFormat.Click += async (s, e) => await HandleFormatClick();

            _btnBrowseSource.Click += (s, e) =>
            {
                using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dlg.Description = "Select Music Folder to Sync to USB";
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        _tbSourcePath.Text = dlg.SelectedPath;
                    }
                }
            };

            _btnSync.Click += async (s, e) => await HandleSyncClick();

            _btnScanAudit.Click += async (s, e) => await HandleAuditClick();
            _btnCleanFix.Click += async (s, e) => await HandleCleanFixClick();

            _btnFatSort.Click += async (s, e) => await HandleFatSortClick();

            _tbProfileSearch.TextChanged += (s, e) => FilterProfiles();
            _lbProfileList.SelectionChanged += (s, e) =>
            {
                var profile = _lbProfileList.SelectedItem as CarProfile;
                if (profile != null)
                {
                    _selectedProfile = profile;
                    DisplayProfileDetails(profile);
                }
            };

            Logger.MessageLogged += (s, e) =>
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    _tbLogConsole.AppendText(e.FormattedText + Environment.NewLine);
                    _tbLogConsole.ScrollToEnd();
                    _tbStatus.Text = e.Message;
                }));
            };
        }

        private void LoadInitialData()
        {
            _profiles = CarDatabase.GetAllProfiles();

            _cbCarPresets.ItemsSource = _profiles;
            _cbCarPresets.SelectedIndex = 0;

            _lbProfileList.ItemsSource = _profiles;
            _lbProfileList.SelectedIndex = 0;

            RefreshDrives();
            Logger.Info("AutoUSB Toolkit initialized. Minimalist workspace ready.");
        }

        private void RefreshDrives()
        {
            bool showFixed = _chkShowFixed.IsChecked == true;
            _availableDrives = DriveDetector.GetAvailableDrives(showFixed);

            if (_availableDrives.Count > 0)
            {
                _selectedDrive = _availableDrives[0];
                _tbSidebarStatusDot.Foreground = Theme.Success;
                _tbSidebarStatusDot.ToolTip = string.Format("{0} USB Drive(s) Connected", _availableDrives.Count);
            }
            else
            {
                _selectedDrive = null;
                _tbSidebarStatusDot.Foreground = Theme.TextMuted;
                _tbSidebarStatusDot.ToolTip = "No USB Drive Detected";
            }

            UpdateDriveInfoDisplay();
        }

        private void UpdateDriveInfoDisplay()
        {
            if (_selectedDrive == null)
            {
                _tbSidebarDriveTitle.Text = "No USB Drive";
                _tbSidebarDriveSpecs.Text = "Plug in a flash drive";
                _pbSidebarDriveUsage.Value = 0;
                return;
            }

            string label = string.IsNullOrEmpty(_selectedDrive.VolumeLabel) ? "NO NAME" : _selectedDrive.VolumeLabel;
            _tbSidebarDriveTitle.Text = string.Format("{0} [{1}]", _selectedDrive.DriveLetter, label);
            _tbSidebarDriveSpecs.Text = string.Format("{0:0.0} GB • {1} • {2}",
                _selectedDrive.TotalSizeGb, _selectedDrive.FileSystem, _selectedDrive.PartitionStyle);
            _pbSidebarDriveUsage.Value = _selectedDrive.UsedPercent;
        }

        private void ApplySelectedProfile()
        {
            if (_selectedProfile == null) return;

            _tbPresetDescription.Text = string.Format("{0} — {1}", _selectedProfile.Make, _selectedProfile.SystemName);
            _tbPresetSpecs.Text = string.Format(
                "• File System: {0}  |  Cluster: {1}  |  Partition: {2}\n" +
                "• Directory Limit: Max {3} songs/folder  |  Depth: {4} levels\n" +
                "• Audio Codecs: {5}\n" +
                "• Car Notes: {6}",
                _selectedProfile.RecommendedFileSystem,
                _selectedProfile.ClusterSizeDisplay,
                _selectedProfile.RequiresMbr ? "MBR Strictly Required" : "MBR or GPT",
                _selectedProfile.MaxFilesPerFolder,
                _selectedProfile.MaxFolderDepth,
                _selectedProfile.SupportedAudio,
                _selectedProfile.Notes);

            // Auto-select UI options
            if (string.Equals(_selectedProfile.RecommendedFileSystem, "FAT32", StringComparison.OrdinalIgnoreCase))
                _cbFileSystem.SelectedIndex = 0;
            else if (string.Equals(_selectedProfile.RecommendedFileSystem, "exFAT", StringComparison.OrdinalIgnoreCase))
                _cbFileSystem.SelectedIndex = 1;
            else
                _cbFileSystem.SelectedIndex = 2;

            if (_selectedProfile.RecommendedClusterSize == 32768)
                _cbClusterSize.SelectedIndex = 0;
            else if (_selectedProfile.RecommendedClusterSize == 16384)
                _cbClusterSize.SelectedIndex = 1;
            else if (_selectedProfile.RecommendedClusterSize == 65536)
                _cbClusterSize.SelectedIndex = 2;

            _chkEnforceMbr.IsChecked = _selectedProfile.RequiresMbr;
        }

        private void FilterProfiles()
        {
            string query = (_tbProfileSearch.Text ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(query))
            {
                _lbProfileList.ItemsSource = _profiles;
            }
            else
            {
                _lbProfileList.ItemsSource = _profiles.Where(p =>
                    p.Make.ToLowerInvariant().Contains(query) ||
                    p.SystemName.ToLowerInvariant().Contains(query) ||
                    p.Category.ToLowerInvariant().Contains(query)).ToList();
            }
            if (_lbProfileList.Items.Count > 0)
                _lbProfileList.SelectedIndex = 0;
        }

        private void DisplayProfileDetails(CarProfile p)
        {
            _tbProfileDetailHeader.Text = p.DisplayTitle;
            _tbProfileDetailBody.Text = string.Format(
                "Manufacturer / Deck: {0}\n" +
                "Infotainment System: {1}\n" +
                "System Category: {2}\n\n" +
                "OPTIMAL STORAGE CRITERIA:\n" +
                "• File System: {3}\n" +
                "• Cluster / Allocation Unit: {4}\n" +
                "• Partition Scheme: {5}\n" +
                "• Max Files per Directory: {6}\n" +
                "• Max Folder Depth: {7} levels\n" +
                "• Recommended Max USB Size: {8} GB\n\n" +
                "MEDIA FORMAT COMPATIBILITY:\n" +
                "• Audio Codecs: {9}\n" +
                "• Video Codecs: {10}\n" +
                "• Tagging Standard: {11}\n" +
                "• Max Album Art: {12}x{12} JPEG\n\n" +
                "CAR SPECIFIC FIRMWARE TIPS:\n{13}",
                p.Make, p.SystemName, p.Category,
                p.RecommendedFileSystem, p.ClusterSizeDisplay,
                p.RequiresMbr ? "MBR (Master Boot Record)" : "MBR / GPT",
                p.MaxFilesPerFolder, p.MaxFolderDepth, p.RecommendedMaxDriveSizeGb,
                p.SupportedAudio, p.SupportedVideo, p.TagStandard, p.MaxArtworkResolution,
                p.Notes);
        }

        #endregion

        #region Actions

        private async Task HandleFormatClick()
        {
            if (_selectedDrive == null)
            {
                MessageBox.Show("Please select a target USB drive first.", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DiskFormatter.IsAdministrator())
            {
                var elevate = MessageBox.Show(
                    "Formatting raw USB drive partitions and writing boot sectors requires Administrator privileges.\n\n" +
                    "Would you like to relaunch AutoUSB Toolkit as Administrator now?",
                    "Administrator Privileges Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (elevate == MessageBoxResult.Yes)
                {
                    DiskFormatter.RelaunchAsAdministrator();
                    return;
                }
            }

            string label = (_tbVolumeLabel.Text ?? "CAR_USB").Trim().ToUpperInvariant();
            if (label.Length > 11) label = label.Substring(0, 11);

            string fs = "FAT32";
            if (_cbFileSystem.SelectedIndex == 1) fs = "exFAT";
            else if (_cbFileSystem.SelectedIndex == 2) fs = "NTFS";

            uint cluster = 32768;
            if (_cbClusterSize.SelectedIndex == 1) cluster = 16384;
            else if (_cbClusterSize.SelectedIndex == 2) cluster = 65536;
            else if (_cbClusterSize.SelectedIndex == 3) cluster = 0;

            var msg = string.Format(
                "Are you sure you want to format {0} ({1:0.0} GB)?\n\n" +
                "• File System: {2}\n" +
                "• Cluster Size: {3} bytes\n" +
                "• Volume Label: {4}\n" +
                "• Enforce MBR Partition: {5}\n\n" +
                "ALL EXISTING DATA ON THIS DRIVE WILL BE PERMANENTLY ERASED.",
                _selectedDrive.DriveLetter, _selectedDrive.TotalSizeGb, fs, cluster, label, _chkEnforceMbr.IsChecked == true);

            var confirm = MessageBox.Show(msg, "Confirm Car USB Format", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            SetBusy(true, "Formatting drive for car stereo...");
            _pbProgress.Value = 0;

            var opts = new FormatOptions
            {
                DriveLetter = _selectedDrive.DriveLetter,
                FileSystem = fs,
                ClusterSize = cluster,
                VolumeLabel = label,
                EnforceMbr = (_chkEnforceMbr.IsChecked == true),
                QuickFormat = (_chkQuickFormat.IsChecked == true),
                UseDirectFat32Engine = (_chkDirectFat32.IsChecked == true)
            };

            var prog = new Progress<int>(p => _pbProgress.Value = p);

            bool ok = await DiskFormatter.FormatAsync(opts, _selectedDrive, prog);
            SetBusy(false, ok ? "Format completed successfully!" : "Format failed.");

            RefreshDrives();

            if (ok)
            {
                MessageBox.Show(string.Format("Successfully formatted {0} for car stereo compatibility!", opts.DriveLetter),
                    "Format Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Formatting could not be completed. Check log console for details.",
                    "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandleSyncClick()
        {
            if (_selectedDrive == null)
            {
                MessageBox.Show("Please select a target USB drive first.", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string src = (_tbSourcePath.Text ?? "").Trim();
            if (string.IsNullOrEmpty(src) || !Directory.Exists(src))
            {
                MessageBox.Show("Please select a valid source music folder on your PC.", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int orgMode = 0;
            if (_rbOrgArtistAlbum.IsChecked == true) orgMode = 1;
            else if (_rbOrgBatches.IsChecked == true) orgMode = 2;

            var opts = new SyncOptions
            {
                SourcePath = src,
                TargetDrive = _selectedDrive.DriveLetter,
                SanitizeFilenames = (_chkSanitizeNames.IsChecked == true),
                PurgeMacWindowsJunk = (_chkPurgeJunk.IsChecked == true),
                NormalizeTagsToId3v23 = (_chkNormalizeTags.IsChecked == true),
                ResizeAlbumArtTo500 = (_chkResizeArt.IsChecked == true),
                FolderOrganizationMode = orgMode,
                RunFatSortAfterSync = (_chkAutoFatSort.IsChecked == true)
            };

            SetBusy(true, "Syncing & optimizing music to USB...");
            _pbProgress.Value = 0;

            var prog = new Progress<SyncProgressReport>(r =>
            {
                _pbProgress.Value = r.Percentage;
                _tbStatus.Text = r.StatusMessage;
            });

            bool ok = await MediaSyncEngine.SyncMediaAsync(opts, _selectedProfile, prog);
            SetBusy(false, ok ? "Sync completed successfully!" : "Sync failed.");

            RefreshDrives();

            if (ok)
            {
                MessageBox.Show("Music files transferred and optimized for your car stereo!",
                    "Sync Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task HandleAuditClick()
        {
            if (_selectedDrive == null)
            {
                MessageBox.Show("Please select a target USB drive first.", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true, "Auditing USB drive for car compatibility issues...");
            _pbProgress.Value = 0;
            _spIssueList.Children.Clear();

            var prog = new Progress<int>(p => _pbProgress.Value = p);
            _currentAudit = await DriveDoctor.AuditAsync(_selectedDrive, _selectedProfile, prog);

            SetBusy(false, "Audit complete.");

            _tbAuditSummary.Text = string.Format(
                "Audit finished: {0} files & {1} folders scanned. {2} issues found ({3} cleanable junk).",
                _currentAudit.TotalFilesScanned,
                _currentAudit.TotalFoldersScanned,
                _currentAudit.TotalIssuesCount,
                _currentAudit.JunkSizeDisplay);

            if (_currentAudit.Issues.Count == 0)
            {
                _spIssueList.Children.Add(CreateLinearIssueRow("SYS-PASS", "Drive 100% Car Stereo Compliant", "No partition bugs, hidden OS trash, or file limit issues found", "✔ Clean", Theme.Success));
                _btnCleanFix.IsEnabled = false;
            }
            else
            {
                int id = 1;
                foreach (var issue in _currentAudit.Issues)
                {
                    SolidColorBrush col = issue.Category == "Critical" ? Theme.Danger :
                                          issue.Category == "Warning" ? Theme.Warning : Theme.TextSecondary;
                    string key = string.Format("ISS-{0}", id++);
                    _spIssueList.Children.Add(CreateLinearIssueRow(key, issue.Title, issue.Description, "⊙ " + issue.Category, col));
                }
                _btnCleanFix.IsEnabled = true;
            }
        }

        private async Task HandleCleanFixClick()
        {
            if (_currentAudit == null || _currentAudit.TotalIssuesCount == 0) return;

            SetBusy(true, "Cleaning junk and fixing filenames...");
            _pbProgress.Value = 0;

            var prog = new Progress<int>(p => _pbProgress.Value = p);
            int fixedCount = await DriveDoctor.CleanAndFixAsync(_currentAudit, prog);

            SetBusy(false, string.Format("Clean & Fix complete ({0} items resolved).", fixedCount));

            MessageBox.Show(string.Format("Successfully cleaned and fixed {0} items on your USB drive!", fixedCount),
                "Clean & Fix Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            // Re-run audit
            await HandleAuditClick();
        }

        private async Task HandleFatSortClick()
        {
            if (_selectedDrive == null)
            {
                MessageBox.Show("Please select a target USB drive first.", "AutoUSB Toolkit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true, "Sorting FAT directory entries for sequential playback...");
            _pbProgress.Value = 0;

            var prog = new Progress<int>(p => _pbProgress.Value = p);
            int sortedFolders = await FatSorter.SortDriveAsync(_selectedDrive.DriveLetter, prog);

            SetBusy(false, "FAT Sorter complete.");
            _tbFatSortSummary.Text = string.Format("Successfully organized {0} folders into sequential track order!", sortedFolders);

            MessageBox.Show("USB drive directories have been physically sorted!\nYour car stereo will now play tracks in sequential numerical order.",
                "FAT Sorter Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetBusy(bool busy, string status)
        {
            _isBusy = busy;
            _tbStatus.Text = status;
            _btnFormat.IsEnabled = !busy;
            _btnSync.IsEnabled = !busy;
            _btnScanAudit.IsEnabled = !busy;
            _btnFatSort.IsEnabled = !busy;
            Cursor = busy ? Cursors.Wait : Cursors.Arrow;
        }

        #endregion
    }
}
