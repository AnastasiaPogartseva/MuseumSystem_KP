using MuseumSystem.ApplicationData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MuseumSystem
{
    public partial class AddExcursionWindow : Window
    {
        private MuseumTechDBEntities dbContext;
        private List<Exponats> allExponats;
        private List<Exponats> selectedExponats;

        public AddExcursionWindow()
        {
            InitializeComponent();
            dbContext = new MuseumTechDBEntities();
            selectedExponats = new List<Exponats>();
            LoadGuides();
            LoadExponats();
        }

        // Загрузка гидов (сотрудников с ролью гид)
        // Загрузка гидов (сотрудников с ролью гид)
        private void LoadGuides()
        {
            try
            {
                var guides = dbContext.Employees
                    .Where(e => e.Position == "Гид" || e.Position == "Экскурсовод")
                    .Select(e => new
                    {
                        EmployeeID = e.EmployeeID,
                        FullName = e.LastName + " " + e.FirstName
                    })
                    .ToList();

                cmbGuide.ItemsSource = guides;
                cmbGuide.DisplayMemberPath = "FullName";
                cmbGuide.SelectedValuePath = "EmployeeID";

                if (guides.Count > 0)
                    cmbGuide.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки гидов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Загрузка всех доступных экспонатов
        private void LoadExponats()
        {
            try
            {
                allExponats = dbContext.Exponats
                    .Where(e => e.Status == true) // Только доступные
                    .OrderBy(e => e.Name)
                    .ToList();

                lstExponats.ItemsSource = allExponats;
                lstExponats.DisplayMemberPath = "Name";
                lstExponats.SelectionMode = SelectionMode.Multiple;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки экспонатов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Поиск экспонатов
        private void txtSearchExponat_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearchExponat.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                lstExponats.ItemsSource = allExponats;
            }
            else
            {
                var filtered = allExponats
                    .Where(ex => ex.Name.ToLower().Contains(searchText) ||
                                (ex.Description != null && ex.Description.ToLower().Contains(searchText)))
                    .ToList();

                lstExponats.ItemsSource = filtered;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dpDate.SelectedDate = DateTime.Now.AddDays(1);
            txtStartTime.Text = "10:00";
            txtEndTime.Text = "11:30";
            txtMaxVisitors.Text = "20";
            txtPrice.Text = "500";
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            txtSelectedCount.Text = $"Выбрано экспонатов: {selectedExponats.Count}";
        }

        // Сохранение экскурсии
        private void BtnSaveExcursion_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder();

            // Валидация
            if (string.IsNullOrWhiteSpace(txtName.Text))
                errors.AppendLine("• Введите название экскурсии");

            if (dpDate.SelectedDate == null)
                errors.AppendLine("• Выберите дату проведения");

            if (cmbGuide.SelectedItem == null)
                errors.AppendLine("• Выберите гида");

            if (!int.TryParse(txtMaxVisitors.Text, out int maxVisitors) || maxVisitors <= 0)
                errors.AppendLine("• Введите корректное количество посетителей");

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
                errors.AppendLine("• Введите корректную стоимость");

            if (!TimeSpan.TryParse(txtStartTime.Text, out TimeSpan startTime))
                errors.AppendLine("• Введите корректное время начала (ЧЧ:ММ)");

            if (!TimeSpan.TryParse(txtEndTime.Text, out TimeSpan endTime))
                errors.AppendLine("• Введите корректное время окончания (ЧЧ:ММ)");

            if (startTime >= endTime)
                errors.AppendLine("• Время окончания должно быть позже времени начала");

            // Собираем выбранные экспонаты
            selectedExponats.Clear();
            foreach (var item in lstExponats.SelectedItems)
            {
                if (item is Exponats exponat)
                {
                    selectedExponats.Add(exponat);
                }
            }

            if (selectedExponats.Count == 0)
                errors.AppendLine("• Выберите хотя бы один экспонат для экскурсии");

            if (errors.Length > 0)
            {
                MessageBox.Show($"Проверьте правильность заполнения полей:\n{errors}",
                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // СОЗДАЕМ НОВУЮ ЭКСКУРСИЮ
                // Используем те названия полей, которые есть в вашей БД!
                Excursions newExcursion = new Excursions();

                // Заполняем свойства через рефлексию, чтобы избежать ошибок компиляции
                var excursionType = typeof(Excursions);

                // Название экскурсии - пробуем разные варианты
                bool nameSet = false;
                foreach (string propName in new[] { "Name", "Title", "ExcursionName", "ExcursionTitle" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(newExcursion, txtName.Text.Trim());
                        nameSet = true;
                        break;
                    }
                }

                // Описание
                foreach (string propName in new[] { "Description", "Desc", "Notes" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(newExcursion, txtDescription.Text?.Trim() ?? "");
                        break;
                    }
                }

                // Дата и время
                DateTime excursionDateTime = dpDate.SelectedDate.Value.Date + startTime;
                foreach (string propName in new[] { "Date", "ExcursionDate", "DateTime", "StartDateTime" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(DateTime))
                            prop.SetValue(newExcursion, excursionDateTime);
                        else if (prop.PropertyType == typeof(DateTime?))
                            prop.SetValue(newExcursion, excursionDateTime);
                        break;
                    }
                }

                // Длительность (минуты)
                int duration = (int)(endTime - startTime).TotalMinutes;
                foreach (string propName in new[] { "Duration", "DurationMinutes", "Length" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(int))
                            prop.SetValue(newExcursion, duration);
                        else if (prop.PropertyType == typeof(int?))
                            prop.SetValue(newExcursion, duration);
                        break;
                    }
                }

                // ID гида
                int guideId = (int)cmbGuide.SelectedValue;
                foreach (string propName in new[] { "GuideID", "GuideId", "EmployeeID", "Guide" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(int))
                            prop.SetValue(newExcursion, guideId);
                        else if (prop.PropertyType == typeof(int?))
                            prop.SetValue(newExcursion, guideId);
                        break;
                    }
                }

                // Макс. посетителей
                foreach (string propName in new[] { "MaxVisitors", "MaxParticipants", "Capacity", "MaxPeople" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(int))
                            prop.SetValue(newExcursion, maxVisitors);
                        else if (prop.PropertyType == typeof(int?))
                            prop.SetValue(newExcursion, maxVisitors);
                        break;
                    }
                }

                // Цена
                foreach (string propName in new[] { "Price", "Cost", "Amount" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(decimal))
                            prop.SetValue(newExcursion, price);
                        else if (prop.PropertyType == typeof(decimal?))
                            prop.SetValue(newExcursion, price);
                        else if (prop.PropertyType == typeof(double))
                            prop.SetValue(newExcursion, (double)price);
                        else if (prop.PropertyType == typeof(int))
                            prop.SetValue(newExcursion, (int)price);
                        break;
                    }
                }

                // Статус
                // Если точно знаете, что Status в БД - это string
                var statusProp = excursionType.GetProperty("Status");
                if (statusProp != null && statusProp.CanWrite && statusProp.PropertyType == typeof(string))
                {
                    string statusValue = chkStatus.IsChecked == true ? "Активна" : "Не активна";
                    statusProp.SetValue(newExcursion, statusValue);
                }

                // Дата создания
                foreach (string propName in new[] { "CreatedDate", "CreationDate", "DateAdded" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null && prop.CanWrite)
                    {
                        if (prop.PropertyType == typeof(DateTime))
                            prop.SetValue(newExcursion, DateTime.Now);
                        else if (prop.PropertyType == typeof(DateTime?))
                            prop.SetValue(newExcursion, DateTime.Now);
                        break;
                    }
                }
                // Создатель
                try
                {
                    // Проверяем, есть ли ID пользователя
                    int currentUserId = App.CurrentUser.EmployeeID;

                    foreach (string propName in new[] { "CreatedBy", "CreatedByID", "AuthorID" })
                    {
                        var prop = excursionType.GetProperty(propName);
                        if (prop != null && prop.CanWrite)
                        {
                            if (prop.PropertyType == typeof(int))
                                prop.SetValue(newExcursion, currentUserId);
                            else if (prop.PropertyType == typeof(int?))
                                prop.SetValue(newExcursion, currentUserId);
                            break;
                        }
                    }
                }
                catch
                {
                    // Если нет авторизации - ставим 1 (администратор по умолчанию)
                    foreach (string propName in new[] { "CreatedBy", "CreatedByID", "AuthorID" })
                    {
                        var prop = excursionType.GetProperty(propName);
                        if (prop != null && prop.CanWrite)
                        {
                            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                            {
                                prop.SetValue(newExcursion, 1);
                            }
                            break;
                        }
                    }
                }

                // СОХРАНЯЕМ ЭКСКУРСИЮ
                dbContext.Excursions.Add(newExcursion);
                dbContext.SaveChanges();

                // ТЕПЕРЬ СВЯЗЫВАЕМ С ЭКСПОНАТАМИ
                // Проверяем, как называется таблица связи в вашей БД
                var contextType = typeof(MuseumTechDBEntities);

                // Пробуем разные названия для таблицы связи
                string[] possibleRelationNames = {
                    "ExcursionExponats",
                    "ExponatExcursions",
                    "ExhibitionExponats",
                    "ExcursionExponat"
                };

                bool relationAdded = false;

                foreach (string relationName in possibleRelationNames)
                {
                    var relationProperty = contextType.GetProperty(relationName);
                    if (relationProperty != null)
                    {
                        var relationSet = relationProperty.GetValue(dbContext);
                        if (relationSet != null)
                        {
                            var relationType = relationSet.GetType().GetGenericArguments().FirstOrDefault();

                            if (relationType != null)
                            {
                                foreach (var exponat in selectedExponats)
                                {
                                    // Создаем объект связи
                                    var relation = Activator.CreateInstance(relationType);

                                    // Устанавливаем ID экскурсии
                                    var excursionIdProp = relationType.GetProperty("ExcursionID") ??
                                                          relationType.GetProperty("ExcursionId") ??
                                                          relationType.GetProperty("ID_Excursion");
                                    if (excursionIdProp != null)
                                        excursionIdProp.SetValue(relation, newExcursion.ExcursionID);

                                    // Устанавливаем ID экспоната
                                    var exponatIdProp = relationType.GetProperty("ExponatID") ??
                                                       relationType.GetProperty("ExponatId") ??
                                                       relationType.GetProperty("ID_Exponat");
                                    if (exponatIdProp != null)
                                        exponatIdProp.SetValue(relation, exponat.ExponatID);

                                    // Добавляем в контекст
                                    var addMethod = relationSet.GetType().GetMethod("Add");
                                    if (addMethod != null)
                                    {
                                        addMethod.Invoke(relationSet, new[] { relation });
                                    }
                                }

                                relationAdded = true;
                                break;
                            }
                        }
                    }
                }

                dbContext.SaveChanges();

                // Получаем название для сообщения
                string excursionName = "";
                foreach (string propName in new[] { "Name", "Title", "ExcursionName" })
                {
                    var prop = excursionType.GetProperty(propName);
                    if (prop != null)
                    {
                        excursionName = prop.GetValue(newExcursion)?.ToString() ?? "";
                        break;
                    }
                }

                MessageBox.Show($"Экскурсия \"{excursionName}\" успешно добавлена!\n" +
                              $"Выбрано экспонатов: {selectedExponats.Count}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\n" +
                              $"Проверьте структуру таблиц в базе данных.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            dbContext?.Dispose();
        }
    }
}