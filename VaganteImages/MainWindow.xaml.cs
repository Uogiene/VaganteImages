using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using VaganteImages.Properties;

namespace VaganteImages
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler eventHandler = PropertyChanged;
            if (eventHandler != null)
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
        }

        const string defaultValue = "Choose a directory.";
        string path = defaultValue;
        public string Path {
            get { return path; }
            set {
                path = value;
                OnPropertyChanged("Path");
            }
        }

        const string folder = "vagante_images\\";
        List<byte> listOfBytes = new List<byte>();

        public MainWindow() {
            InitializeComponent();
            this.DataContext = this;
        }

        private void browse_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            if (fbd.SelectedPath != "") {
                Path = fbd.SelectedPath + "\\";
                Settings.Default.Directory = Path;
                Settings.Default.Save();
            }

            EnableButtons();
        }

        private void extract_Click(object sender, RoutedEventArgs e) {
            string mainFile = Path + "data.vra";

            bool boo1 = File.Exists(mainFile);
            bool boo2 = File.Exists(mainFile + ".bak");

            if (!boo1 && !boo2) {
                System.Windows.MessageBox.Show("Data file not found", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                Directory.CreateDirectory(folder);
                if (boo2) {
                    mainFile += ".bak";
                }

                FileStream fs = new FileStream(mainFile, FileMode.Open);
                int length = (int)fs.Length;
                byte[] arrayOfBytes = new byte[length];
                fs.Read(arrayOfBytes, 0, (int)length);
                fs.Close();
                listOfBytes = new List<byte>(arrayOfBytes);

                List<byte> name = new List<byte>();
                List<byte> hex = new List<byte>();
                List<byte> str4 = new List<byte>();
                bool start = false;

                for (int i = 0; i < length; i++) {
                    byte oneByte = arrayOfBytes[i];
                    str4.Add(oneByte);
                    if (str4.Count > 4) {
                        str4.RemoveAt(0);

                        if (str4[0] == 103 && str4[1] == 102 && str4[2] == 120 && str4[3] == 47 && !start) {
                            name = new List<byte>();
                        } else if (str4[0] == 137 && str4[1] == 80 && str4[2] == 78 && str4[3] == 71) {
                            hex = new List<byte>();
                            hex.Add(137);
                            hex.Add(80);
                            hex.Add(78);
                            start = true;
                        } else if (str4[0] == 174 && str4[1] == 66 && str4[2] == 96 && str4[3] == 130) {
                            hex.Add(130);
                            SaveToImage(hex, name);
                            start = false;
                        }
                    }

                    hex.Add(oneByte);
                    name.Add(oneByte);
                }

                EnableButtons();
            }
        }

        private void remake_Click(object sender, RoutedEventArgs e) {
            string mainFile = Path + "data.vra";
            bool boo1 = File.Exists(mainFile);
            bool boo2 = File.Exists(mainFile + ".bak");

            if (!boo1 && !boo2) {
                System.Windows.MessageBox.Show("Data file not found", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                Directory.CreateDirectory(folder);
                if (!boo2) 
                    File.Copy(mainFile, mainFile + ".bak");

                if (boo1)
                    File.Delete(mainFile);
                FileStream fs = new FileStream(mainFile, FileMode.Create);

                mainFile += ".bak";
                byte[] arrayOfBytes = File.ReadAllBytes(mainFile);

                List<byte> name = new List<byte>();
                List<byte> hex = new List<byte>();
                List<byte> str4 = new List<byte>();
                bool start = false;

                for (int i = 0; i < arrayOfBytes.Length; i++) {
                    byte oneByte = arrayOfBytes[i];
                    str4.Add(oneByte);
                    if (str4.Count > 4) {
                        str4.RemoveAt(0);

                        if (str4[0] == 103 && str4[1] == 102 && str4[2] == 120 && str4[3] == 47 && !start) {
                            name = new List<byte>();
                        } else if (str4[0] == 137 && str4[1] == 80 && str4[2] == 78 && str4[3] == 71) {
                            hex = new List<byte>();
                            hex.Add(137);
                            hex.Add(80);
                            hex.Add(78);
                            start = true;
                        } else if (str4[0] == 174 && str4[1] == 66 && str4[2] == 96 && str4[3] == 130) {
                            hex.Add(130);

                            string whole = System.Text.Encoding.UTF8.GetString(name.ToArray());
                            whole = whole.Substring(0, whole.IndexOf(".png") + 4); //strip excess
                            byte[] arr = File.ReadAllBytes(folder + whole);

                            fs.Position = fs.Position - 7; //ReAdjust
                            byte[] length = BitConverter.GetBytes(arr.Length);
                            fs.Write(length, 0, length.Length);
                            fs.Write(arr, 0, arr.Length);
                            fs.Position = fs.Position - 1;

                            start = false;
                        }
                    }
                    if (!start) {
                        fs.WriteByte(oneByte);
                    }

                    hex.Add(oneByte);
                    name.Add(oneByte);
                }
                fs.Close();

                EnableButtons();
            }
        }


        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (Settings.Default.Directory != "")
                Path = Settings.Default.Directory;

            EnableButtons();
        }

        private void EnableButtons() {
            if (Path != "" && Path != defaultValue)
                extract.IsEnabled = true;

            if (Directory.Exists(folder))
                remake.IsEnabled = true;
        }

        private static void SaveToImage(List<byte> hex, List<byte> fileName) {
            string whole = System.Text.Encoding.UTF8.GetString(fileName.ToArray());
            whole = whole.Substring(0, whole.IndexOf(".png") + 4); //strip excess
            int index = whole.LastIndexOf("/");
            if (index > 0) {
                Directory.CreateDirectory(folder + whole.Substring(0, index));
            }
            File.WriteAllBytes(folder + whole, hex.ToArray());
        }
    }
}
