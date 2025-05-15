using Microsoft.Win32;      //OpenFileDialog sınıfı için gerekli
using System;
using System.Collections.Generic;
using System.Diagnostics;           //Process sınıfı için gerekli
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FFmpegConverterApp
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cbOperation.SelectedIndex = 0;
        }

        //kullanıcıdan bir video dosyası seçmesini iste
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video files |*.mp4;*.avi;*.mov;*.mkv";         //sadecce video dosyalarını filtreler
            if(openFileDialog.ShowDialog() == true)
            {
                txtInputFile.Text = openFileDialog.FileName;            //UI'daki textbox'a bu dosya yolunu yaz.
            }
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
            string selectedOperation = (cbOperation.SelectedItem as ComboBoxItem)?.Content.ToString();
            

            if (selectedOperation == "Yeniden boyutlandır")
            {
                boyut.Visibility = Visibility.Visible;
                boyut.SelectedIndex = 0;
            }
            else
            {

                boyut.Visibility = Visibility.Collapsed;
            }
            if(selectedOperation == "Kırp")
            {
                cropPanel.Visibility = Visibility.Visible;
               
            }
            else
            {
                cropPanel.Visibility= Visibility.Collapsed;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            string inputPath = txtInputFile.Text;
            if (string.IsNullOrEmpty(inputPath))
            {
                MessageBox.Show("Lütfen bir video dosyası seçin !");
                return;
            }

            string outputPath = string.Empty;
            string arguments = string.Empty;

            string selectedOperation = (cbOperation.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (selectedOperation)
            {
                case "MP3'e dönüştür":
                    outputPath = System.IO.Path.ChangeExtension(inputPath, ".mp3");
                    arguments = $"-i \"{inputPath}\" \"{outputPath}\"";
                    break;
                case "Yeniden boyutlandır":

                    string selectedResolution = (boyut.SelectedItem as ComboBoxItem)?.Content.ToString();

                    if (!string.IsNullOrWhiteSpace(selectedResolution))
                    {

                        string[] parts = selectedResolution.Split('x');
                        if (parts.Length == 2)
                        {
                            string width = parts[0].Trim();         //"1280"
                            string height = parts[1].Trim();        //"720"

                            string resizeArgument = $"-vf scale={width}:{height}";

                            string directory = System.IO.Path.GetDirectoryName(inputPath);
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(inputPath) + "_resized.mp4";
                            outputPath = System.IO.Path.Combine(directory, fileName);
                            arguments = $"-i \"{inputPath}\" {resizeArgument}  \"{outputPath}\"";

                        }
                    }

                    break;

                    case "Kırp" :
                        string startTime = txtStartTime.Text.Trim();        //Örn: 00:00:05
                        string endTime = txtEndTime.Text.Trim();           //Örn: 00:00:10

                        if(string.IsNullOrEmpty(startTime) || string.IsNullOrEmpty(endTime))
                    {
                        MessageBox.Show("Başlangıç ve btiş zamanlarını giriniz !");
                        return;
                    }
                    outputPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(inputPath),
                        System.IO.Path.GetFileNameWithoutExtension(inputPath) + "_cropped.mp4");

                    arguments = $"-ss {startTime} -t {endTime} -i \"{inputPath}\" -c copy \"{outputPath}\"";
                    //c:v = video codec belirtir
                    //libx264 = H.264'ün video sıkıştırma standardıdır. Günümüzde en yaygın kullanılan video formatı(youtube ,mp4 vs)
                    //-c:a  = audio:codec (ses kodeği)
                    //aac : Advanced Audio Coding — modern, verimli ve yaygın bir ses formatıdır
                    //arguments = $"-ss {startTime} -t {endTime} -i \"{inputPath}\" -c:v libx264 -c:a aac \"{outputPath}\"";
                    break;

                case "Resim Yakala":
                    string outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPath), "frames");
                    Directory.CreateDirectory(outputDir);

                    outputPath = System.IO.Path.Combine(outputDir, "frame_%03d.jpg");
                    arguments = $"-i \"{inputPath}\" -vf fps=1/5 \"{outputPath}\"";        //her 5 saniyede 1 video

                    break;

                default:
                    MessageBox.Show("Lütfen bir işlem seçin");
                    return;
            }
            txtStatus.Text = "Durum : İşleminiz gerçekleşitirliyor. Lütfen Bekleyiniz...";
            txtStatus.Foreground = Brushes.Orange;


            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true

            };
            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        txtStatus.Text = $"İşlem Tamamlandı :\n{outputPath}";
                        txtStatus.Foreground = Brushes.Green;
                    }
                    else
                    {
                        txtStatus.Text = "Hata Oluştu!";
                        txtStatus.Foreground = Brushes.Red;
                    }
                    Console.WriteLine(output);

                }
            }
            //int timeOutSeconds = 40;        //max süre 30 sn

            //using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeOutSeconds)))
            //{
            //    try
            //    {
            //        await Task.Run(() =>
            //        {
            //            ProcessStartInfo startInfo = new ProcessStartInfo
            //            {
            //                FileName = "ffmpeg",
            //                Arguments = arguments,
            //                UseShellExecute = false,
            //                RedirectStandardOutput = true,
            //                RedirectStandardError = true,
            //                CreateNoWindow = true
            //            };

            //            using (Process process = Process.Start(startInfo))
            //            {
            //                bool exited = process.WaitForExit(timeOutSeconds * 1000); // ms cinsinden
            //                 string output = process.StandarError.ReadToEnd(); 

            //                string errorOutput = process.StandardError.ReadToEndAsync(); // StandardError'dan hata mesajı alalım

            //                Application.Current.Dispatcher.Invoke(() =>
            //                {
            //                    if (exited && process.ExitCode == 0 && string.IsNullOrEmpty(errorOutput))
            //                    {
            //                        txtStatus.Text = $"İşlem Tamamlandı :\n{outputPath}";
            //                        txtStatus.Foreground = Brushes.Green;
            //                    }
            //                    else if (!exited)
            //                    {
            //                        try
            //                        {
            //                            process.Kill(); // Süre dolduysa işlemi sonlandır
            //                        }
            //                        catch { }

            //                        txtStatus.Text = "Zaman aşımı: İşlem belirtilen sürede tamamlanamadı.";
            //                        txtStatus.Foreground = Brushes.Blue;
            //                    }
            //                    else
            //                    {
            //                        txtStatus.Text = "Hata Oluştu!";
            //                        txtStatus.Foreground = Brushes.Red;
            //                    }
            //                });
            //            }
            //        }, cts.Token);
            //    }
            //    catch (TaskCanceledException)
            //    {
            //        txtStatus.Text = "İşlem zaman aşımına uğradı (kullanıcı iptal etti veya süre doldu).";
            //        txtStatus.Foreground = Brushes.Red;
            //    }
            catch (Exception ex)
            {
                txtStatus.Text = $"Beklenmeyen bir hata oluştu: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
            }
        }


    
            
    }
}
