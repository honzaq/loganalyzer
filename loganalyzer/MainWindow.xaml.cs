using Serilog;
using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Globalization;

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
            var measure = Stopwatch.StartNew();
            //var res = await Task.Run(() => Read());
            var result = await ReadAsync();
            measure.Stop();
            Log.Information("Load log file takes.. result:{0} takes {1}ms", result, measure.ElapsedMilliseconds);
        }

        private int ToIntFast(in char[] str, int from, int len)
        {
            int n = 0;
            while(len-- > 0) {
                if(str[from] == ' ') {
                    ++from;
                    continue;
                }
                n = n* 10 + str[from++] - '0';
            }
            return n;
        }

        enum Levels : byte
        {
            Debug = 0,
            Block,
            Info,
            Function,
            Notice,
            Warning,
            Thread,
            Error,
            Fatal
        }

        private async Task<bool> ReadAsync()
        {
            bool result = false;
            await Task.Run(() =>
            {
                bool isAswLogFile = false;
                const int bufferSize = 65536;
                using (FileStream fs = File.Open(@"c:\Download\icarus.log", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int startPos = 0;
                    char[] buffer = new char[bufferSize];
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8, true, bufferSize))
                    {
                        int readSize = 0;
                        while ((readSize = sr.ReadBlock(buffer, 0, bufferSize)) != 0)
                        {
                            Log.Information($"Readed bytes {readSize} first character 0:{buffer[0]} 24:{buffer[24]} 26:{buffer[26]} 34:{buffer[34]} 36:{buffer[36]} 48:{buffer[48]} 50:{buffer[50]} 62:{buffer[62]} 60-65:{buffer[60]}{buffer[61]}{buffer[62]}{buffer[63]}{buffer[64]}{buffer[65]}");
                            startPos += bufferSize;

                            // Log line description
                            //  sample: [2018-05-02 10:06:47.900] [error  ] [modul1     ] [  308: 4616] Somewhere: File could not be opened. Code 0.
                            //          [date max 23chars       ]
                            //                                    [severity max 7 chars]
                            //                                              [logger max 11chars]
                            //                                                            [PID:THID max 5chars each]
                            //                                                                          Log text

                            // One time check if file is AswLog
                            if(!isAswLogFile)
                            {
                                if (readSize < 63) { result = false; return; }
                                if (buffer[0] != '[' || buffer[24] != ']' || buffer[26] != '[' || buffer[34] != ']' || buffer[36] != '[' || buffer[48] != ']' || buffer[50] != '[' || buffer[62] != ']')
                                {
                                    result = false;
                                    return;
                                }
                                isAswLogFile = true;
                            }

                            ////////////////////////////////////////////////////////////////////////////////////////////////////
                            /// Parse line
                            ////////////////////////////////////////////////////////////////////////////////////////////////////
                            // Date + Time

                            var measure = Stopwatch.StartNew();
                            DateTime dateTime = new DateTime(
                                ToIntFast(buffer, 1, 4), 
                                ToIntFast(buffer, 6, 2), 
                                ToIntFast(buffer, 9, 2),
                                ToIntFast(buffer, 12, 2),
                                ToIntFast(buffer, 15, 2),
                                ToIntFast(buffer, 18, 2),
                                ToIntFast(buffer, 21, 3));
                            measure.Stop();
                            Log.Information("Parse time takes.. result:{0} takes {1}tiks", result, measure.ElapsedMilliseconds);
                            Log.Information("DT:{0}", dateTime);

                            // Level
                            Levels level;
                            switch(buffer[27])
                            {
                                case 'd':
                                    level = Levels.Debug;
                                    break;
                                case 'b':
                                    level = Levels.Block;
                                    break;
                                case 'i':
                                    level = Levels.Info;
                                    break;
                                case 'f':
                                    level = buffer[28] == 'n' ? Levels.Function : Levels.Fatal;
                                    break;
                                case 'n':
                                    level = Levels.Notice;
                                    break;
                                case 'w':
                                    level = Levels.Warning;
                                    break;
                                case 'e':
                                    level = Levels.Error;
                                    break;
                                default:
                                    // throw? return error?
                                    return;
                            }
                            Log.Information("Level:{0}", level);

                            // Component
                            string component = new string(buffer, 37, 11).TrimEnd();
                            Log.Information("Component: '{0}'", component);

                            // PID
                            int PID = ToIntFast(buffer, 51, 5);
                            Log.Information("PID: {0}", PID);

                            // THID
                            int THID = ToIntFast(buffer, 57, 5);
                            Log.Information("THID: {0}", THID);

                            // MSG
                            string msg = new string(buffer, 64, 60);
                            Log.Information("Msg: '{0}'", msg);
                            return;
                            /////////////////////////////////////////////////////////////////////////////////////////////////
                            /// Parse line end
                            /////////////////////////////////////////////////////////////////////////////////////////////////
                        }
                    }
                }

                //byte[] buffer = new byte[bufferSize];
                //using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(@"c:\Download\icarus.log", FileMode.Open))
                //{
                //    using (var accessor = mmf.CreateViewStream())
                //    {
                //        int readSize = 0;
                //        while ((readSize = accessor.Read(buffer, 0, bufferSize)) != 0)
                //        {
                //            Log.Information("Readed bytes {0}", readSize);
                //        }
                //    }
                //}

                result = true;
            });
            return result;
        }
    }
}
