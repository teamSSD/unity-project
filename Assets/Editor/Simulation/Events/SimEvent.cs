using System.Text;

namespace Game.Editor.Simulation.Events
{
    /// <summary>모든 시뮬 이벤트의 base. day/phase는 시간 기준선.
    /// 이벤트별 필드는 하위 record가 정의. JSONL 직렬화는 각 record가 담당 (Unity JsonUtility는 상속 불가라 수동).</summary>
    public abstract class SimEvent
    {
        public int day;
        public int phase; // 0=Preparation, 1=Morning, 2=Afternoon, 3=Evening, 4=Night

        /// <summary>JSONL 한 줄. 형식: {"type":"Name","day":N,"phase":N,...}</summary>
        public abstract string ToJsonLine();

        /// <summary>필드값 → JSON 문자열 escape.</summary>
        protected static string J(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (c < 0x20) sb.AppendFormat("\\u{0:X4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        protected string BaseFields(string type) =>
            $"\"type\":{J(type)},\"day\":{day},\"phase\":{phase}";
    }
}
