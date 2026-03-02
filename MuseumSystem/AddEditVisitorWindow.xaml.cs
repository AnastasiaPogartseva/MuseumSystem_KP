using MuseumSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MuseumSystem.Pages
{
    public partial class AddEditVisitorWindow : Window
    {
        private Visitors _currentVisitor;
        private bool _isEditMode;

        public AddEditVisitorWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            _currentVisitor = new Visitors();
            _currentVisitor.RegistrationDate = DateTime.Now;
            LoadVisitorTypes();
            TitleText.Text = "Добавление нового посетителя";

            // По умолчанию показываем поля для физического лица
            ShowIndividualControls(true);
        }

        public AddEditVisitorWindow(Visitors visitor)
        {
            InitializeComponent();
            _isEditMode = true;
            _currentVisitor = visitor;
            LoadVisitorTypes();
            LoadVisitorData();
            TitleText.Text = "Редактирование посетителя";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация после загрузки окна
            if (!_isEditMode)
            {
                ShowIndividualControls(true);
            }
        }

        private void LoadVisitorTypes()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var types = context.VisitorTypes.ToList();
                    cmbVisitorTypeCategory.ItemsSource = types;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVisitorData()
        {
            if (_currentVisitor == null) return;

            // Заполняем общие поля
            if (txtPhone != null) txtPhone.Text = _currentVisitor.Phone ?? "";
            if (txtEmail != null) txtEmail.Text = _currentVisitor.Email ?? "";

            // Определяем тип посетителя
            if (!string.IsNullOrEmpty(_currentVisitor.GroupName))
            {
                // Это группа
                if (cmbVisitorType != null) cmbVisitorType.SelectedIndex = 1; // Группа
                if (txtGroupName != null) txtGroupName.Text = _currentVisitor.GroupName;
                if (txtCourseNumber != null) txtCourseNumber.Text = _currentVisitor.CourseNumber?.ToString();
                ShowIndividualControls(false); // Показываем поля группы
            }
            else
            {
                // Физическое лицо
                if (cmbVisitorType != null) cmbVisitorType.SelectedIndex = 0; // Физическое лицо
                if (txtLastName != null) txtLastName.Text = _currentVisitor.LastName ?? "";
                if (txtFirstName != null) txtFirstName.Text = _currentVisitor.FirstName ?? "";
                ShowIndividualControls(true); // Показываем поля ФИО
            }

            // Выбираем тип посетителя (льготная категория)
            if (_currentVisitor.TypeID.HasValue && cmbVisitorTypeCategory != null)
            {
                foreach (var item in cmbVisitorTypeCategory.Items)
                {
                    if (item is VisitorTypes type && type.TypeID == _currentVisitor.TypeID.Value)
                    {
                        cmbVisitorTypeCategory.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void cmbVisitorType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbVisitorType.SelectedItem is ComboBoxItem item)
                {
                    bool isGroup = item.Content.ToString() == "Группа";
                    ShowIndividualControls(!isGroup); // true - физлицо, false - группа

                    // Очищаем поля при переключении
                    if (isGroup)
                    {
                        if (txtLastName != null) txtLastName.Text = "";
                        if (txtFirstName != null) txtFirstName.Text = "";
                    }
                    else
                    {
                        if (txtGroupName != null) txtGroupName.Text = "";
                        if (txtCourseNumber != null) txtCourseNumber.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переключении типа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowIndividualControls(bool showIndividual)
        {
            try
            {
                // Проверяем, что все элементы управления существуют
                if (lblLastName == null || txtLastName == null ||
                    lblFirstName == null || txtFirstName == null ||
                    lblGroupName == null || txtGroupName == null ||
                    lblCourseNumber == null || txtCourseNumber == null)
                {
                    // Если элементы еще не инициализированы, выходим
                    return;
                }

                if (showIndividual) // Показываем поля для физического лица
                {
                    // Скрываем поля группы
                    lblGroupName.Visibility = Visibility.Collapsed;
                    txtGroupName.Visibility = Visibility.Collapsed;
                    lblCourseNumber.Visibility = Visibility.Collapsed;
                    txtCourseNumber.Visibility = Visibility.Collapsed;

                    // Показываем поля ФИО
                    lblLastName.Visibility = Visibility.Visible;
                    txtLastName.Visibility = Visibility.Visible;
                    lblFirstName.Visibility = Visibility.Visible;
                    txtFirstName.Visibility = Visibility.Visible;
                }
                else // Показываем поля для группы
                {
                    // Скрываем поля ФИО
                    lblLastName.Visibility = Visibility.Collapsed;
                    txtLastName.Visibility = Visibility.Collapsed;
                    lblFirstName.Visibility = Visibility.Collapsed;
                    txtFirstName.Visibility = Visibility.Collapsed;

                    // Показываем поля группы
                    lblGroupName.Visibility = Visibility.Visible;
                    txtGroupName.Visibility = Visibility.Visible;
                    lblCourseNumber.Visibility = Visibility.Visible;
                    txtCourseNumber.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"Ошибка отображения: {ex.Message}");
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;

                // Заполняем общие поля
                _currentVisitor.Phone = txtPhone.Text;
                _currentVisitor.Email = txtEmail.Text;

                // Выбранный тип посетителя (льготная категория)
                if (cmbVisitorTypeCategory.SelectedItem is VisitorTypes selectedType)
                {
                    _currentVisitor.TypeID = selectedType.TypeID;
                }

                // Определяем тип и заполняем соответствующие поля
                if (cmbVisitorType.SelectedItem is ComboBoxItem typeItem)
                {
                    bool isGroup = typeItem.Content.ToString() == "Группа";

                    if (isGroup)
                    {
                        // Для группы
                        _currentVisitor.LastName = txtGroupName.Text;
                        _currentVisitor.FirstName = "";
                        _currentVisitor.GroupName = txtGroupName.Text;

                        if (!string.IsNullOrEmpty(txtCourseNumber.Text))
                        {
                            _currentVisitor.CourseNumber = int.Parse(txtCourseNumber.Text);
                        }
                        else
                        {
                            _currentVisitor.CourseNumber = null;
                        }
                    }
                    else
                    {
                        // Для физического лица
                        _currentVisitor.LastName = txtLastName.Text;
                        _currentVisitor.FirstName = txtFirstName.Text;
                        _currentVisitor.GroupName = null;
                        _currentVisitor.CourseNumber = null;
                    }
                }

                using (var context = new MuseumTechDBEntities())
                {
                    if (_isEditMode)
                    {
                        // Редактирование
                        var existingVisitor = context.Visitors.Find(_currentVisitor.VisitorID);
                        if (existingVisitor != null)
                        {
                            existingVisitor.LastName = _currentVisitor.LastName;
                            existingVisitor.FirstName = _currentVisitor.FirstName;
                            existingVisitor.GroupName = _currentVisitor.GroupName;
                            existingVisitor.CourseNumber = _currentVisitor.CourseNumber;
                            existingVisitor.TypeID = _currentVisitor.TypeID;
                            existingVisitor.Phone = _currentVisitor.Phone;
                            existingVisitor.Email = _currentVisitor.Email;
                            // Не меняем RegistrationDate при редактировании
                        }
                    }
                    else
                    {
                        // Добавление
                        context.Visitors.Add(_currentVisitor);
                    }
                    context.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверяем выбран ли тип посетителя
            if (cmbVisitorType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип посетителя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (cmbVisitorType.SelectedItem is ComboBoxItem typeItem)
            {
                bool isGroup = typeItem.Content.ToString() == "Группа";

                if (isGroup)
                {
                    // Валидация для группы
                    if (string.IsNullOrWhiteSpace(txtGroupName.Text))
                    {
                        MessageBox.Show("Введите название группы", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (!string.IsNullOrEmpty(txtCourseNumber.Text))
                    {
                        if (!int.TryParse(txtCourseNumber.Text, out int course))
                        {
                            MessageBox.Show("Курс должен быть числом", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                        if (course < 1 || course > 5)
                        {
                            MessageBox.Show("Курс должен быть от 1 до 5", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }
                else
                {
                    // Валидация для физического лица
                    if (string.IsNullOrWhiteSpace(txtLastName.Text))
                    {
                        MessageBox.Show("Введите фамилию", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                    {
                        MessageBox.Show("Введите имя", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            // Валидация телефона
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Введите телефон", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Валидация email (необязательное поле)
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                if (!IsValidEmail(txtEmail.Text))
                {
                    MessageBox.Show("Введите корректный email адрес", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}