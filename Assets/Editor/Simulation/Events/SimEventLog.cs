using System.Collections.Generic;
using System.IO;

namespace Game.Editor.Simulation.Events
{
    /// <summary>Append-only 이벤트 로그. 메모리에 담고 마지막에 JSONL 파일로 flush.
    /// 리포트 파생은 in-memory list 재순회로 처리.</summary>
    public class SimEventLog
    {
        private readonly List<SimEvent> _events = new();
        public IReadOnlyList<SimEvent> Events => _events;

        public void Add(SimEvent e) => _events.Add(e);

        public void Flush(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using var w = new StreamWriter(filePath, append: false);
            foreach (var e in _events) w.WriteLine(e.ToJsonLine());
        }

        public int Count => _events.Count;

        /// <summary>필터 편의. 타입별 뽑기.</summary>
        public IEnumerable<T> OfType<T>() where T : SimEvent
        {
            foreach (var e in _events) if (e is T t) yield return t;
        }
    }
}
