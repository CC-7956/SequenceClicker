using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private bool _IsStart;

        public TimedTask(double time, bool start)
        {
            Time = time;
            SubSeq = new ObservableCollection<MyTask>();
            _IsStart = start;
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
            if (_IsStart)
            {
                parts.Add("start");
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
            }
            else
            {
                parts.Add("end");
            }
            return string.Join(" ", parts);
        }
    }
}
