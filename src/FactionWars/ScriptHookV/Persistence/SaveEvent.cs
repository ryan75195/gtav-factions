using System;

namespace FactionWars.ScriptHookV.Persistence
{
    public sealed class SaveEvent : EventArgs
    {
        public string Path { get; }
        public DateTime ModifiedAtUtc { get; }

        public SaveEvent(string path, DateTime modifiedAtUtc)
        {
            Path = path;
            ModifiedAtUtc = modifiedAtUtc;
        }
    }
}
