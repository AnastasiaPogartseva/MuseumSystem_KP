using Microsoft.Win32;
using MuseumSystem.ApplicationData;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MuseumSystem.Pages
{
    public partial class ExponatsPage : Page
    {
        public ExponatsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadExponats();
            LoadCategories();
            LoadConditions();
        }

        private void LoadExponats()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var exponats = context.Exponats
                        .Include(x => x.Categories)
                        .ToList();

                    dgExponats.ItemsSource = exponats;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var categories = context.Categories.ToList();
                    categories.Insert(0, new Categories { CategoryID = 0, CategoryName = "Все категории" });
                    cmbCategoryFilter.ItemsSource = categories;
                    cmbCategoryFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConditions()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var conditions = context.Exponats
                        .Select(x => x.Condition)
                        .Distinct()
                        .ToList();

                    conditions.Insert(0, "Все состояния");
                    cmbConditionFilter.ItemsSource = conditions;
                    cmbConditionFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки состояний: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbConditionFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbCategoryFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndex = 0;
            cmbConditionFilter.SelectedIndex = 0;
            LoadExponats();
        }

        private void ApplyFilters()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var query = context.Exponats.Include(x => x.Categories).AsQueryable();

                    // Поиск по тексту
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    {
                        string search = txtSearch.Text.ToLower();
                        query = query.Where(x =>
                            x.Name.ToLower().Contains(search) ||
                            x.InventoryNumber.ToLower().Contains(search));
                    }

                    // Фильтр по категории
                    if (cmbCategoryFilter.SelectedItem != null &&
                        cmbCategoryFilter.SelectedIndex > 0)
                    {
                        var selectedCategory = cmbCategoryFilter.SelectedItem as Categories;
                        if (selectedCategory != null)
                        {
                            query = query.Where(x => x.CategoryID == selectedCategory.CategoryID);
                        }
                    }

                    // Фильтр по статусу
                    if (cmbStatusFilter.SelectedItem != null &&
                        cmbStatusFilter.SelectedIndex > 0)
                    {
                        var selectedStatus = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
                        if (selectedStatus == "Доступен")
                        {
                            query = query.Where(x => x.Status == true);
                        }
                        else if (selectedStatus == "Не доступен")
                        {
                            query = query.Where(x => x.Status == false);
                        }
                    }

                    // Фильтр по состоянию
                    if (cmbConditionFilter.SelectedItem != null &&
                        cmbConditionFilter.SelectedIndex > 0)
                    {
                        string selectedCondition = cmbConditionFilter.SelectedItem.ToString();
                        query = query.Where(x => x.Condition == selectedCondition);
                    }

                    dgExponats.ItemsSource = query.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgExponats_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Скрываем ненужные колонки
            if (e.PropertyName == "CategoryID" ||
                e.PropertyName == "Categories" ||
                e.PropertyName == "Exponats1" ||
                e.PropertyName == "Exponats2" ||
                e.PropertyName == "ExcursionExponats" ||
                e.PropertyName == "Restorations")
            {
                e.Cancel = true;
            }

            // Переименовываем колонки
            if (e.PropertyName == "InventoryNumber")
            {
                e.Column.Header = "Инвентарный номер";
            }
            else if (e.PropertyName == "Name")
            {
                e.Column.Header = "Название";
            }
            else if (e.PropertyName == "YearCreated")
            {
                e.Column.Header = "Год создания";
            }
            else if (e.PropertyName == "Condition")
            {
                e.Column.Header = "Состояние";
            }
            else if (e.PropertyName == "Status")
            {
                // Создаем шаблонную колонку для статуса с цветным отображением
                var templateColumn = new DataGridTemplateColumn
                {
                    Header = "Статус",
                    CellTemplate = CreateStatusCellTemplate()
                };
                e.Column = templateColumn;
            }
        }

        private DataTemplate CreateStatusCellTemplate()
        {
            var template = new DataTemplate();

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(5, 2, 5, 2));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(2));

            // Создаем привязку для цвета фона
            var colorBinding = new Binding("Status");
            colorBinding.Converter = new StatusToColorConverter();
            borderFactory.SetBinding(Border.BackgroundProperty, colorBinding);

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetValue(TextBlock.ForegroundProperty, System.Windows.Media.Brushes.White);
            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Создаем привязку для текста статуса
            var textBinding = new Binding("Status");
            textBinding.Converter = new StatusToTextConverter();
            textFactory.SetBinding(TextBlock.TextProperty, textBinding);

            borderFactory.AppendChild(textFactory);
            template.VisualTree = borderFactory;

            return template;
        }

        private void dgExponats_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgExponats.SelectedItem != null)
            {
                MenuItemView_Click(sender, e);
            }
        }

        // Кнопка "Добавить"
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddExponatWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    // Обновляем список после добавления
                    LoadExponats();
                    MessageBox.Show("Экспонат успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении экспоната: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Редактировать"
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedExponat = dgExponats.SelectedItem as Exponats;
            if (selectedExponat == null)
            {
                MessageBox.Show("Выберите экспонат для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editWindow = new AddExponatWindow(selectedExponat);
                editWindow.Owner = Window.GetWindow(this);

                if (editWindow.ShowDialog() == true)
                {
                    // Обновляем список после редактирования
                    LoadExponats();
                    MessageBox.Show("Экспонат успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании экспоната: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Удалить"
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedExponat = dgExponats.SelectedItem as Exponats;
            if (selectedExponat == null)
            {
                MessageBox.Show("Выберите экспонат для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Вы уверены, что хотите удалить экспонат '{selectedExponat.Name}'?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new MuseumTechDBEntities())
                    {
                        var exponat = context.Exponats.Find(selectedExponat.ExponatID);
                        if (exponat != null)
                        {
                            context.Exponats.Remove(exponat);
                            context.SaveChanges();
                        }
                    }

                    LoadExponats();
                    MessageBox.Show("Экспонат успешно удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении экспоната: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Контекстное меню: Просмотреть
        private void MenuItemView_Click(object sender, RoutedEventArgs e)
        {
            var selectedExponat = dgExponats.SelectedItem as Exponats;
            if (selectedExponat != null)
            {
                MessageBox.Show($"Информация об экспонате:\n\n" +
                    $"Название: {selectedExponat.Name}\n" +
                    $"Инвентарный номер: {selectedExponat.InventoryNumber}\n" +
                    $"Категория: {selectedExponat.Categories?.CategoryName}\n" +
                    $"Год создания: {selectedExponat.YearCreated}\n" +
                    $"Состояние: {selectedExponat.Condition}\n" +
                    $"Статус: {(selectedExponat.Status == true ? "Доступен" : "Не доступен")}",
                    "Просмотр экспоната", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Контекстное меню: Редактировать
        private void MenuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            BtnEdit_Click(sender, e);
        }

        // Контекстное меню: Удалить
        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            BtnDelete_Click(sender, e);
        }
    }

    // Конвертеры для отображения статуса
    public class StatusToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool status)
            {
                return status ? "#4CAF50" : "#f44336";
            }
            return "#FF9800";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToTextConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool status)
            {
                return status ? "Доступен" : "Не доступен";
            }
            return "Неизвестно";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}