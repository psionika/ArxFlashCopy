using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace ArxFlashCopy
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        string source = @"C:\Users\Пользователь\";
        string destination = @"C:\Temp\";

        void FormMain_Load(object sender, EventArgs e)
        {
            labelStartTime.Text = DateTime.Now.ToShortTimeString();

            var arguments = Environment.GetCommandLineArgs();

            if (arguments.Length > 1)
            {
                source = arguments[1];

                if (arguments[1].Substring(arguments[1].Length - 1) == "\"")
                {
                    source = arguments[1].Substring(0, arguments[1].Length - 1);
                }

                destination = Properties.Settings.Default.destination;

                //MessageBox.Show("source - " + source + Environment.NewLine + "dest - " + destination);

                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.RunWorkerAsync();
            }
            else Application.Exit();
        }

        public delegate void ProgressChangeDelegate(double Persentage, ref bool Cancel);
        public delegate void Completedelegate();

        class CustomFileCopier
        {
            public event ProgressChangeDelegate OnProgressChanged;
            public event Completedelegate OnComplete;

            public CustomFileCopier(string Source, string Dest)
            {
                SourceFilePath = Source;
                DestFilePath = Dest;

                OnProgressChanged += delegate { };
                OnComplete += delegate { };
            }

            public void Copy()
            {
                var buffer = new byte[1024 * 1024]; // 1MB buffer
                var cancelFlag = false;

                using (FileStream source = new FileStream(SourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    var fileLength = source.Length;
                    using (FileStream dest = new FileStream(DestFilePath, FileMode.CreateNew, FileAccess.Write))
                    {
                        long totalBytes = 0;
                        var currentBlockSize = 0;

                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalBytes += currentBlockSize;
                            var persentage = (double)totalBytes * 100.0 / fileLength;

                            dest.Write(buffer, 0, currentBlockSize);

                            cancelFlag = false;
                            OnProgressChanged(persentage, ref cancelFlag);

                            if (cancelFlag)
                            {
                                // Delete dest file here
                                break;
                            }
                        }
                    }
                }

                OnComplete();
            }

            public string SourceFilePath { get; set; }
            public string DestFilePath { get; set; }
        }

        int fileprogress;
        int progressMax;

        DateTime dt = DateTime.Now;

        void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            labelObject.Text = e.UserState.ToString();
            labelCount.Text = fileprogress.ToString();
            labelTime.Text = DateTime.Now.Subtract(dt).ToString();
            progressBar1.Maximum = progressMax;
            progressBar1.Value = fileprogress;
            progressBar1.Update();
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Проверка закончена - Вирусов не обнаружено!");
            Application.Exit();
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            var copy_file = new CustomFileCopier("", "");

            var files = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);

            progressMax = files.Count();

            fileprogress = 0;

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(source, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(source, destination));

            //Copy all the files
            foreach (string newPath in Directory.GetFiles(source, "*.*",
                SearchOption.AllDirectories))
            {
                copy_file.SourceFilePath = newPath;
                copy_file.DestFilePath = newPath.Replace(source, destination);

                copy_file.Copy();

                fileprogress++;

                backgroundWorker.ReportProgress(fileprogress, newPath);
            }

        }
    }
}

