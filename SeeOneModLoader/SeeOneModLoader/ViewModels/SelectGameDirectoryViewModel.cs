using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SeeOneModLoader.Views;
using System;
using System.Linq;
using System.Reactive;

namespace SeeOneModLoader.ViewModels
{
    public class SelectGameDirectoryViewModel : ViewModelBase
    {
        public string Title => "Select the directory of the FortressCraft: Chapter 1 files";
        public string UseSteamDir => "Detect Steam directory";
        public string Browse => "Browse...";
        public string PathLabel => "Path: ";
        public string TryResolveStr => "Try To Resolve Again";

        private SteamLibraryResolver? SteamResolver = null;

        public ReactiveCommand<Unit, Unit> DoCheckUseSteamCommand { get; }
        public ReactiveCommand<Unit, Unit> DoBrowseCommand { get; }
        public ReactiveCommand<Unit, Unit> DoTryResolve { get; }

        public SelectGameDirectoryViewModel()
        {
            DoCheckUseSteamCommand = ReactiveCommand.Create(CheckUseSteam);
            DoBrowseCommand = ReactiveCommand.Create(BrowseCommand);
            DoTryResolve = ReactiveCommand.Create(TryResolve);
            if (!CustomDirectoryEnabled)
            {
                TryResolve();
            }
        }

        void TryResolve()
        {
            SteamResolver = new SteamLibraryResolver();
            string? steamFcPath = SteamResolver.Resolve();
            if (steamFcPath == null)
            {
                Error = true;
                GameDirectory = "Could not resolve path, please make sure that Steam is running";
            }
            else
            {
                Error = false;
                ErrorAndSteam = false;
                GameDirectory = steamFcPath;
                //gameDirectory = steamFcPath;
            }
        }

        void CheckUseSteam()
        {
            CustomDirectoryEnabled = !CustomDirectoryEnabled;
            if (CustomDirectoryEnabled)
            {
                Error = true;
                GameDirectory = "Please browse to a valid FortressCraft path";
            }
            else
            {
                TryResolve();
            }
        }

        async void BrowseCommand()
        {
            var folders = await MainWindow.Instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                Title = "Select FortressCraft Folder",
                AllowMultiple = false
            });

            if (folders != null && folders.Count > 0)
            {
                IStorageItem? item = folders[0];
                if (item != null)
                {
                    GameDirectory = item.TryGetUri(out var uri) ? uri.LocalPath : item.Name;
                    Error = false;
                }
            }
        }

        private bool customDirectoryEnabled = false;
        public bool CustomDirectoryEnabled
        {
            get => customDirectoryEnabled;
            set => this.RaiseAndSetIfChanged(ref customDirectoryEnabled, value);
        }

        private string gameDirectory = "";
        public string GameDirectory
        {
            get => gameDirectory;
            set {
                this.RaiseAndSetIfChanged(ref gameDirectory, value);
            }
        }

        private bool error = false;
        public bool Error
        {
            get => error;
            set {
                this.RaiseAndSetIfChanged(ref error, value);
                if (value)
                {
                    PathColour = "red";
                }
                else
                {
                    PathColour = "limegreen";
                }
            }
        }

        private bool errorAndSteam;
        public bool ErrorAndSteam
        {
            get
            {
                errorAndSteam = error && !customDirectoryEnabled;
                return errorAndSteam;
            }
            set => this.RaiseAndSetIfChanged(ref errorAndSteam, value);
        }

        private string pathColour = "";
        public string PathColour
        {
            get => pathColour;
            set => this.RaiseAndSetIfChanged(ref pathColour, value);
        }
    }
}