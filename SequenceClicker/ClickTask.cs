using System.Text;

namespace SequenceClicker
{
    public class ClickTask : MyTask
    {
        bool _leftclick;
        public bool Leftclick
        {
            get => _leftclick;
            set
            {
                if (_leftclick != value)
                {
                    _leftclick = value;
                    OnPropertyChanged(nameof(_leftclick));
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

        double _delay;
        public double _Delay
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

        public ClickTask(bool leftclick, int repeats)
        {
            Leftclick = leftclick;
            Repeats = repeats;
        }

        public ClickTask(bool leftclick, int repeats, double delay)
        {
            Leftclick = leftclick;
            Repeats = repeats;
            _Delay = delay;
        }

        public override string GetSave()
        {
            StringBuilder sb = new StringBuilder("Click[");
            sb.Append($"Leftclick:{_leftclick};");
            sb.Append($"Repeats:{_repeats}");
            if (_delay > 0)
            {
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
            string[] values = saveTxt.Split(";");
            if (values.Length == 3)
            {
                return new ClickTask(values[0].Split(":")[1].ToLower() == "true" ? true : false, int.Parse(values[1].Split(":")[1]), double.Parse(values[2].Split(":")[1]));
            }
            return new ClickTask(values[0].Split(":")[1].ToLower() == "true" ? true : false, int.Parse(values[1].Split(":")[1]));
        }

        public override string ToString()
        {
            string s = "";
            if (Leftclick)
            {
                s += $"Leftclick {Repeats}x";
            }
            else
            {
                s += $"Rightclick {Repeats}x";
            }
            if (Repeats > 1)
            {
                s += $" w. {_Delay}s delay";
            }
            return s;
        }
    }
}
