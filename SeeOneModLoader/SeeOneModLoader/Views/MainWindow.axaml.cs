using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SeeOneModLoader.Patch;
using SeeOneModLoader.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Threading.Tasks;

namespace SeeOneModLoader.Views
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance;
        private int _currentIndex;
        private TabControl _tabs;

        private SelectGameDirectoryViewModel gameDirectoryViewModel;
        private SelectPatchesViewModel patchesViewModel;
        private SelectModsViewModel modsViewModel;
        private SelectOutputDirectoryViewModel outputDirectoryViewModel;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            _currentIndex = 0;

            this._tabs = this.Get<TabControl>("LoaderTabs");
            this._tabs.SelectionChanged += _tabs_SelectionChanged;

            var nextButton = this.Get<Button>("Next");
            nextButton.Click += NextButton_Click;

            var previousButton = this.Get<Button>("Previous");
            previousButton.Click += PreviousButton_Click;

            this.gameDirectoryViewModel = new SelectGameDirectoryViewModel();
            this.patchesViewModel = new SelectPatchesViewModel();
            this.modsViewModel = new SelectModsViewModel();
            this.outputDirectoryViewModel = new SelectOutputDirectoryViewModel();

            this.DataContext = this.gameDirectoryViewModel;
        }

        private void _tabs_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            this._tabs.SelectedIndex = _currentIndex;
        }

        private void PreviousButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            TabItem tabViewItem = (TabItem)this._tabs.GetLogicalChildren().ElementAt(this._tabs.SelectedIndex);
            var tabView = tabViewItem.Content;

            if (tabView is SelectPatchesView)
            {
                this.Get<Button>("Previous").IsEnabled = false;
                this._tabs.SelectedIndex = --_currentIndex;
                this.DataContext = this.gameDirectoryViewModel;
            }
            else if (tabView is SelectOutputDirectoryView)
            {
                this._tabs.SelectedIndex = --_currentIndex;
                this.DataContext = this.patchesViewModel;
            }
            else if (tabView is SelectModsView)
            {
                this._tabs.SelectedIndex = --_currentIndex;
                this.DataContext = this.outputDirectoryViewModel;
                this.Get<Button>("Next").Content = "Next";
            }
        }

        private void NextButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            TabItem tabViewItem = (TabItem)this._tabs.GetLogicalChildren().ElementAt(this._tabs.SelectedIndex);
            var tabView = tabViewItem.Content;

            if (tabView is SelectGameDirectoryView)
            {
                SelectGameDirectoryView view = (SelectGameDirectoryView) tabView;
                if (view.CanProceed())
                {
                    this._tabs.SelectedIndex = ++_currentIndex;
                    this.DataContext = this.patchesViewModel;
                    this.Get<Button>("Previous").IsEnabled = true;
                }
            }
            else if (tabView is SelectPatchesView)
            {
                this._tabs.SelectedIndex = ++_currentIndex;
                this.DataContext = this.outputDirectoryViewModel;
            }
            else if (tabView is SelectOutputDirectoryView)
            {
                SelectOutputDirectoryView view = (SelectOutputDirectoryView)tabView;
                if (view.CanProceed())
                {
                    this._tabs.SelectedIndex = ++_currentIndex;
                    this.DataContext = this.modsViewModel;
                    this.Get<Button>("Next").Content = "Play";
                }
            }
            else if (tabView is SelectModsView)
            {
                System.Console.WriteLine("Launching game");
                Patcher patcher = new Patcher(this.gameDirectoryViewModel.GameDirectory, this.outputDirectoryViewModel.OutputDirectory);

                List<string> enabledPatches = new List<string>();

                foreach(SelectPatchesViewModel.Patch patch in this.patchesViewModel.Items)
                {
                    if (patch.IsChecked)
                    {
                        enabledPatches.Add(patch.Name);
                    }
                }

                var assembly = patcher.Patch(enabledPatches);
                patcher.Run(assembly);
            }
        }
    }
}