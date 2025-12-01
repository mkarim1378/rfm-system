using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OfficeOpenXml;

namespace RFMSystem
{
    public partial class MainWindow : Window
    {
        private readonly Database _db;
        private DataTable? _excelData;
        private DataTable? _filteredData;
        private Dictionary<string, bool?> _columnFilterStates = new(); // null = all, true = only 1, false = only empty
        private Dictionary<string, bool> _columnSortStates = new();

        public MainWindow()
        {
            InitializeComponent();
            _db = new Database();
            _db.LogAction("app_started", new Dictionary<string, object> { ["timestamp"] = DateTime.Now.ToString("O") });
            
            ShowDefaultContent();
        }

        private void ShowDefaultContent()
        {
            var defaultText = new TextBlock
            {
                Text = "Main content area",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            MainContentArea.Content = defaultText;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Handle search text changes
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var query = ((TextBox)sender).Text;
                if (!string.IsNullOrEmpty(query))
                {
                    _db.SaveSearch(query);
                    _db.LogAction("search_performed", new Dictionary<string, object> { ["query"] = query });
                }
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("history_button_clicked");
            MessageBox.Show("History functionality will be implemented", "History");
        }

        private void UploadDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("upload_download_button_clicked");
            OpenUploadDialog();
        }

        private void RFMButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("rfm_button_clicked");
            MessageBox.Show("RFM functionality will be implemented", "RFM");
        }

