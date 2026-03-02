using MuseumSystem.Pages;
using System.Windows;


namespace MuseumSystem
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new DashboardPage());
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DashboardPage());
        }

        private void BtnExponats_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExponatsPage());
        }

        private void BtnExcursions_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ExcursionsPage());
        }

        private void BtnVisitors_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new VisitorsPage());
        }


        private void BtnAddExponat_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddExponatWindow(); // Вызываем конструктор без параметров
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                // Обновляем данные на странице экспонатов, если она открыта
                if (MainFrame.Content is ExponatsPage page)
                {
                    // Предполагаем, что у ExponatsPage есть метод LoadExponats()
                    // Если нет, добавьте его или используйте рефлексию
                    var method = page.GetType().GetMethod("LoadExponats");
                    method?.Invoke(page, null);
                }
            }
        }
        private void BtnAddExcursion_Click(object sender, RoutedEventArgs e)
        {
            AddExcursionWindow addExcursionWindow = new AddExcursionWindow();
            addExcursionWindow.ShowDialog();
        }
    }
}