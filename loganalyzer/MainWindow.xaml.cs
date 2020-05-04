using Serilog;
using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace loganalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            CreateAppDataPath();

            // Create logger
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(Global.Vars.DataPath, @"logs\log-.txt"), rollingInterval: RollingInterval.Month)
                .WriteTo.Debug()
                .CreateLogger();

            InitializeComponent();

            Log.Information("LogAnalyzer starting... PID:{0}", Process.GetCurrentProcess().Id);

            ReadLogFile();

            Log.Information("Main DONE.");
        }

        private void CreateAppDataPath()
        {
            // Create Application Data folder
            var UserPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Global.Vars.DataPath = Path.Combine(UserPath, @"Noen\LogAnalyzer");

            // Ensure data folder exist folders
            try
            {
                if (!Directory.Exists(Global.Vars.DataPath))
                {
                    Directory.CreateDirectory(Global.Vars.DataPath);
                }
            }
            catch
            {
                MessageBox.Show(string.Format("Cound not create application data folder {0}", Global.Vars.DataPath), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private async void ReadLogFile()
        {
            var measure = System.Diagnostics.Stopwatch.StartNew();
            //var res = await Task.Run(() => Read());
            var result = await ReadAsync();
            measure.Stop();
            Log.Information("Load log file takes.. result:{0} takes {1}ms", result, measure.ElapsedMilliseconds);
        } 

        private async Task<bool> ReadAsync()
        {
            bool result = false;
            await Task.Run(() =>
            {
                const int bufferSize = 65536;
                using (FileStream fs = File.Open(@"c:\Download\icarus.log", FileMode.Open))
                {
                    int startPos = 0;
                    char[] buffer = new char[bufferSize];
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, bufferSize))
                    {
                        int readSize = 0;
                        while ((readSize = sr.ReadBlock(buffer, 0, bufferSize)) != 0)
                        {
                            Log.Information("Readed bytes {0}", readSize);
                            startPos += bufferSize;
                        }
                    }
                }

                result = true;
            });
            return result;
        }
    }
}
