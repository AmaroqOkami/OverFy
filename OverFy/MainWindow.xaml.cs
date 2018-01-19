﻿using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace OverFy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SpotifyWorker work;
        ObservableCollection<String> listProperties;

        public MainWindow(SpotifyWorker _work, AppSettings appSettings)
        {
            InitializeComponent();

            work = _work;
            listProperties = new ObservableCollection<string>();

            foreach (var item in appSettings.PropertiesOrder)
            {
                listProperties.Add(item);
            }

            gridSongProperties.ItemsSource = listProperties;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            work.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private async void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var view = new AddPropertyView();
            string newProperty = (string)await DialogHost.Show(view);

            listProperties.Insert(0, newProperty);

            CopyProperties();
        }

        private void Button_MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int index = gridSongProperties.SelectedIndex;
            var selectedItem = listProperties[index].ToString();

            listProperties.Insert(index + 2, selectedItem);
            listProperties.RemoveAt(index);

            CopyProperties();
        }

        private void CopyProperties()
        {
            App.appSettings.PropertiesOrder = new System.Collections.Specialized.StringCollection();
            foreach (var item in listProperties)
            {
                App.appSettings.PropertiesOrder.Add(item);
            }
        }

        private void Button_MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int index = gridSongProperties.SelectedIndex;
            var selectedItem = listProperties[index].ToString();

            listProperties.Insert(index - 1, selectedItem);
            listProperties.RemoveAt(index + 1);

            CopyProperties();
        }

        private void Button_ShowSettings_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
