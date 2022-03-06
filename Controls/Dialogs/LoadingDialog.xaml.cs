using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace Translator.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for LoadingDialog.xaml
    /// </summary>
    public partial class LoadingDialog : Window
    {
        public delegate void onLoaded();
        public event onLoaded OnLoaded;
        private bool isFinished = false;

        public LoadingDialog(string projectName)
        {
            InitializeComponent();
            Loaded += LoadingDialog_Loaded;
            TextProject.Text = System.IO.Path.GetFileNameWithoutExtension(projectName);
        }

        private void LoadingDialog_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => {

                OnLoaded?.Invoke();

            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        public void FinishDialog()
        {
            isFinished = true;
            DialogResult = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!isFinished)
                e.Cancel = true;
        }
    }
}
