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

        public SelectGameDirectoryViewModel GameDirectoryViewModel;
        public SelectPatchesViewModel PatchesViewModel;
        public SelectModsViewModel ModsViewModel;
        public SelectOutputDirectoryViewModel OutputDirectoryViewModel;

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

            this.GameDirectoryViewModel = new SelectGameDirectoryViewModel();
            this.PatchesViewModel = new SelectPatchesViewModel();
            this.ModsViewModel = new SelectModsViewModel();
            this.OutputDirectoryViewModel = new SelectOutputDirectoryViewModel();

            this.DataContext = this.GameDirectoryViewModel;
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
                this.DataContext = this.GameDirectoryViewModel;
            }
            else if (tabView is SelectOutputDirectoryView)
            {
                this._tabs.SelectedIndex = --_currentIndex;
                this.DataContext = this.PatchesViewModel;
            }
            else if (tabView is SelectModsView)
            {
                this._tabs.SelectedIndex = --_currentIndex;
                this.DataContext = this.OutputDirectoryViewModel;
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
                    this.DataContext = this.PatchesViewModel;
                    this.Get<Button>("Previous").IsEnabled = true;
                }
            }
            else if (tabView is SelectPatchesView)
            {
                this._tabs.SelectedIndex = ++_currentIndex;
                this.DataContext = this.OutputDirectoryViewModel;
            }
            else if (tabView is SelectOutputDirectoryView)
            {
                SelectOutputDirectoryView view = (SelectOutputDirectoryView)tabView;
                if (view.CanProceed())
                {
                    this._tabs.SelectedIndex = ++_currentIndex;
                    this.DataContext = this.ModsViewModel;
                    this.Get<Button>("Next").Content = "Play";
                }
            }
            else if (tabView is SelectModsView)
            {
                System.Console.WriteLine("Launching game");
                LogWindow logWindow = new LogWindow();
                logWindow.MainWindow = this;
                logWindow.Show();
                this.Hide();
            }
        }
    }
}