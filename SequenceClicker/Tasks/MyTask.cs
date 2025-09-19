using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace SequenceClicker.Tasks
{
    public abstract class MyTask : INotifyPropertyChanged
    {
        public string Name { get; private set; }
        public string DisplayText => ToString();
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract string GetSave();
        public static MyTask LoadSave(string savedText)
        {
            string type = savedText.Split('[')[0];
            switch (type)
            {
                case "Click": return ClickTask.LoadSave(savedText);
                case "Delay": return DelayTask.LoadSave(savedText);
                case "Move": return MoveTask.LoadSave(savedText);
                default: MessageBox.Show($"Unknown Task type.\n{type} does not exist.", "Error while loading", MessageBoxButton.OK, MessageBoxImage.Error); return null;
            }
        }

        protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
