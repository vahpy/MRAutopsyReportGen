
namespace HoloAutopsy.Record.Logging
{
    public interface ObjectLogger
    {
        public string GetName();
        public string Fetch(int frameNum);
        public void Call(string[] data);
        public void ResetChangeTrackers();
        public void Undo();
    }

}