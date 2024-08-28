using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; // Для использования диалоговых окон
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;

namespace _WPF_TDMS
{
    public partial class DataAnalysisWindow : Window
    {
        private DataTable dataTable;

        public DataAnalysisWindow(DataTable table)
        {
            InitializeComponent();
            dataTable = table;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = (TreeViewItem)((TreeView)sender).SelectedItem;

            // Очищаем содержимое основного Grid
            ContentGrid.Children.Clear();

            // В зависимости от выбранного элемента загружаем соответствующий UI
            switch (selectedItem.Name)
            {
                case "CatalogCreationItem":
                    ShowCatalogCreation();
                    break;
                case "DataEditingItem":
                    ShowDataEditing();
                    break;
                case "DataTransformationItem":
                    ShowDataTransformation();
                    break;
                case "EDAItem":
                    ShowEDA();
                    break;
                case "DataSplitItem":
                    ShowDataSplit();
                    break;
                case "HelpItem":
                    ShowHelp();
                    break;
            }
        }

        private void ShowCatalogCreation()
        {
            var stackPanel = new StackPanel();

            var folderButton = new Button { Content = "Выбрать папку для анализа" };
            folderButton.Click += (s, e) =>
            {
                var dialog = new OpenFileDialog();
                dialog.ValidateNames = false;
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.FileName = "Выберите папку";

                if (dialog.ShowDialog() == true)
                {
                    string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                    Directory.CreateDirectory(System.IO.Path.Combine(folderPath, "AnalysisResults"));
                    MessageBox.Show("Каталог для анализа создан!");
                }
            };

            stackPanel.Children.Add(folderButton);
            ContentGrid.Children.Add(stackPanel);
        }

        private void ShowDataEditing()
        {
            var stackPanel = new StackPanel();

            // Проверка данных
            var checkButton = new Button { Content = "Проверить данные", Margin = new Thickness(10) };
            var infoText = new TextBlock { Margin = new Thickness(10) };

            checkButton.Click += (s, e) =>
            {
                int rowCount = dataTable.Rows.Count;
                int colCount = dataTable.Columns.Count;

                string types = string.Join(Environment.NewLine, dataTable.Columns.Cast<DataColumn>()
                    .Select(c => $"{c.ColumnName}: {c.DataType.Name}"));

                infoText.Text = $"Количество строк: {rowCount}\nКоличество столбцов: {colCount}\nТипы данных:\n{types}";
            };

            // Обработка пропущенных данных
            var columnComboBox = new ComboBox { Margin = new Thickness(10) };
            foreach (DataColumn column in dataTable.Columns)
            {
                columnComboBox.Items.Add(column.ColumnName);
            }

            var methodComboBox = new ComboBox { Margin = new Thickness(10) };
            methodComboBox.Items.Add("Медиана");
            methodComboBox.Items.Add("Среднее");
            methodComboBox.Items.Add("Пользовательское значение");

            var valueTextBox = new TextBox { Margin = new Thickness(10), Visibility = Visibility.Collapsed };
            methodComboBox.SelectionChanged += (s, e) =>
            {
                valueTextBox.Visibility = methodComboBox.SelectedItem.ToString() == "Пользовательское значение" ? Visibility.Visible : Visibility.Collapsed;
            };

            var fillMissingButton = new Button { Content = "Заполнить пропуски", Margin = new Thickness(10) };
            fillMissingButton.Click += (s, e) =>
            {
                string selectedColumn = columnComboBox.SelectedItem?.ToString();
                string selectedMethod = methodComboBox.SelectedItem?.ToString();

                if (selectedColumn == null || selectedMethod == null)
                {
                    MessageBox.Show("Пожалуйста, выберите столбец и метод заполнения пропусков.");
                    return;
                }

                var nonNullValues = dataTable.AsEnumerable()
                    .Where(row => row[selectedColumn] != DBNull.Value)
                    .Select(row =>
                    {
                        double value;
                        bool success = double.TryParse(row[selectedColumn].ToString(), out value);
                        return success ? value : (double?)null;
                    })
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();

                if (!nonNullValues.Any()) return;

                double fillValue = selectedMethod switch
                {
                    "Медиана" => nonNullValues.OrderBy(n => n).ElementAt(nonNullValues.Count / 2),
                    "Среднее" => nonNullValues.Average(),
                    "Пользовательское значение" => double.Parse(valueTextBox.Text),
                    _ => throw new InvalidOperationException("Неизвестный метод")
                };

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row[selectedColumn] == DBNull.Value)
                    {
                        row[selectedColumn] = fillValue;
                    }
                }

