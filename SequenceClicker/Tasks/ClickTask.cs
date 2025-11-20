using System.Text;

namespace SequenceClicker.Tasks
{
    public class ClickTask : MyTask
    {
        bool _isLeftclick;
        public bool IsLeftclick
        {
            get => _isLeftclick;
            set
            {
                if (_isLeftclick != value)
                {
                    _isLeftclick = value;
                    OnPropertyChanged(nameof(_isLeftclick));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }
        int _repeats;
        public int Repeats
        {
            get => _repeats;
            set
            {
                if (_repeats != value)
                {
                    _repeats = value;
                    OnPropertyChanged(nameof(_repeats));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }
        int _delay;
        public int Delay
        {
            get => _delay;
            set
            {
                if (_delay != value)
                {
                    _delay = value;
                    OnPropertyChanged(nameof(_delay));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        /// <summary>
        /// Default Constructor for a Click Task
        /// </summary>
        /// <param name="leftclick">Boolean if it is a Leftclick</param>
        public ClickTask(bool leftclick)
        {
            IsLeftclick = leftclick;
            Repeats = 1;
        }

        public ClickTask(bool leftclick, int repeats, int delay)
        {
            IsLeftclick = leftclick;
            Repeats = repeats;
            Delay = delay;
        }

        public override string GetSave()
        {
            StringBuilder sb = new StringBuilder("Click[");
            sb.Append($"Leftclick:{_isLeftclick}");
            if (_delay > 0)
            {
                sb.Append($";Repeats:{_repeats}");
                sb.Append($";Delay:{_delay}");
            }
            sb.Append("]\n");
            return sb.ToString();
        }
        public static ClickTask LoadSave(string saveTxt)
        {
            saveTxt = saveTxt.Trim();
            saveTxt = saveTxt.Remove(0, 5);
            saveTxt = saveTxt.Replace("[", "");
            saveTxt = saveTxt.Replace("]", "");

            if (saveTxt.Contains(";"))
            {
                string[] values = saveTxt.Split(";");
                return new ClickTask(values[0].Split(":")[1].ToLower() == "true" ? true : false, int.Parse(values[1].Split(":")[1]), int.Parse(values[2].Split(":")[1]));
            }
            return new ClickTask(saveTxt.Split(":")[1].ToLower() == "true" ? true : false);
        }
        [Obsolete("New Method in MainWindow WIP")]
        public static bool ValidInput(string repeats, bool delay)
        {
            if (uint.TryParse(repeats.Trim(), out uint rep) && rep > 1 && !delay)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            string s = "";
            if (IsLeftclick)
            {
                s += $"Leftclick {Repeats}x";
            }
            else
            {
                s += $"Rightclick {Repeats}x";
            }
            if (Repeats > 1)
            {
                var parts = new List<string>();
                TimeSpan ts = TimeSpan.FromMilliseconds(Delay);
                if (ts.Hours > 0)
                    parts.Add($"{ts.Hours}h");
                if (ts.Minutes > 0)
                    parts.Add($"{ts.Minutes}m");
                if (ts.Seconds > 0)
                    parts.Add($"{ts.Seconds}s");
                if (ts.Milliseconds > 0)
                    parts.Add($"{ts.Milliseconds}ms");
                // fallback: if all are 0, show "0ms"
                if (parts.Count == 0)
                    parts.Add("0ms");
                s += $" w. {string.Join(" ", parts)} delay";
            }
            return s;
        }
    }
}
