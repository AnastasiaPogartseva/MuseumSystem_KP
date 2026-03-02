using MuseumSystem.ApplicationData;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MuseumSystem
{
    public partial class AddExponatWindow : Window
    {
        // Конструктор для добавления нового экспоната
        public AddExponatWindow()
        {
            InitializeComponent(); // Этого не хватало!
            LoadCategories();
            ClearForm(); // Очищаем форму
        }

        // Конструктор для редактирования (если нужен)
        public AddExponatWindow(Exponats selectedExponat)
        {
            InitializeComponent();
            LoadCategories();
            LoadExponatData(selectedExponat); // Загружаем данные выбранного экспоната
        }

        // Загрузка категорий для ComboBox
        private void LoadCategories()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var categories = context.Categories
                        .OrderBy(c => c.CategoryName)
                        .ToList();

                    cmbCategory.ItemsSource = categories;
                    cmbCategory.DisplayMemberPath = "CategoryName";
                    cmbCategory.SelectedValuePath = "CategoryID";

                    if (categories.Count > 0)
                        cmbCategory.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Загрузка данных для редактирования
        private void LoadExponatData(Exponats exponat)
        {
            if (exponat == null) return;

            txtInventoryNumber.Text = exponat.InventoryNumber;
            txtName.Text = exponat.Name;
            txtDescription.Text = exponat.Description;
            txtYearCreated.Text = exponat.YearCreated?.ToString() ?? "";
            txtHistory.Text = exponat.History;

            // Выбор категории
            if (exponat.CategoryID.HasValue)
            {
                foreach (var item in cmbCategory.Items)
                {
                    if (item is Categories cat && cat.CategoryID == exponat.CategoryID.Value)
                    {
                        cmbCategory.SelectedItem = item;
                        break;
                    }
                }
            }

            // Выбор состояния
            if (!string.IsNullOrEmpty(exponat.Condition))
            {
                foreach (ComboBoxItem item in cmbCondition.Items)
                {
                    if (item.Content.ToString() == exponat.Condition)
                    {
                        cmbCondition.SelectedItem = item;
                        break;
                    }
                }
            }

            chkStatus.IsChecked = exponat.Status ?? true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация при загрузке окна
        }

        private void BtnSaveExponat_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            if (string.IsNullOrWhiteSpace(txtInventoryNumber.Text))
                errors.AppendLine("• Введите инвентарный номер");

            if (string.IsNullOrWhiteSpace(txtName.Text))
                errors.AppendLine("• Введите название экспоната");

            if (cmbCategory.SelectedItem == null)
                errors.AppendLine("• Выберите категорию");

            if (!string.IsNullOrWhiteSpace(txtYearCreated.Text))
            {
                if (!int.TryParse(txtYearCreated.Text, out int year))
                    errors.AppendLine("• Введите корректный год создания");
                else if (year < 0 || year > DateTime.Now.Year)
                    errors.AppendLine($"• Год должен быть от 0 до {DateTime.Now.Year}");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show($"Проверьте правильность заполнения полей:\n{errors}",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    // Проверка на уникальность инвентарного номера
                    bool exists = context.Exponats
                        .Any(exponat => exponat.InventoryNumber == txtInventoryNumber.Text.Trim());

                    if (exists)
                    {
                        MessageBox.Show("Экспонат с таким инвентарным номером уже существует",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int categoryId = 0;
                    if (cmbCategory.SelectedValue != null)
                    {
                        int.TryParse(cmbCategory.SelectedValue.ToString(), out categoryId);
                    }

                    string condition = "Хорошее";
                    if (cmbCondition.SelectedItem != null)
                    {
                        if (cmbCondition.SelectedItem is ComboBoxItem item)
                            condition = item.Content.ToString();
                        else
                            condition = cmbCondition.SelectedItem.ToString();
                    }

                    int? yearCreated = null;
                    if (!string.IsNullOrWhiteSpace(txtYearCreated.Text))
                    {
                        yearCreated = int.Parse(txtYearCreated.Text);
                    }

                    Exponats newExponat = new Exponats
                    {
                        InventoryNumber = txtInventoryNumber.Text.Trim(),
                        Name = txtName.Text.Trim(),
                        Description = txtDescription.Text?.Trim() ?? "",
                        YearCreated = yearCreated,
                        CategoryID = categoryId > 0 ? categoryId : (int?)null,
                        Condition = condition,
                        Status = chkStatus.IsChecked ?? true,
                        History = txtHistory.Text?.Trim() ?? "",
                        CreatedDate = DateTime.Now,
                        CreatedBy = 1 // Замените на ID текущего пользователя
                    };

                    context.Exponats.Add(newExponat);
                    context.SaveChanges();

                    MessageBox.Show($"Экспонат \"{newExponat.Name}\" успешно добавлен!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearForm()
        {
            txtInventoryNumber.Clear();
            txtName.Clear();
            txtDescription.Clear();
            txtYearCreated.Clear();
            txtHistory.Clear();

            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;

            if (cmbCondition.Items.Count > 0)
                cmbCondition.SelectedIndex = 1; // Хорошее

            chkStatus.IsChecked = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}