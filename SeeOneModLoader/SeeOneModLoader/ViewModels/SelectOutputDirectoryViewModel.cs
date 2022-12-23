using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using SeeOneModLoader.Patch;
using SeeOneModLoader.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace SeeOneModLoader.ViewModels
{
    public class SelectOutputDirectoryViewModel : ViewModelBase
    {
        public string Title => "Select where to place the patched FortressCraft files";
        public string Browse => "Browse...";

        public ReactiveCommand<Unit, Unit> DoBrowseCommand { get; }

        public SelectOutputDirectoryViewModel()
        {
            DoBrowseCommand = ReactiveCommand.Create(BrowseCommand);

            if (outputDirectory == "")
            {
                Error = true;
                outputDirectory = "Please select an output directory.";
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
                    OutputDirectory = item.TryGetUri(out var uri) ? uri.LocalPath : item.Name;
                    Error = false;
                }
            }
        }

        private string outputDirectory = "";
        public string OutputDirectory
        {
            get => outputDirectory;
            set
            {
                this.RaiseAndSetIfChanged(ref outputDirectory, value);
            }
        }

        private bool error = false;
        public bool Error
        {
            get => error;
            set
            {
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

        private string pathColour = "";
        public string PathColour
        {
            get => pathColour;
            set => this.RaiseAndSetIfChanged(ref pathColour, value);
        }
    }
}