using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using SeeOneModLoader.Patch;
using SeeOneModLoader.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SeeOneModLoader.Views
{
    public partial class LogWindow : Window
    {
        public MainWindow? MainWindow;

        private List<string> _logs;
        private string _stage;
        private int _progress;
        private int _progressMax;
        private bool _close;

        private Mutex mut = new Mutex();

        public LogWindow()
        {
            InitializeComponent();

            this._logs = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                this._logs.Append("     ");
            }
            this._stage = "Initialised window";
            this._progress = 0;
            this._progressMax = 0;
            this._close = false;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (sender, e) =>
            {
                if (this._close)
                {
                    this.Close();
                    if (this.MainWindow != null)
                    {
                        this.MainWindow.Close();
                    }

                    return;
                }
                mut.WaitOne();

                var stage = this.Get<TextBlock>("Stage");
                stage.Text = _stage;

                var log = this.Get<TextBox>("Log");

                if (log.Text == null)
                {
                    log.Text = "";
                }

                foreach (string logLine in this._logs)
                {
                    log.Text += logLine + System.Environment.NewLine;
                }

                string[] splitLog = log.Text.Split(System.Environment.NewLine);
                if (splitLog.Length > 20)
                {
                    log.Text = string.Join(System.Environment.NewLine, splitLog.TakeLast(20));
                }

                this._logs.Clear();

                var progress = this.Get<ProgressBar>("Progress");
                if (this._progress == -1 || this._progressMax == -1)
                {
                    progress.IsVisible = false;
                }
                else
                {
                    progress.Value = this._progress;
                    progress.Maximum = this._progressMax;
                }

                mut.ReleaseMutex();
            };

            timer.Start();

            Thread t = new Thread(new ThreadStart(StartPatcher));
            t.Start();
        }

        public void StartPatcher()
        {
            if (this.MainWindow == null)
            {
                throw new Exception("MainWindow not found");
            }

            Patcher patcher = new Patcher(Engine.FNA, this.MainWindow.GameDirectoryViewModel.GameDirectory, this.MainWindow.OutputDirectoryViewModel.OutputDirectory);

            patcher.Log += Patcher_Log;
            patcher.Progress += Patcher_Progress;
            patcher.Stage += Patcher_Stage;

            patcher.PatchWithRuntimes();

            List<string> enabledPatches = new List<string>();

            foreach (SelectPatchesViewModel.Patch patch in this.MainWindow.PatchesViewModel.Items)
            {
                if (patch.IsChecked)
                {
                    enabledPatches.Add(patch.Name);
                }
            }

            var assembly = patcher.Patch(enabledPatches);
            string patchedBinaryPath = patcher.Run(assembly);
            StartBinary(patchedBinaryPath);
        }

        public void StartBinary(string path)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(path)
                }
            };

            proc.OutputDataReceived += Proc_OutputDataReceived;
            proc.ErrorDataReceived += Proc_ErrorDataReceived;


            _stage = "Running";
            _progress = -1;
            _progressMax = -1;

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit(); //you need this in order to flush the output buffer
            this._close = true;
        }

        private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            mut.WaitOne();
            this._logs.Add(e.Data);
            mut.ReleaseMutex();
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            mut.WaitOne();
            this._logs.Add(e.Data);
            mut.ReleaseMutex();
        }

        private void Patcher_Stage(object? sender, Patcher.PatcherStageEventArgs e)
        {
            mut.WaitOne();
            this._stage = e.Message;
            mut.ReleaseMutex();
        }

        private void Patcher_Progress(object? sender, Patcher.PatcherProgressEventArgs e)
        {
            mut.WaitOne();
            this._progress = e.Progress;
            this._progressMax = e.ProgressMax;
            mut.ReleaseMutex();
        }

        private void Patcher_Log(object? sender, Patcher.PatcherLogEventArgs e)
        {
            mut.WaitOne();
            this._logs.Add(e.Message);
            mut.ReleaseMutex();
        }
    }
}