        private void CRMButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("crm_button_clicked");
            MessageBox.Show("CRM functionality will be implemented", "CRM");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("settings_button_clicked");
            OpenSettingsPopup();
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            _db.LogAction("dashboard_button_clicked");
            OpenDashboardPopup();
        }

        private void ProfileIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _db.LogAction("profile_icon_clicked");
            MessageBox.Show("Profile functionality will be implemented", "Profile");
        }

        private void BlurOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAllPopups();
        }

        private void OpenUploadDialog()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Select Excel File"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadExcelFile(dialog.FileName);
            }
        }

        private async void LoadExcelFile(string filePath)
        {
            try
            {
                ShowLoadingIndicator();
                
                await Task.Run(() =>
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using var package = new ExcelPackage(new FileInfo(filePath));
                    var worksheet = package.Workbook.Worksheets[0];

                    _excelData = new DataTable();
                    _filteredData = new DataTable();

                    // Read headers
                    var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column];
                    foreach (var cell in headerRow)
                    {
                        _excelData.Columns.Add(cell.Text);
                        _filteredData.Columns.Add(cell.Text);
                    }

                    // Read data
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var dataRow = _excelData.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            dataRow[col - 1] = cellValue ?? DBNull.Value;
                        }
                        _excelData.Rows.Add(dataRow);
                    }

                    _filteredData = _excelData.Copy();
                });

                Dispatcher.Invoke(() =>
                {
                    HideLoadingIndicator();
                    DisplayExcelTable();
                    _db.LogAction("excel_file_uploaded", new Dictionary<string, object>
                    {
                        ["file_path"] = filePath,
                        ["rows"] = _excelData.Rows.Count,
                        ["columns"] = _excelData.Columns.Count
                    });
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    HideLoadingIndicator();
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void ShowLoadingIndicator()
        {
            var loadingPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var progressRing = new System.Windows.Controls.ProgressBar
            {
                IsIndeterminate = true,
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 0, 20)
            };

            var loadingText = new TextBlock
            {
                Text = "در حال پردازش فایل اکسل... لطفاً صبر کنید",
                FontSize = 16,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
            };

            loadingPanel.Children.Add(progressRing);
            loadingPanel.Children.Add(loadingText);
            MainContentArea.Content = loadingPanel;
        }

        private void HideLoadingIndicator()
        {
            // Will be replaced by DisplayExcelTable
        }

        private void DisplayExcelTable()
        {
            if (_filteredData == null || _filteredData.Rows.Count == 0)
                return;

            var mainPanel = new Grid();
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Calculate statistics
            var firstCol = _excelData!.Columns[0].ColumnName;
            var uniqueCount = _excelData.AsEnumerable()
                .Select(row => row[firstCol]?.ToString() ?? "")
                .Distinct()
                .Count();

            var columnOneCounts = new Dictionary<string, int>();
            foreach (DataColumn col in _excelData.Columns)
            {
                var count = _excelData.AsEnumerable()
                    .Count(row =>
                    {
                        var val = row[col];
                        if (val == DBNull.Value || val == null) return false;
                        var valStr = val.ToString()?.Trim();
                        return valStr == "1" || valStr == "1.0";
                    });
                columnOneCounts[col.ColumnName] = count;
            }

            // Filter controls row
            var filterPanel = new WrapPanel
            {
                Margin = new Thickness(10),
                Orientation = Orientation.Horizontal
            };

            foreach (DataColumn col in _excelData.Columns)
            {
                if (!_columnFilterStates.ContainsKey(col.ColumnName))
                    _columnFilterStates[col.ColumnName] = null;

                var filterControl = CreateThreeStateFilter(col.ColumnName);
                var filterContainer = new StackPanel
                {
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                filterContainer.Children.Add(new TextBlock
                {
                    Text = col.ColumnName,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                filterContainer.Children.Add(filterControl);
                filterPanel.Children.Add(filterContainer);
            }

            mainPanel.Children.Add(filterPanel);
            Grid.SetRow(filterPanel, 0);

            // Table container
            var tableContainer = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(10)
            };

            var tablePanel = new Grid();
            tablePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tablePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Statistics row
            var statsRow = new Grid();
            statsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var uniqueText = new TextBlock
            {
                Text = $"تعداد ردیف‌های با شماره یکتا: {uniqueCount}",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Margin = new Thickness(10)
            };

            var rowCountText = new TextBlock
            {
                Text = $"نمایش {_filteredData.Rows.Count} ردیف",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                Margin = new Thickness(10)
            };

            statsRow.Children.Add(uniqueText);
            statsRow.Children.Add(rowCountText);
            Grid.SetColumn(rowCountText, 1);

            tablePanel.Children.Add(statsRow);
            Grid.SetRow(statsRow, 0);

            // DataGrid
            var visibleColumns = _excelData.Columns.Cast<DataColumn>()
                .Where(col => _columnFilterStates.GetValueOrDefault(col.ColumnName, null) == null)
                .ToList();

            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                ItemsSource = _filteredData.DefaultView,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            foreach (var col in visibleColumns)
            {
                var oneCount = columnOneCounts.GetValueOrDefault(col.ColumnName, 0);
                var header = new StackPanel { Orientation = Orientation.Horizontal };
                header.Children.Add(new TextBlock { Text = col.ColumnName, FontWeight = FontWeights.Bold });
                header.Children.Add(new TextBlock { Text = $" ({oneCount})", FontSize = 10, Foreground = Brushes.Gray });

                var dataGridCol = new DataGridTextColumn
                {
                    Header = header,
                    Binding = new System.Windows.Data.Binding(col.ColumnName)
                    {
                        Converter = new ValueConverter()
                    },
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                };
                dataGrid.Columns.Add(dataGridCol);
            }

            var scrollViewer = new ScrollViewer
            {
                Content = dataGrid,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            tablePanel.Children.Add(scrollViewer);
            Grid.SetRow(scrollViewer, 1);

            tableContainer.Child = tablePanel;
            mainPanel.Children.Add(tableContainer);
            Grid.SetRow(tableContainer, 1);

            MainContentArea.Content = mainPanel;
        }

        private UIElement CreateThreeStateFilter(string columnName)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var currentState = _columnFilterStates.GetValueOrDefault(columnName, null);

            var leftBtn = new ToggleButton
            {
                Content = "○",
                FontSize = 16,
                Width = 30,
                Height = 30,
                IsChecked = currentState == false,
                Margin = new Thickness(2),
                ToolTip = "فقط مقادیر خالی"
            };

            var middleBtn = new ToggleButton
            {
                Content = "○",
                FontSize = 16,
                Width = 30,
                Height = 30,
                IsChecked = currentState == null,
                Margin = new Thickness(2),
                ToolTip = "همه مقادیر"
            };

            var rightBtn = new ToggleButton
            {
                Content = "○",
                FontSize = 16,
                Width = 30,
                Height = 30,
                IsChecked = currentState == true,
                Margin = new Thickness(2),
                ToolTip = "فقط مقادیر 1"
            };

            leftBtn.Checked += (s, e) => 
            { 
                UpdateFilterButtons(leftBtn, middleBtn, rightBtn, false);
                SetFilterState(columnName, false);
            };

            middleBtn.Checked += (s, e) => 
            { 
                UpdateFilterButtons(leftBtn, middleBtn, rightBtn, null);
                SetFilterState(columnName, null);
            };

            rightBtn.Checked += (s, e) => 
            { 
                UpdateFilterButtons(leftBtn, middleBtn, rightBtn, true);
                SetFilterState(columnName, true);
            };

            panel.Children.Add(leftBtn);
            panel.Children.Add(middleBtn);
            panel.Children.Add(rightBtn);

            return panel;
        }

        private void UpdateFilterButtons(ToggleButton left, ToggleButton middle, ToggleButton right, bool? selectedState)
        {
            left.IsChecked = selectedState == false;
            middle.IsChecked = selectedState == null;
            right.IsChecked = selectedState == true;
        }

        private void SetFilterState(string columnName, bool? state)
        {
            _columnFilterStates[columnName] = state;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_excelData == null) return;

            var filteredRows = _excelData.AsEnumerable().ToList();

            // Apply each filter
            foreach (var kvp in _columnFilterStates)
            {
                var col = kvp.Key;
                var state = kvp.Value;

                if (state == null) continue; // No filter for this column

                if (state == true) // Only "1" values
                {
                    filteredRows = filteredRows.Where(row =>
                    {
                        var val = row[col];
                        if (val == DBNull.Value || val == null) return false;
                        var valStr = val.ToString()?.Trim();
                        return valStr == "1" || valStr == "1.0";
                    }).ToList();
                }
                else if (state == false) // Only empty values
                {
                    filteredRows = filteredRows.Where(row =>
                    {
                        var val = row[col];
                        return val == DBNull.Value || val == null || string.IsNullOrWhiteSpace(val.ToString());
                    }).ToList();
                }
            }

            // Create new filtered table
            _filteredData = _excelData.Clone();
            foreach (var row in filteredRows)
            {
                var newRow = _filteredData.NewRow();
                foreach (DataColumn column in _excelData.Columns)
                {
                    newRow[column.ColumnName] = row[column.ColumnName];
                }
                _filteredData.Rows.Add(newRow);
            }

            DisplayExcelTable();
        }

        private void OpenSettingsPopup()
        {
            // TODO: Implement settings popup
            MessageBox.Show("Settings popup will be implemented", "Settings");
        }

        private void OpenDashboardPopup()
        {
            // TODO: Implement dashboard popup
            var actions = _db.GetRecentActions(7, 10);
            var message = "Recent Activity:\n\n";
            foreach (var action in actions)
            {
                message += $"{action["action_type"]} - {action["timestamp"]}\n";
            }
            MessageBox.Show(message, "Dashboard");
        }

        private void CloseAllPopups()
        {
            BlurOverlay.Visibility = Visibility.Collapsed;
            BlurOverlay.Opacity = 0;
        }
    }

    public class ValueConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
                return "";
            var str = value.ToString();
            return str?.Length > 50 ? str.Substring(0, 50) : str ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

