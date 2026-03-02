using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MuseumSystem
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static class CurrentUser
        {
            public static int EmployeeID { get; set; }
            public static string FullName { get; set; }
            public static string Position { get; set; }
        }

        public static class AppFrame
        {
            public static Frame FrameMain { get; set; }
        }
    }
}
