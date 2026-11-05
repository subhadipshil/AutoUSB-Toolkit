using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoUSBToolkit.UI
{
    /// <summary>
    /// Linear-inspired Minimalist Theme & Controls.
    /// Deep obsidian surfaces, subtle 1px borders, precision typography,
    /// dark scrollbars, and airtight dark control templates.
    /// </summary>
    public static class Theme
    {
        // Linear Color Palette
        public static readonly SolidColorBrush WindowBackground  = new SolidColorBrush(Color.FromRgb(20, 21, 25));   // #141519
        public static readonly SolidColorBrush SidebarBackground = new SolidColorBrush(Color.FromRgb(16, 17, 20));   // #101114
        public static readonly SolidColorBrush ContentBackground = new SolidColorBrush(Color.FromRgb(22, 23, 27));   // #16171B
        public static readonly SolidColorBrush CardBackground    = new SolidColorBrush(Color.FromRgb(26, 27, 32));   // #1A1B20
        public static readonly SolidColorBrush CardHover         = new SolidColorBrush(Color.FromRgb(34, 36, 43));   // #22242B
        public static readonly SolidColorBrush InputBackground   = new SolidColorBrush(Color.FromRgb(18, 19, 23));   // #121317
        public static readonly SolidColorBrush BorderSubtle      = new SolidColorBrush(Color.FromRgb(35, 37, 44));   // #23252C
        public static readonly SolidColorBrush BorderHighlight   = new SolidColorBrush(Color.FromRgb(48, 51, 62));   // #30333E

        // Linear Typography
        public static readonly SolidColorBrush TextPrimary   = new SolidColorBrush(Color.FromRgb(240, 241, 244));     // #F0F1F4
        public static readonly SolidColorBrush TextSecondary = new SolidColorBrush(Color.FromRgb(138, 143, 154));     // #8A8F9A
        public static readonly SolidColorBrush TextMuted     = new SolidColorBrush(Color.FromRgb(92, 96, 106));       // #5C606A
        public static readonly SolidColorBrush TextDisabled  = new SolidColorBrush(Color.FromRgb(70, 74, 84));       // #464A54

        // Linear Accent & Status Colors
        public static readonly SolidColorBrush LinearAccent      = new SolidColorBrush(Color.FromRgb(94, 106, 210));   // #5E6AD2
        public static readonly SolidColorBrush LinearAccentHover = new SolidColorBrush(Color.FromRgb(112, 124, 232)); // #707CE8
        public static readonly SolidColorBrush Success           = new SolidColorBrush(Color.FromRgb(34, 197, 94));    // #22C55E
        public static readonly SolidColorBrush Warning           = new SolidColorBrush(Color.FromRgb(245, 158, 11));   // #F59E0B
        public static readonly SolidColorBrush Danger            = new SolidColorBrush(Color.FromRgb(239, 68, 68));    // #EF4444
        public static readonly SolidColorBrush DangerBg          = new SolidColorBrush(Color.FromRgb(42, 22, 26));    // #2A161A
        public static readonly SolidColorBrush DangerBorder      = new SolidColorBrush(Color.FromRgb(78, 28, 36));    // #4E1C24

        // Cached App Icon
        private static ImageSource _cachedAppIcon;

        /// <summary>
        /// Applies the dark theme across global WPF system brushes and registers modern dark scrollbars.
        /// </summary>
        public static void ApplyGlobalSystemTheme()
        {
            try
            {
                var res = Application.Current.Resources;
                res[SystemColors.WindowBrushKey] = CardBackground;
                res[SystemColors.WindowTextBrushKey] = TextPrimary;
                res[SystemColors.HighlightBrushKey] = CardHover;
                res[SystemColors.HighlightTextBrushKey] = Brushes.White;
                res[SystemColors.ControlBrushKey] = ContentBackground;
                res[SystemColors.ControlTextBrushKey] = TextPrimary;
                res[SystemColors.MenuBrushKey] = CardBackground;
                res[SystemColors.MenuTextBrushKey] = TextPrimary;
                res[SystemColors.ScrollBarBrushKey] = ContentBackground;
            }
            catch { }
        }

        /// <summary>
        /// Retrieves the AutoUSB Toolkit app icon from embedded resources or local disk.
        /// </summary>
        public static ImageSource GetAppIconImage()
        {
            if (_cachedAppIcon != null)
                return _cachedAppIcon;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("AutoUSBToolkit.icons.app.png"))
                {
                    if (stream != null)
                    {
                        var bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = stream;
                        bi.EndInit();
                        bi.Freeze();
                        _cachedAppIcon = bi;
                        return _cachedAppIcon;
                    }
                }
            }
            catch { }

            // Fallback to local files
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"icons\app.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\icons\app.png"),
                @"D:\LOCAL DOCUMENTS\GitHub\AutoUSB-Toolkit\icons\app.png",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"icons\app.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\icons\app.ico")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var bi = new BitmapImage(new Uri(Path.GetFullPath(path)));
                        bi.Freeze();
                        _cachedAppIcon = bi;
                        return _cachedAppIcon;
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Creates a rounded, high-quality rendered Image element of the application icon.
        /// </summary>
        public static UIElement CreateAppIconElement(double size = 24, double cornerRadius = 5)
        {
            var src = GetAppIconImage();
            var img = new Image
            {
                Source = src,
                Width = size,
                Height = size,
                Stretch = Stretch.UniformToFill
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            var border = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(cornerRadius),
                Background = CardBackground,
                ClipToBounds = true,
                Child = img
            };
            return border;
        }

        public static Border CreateCard(double padding = 16, double cornerRadius = 6)
        {
            return new Border
            {
                Background = CardBackground,
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(cornerRadius),
                Padding = new Thickness(padding),
                Margin = new Thickness(0, 0, 0, 12)
            };
        }

        public static Border CreateBadge(string text, SolidColorBrush bg, SolidColorBrush fg)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = fg,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            return new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Child = tb,
                Margin = new Thickness(4, 0, 4, 0)
            };
        }

        public static Button CreatePrimaryButton(string text, double height = 34)
        {
            var btn = new Button
            {
                Content = text,
                Height = height,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Padding = new Thickness(16, 0, 16, 0)
            };

            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, LinearAccent));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderSubtle));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));

            var trigHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigHover.Setters.Add(new Setter(Control.BackgroundProperty, LinearAccentHover));
            style.Triggers.Add(trigHover);

            btn.Style = style;
            return btn;
        }

        public static Button CreateSecondaryButton(string text, double height = 30)
        {
            var btn = new Button
            {
                Content = text,
                Height = height,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Cursor = Cursors.Hand,
                Padding = new Thickness(12, 0, 12, 0)
            };

            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, CardBackground));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, BorderSubtle));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));

            var trigHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigHover.Setters.Add(new Setter(Control.BackgroundProperty, CardHover));
            style.Triggers.Add(trigHover);

            btn.Style = style;
            return btn;
        }

        public static Button CreateDangerButton(string text, double height = 36)
        {
            var btn = new Button
            {
                Content = text,
                Height = height,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Padding = new Thickness(16, 0, 16, 0)
            };

            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Danger));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, DangerBorder));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));

            var trigHover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigHover.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 38, 38))));
            style.Triggers.Add(trigHover);

            btn.Style = style;
            return btn;
        }

        /// <summary>
        /// Creates a custom button template with guaranteed dark disabled styling.
        /// Prevents WPF's default Aero/Classic theme from painting a white rectangle when IsEnabled=false.
        /// </summary>
        private static ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "ButtonBorder";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            // Template Triggers ensure disabled state is strictly dark and never white
            var trigDisabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            trigDisabled.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(24, 25, 30)), "ButtonBorder"));
            trigDisabled.Setters.Add(new Setter(Border.BorderBrushProperty, BorderSubtle, "ButtonBorder"));
            trigDisabled.Setters.Add(new Setter(TextElement.ForegroundProperty, TextDisabled));
            trigDisabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(trigDisabled);

            return template;
        }

        public static CheckBox CreateCheckBox(string text, bool isChecked = false)
        {
            var cb = new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Foreground = TextPrimary,
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 6),
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            return cb;
        }

        public static UIElement CreateSectionHeader(string title, string subtitle = null)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            var tbTitle = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextPrimary
            };
            sp.Children.Add(tbTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var tbSub = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Foreground = TextSecondary,
                    Margin = new Thickness(0, 3, 0, 0)
                };
                sp.Children.Add(tbSub);
            }

            return sp;
        }

        public static Border CreatePropertyRow(string label, string description, UIElement control, double height = 54)
        {
            var border = new Border
            {
                BorderBrush = BorderSubtle,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 8, 0, 8),
                MinHeight = height
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var tbLabel = new TextBlock
            {
                Text = label,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = TextPrimary
            };
            textStack.Children.Add(tbLabel);

            if (!string.IsNullOrEmpty(description))
            {
                var tbDesc = new TextBlock
                {
                    Text = description,
                    FontSize = 11,
                    Foreground = TextSecondary,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                textStack.Children.Add(tbDesc);
            }

            grid.Children.Add(textStack);
            Grid.SetColumn(textStack, 0);

            if (control != null)
            {
                var wrap = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                wrap.Children.Add(control);
                grid.Children.Add(wrap);
                Grid.SetColumn(wrap, 2);
            }

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// Styles ComboBox with a complete dark ControlTemplate and custom ComboBoxItem template,
        /// completely preventing any white popup, white text on white, or chunky scrollbars.
        /// </summary>
        public static void StyleComboBox(ComboBox cb)
        {
            cb.Background = InputBackground;
            cb.Foreground = TextPrimary;
            cb.BorderBrush = BorderSubtle;
            cb.BorderThickness = new Thickness(1);

            // Override system popup colors specifically for this ComboBox scope
            cb.Resources[SystemColors.WindowBrushKey] = ContentBackground;
            cb.Resources[SystemColors.WindowTextBrushKey] = TextPrimary;
            cb.Resources[SystemColors.HighlightBrushKey] = CardHover;
            cb.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;

            // Custom Dark Template for ComboBox
            var template = new ControlTemplate(typeof(ComboBox));
            var grid = new FrameworkElementFactory(typeof(Grid));

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "ComboBorder";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));

            var toggle = new FrameworkElementFactory(typeof(ToggleButton));
            toggle.SetValue(ToggleButton.ClickModeProperty, ClickMode.Press);
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new System.Windows.Data.Binding("IsDropDownOpen") { Mode = System.Windows.Data.BindingMode.TwoWay, RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            toggle.SetValue(UIElement.OpacityProperty, 0.0);

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.Name = "ContentSite";
            content.SetValue(ContentPresenter.IsHitTestVisibleProperty, false);
            content.SetValue(ContentPresenter.MarginProperty, new Thickness(10, 2, 26, 2));
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            content.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding("SelectionBoxItem") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            content.SetBinding(ContentPresenter.ContentTemplateProperty, new System.Windows.Data.Binding("SelectionBoxItemTemplate") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var arrow = new FrameworkElementFactory(typeof(TextBlock));
            arrow.SetValue(TextBlock.TextProperty, "▾");
            arrow.SetValue(TextBlock.ForegroundProperty, TextSecondary);
            arrow.SetValue(TextBlock.FontSizeProperty, 11.0);
            arrow.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            arrow.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            arrow.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            arrow.SetValue(UIElement.IsHitTestVisibleProperty, false);

            var popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "PART_Popup";
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.FocusableProperty, false);
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
            popup.SetBinding(Popup.IsOpenProperty, new System.Windows.Data.Binding("IsDropDownOpen") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var popBorder = new FrameworkElementFactory(typeof(Border));
            popBorder.SetValue(Border.BackgroundProperty, ContentBackground);
            popBorder.SetValue(Border.BorderBrushProperty, BorderHighlight);
            popBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            popBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            popBorder.SetValue(Border.PaddingProperty, new Thickness(3));
            popBorder.SetValue(FrameworkElement.MaxHeightProperty, 260.0);
            popBorder.SetValue(FrameworkElement.MinWidthProperty, new System.Windows.Data.Binding("ActualWidth") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
            scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
            scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

            var items = new FrameworkElementFactory(typeof(ItemsPresenter));
            scroll.AppendChild(items);
            popBorder.AppendChild(scroll);
            popup.AppendChild(popBorder);

            grid.AppendChild(border);
            grid.AppendChild(content);
            grid.AppendChild(arrow);
            grid.AppendChild(toggle);
            grid.AppendChild(popup);

            template.VisualTree = grid;
            cb.Template = template;

            // Airtight ComboBoxItem Container Style with custom ControlTemplate
            var itemStyle = new Style(typeof(ComboBoxItem));
            var itemTemplate = new ControlTemplate(typeof(ComboBoxItem));
            var itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "ItemBorder";
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(8, 6, 8, 6));
            itemBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
            itemContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemBorder.AppendChild(itemContent);
            itemTemplate.VisualTree = itemBorder;

            var trigHover = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            trigHover.Setters.Add(new Setter(Border.BackgroundProperty, CardHover, "ItemBorder"));
            trigHover.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
            itemTemplate.Triggers.Add(trigHover);

            var trigSelected = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            trigSelected.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(40, 44, 58)), "ItemBorder"));
            trigSelected.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
            itemTemplate.Triggers.Add(trigSelected);

            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemTemplate));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            cb.ItemContainerStyle = itemStyle;
        }

        /// <summary>
        /// Styles ListBox with a clean dark theme, no horizontal overflow, and refined item selection.
        /// </summary>
        public static void StyleListBox(ListBox lb)
        {
            lb.Background = InputBackground;
            lb.Foreground = TextPrimary;
            lb.BorderBrush = BorderSubtle;
            lb.BorderThickness = new Thickness(1);
            ScrollViewer.SetHorizontalScrollBarVisibility(lb, ScrollBarVisibility.Disabled);

            var itemStyle = new Style(typeof(ListBoxItem));
            var itemTemplate = new ControlTemplate(typeof(ListBoxItem));
            var itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "ItemBorder";
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(10, 6, 10, 6));
            itemBorder.SetValue(Border.MarginProperty, new Thickness(2, 1, 2, 1));
            itemBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var itemContent = new FrameworkElementFactory(typeof(ContentPresenter));
            itemContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemBorder.AppendChild(itemContent);
            itemTemplate.VisualTree = itemBorder;

            var trigHover = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            trigHover.Setters.Add(new Setter(Border.BackgroundProperty, CardHover, "ItemBorder"));
            trigHover.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
            itemTemplate.Triggers.Add(trigHover);

            var trigSelected = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            trigSelected.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(40, 44, 58)), "ItemBorder"));
            trigSelected.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.White));
            itemTemplate.Triggers.Add(trigSelected);

            itemStyle.Setters.Add(new Setter(Control.TemplateProperty, itemTemplate));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, TextPrimary));
            lb.ItemContainerStyle = itemStyle;
        }
    }
}
