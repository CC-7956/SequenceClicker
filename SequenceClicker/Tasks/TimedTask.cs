using System.Collections.ObjectModel;

namespace SequenceClicker.Tasks
{
    internal class TimedTask : MyTask
    {
        double _time;
        public double Time
        {
            get { return _time; }

            set
            {
                _time = value;
                OnPropertyChanged(nameof(_time));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        ObservableCollection<MyTask> _subSeq;
        public ObservableCollection<MyTask> SubSeq
        {
            get { return _subSeq; }
            set
            {
                _subSeq = value;
            }
        }

        public TimedTask(double time)
        {
            Time = time;
            SubSeq = new ObservableCollection<MyTask>();
        }

        public static bool ValidInput(string min, string sec)
        {
            if ((double.TryParse(min.Trim(), out double d) && d >= 0.00001) || (double.TryParse(sec.Trim(), out double q) && q >= 0.001))
            {
                return true;
            }
            return false;
        }

        public override string GetSave()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            int totalMilliseconds = (int)Math.Round(_time * 1000);
            TimeSpan ts = TimeSpan.FromMilliseconds(totalMilliseconds);
            var parts = new List<string>();
            parts.Add("Timed");
            if (ts.Hours > 0)
                parts.Add($"{ts.Hours}h");
            if (ts.Minutes > 0)
                parts.Add($"{ts.Minutes}m");
            if (ts.Seconds > 0)
                parts.Add($"{ts.Seconds}s");
            if (ts.Milliseconds > 0)
                parts.Add($"{ts.Milliseconds}ms");

            // fallback: if all are 0, show "0ms"
            if (parts.Count == 1)
                parts.Add("0ms");

            foreach (var task in SubSeq)
            {
                parts.Add("\n  ");
                parts.Add(task.ToString());
            }

            return string.Join(" ", parts);
        }
    }
}