                MessageBox.Show($"Пропуски в столбце '{selectedColumn}' заполнены методом '{selectedMethod}'.");
            };

            // Очистка данных
            var cleanButton = new Button { Content = "Очистить данные", Margin = new Thickness(10) };
            cleanButton.Click += (s, e) =>
            {
                // Пример логики очистки данных
                foreach (DataRow row in dataTable.Rows)
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (row[column] == DBNull.Value)
                        {
                            row[column] = null;
                        }
                    }
                }

                MessageBox.Show("Данные очищены.");
            };

            // Кнопки Применить и Закрыть
            var applyButton = new Button { Content = "Применить", Margin = new Thickness(10) };
            var closeButton = new Button { Content = "Закрыть", Margin = new Thickness(10) };

            applyButton.Click += (s, e) =>
            {
                // Логика для применения всех изменений
                MessageBox.Show("Изменения применены.");
            };

            closeButton.Click += (s, e) => { this.Close(); };

            stackPanel.Children.Add(checkButton);
            stackPanel.Children.Add(infoText);
            stackPanel.Children.Add(new TextBlock { Text = "Обработка пропусков:", Margin = new Thickness(10) });
            stackPanel.Children.Add(columnComboBox);
            stackPanel.Children.Add(methodComboBox);
            stackPanel.Children.Add(valueTextBox);
            stackPanel.Children.Add(fillMissingButton);
            stackPanel.Children.Add(cleanButton);
            stackPanel.Children.Add(applyButton);
            stackPanel.Children.Add(closeButton);

            ContentGrid.Children.Add(stackPanel);
        }

        private void ShowDataTransformation()
        {
            var stackPanel = new StackPanel();

            var columnComboBox = new ComboBox { Margin = new Thickness(10) };
            foreach (DataColumn column in dataTable.Columns)
            {
                if (column.DataType == typeof(double) || column.DataType == typeof(int))
                {
                    columnComboBox.Items.Add(column.ColumnName);
                }
            }

            if (columnComboBox.Items.Count == 0)
            {
                MessageBox.Show("Нет подходящих столбцов для нормализации.");
                return;
            }

            var methodComboBox = new ComboBox { Margin = new Thickness(10) };
            methodComboBox.Items.Add("Min-Max Нормализация");
            methodComboBox.Items.Add("Z-Score Нормализация");

            var applyButton = new Button { Content = "Преобразовать", Margin = new Thickness(10) };

            applyButton.Click += (s, e) =>
            {
                string selectedColumn = columnComboBox.SelectedItem?.ToString();
                string selectedMethod = methodComboBox.SelectedItem?.ToString();

                if (selectedColumn == null || selectedMethod == null)
                {
                    MessageBox.Show("Пожалуйста, выберите столбец и метод нормализации.");
                    return;
                }

                var values = dataTable.AsEnumerable()
                    .Where(row => row[selectedColumn] != DBNull.Value)
                    .Select(row =>
                    {
                        double value;
                        bool success = double.TryParse(row[selectedColumn].ToString(), out value);
                        return success ? value : (double?)null;
                    })
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToList();

                if (!values.Any()) return;

                double minValue = values.Min();
                double maxValue = values.Max();
                double meanValue = values.Average();
                double stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - meanValue, 2)) / values.Count);

                foreach (DataRow row in dataTable.Rows)
                {
                    if (row[selectedColumn] != DBNull.Value)
                    {
                        double value = Convert.ToDouble(row[selectedColumn]);
                        double newValue = selectedMethod switch
                        {
                            "Min-Max Нормализация" => (value - minValue) / (maxValue - minValue),
                            "Z-Score Нормализация" => (value - meanValue) / stdDev,
                            _ => value
                        };
                        row[selectedColumn] = newValue;
                    }
                }

                MessageBox.Show($"Данные в столбце '{selectedColumn}' нормализованы методом '{selectedMethod}'.");
            };

            stackPanel.Children.Add(new TextBlock { Text = "Выберите столбец для нормализации:", Margin = new Thickness(10) });
            stackPanel.Children.Add(columnComboBox);
            stackPanel.Children.Add(new TextBlock { Text = "Выберите метод нормализации:", Margin = new Thickness(10) });
            stackPanel.Children.Add(methodComboBox);
            stackPanel.Children.Add(applyButton);

            ContentGrid.Children.Add(stackPanel);
        }

        private void ShowEDA()
        {
            var stackPanel = new StackPanel();

            var columnComboBox = new ComboBox { Margin = new Thickness(10) };
            foreach (DataColumn column in dataTable.Columns)
            {
                columnComboBox.Items.Add(column.ColumnName);
            }

            var chartTypeComboBox = new ComboBox { Margin = new Thickness(10) };
            chartTypeComboBox.Items.Add("Гистограмма");
            chartTypeComboBox.Items.Add("Боксплот");
            chartTypeComboBox.Items.Add("Диаграмма рассеивания");

            var generateChartButton = new Button { Content = "Построить график", Margin = new Thickness(10) };
            generateChartButton.Click += (s, e) =>
            {
                string selectedColumn = columnComboBox.SelectedItem?.ToString();
                string selectedChartType = chartTypeComboBox.SelectedItem?.ToString();

                if (selectedColumn == null || selectedChartType == null)
                {
                    MessageBox.Show("Пожалуйста, выберите столбец и тип графика.");
                    return;
                }

                // Логика генерации графиков в зависимости от выбранного типа
                var values = dataTable.AsEnumerable()
                    .Where(row => row[selectedColumn] != DBNull.Value)
                    .Select(row => Convert.ToDouble(row[selectedColumn]))
                    .ToList();

                if (!values.Any()) return;

                // Используем конкретный класс для графиков
                var cartesianChart = new CartesianChart();

                switch (selectedChartType)
                {
                    case "Гистограмма":
                        cartesianChart.Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Values = new ChartValues<double>(values)
                    }
                };
                        break;

                    case "Боксплот":
                        // Логика построения боксплота
                        break;

                    case "Диаграмма рассеивания":
                        // Логика построения диаграммы рассеивания
                        break;
                }

                // Отображение графика в UI
                stackPanel.Children.Add(cartesianChart);
            };

            stackPanel.Children.Add(new TextBlock { Text = "Выберите столбец:", Margin = new Thickness(10) });
            stackPanel.Children.Add(columnComboBox);
            stackPanel.Children.Add(new TextBlock { Text = "Выберите тип графика:", Margin = new Thickness(10) });
            stackPanel.Children.Add(chartTypeComboBox);
            stackPanel.Children.Add(generateChartButton);

            ContentGrid.Children.Add(stackPanel);
        }


        private void ShowDataSplit()
        {
            var stackPanel = new StackPanel();

            var ratioTextBox = new TextBox { Margin = new Thickness(10), Text = "0.8" };

            var splitButton = new Button { Content = "Разделить данные", Margin = new Thickness(10) };
            splitButton.Click += (s, e) =>
            {
                double trainRatio;
                if (!double.TryParse(ratioTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out trainRatio) || trainRatio <= 0 || trainRatio >= 1)
                {
                    MessageBox.Show("Пожалуйста, введите корректное значение для разделения (например, 0.8).");
                    return;
                }

                int trainSize = (int)(dataTable.Rows.Count * trainRatio);

                DataTable trainTable = dataTable.AsEnumerable().Take(trainSize).CopyToDataTable();
                DataTable testTable = dataTable.AsEnumerable().Skip(trainSize).CopyToDataTable();

                MessageBox.Show($"Данные разделены: {trainTable.Rows.Count} строк для обучения, {testTable.Rows.Count} строк для тестирования.");
            };

            stackPanel.Children.Add(new TextBlock { Text = "Введите долю данных для обучения (например, 0.8):", Margin = new Thickness(10) });
            stackPanel.Children.Add(ratioTextBox);
            stackPanel.Children.Add(splitButton);

            ContentGrid.Children.Add(stackPanel);
        }

        private void ShowHelp()
        {
            var textBlock = new TextBlock
            {
                Text = "Справочная информация по использованию программы:\n1. Для проверки данных выберите соответствующий пункт.\n2. Для обработки пропусков выберите столбец и метод.\n3. Для нормализации данных выберите столбец и метод.",
                Margin = new Thickness(10)
            };

            ContentGrid.Children.Add(textBlock);
        }
    }
}
