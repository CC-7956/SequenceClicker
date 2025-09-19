using System.Text;

namespace SequenceClicker.Tasks
{
    public class DelayTask : MyTask
    {
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

        public DelayTask(int delay)
        {
            Delay = delay;
        }

        public override string GetSave()
        {
            StringBuilder sb = new StringBuilder("Delay[");
            sb.Append($"Delay:{_delay}");
            sb.Append("]\n");
            return sb.ToString();
        }
        public static DelayTask LoadSave(string saveTxt)
        {
            saveTxt = saveTxt.Trim();
            saveTxt = saveTxt.Remove(0, 5);
            saveTxt = saveTxt.Replace("[", "");
            saveTxt = saveTxt.Replace("]", "");
            return new DelayTask(int.Parse(saveTxt.Split(":")[1]));
        }

        public static bool ValidInput(string min, string sec)
        {
            if ((double.TryParse(min.Trim(), out double d) && d > 0) || (double.TryParse(sec.Trim(), out double q) && q > 0))
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(_delay);
            var parts = new List<string>();
            parts.Add("Delay ");
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

            return string.Join(" ", parts);
        }
    }
}
