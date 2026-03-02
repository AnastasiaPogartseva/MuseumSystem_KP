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
    public partial class ExcursionsPage : Page
    {
        public ExcursionsPage()
        {
            try
            {
                InitializeComponent();

                // Подписываемся на событие загрузки
                this.Loaded += ExcursionsPage_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExcursionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем данные только после полной инициализации страницы
            LoadExcursions();
        }

        private void LoadExcursions()
        {
            try
            {
                // Проверяем, что элементы управления существуют
                if (!CheckControls())
                {
                    MessageBox.Show("Элементы управления не инициализированы", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadingOverlay.Visibility = Visibility.Visible;

                using (var context = new MuseumTechDBEntities())
                {
                    var excursions = context.Excursions
                        .Include(x => x.Employees)
                        .Include(x => x.Exhibitions)
                        .ToList();

                    // Создаем анонимные объекты для отображения
                    var displayItems = new List<object>();

                    foreach (var x in excursions)
                    {
                        if (x == null) continue;

                        var item = new
                        {
                            ExcursionID = x.ExcursionID,
                            ExhibitionName = x.Exhibitions?.Name ?? "Без названия",
                            GuideName = GetGuideFullName(x.Employees),
                            DisplayDate = x.ExcursionDate,
                            StartTimeDisplay = FormatTime(x.StartTime),
                            EndTimeDisplay = FormatTime(x.EndTime),
                            MaxVisitorsDisplay = x.MaxVisitors ?? 0,
                            CurrentVisitorsDisplay = x.CurrentVisitors ?? 0,
                            MeetingPointDisplay = x.MeetingPoint ?? "Не указано",
                            StatusText = GetStatusText(x),
                            StatusColor = GetStatusColor(x),
                            Excursion = x
                        };
                        displayItems.Add(item);
                    }

                    dgExcursions.ItemsSource = displayItems;

                    // Статистика
                    UpdateStatistics(excursions);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}\n\nСтек: {ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // Проверка инициализации всех элементов управления
        private bool CheckControls()
        {
            try
            {
                return dgExcursions != null &&
                       cmbStatusFilter != null &&
                       txtSearch != null &&
                       LoadingOverlay != null &&
                       TotalExcursionsText != null &&
                       ActiveExcursionsText != null &&
                       TotalVisitorsText != null;
            }
            catch
            {
                return false;
            }
        }

        private string GetGuideFullName(Employees employee)
        {
            try
            {
                if (employee == null) return "Не назначен";

                string firstName = employee.FirstName ?? "";
                string lastName = employee.LastName ?? "";

                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                    return "Не указан";

                return $"{firstName} {lastName}".Trim();
            }
            catch
            {
                return "Не назначен";
            }
        }

        private string FormatTime(TimeSpan time)
        {
            try
            {
                return time.ToString(@"hh\:mm");
            }
            catch
            {
                return "00:00";
            }
        }

        private string GetStatusText(Excursions excursion)
        {
            try
            {
                if (excursion == null) return "Неизвестно";

                if (excursion.ExcursionDate < DateTime.Today)
                    return "Завершена";
                if (excursion.ExcursionDate == DateTime.Today)
                    return "Сегодня";
                if ((excursion.CurrentVisitors ?? 0) >= (excursion.MaxVisitors ?? 0))
                    return "Нет мест";
                return "Активна";
            }
            catch
            {
                return "Неизвестно";
            }
        }

        private string GetStatusColor(Excursions excursion)
        {
            try
            {
                if (excursion == null) return "#9E9E9E";

                if (excursion.ExcursionDate < DateTime.Today)
                    return "#9E9E9E";
                if (excursion.ExcursionDate == DateTime.Today)
                    return "#4CAF50";
                if ((excursion.CurrentVisitors ?? 0) >= (excursion.MaxVisitors ?? 0))
                    return "#f44336";
                return "#2196F3";
            }
            catch
            {
                return "#9E9E9E";
            }
        }

        private void UpdateStatistics(List<Excursions> excursions)
        {
            try
            {
                if (TotalExcursionsText != null)
                    TotalExcursionsText.Text = excursions.Count.ToString();

                int activeCount = excursions.Count(x => x != null && x.ExcursionDate >= DateTime.Today);
                if (ActiveExcursionsText != null)
                    ActiveExcursionsText.Text = activeCount.ToString();

                int totalVisitors = excursions.Sum(x => x?.CurrentVisitors ?? 0);
                if (TotalVisitorsText != null)
                    TotalVisitorsText.Text = totalVisitors.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления статистики: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                // Проверяем, что страница полностью загружена
                if (!CheckControls())
                {
                    MessageBox.Show("Страница еще загружается, попробуйте через секунду", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using (var context = new MuseumTechDBEntities())
                {
                    // Получаем все экскурсии
                    var allExcursions = context.Excursions
                        .Include(x => x.Employees)
                        .Include(x => x.Exhibitions)
                        .ToList();

                    if (allExcursions == null || allExcursions.Count == 0)
                    {
                        dgExcursions.ItemsSource = new List<object>();
                        UpdateStatistics(new List<Excursions>());
                        return;
                    }

                    // Применяем фильтры
                    IEnumerable<Excursions> filtered = allExcursions;

                    // Фильтр по тексту
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    {
                        string search = txtSearch.Text.ToLower().Trim();
                        filtered = filtered.Where(x =>
                            x != null &&
                            ((x.Exhibitions != null && x.Exhibitions.Name != null &&
                              x.Exhibitions.Name.ToLower().Contains(search)) ||
                             (x.Employees != null &&
                              ((x.Employees.FirstName != null && x.Employees.FirstName.ToLower().Contains(search)) ||
                               (x.Employees.LastName != null && x.Employees.LastName.ToLower().Contains(search)))))
                        );
                    }

                    // Фильтр по статусу
                    string selectedStatus = GetSelectedStatus();

                    if (selectedStatus == "Активные")
                    {
                        filtered = filtered.Where(x => x != null && x.ExcursionDate >= DateTime.Today);
                    }
                    else if (selectedStatus == "Завершенные")
                    {
                        filtered = filtered.Where(x => x != null && x.ExcursionDate < DateTime.Today);
                    }

                    // Создаем список для отображения
                    var excursions = filtered.ToList();
                    var displayItems = new List<object>();

                    foreach (var x in excursions)
                    {
                        if (x == null) continue;

                        try
                        {
                            var item = new
                            {
                                ExcursionID = x.ExcursionID,
                                ExhibitionName = x.Exhibitions?.Name ?? "Без названия",
                                GuideName = GetGuideFullName(x.Employees),
                                DisplayDate = x.ExcursionDate,
                                StartTimeDisplay = FormatTime(x.StartTime),
                                EndTimeDisplay = FormatTime(x.EndTime),
                                MaxVisitorsDisplay = x.MaxVisitors ?? 0,
                                CurrentVisitorsDisplay = x.CurrentVisitors ?? 0,
                                MeetingPointDisplay = x.MeetingPoint ?? "Не указано",
                                StatusText = GetStatusText(x),
                                StatusColor = GetStatusColor(x),
                                Excursion = x
                            };
                            displayItems.Add(item);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка создания элемента: {ex.Message}");
                        }
                    }

                    dgExcursions.ItemsSource = displayItems;
                    UpdateStatistics(excursions);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSelectedStatus()
        {
            try
            {
                if (cmbStatusFilter?.SelectedItem == null)
                    return "Все экскурсии";

                if (cmbStatusFilter.SelectedItem is ComboBoxItem item && item.Content != null)
                {
                    return item.Content.ToString() ?? "Все экскурсии";
                }

                return cmbStatusFilter.SelectedItem.ToString() ?? "Все экскурсии";
            }
            catch
            {
                return "Все экскурсии";
            }
        }

        // Обработчики событий с проверкой инициализации
        public void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                if (e.Key == Key.Enter)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void cmbStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                txtSearch.Text = "";
                cmbStatusFilter.SelectedIndex = 0;
                LoadExcursions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Добавить"
        public void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditExcursionWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    LoadExcursions();
                    MessageBox.Show("Экскурсия успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении экскурсии: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Кнопка "Редактировать"
        public void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls() || dgExcursions.SelectedItem == null)
                {
                    MessageBox.Show("Выберите экскурсию для редактирования", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dynamic selected = dgExcursions.SelectedItem;

                if (selected?.Excursion == null)
                {
                    MessageBox.Show("Не удалось получить данные экскурсии", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Получаем полный объект из базы данных
                using (var context = new MuseumTechDBEntities())
                {
                    var excursion = context.Excursions.Find(selected.Excursion.ExcursionID);
                    if (excursion == null)
                    {
                        MessageBox.Show("Экскурсия не найдена в базе данных", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var editWindow = new AddEditExcursionWindow(excursion);
                    editWindow.Owner = Window.GetWindow(this);

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadExcursions();
                        MessageBox.Show("Данные экскурсии успешно обновлены!", "Успех",
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
                if (!CheckControls() || dgExcursions.SelectedItem == null)
                {
                    MessageBox.Show("Выберите экскурсию для удаления", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить выбранную экскурсию?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    dynamic selected = dgExcursions.SelectedItem;
                    if (selected?.Excursion != null)
                    {
                        using (var context = new MuseumTechDBEntities())
                        {
                            var excursion = context.Excursions.Find(selected.Excursion.ExcursionID);
                            if (excursion != null)
                            {
                                context.Excursions.Remove(excursion);
                                context.SaveChanges();
                            }
                        }
                    }
                    LoadExcursions();
                    MessageBox.Show("Экскурсия успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void dgExcursions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!CheckControls() || dgExcursions.SelectedItem == null) return;
                MenuItemView_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MenuItemView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                dynamic selected = dgExcursions.SelectedItem;
                if (selected == null) return;

                string exhibitionName = selected.ExhibitionName ?? "Не указана";
                string guideName = selected.GuideName ?? "Не указан";
                string date = selected.DisplayDate != null
                    ? ((DateTime)selected.DisplayDate).ToShortDateString()
                    : "Не указана";

                MessageBox.Show($"Экскурсия: {exhibitionName}\nГид: {guideName}\nДата: {date}\nВремя: {selected.StartTimeDisplay}\nМесто сбора: {selected.MeetingPointDisplay}",
                    "Просмотр экскурсии", MessageBoxButton.OK, MessageBoxImage.Information);
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

        public void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            BtnDelete_Click(sender, e);
        }

        public void MenuItemParticipants_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                dynamic selected = dgExcursions.SelectedItem;
                if (selected == null) return;

                int current = selected.CurrentVisitorsDisplay;
                int max = selected.MaxVisitorsDisplay;

                MessageBox.Show($"Участников: {current} из {max}",
                    "Участники экскурсии", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MenuItemComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                dynamic selected = dgExcursions.SelectedItem;
                if (selected != null)
                {
                    MessageBox.Show("Экскурсия отмечена как проведенная", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MenuItemCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CheckControls()) return;

                dynamic selected = dgExcursions.SelectedItem;
                if (selected == null) return;

                var result = MessageBox.Show("Отменить выбранную экскурсию?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Экскурсия отменена", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}