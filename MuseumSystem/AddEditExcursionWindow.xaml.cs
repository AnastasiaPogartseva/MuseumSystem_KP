using MuseumSystem.ApplicationData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MuseumSystem.Pages
{
    public partial class AddEditExcursionWindow : Window
    {
        private Excursions _currentExcursion;
        private bool _isEditMode;

        // Конструктор для добавления
        public AddEditExcursionWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            _currentExcursion = new Excursions();
            _currentExcursion.CurrentVisitors = 0;
            _currentExcursion.CreatedDate = DateTime.Now;
            LoadExhibitions();
            LoadGuides();
            TitleText.Text = "Добавление новой экскурсии";
            dpExcursionDate.SelectedDate = DateTime.Today;
        }

        // Конструктор для редактирования
        public AddEditExcursionWindow(Excursions excursion)
        {
            InitializeComponent();
            _isEditMode = true;
            _currentExcursion = excursion;
            LoadExhibitions();
            LoadGuides();
            LoadExcursionData();
            TitleText.Text = "Редактирование экскурсии";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Дополнительная инициализация после загрузки окна
        }

        private void LoadExhibitions()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var exhibitions = context.Exhibitions.ToList();
                    cmbExhibition.ItemsSource = exhibitions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки выставок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGuides()
        {
            try
            {
                using (var context = new MuseumTechDBEntities())
                {
                    var guides = context.Employees
                        .Where(x => x.Position == "Гид" || x.Position == "Экскурсовод" || x.Position.Contains("гид"))
                        .Select(x => new
                        {
                            EmployeeID = x.EmployeeID,
                            FullName = (x.LastName + " " + x.FirstName).Trim()
                        })
                        .ToList();

                    if (guides.Count == 0)
                    {
                        // Если нет гидов по должности, показываем всех сотрудников
                        guides = context.Employees
                            .Select(x => new
                            {
                                EmployeeID = x.EmployeeID,
                                FullName = (x.LastName + " " + x.FirstName).Trim()
                            })
                            .ToList();
                    }

                    cmbGuide.ItemsSource = guides;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки гидов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExcursionData()
        {
            if (_currentExcursion == null) return;

            // Выставка
            if (_currentExcursion.ExhibitionID.HasValue)
            {
                foreach (var item in cmbExhibition.Items)
                {
                    if (item is Exhibitions ex && ex.ExhibitionID == _currentExcursion.ExhibitionID.Value)
                    {
                        cmbExhibition.SelectedItem = item;
                        break;
                    }
                }
            }

            // Дата
            if (_currentExcursion.ExcursionDate != null)
            {
                dpExcursionDate.SelectedDate = _currentExcursion.ExcursionDate;
            }

            // Время
            txtStartTime.Text = _currentExcursion.StartTime.ToString(@"hh\:mm");
            txtEndTime.Text = _currentExcursion.EndTime.ToString(@"hh\:mm");

            // Гид
            if (_currentExcursion.EmployeeID.HasValue)
            {
                cmbGuide.SelectedValue = _currentExcursion.EmployeeID.Value;
            }

            // Количество мест
            txtMaxVisitors.Text = _currentExcursion.MaxVisitors?.ToString() ?? "20";

            // Место сбора
            txtMeetingPoint.Text = _currentExcursion.MeetingPoint ?? "Вестибюль музея";
        }

        private void TimeValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9:]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void txtTime_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // Проверяем и форматируем время
            if (TimeSpan.TryParse(textBox.Text, out TimeSpan time))
            {
                textBox.Text = time.ToString(@"hh\:mm");
            }
            else
            {
                textBox.Text = "00:00";
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateTime(TextBox textBox)
        {
            if (TimeSpan.TryParse(textBox.Text, out TimeSpan time))
            {
                return time.Hours >= 0 && time.Hours < 24 && time.Minutes >= 0 && time.Minutes < 60;
            }
            return false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;

                // Выставка
                if (cmbExhibition.SelectedItem is Exhibitions selectedExhibition)
                {
                    _currentExcursion.ExhibitionID = selectedExhibition.ExhibitionID;
                }

                // Дата
                if (dpExcursionDate.SelectedDate.HasValue)
                {
                    _currentExcursion.ExcursionDate = dpExcursionDate.SelectedDate.Value;
                }

                // Время
                if (TimeSpan.TryParse(txtStartTime.Text, out TimeSpan startTime))
                {
                    _currentExcursion.StartTime = startTime;
                }

                if (TimeSpan.TryParse(txtEndTime.Text, out TimeSpan endTime))
                {
                    _currentExcursion.EndTime = endTime;
                }

                // Гид
                if (cmbGuide.SelectedValue != null)
                {
                    if (int.TryParse(cmbGuide.SelectedValue.ToString(), out int guideId))
                    {
                        _currentExcursion.EmployeeID = guideId;
                    }
                }

                // Количество мест
                if (int.TryParse(txtMaxVisitors.Text, out int maxVisitors))
                {
                    _currentExcursion.MaxVisitors = maxVisitors;
                }

                // Место сбора
                _currentExcursion.MeetingPoint = txtMeetingPoint.Text;

                // Статус (если не указан, устанавливаем по умолчанию)
                if (string.IsNullOrEmpty(_currentExcursion.Status))
                {
                    _currentExcursion.Status = "Активна";
                }

                using (var context = new MuseumTechDBEntities())
                {
                    if (_isEditMode)
                    {
                        // Редактирование
                        var existingExcursion = context.Excursions.Find(_currentExcursion.ExcursionID);
                        if (existingExcursion != null)
                        {
                            existingExcursion.ExhibitionID = _currentExcursion.ExhibitionID;
                            existingExcursion.EmployeeID = _currentExcursion.EmployeeID;
                            existingExcursion.ExcursionDate = _currentExcursion.ExcursionDate;
                            existingExcursion.StartTime = _currentExcursion.StartTime;
                            existingExcursion.EndTime = _currentExcursion.EndTime;
                            existingExcursion.MaxVisitors = _currentExcursion.MaxVisitors;
                            existingExcursion.MeetingPoint = _currentExcursion.MeetingPoint;
                            // Не меняем CurrentVisitors и CreatedBy/CreatedDate при редактировании
                        }
                    }
                    else
                    {
                        // Добавление
                        _currentExcursion.CurrentVisitors = 0;
                        // Можно установить CreatedBy из текущего пользователя, если есть система авторизации
                        // _currentExcursion.CreatedBy = текущий пользователь;
                        context.Excursions.Add(_currentExcursion);
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
            // Проверка выставки
            if (cmbExhibition.SelectedItem == null)
            {
                MessageBox.Show("Выберите название экскурсии", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка даты
            if (!dpExcursionDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату проведения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка времени
            if (!ValidateTime(txtStartTime))
            {
                MessageBox.Show("Введите корректное время начала (ЧЧ:ММ)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!ValidateTime(txtEndTime))
            {
                MessageBox.Show("Введите корректное время окончания (ЧЧ:ММ)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка, что время начала меньше времени окончания
            if (TimeSpan.TryParse(txtStartTime.Text, out TimeSpan start) &&
                TimeSpan.TryParse(txtEndTime.Text, out TimeSpan end))
            {
                if (start >= end)
                {
                    MessageBox.Show("Время начала должно быть меньше времени окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            // Проверка гида
            if (cmbGuide.SelectedItem == null)
            {
                MessageBox.Show("Выберите гида", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка количества мест
            if (string.IsNullOrWhiteSpace(txtMaxVisitors.Text))
            {
                MessageBox.Show("Введите максимальное количество посетителей", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!int.TryParse(txtMaxVisitors.Text, out int maxVisitors))
            {
                MessageBox.Show("Количество посетителей должно быть числом", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (maxVisitors <= 0)
            {
                MessageBox.Show("Количество посетителей должно быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Проверка места сбора
            if (string.IsNullOrWhiteSpace(txtMeetingPoint.Text))
            {
                MessageBox.Show("Введите место сбора", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}