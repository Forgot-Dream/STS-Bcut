using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using ImTools;
using STS_Bcut.src;
using STS_Bcut.src.Common;

namespace STS_Bcut
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private MainViewModel viewModel;
        public MainView()
        {
            InitializeComponent();
            viewModel = (MainViewModel?)DataContext;
        }

        private void File_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var Files = new List<string>((IEnumerable<string>)e.Data.GetData(DataFormats.FileDrop));
                foreach (var file in Files)
                {
                    viewModel.Files.Add(new AudioFile()
                    {
                        FullName = Path.GetFileName(file),
                        FullPath = file,
                        IsSelected = false
                    });
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
        }
    }
}
