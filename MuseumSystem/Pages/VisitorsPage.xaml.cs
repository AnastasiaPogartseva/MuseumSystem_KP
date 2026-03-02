using MuseumSystem.ApplicationData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace MuseumSystem.Pages
{
    public partial class VisitorsPage : Page
    {
        public VisitorsPage()
        {
            try
            {
                InitializeComponent();
                this.Loaded += VisitorsPage_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VisitorsPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadVisitors();
        }

        private void LoadVisitors()
        {
            try
            {
                // Проверяем элементы управления
                if (!CheckControls()) return;

                LoadingOverlay.Visibility = Visibility.Visible;

                using (var context = new MuseumTechDBEntities())
                {
                    var visitors = context.Visitors
                        .Include(x => x.VisitorTypes)
                        .ToList();

                    // Создаем анонимные объекты для отображения
                    var displayItems = visitors.Select(x => new
                    {
                        VisitorID = x.VisitorID,
                        // Формируем ФИО
                        FullName = $"{x.LastName ?? ""} {x.FirstName ?? ""}".Trim(),
                        // Определяем тип посетителя
                        VisitorType = GetVisitorType(x),
                        // Для групп
                        GroupInfo = GetGroupInfo(x),
                        Phone = x.Phone ?? "—",
                        Email = x.Email ?? "—",
                        RegistrationDate = x.RegistrationDate ?? DateTime.Now,
                        StatusText = GetStatusText(x),
                        StatusColor = GetStatusColor(x),
                        VisitsCount = x.ExcursionVisitors?.Count ?? 0,
                        // Сохраняем ссылку на оригинальный объект
                        Visitor = x
                    }).ToList();

                    dgVisitors.ItemsSource = displayItems;

                    // Обновляем статистику
                    TotalVisitorsText.Text = visitors.Count.ToString();

                    // Активные сегодня - те, у кого есть экскурсии сегодня
                    int activeToday = visitors.Count(x => x.ExcursionVisitors != null &&
                        x.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                            ev.Excursions.ExcursionDate == DateTime.Today));
                    ActiveTodayText.Text = activeToday.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private bool CheckControls()
        {
            return dgVisitors != null &&
                   txtSearch != null &&
                   cmbTypeFilter != null &&
                   LoadingOverlay != null &&
                   TotalVisitorsText != null &&
                   ActiveTodayText != null;
        }

        private string GetVisitorType(Visitors visitor)
        {
            if (visitor == null) return "Неизвестно";

            // Если есть GroupName - это группа
            if (!string.IsNullOrWhiteSpace(visitor.GroupName))
                return "Группа";

            // Если есть TypeID - смотрим тип
            if (visitor.TypeID.HasValue && visitor.VisitorTypes != null)
                return visitor.VisitorTypes.TypeName ?? "Физическое лицо";

            return "Физическое лицо";
        }

        private string GetGroupInfo(Visitors visitor)
        {
            if (!string.IsNullOrWhiteSpace(visitor.GroupName))
            {
                if (visitor.CourseNumber.HasValue)
                    return $"{visitor.GroupName} (курс {visitor.CourseNumber})";
                return visitor.GroupName;
            }
            return "—";
        }

        private string GetStatusText(Visitors visitor)
        {
            if (visitor == null) return "Неизвестно";

            // Проверяем, есть ли у посетителя экскурсии сегодня
            if (visitor.ExcursionVisitors != null &&
                visitor.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                    ev.Excursions.ExcursionDate == DateTime.Today))
                return "Посещает сегодня";

            // Проверяем, есть ли экскурсии на этой неделе
            if (visitor.ExcursionVisitors != null &&
                visitor.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                    ev.Excursions.ExcursionDate >= DateTime.Today.AddDays(-7)))
                return "Активен";

            return "Неактивен";
        }

        private string GetStatusColor(Visitors visitor)
        {
            if (visitor == null) return "#9E9E9E";

            if (visitor.ExcursionVisitors != null &&
                visitor.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                    ev.Excursions.ExcursionDate == DateTime.Today))
                return "#4CAF50"; // Зеленый

            if (visitor.ExcursionVisitors != null &&
                visitor.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                    ev.Excursions.ExcursionDate >= DateTime.Today.AddDays(-7)))
                return "#2196F3"; // Синий

            return "#9E9E9E"; // Серый
        }

        private void ApplyFilters()
        {
            try
            {
                if (!CheckControls()) return;

                using (var context = new MuseumTechDBEntities())
                {
                    var query = context.Visitors
                        .Include(x => x.VisitorTypes)
                        .Include(x => x.ExcursionVisitors.Select(ev => ev.Excursions))
                        .AsQueryable();

                    // Поиск по тексту
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    {
                        string search = txtSearch.Text.ToLower().Trim();
                        query = query.Where(x =>
                            (x.LastName != null && x.LastName.ToLower().Contains(search)) ||
                            (x.FirstName != null && x.FirstName.ToLower().Contains(search)) ||
                            (x.GroupName != null && x.GroupName.ToLower().Contains(search)) ||
                            (x.Email != null && x.Email.ToLower().Contains(search)) ||
                            (x.Phone != null && x.Phone.Contains(search))
                        );
                    }

                    // Фильтр по типу
                    if (cmbTypeFilter.SelectedItem != null && cmbTypeFilter.SelectedIndex > 0)
                    {
                        string selectedType = "";

                        if (cmbTypeFilter.SelectedItem is ComboBoxItem item)
                        {
                            selectedType = item.Content?.ToString() ?? "";
                        }

                        if (!string.IsNullOrEmpty(selectedType))
                        {
                            if (selectedType == "Физические лица")
                            {
                                query = query.Where(x => string.IsNullOrEmpty(x.GroupName));
                            }
                            else if (selectedType == "Группы")
                            {
                                query = query.Where(x => !string.IsNullOrEmpty(x.GroupName));
                            }
                            else if (selectedType == "Льготные категории")
                            {
                                // Предполагаем, что льготники имеют определенный TypeID
                                query = query.Where(x => x.TypeID == 2); // Измените на нужный ID
                            }
                        }
                    }

                    var visitors = query.ToList();

                    var displayItems = visitors.Select(x => new
                    {
                        VisitorID = x.VisitorID,
                        FullName = $"{x.LastName ?? ""} {x.FirstName ?? ""}".Trim(),
                        VisitorType = GetVisitorType(x),
                        GroupInfo = GetGroupInfo(x),
                        Phone = x.Phone ?? "—",
                        Email = x.Email ?? "—",
                        DocumentNumber = "—", // В модели нет DocumentNumber
                        RegistrationDate = x.RegistrationDate ?? DateTime.Now,
                        StatusText = GetStatusText(x),
                        StatusColor = GetStatusColor(x),
                        VisitsCount = x.ExcursionVisitors?.Count ?? 0,
                        Visitor = x
                    }).ToList();

                    dgVisitors.ItemsSource = displayItems;

                    // Обновляем статистику
                    TotalVisitorsText.Text = visitors.Count.ToString();

                    int activeToday = visitors.Count(x => x.ExcursionVisitors != null &&
                        x.ExcursionVisitors.Any(ev => ev.Excursions != null &&
                            ev.Excursions.ExcursionDate == DateTime.Today));
                    ActiveTodayText.Text = activeToday.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики событий
        public void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplyFilters();
            }
        }

        public void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        public void cmbTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        public void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbTypeFilter.SelectedIndex = 0;
            LoadVisitors();
        }

        // Кнопка "Добавить"
        public void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditVisitorWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    LoadVisitors();
                    MessageBox.Show("Посетитель успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении посетителя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Редактировать"
        public void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgVisitors.SelectedItem == null)
                {
                    MessageBox.Show("Выберите посетителя для редактирования", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dynamic selected = dgVisitors.SelectedItem;

                if (selected?.Visitor == null)
                {
                    MessageBox.Show("Не удалось получить данные посетителя", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем полный объект из базы данных
                using (var context = new MuseumTechDBEntities())
                {
                    var visitor = context.Visitors.Find(selected.Visitor.VisitorID);
                    if (visitor == null)
                    {
                        MessageBox.Show("Посетитель не найден в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var editWindow = new AddEditVisitorWindow(visitor);
                    editWindow.Owner = Window.GetWindow(this);

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadVisitors();
                        MessageBox.Show("Данные посетителя успешно обновлены!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgVisitors.SelectedItem == null)
                {
                    MessageBox.Show("Выберите посетителя для удаления", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранного посетителя?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    dynamic selected = dgVisitors.SelectedItem;
                    if (selected?.Visitor != null)
                    {
                        using (var context = new MuseumTechDBEntities())
                        {
                            var visitor = context.Visitors.Find(selected.Visitor.VisitorID);
                            if (visitor != null)
                            {
                                context.Visitors.Remove(visitor);
                                context.SaveChanges();
                            }
                        }
                        LoadVisitors();
                        MessageBox.Show("Посетитель успешно удален", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void dgVisitors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgVisitors.SelectedItem != null)
            {
                MenuItemView_Click(sender, e);
            }
        }

        public void MenuItemView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic selected = dgVisitors.SelectedItem;
                if (selected == null) return;

                string type = selected.VisitorType;
                string groupInfo = selected.GroupInfo != "—" ? $"\nГруппа: {selected.GroupInfo}" : "";

                MessageBox.Show($"Посетитель: {selected.FullName}\n" +
                              $"Тип: {type}{groupInfo}\n" +
                              $"Телефон: {selected.Phone}\n" +
                              $"Email: {selected.Email}\n" +
                              $"Дата регистрации: {selected.RegistrationDate.ToShortDateString()}\n" +
                              $"Посещений: {selected.VisitsCount}",
                    "Просмотр посетителя", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MenuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            BtnEdit_Click(sender, e);
        }

        public void MenuItemHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dynamic selected = dgVisitors.SelectedItem;
                if (selected == null) return;

                // Здесь можно показать историю посещений
                MessageBox.Show($"История посещений для: {selected.FullName}\n\n" +
                              $"Всего посещений: {selected.VisitsCount}\n" +
                              $"Функция просмотра детальной истории временно недоступна",
                    "История посещений", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            BtnDelete_Click(sender, e);
        }
    }
}