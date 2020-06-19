﻿using Microsoft.Win32;
using SWISDR.Core;
using SWISDR.Core.ApplicationState;
using SWISDR.Core.Entries;
using SWISDR.Core.Timetable;
using SWISDR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

namespace SWISDR.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Func<string, Task<Timetable>> _loadTimetable;

        private readonly Func<TrainNumber> _windowFactory;
        private readonly IApplicationStateService _appStateService;
        private Timetable _timetable;
        public ObservableCollection<EntryViewModel> Entries { get; set; }

        public MainWindow(
            Func<string, Task<Timetable>> loadTimetableFunc,
            Func<TrainNumber> windowFactory,
            IApplicationStateService appStateService)
        {
            Entries = new ObservableCollection<EntryViewModel>();
            InitializeComponent();
            _loadTimetable = loadTimetableFunc;
            _windowFactory = windowFactory;
            _appStateService = appStateService;
        }
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTimetable();
        }

        private void AddTrainBtn_Click(object sender, RoutedEventArgs e)
        {
            AddTrain();
        }

        private void EntriesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //e.Row.Item
            //var element = e.EditingElement;
        }

        private async void LoadStateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appState = await _appStateService.Load(this);
                if (appState == null)
                    return;

                Entries.Clear();

                foreach (var entry in appState.Entries)
                    Entries.Add(new EntryViewModel(_timetable[entry.Number], entry));
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async void SaveStateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var appState = new ApplicationState(Entries.Select(vm => vm.Model).ToList());
                await _appStateService.Save(appState, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                AddTrain();
                e.Handled = true;
            }
        }

        private void AddEntryFor(int number)
        {
            try
            {
                var entry = new Entry(_timetable[number]);
                Entries.Add(new EntryViewModel(_timetable[number], entry));
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show($"Nie znaleziono pociągu numer {number}");
            }
        }

        private async Task LoadTimetable()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Plik rozkładu (*.roz)|*.roz",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) != true)
                return;

            _timetable = await _loadTimetable(dialog.FileName).ConfigureAwait(true);
            EntriesGrid.ItemsSource = Entries;
        }
        private void AddTrain()
        {
            var dialogWindow = _windowFactory();
            _ = dialogWindow.ShowDialog();
            if (!dialogWindow.Result)
                return;

            AddEntryFor(dialogWindow.Number);
        }
    }
}