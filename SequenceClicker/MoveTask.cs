using System.Text;

namespace SequenceClicker
{
    public class MoveTask : MyTask
    {
        int _xpos;
        public int XPos
        {
            get => _xpos;
            set
            {
                if (_xpos != value)
                {
                    _xpos = value;
                    OnPropertyChanged(nameof(_xpos));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }
        int _ypos;
        public int YPos
        {
            get => _ypos;
            set
            {
                if (_ypos != value)
                {
                    _ypos = value;
                    OnPropertyChanged(nameof(_ypos));
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public MoveTask(int x, int y)
        {
            XPos = x;
            YPos = y;
        }

        public override string GetSave()
        {
            StringBuilder sb = new StringBuilder("Move[");
            sb.Append($"X:{_xpos};");
            sb.Append($"Y:{_ypos}");
            sb.Append("]\n");
            return sb.ToString();
        }
        public static MoveTask LoadSave(string saveTxt)
        {
            saveTxt = saveTxt.Trim();
            saveTxt = saveTxt.Remove(0, 4);
            saveTxt = saveTxt.Replace("[", "");
            saveTxt = saveTxt.Replace("]", "");
            string[] values = saveTxt.Split(";");
            return new MoveTask(int.Parse(values[0].Split(":")[1]), int.Parse(values[1].Split(":")[1]));
        }

        public override string ToString()
        {
            return $"Move to {XPos}|{YPos}";
        }
    }
}
