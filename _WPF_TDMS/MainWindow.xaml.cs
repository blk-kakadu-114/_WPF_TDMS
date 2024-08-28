using CsvHelper;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _WPF_TDMS
{
    public partial class MainWindow : Window
    {
        private string selectedFilePath;
        private bool isMenuOpen = false;

        public MainWindow()
        {
            InitializeComponent();
            ToggleMenu(false);
        }

        private void ToggleMenu(bool open)
        {
            if (open)
            {
                // Открываем меню
                MenuColumn.Width = new GridLength(200);
                OpenMenuButton.Content = "❌"; // Кнопка закрытия меню
            }
            else
            {
                // Закрываем меню
                MenuColumn.Width = new GridLength(0);
                OpenMenuButton.Content = "☰"; // Кнопка открытия меню
            }
            isMenuOpen = open;
        }

        private void ToggleMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(!isMenuOpen);
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;

                // Загружаем данные из CSV и отображаем в DataGrid
                DataTable dataTable = LoadCsvToDataTable(selectedFilePath);
                dataGrid.ItemsSource = dataTable.DefaultView;
            }
        }

        private DataTable LoadCsvToDataTable(string csvFilePath)
        {
            DataTable dataTable = new DataTable();
            using (var reader = new StreamReader(csvFilePath))
            {
                bool isFirstRow = true;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    if (isFirstRow)
                    {
                        // Добавляем колонки
                        foreach (var header in values)
                        {
                            dataTable.Columns.Add(header);
                        }
                        isFirstRow = false;
                    }
                    else
                    {
                        // Проверяем, чтобы количество значений соответствовало количеству столбцов
                        if (values.Length > dataTable.Columns.Count)
                        {
                            // Обрезаем лишние значения
                            Array.Resize(ref values, dataTable.Columns.Count);
                        }
                        else if (values.Length < dataTable.Columns.Count)
                        {
                            // Добавляем пустые значения, если их меньше
                            var missingValues = new string[dataTable.Columns.Count - values.Length];
                            values = values.Concat(missingValues).ToArray();
                        }

                        // Добавляем строки
                        dataTable.Rows.Add(values);
                    }
                }
            }
            return dataTable;
        }

        private void LoadCsv(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                dataGrid.ItemsSource = new List<dynamic>(records);
            }
        }

        private void LoadExcel(string filePath)
        {
            var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            var dataTable = new System.Data.DataTable();

            foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
            {
                dataTable.Columns.Add(firstRowCell.Text);
            }

            for (var rowNum = 2; rowNum <= worksheet.Dimension.End.Row; rowNum++)
            {
                var wsRow = worksheet.Cells[rowNum, 1, rowNum, worksheet.Dimension.End.Column];
                var row = dataTable.NewRow();
                foreach (var cell in wsRow)
                {
                    row[cell.Start.Column - 1] = cell.Text;
                }
                dataTable.Rows.Add(row);
            }

            dataGrid.ItemsSource = dataTable.DefaultView;
        }

        private void SaveTableButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx";

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string fileType = System.IO.Path.GetExtension(filePath);

                try
                {
                    if (fileType == ".csv")
                    {
                        SaveCsv(filePath);
                    }
                    else if (fileType == ".xlsx")
                    {
                        SaveExcel(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveCsv(string filePath)
        {
            try
            {
                var dataTable = ((System.Data.DataView)dataGrid.ItemsSource).ToTable();
                using (var writer = new StreamWriter(filePath))
                {
                    // Запись заголовков
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        writer.Write(dataTable.Columns[i]);
                        if (i < dataTable.Columns.Count - 1)
                        {
                            writer.Write(",");
                        }
                    }
                    writer.WriteLine();

                    // Запись строк данных
                    foreach (DataRow row in dataTable.Rows)
                    {
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            writer.Write(row[i].ToString());
                            if (i < dataTable.Columns.Count - 1)
                            {
                                writer.Write(",");
                            }
                        }
                        writer.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении CSV файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveExcel(string filePath)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var dataTable = ((System.Data.DataView)dataGrid.ItemsSource).ToTable();
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                    // Заголовки
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
                    }

                    // Данные
                    for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
                    {
                        for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
                        {
                            worksheet.Cells[rowIndex + 2, colIndex + 1].Value = dataTable.Rows[rowIndex][colIndex];
                        }
                    }

                    package.SaveAs(new FileInfo(filePath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении Excel файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ShowNullValuesButton_Click(object sender, RoutedEventArgs e)
        {
            // Код для показа нулевых значений
            MessageBox.Show("Функция показа нулевых значений.");
        }

        private void ShowDataAnalysisWindow_Click(object sender, RoutedEventArgs e)
        {
            // Передаем таблицу в новое окно
            var dataTable = ((DataView)dataGrid.ItemsSource).ToTable();
            var dataAnalysisWindow = new DataAnalysisWindow(dataTable);
            dataAnalysisWindow.Show();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SideMenu != null)
            {
                // Меняем содержимое бокового меню в зависимости от выбранного раздела
                var selectedTab = ((TabControl)sender).SelectedItem as TabItem;
                if (selectedTab.Header.ToString() == "Обработка")
                {
                    // Показываем функции, связанные с обработкой данных
                    ShowProcessingMenu();
                }
                else
                {
                    // Прячем меню, если выбран другой раздел
                    ToggleMenu(false);
                }
            }
        }

        private void ShowProcessingMenu()
        {
            // Показываем боковое меню и настраиваем его содержимое для обработки данных
            ToggleMenu(true);
            // Здесь можно добавить логику для изменения содержимого меню в зависимости от задачи
        }
    }
}
