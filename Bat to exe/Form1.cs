using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bat_to_exe
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0) return;
            Clipboard.SetText(textBox1.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text = Clipboard.GetText();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Batch files (*.bat)|*.bat",
                Title = "選擇一個批處理文件"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedFile = openFileDialog.FileName;
                textBox1.Text = File.ReadAllText(selectedFile);
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            button5.Enabled = false;
            textBox2.Enabled = false;
            progressBar1.Value = 0;

            string pattern = @"set\s*/p\s*[^=]*=[^=]*";

            string CScode = $@"
using System;
using System.Diagnostics;
using System.IO;

class Program
{{
    static void Main(string[] args)
    {{
        string randomFileName = Path.GetRandomFileName().Replace("". "", """");
        string tempFilePath = Path.Combine(Path.GetTempPath(), randomFileName + "".bat"");

        string commandContent = @""{Regex.Replace(textBox1.Text, pattern, "wait_user_input").Replace("\"", "\"\"").Replace("{{", "{{").Replace("}}", "}}").Replace("\n", "\r\n")}"";  

        File.WriteAllText(tempFilePath, commandContent);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {{
            FileName = ""cmd.exe"",
            Arguments = ""/C "" + tempFilePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }};

        Process process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {{
            if (!string.IsNullOrEmpty(e.Data))
            {{
                Console.WriteLine(e.Data);
            }}
        }});
        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {{
            if (!string.IsNullOrEmpty(e.Data))
            {{
                Console.WriteLine(e.Data);
            }}
        }});

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (File.Exists(tempFilePath))
        {{
            File.Delete(tempFilePath);
        }}
    }}
}}";
            textBox3.Text = CScode;

            string randomFileName = Path.GetRandomFileName().Replace(".", "");
            string tempDirPath = Path.Combine(Path.GetTempPath(), randomFileName);
            string tempCsFilePath = Path.Combine(tempDirPath, "Program.cs");
            string tempExeFilePath = Path.Combine(tempDirPath, "Program.exe");

            Directory.CreateDirectory(tempDirPath);

            using (FileStream fs = new FileStream(tempCsFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                writer.Write(CScode);
            }

            progressBar1.Value = 30;

            List<(byte[] resourceData, string outputFileName)> resources = new List<(byte[], string)>
            {
                (Properties.Resources.alinkui, "alinkui.dll"),
                (Properties.Resources.csc, "csc.exe"),
                (Properties.Resources.cscui, "cscui.dll"),
                (Properties.Resources.CvtResUI, "CvtResUI.dll"),
                (Properties.Resources.FileTrackerUI, "FileTrackerUI.dll"),
                (Properties.Resources.Microsoft_VisualBasic_Activities_CompilerUI, "Microsoft.VisualBasic.Activities.CompilerUI.dll"),
                (Properties.Resources.System_Diagnostics_Process, "System.Diagnostics.Process.dll"),
                (Properties.Resources.System, "System.dll"),
                (Properties.Resources.System_IO, "System.IO.dll"),
                (Properties.Resources.vbc7ui, "vbc7ui.dll")
            };

            string outputDirectory = Path.Combine(Path.GetTempPath(), "CSC");
            Directory.CreateDirectory(outputDirectory);

            foreach (var (resourceData, outputFileName) in resources)
            {
                if (resourceData != null)
                {
                    string outputPath = Path.Combine(outputDirectory, outputFileName);
                    File.WriteAllBytes(outputPath, resourceData);
                }
                else
                {
                    MessageBox.Show($"Resource not found for output file: {outputFileName}");
                }
            }

            ProcessStartInfo compileStartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(outputDirectory, "csc.exe"),
                Arguments = $"/reference:System.dll /reference:System.Diagnostics.Process.dll /reference:System.IO.dll \"{tempCsFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempDirPath
            };

            try
            {
                using (Process process = Process.Start(compileStartInfo))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            progressBar1.Value = 80;

            if (textBox2.Text.Length == 0)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string exePath = Path.GetDirectoryName(assembly.Location);
                textBox2.Text = Path.Combine(exePath, "Program.exe");
            }

            await CopyFileAsync(tempExeFilePath, Path.GetFullPath(textBox2.Text));
            Directory.Delete(tempDirPath, recursive: true);
            progressBar1.Value = 100;
            button5.Enabled = true;
            textBox2.Enabled = true;
        }

        private async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize: 4096, useAsync: true))
            using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }
}
