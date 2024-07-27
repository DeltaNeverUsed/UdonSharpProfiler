using VRC.SDK3.Data;

namespace UdonSharpProfiler {
    public enum PerfettoTrackEventType {
        TYPE_SLICE_BEGIN,
        TYPE_SLICE_END,

        TYPE_SLICE_COMPLETE,

        TYPE_INSTANT,
    }

    public static class PerfettoHelper {
        private static string PerfettoTrackEventTypeToString(this PerfettoTrackEventType eventType) {
            switch (eventType) {
                case PerfettoTrackEventType.TYPE_SLICE_BEGIN:
                    return "B";
                case PerfettoTrackEventType.TYPE_SLICE_END:
                    return "E";
                case PerfettoTrackEventType.TYPE_SLICE_COMPLETE:
                    return "X";
                case PerfettoTrackEventType.TYPE_INSTANT:
                    return "I";
            }

            return "";
        }

        public static DataDictionary CreatePacket() {
            var packet = new DataDictionary();
            return packet;
        }

        public static DataDictionary AddTimeStamp(this DataDictionary packet, long ticks) {
            packet.Add("ts", ticks);
            return packet;
        }

        public static DataDictionary AddDuration(this DataDictionary packet, long ticks) {
            packet.Add("dur", ticks);
            return packet;
        }

        public static DataDictionary AdjustTimeStamp(this DataDictionary packet, long min) {
            packet["ts"] = packet["ts"].Long - min;
            return packet;
        }

        public static DataDictionary AddEventType(this DataDictionary packet, PerfettoTrackEventType eventType) {
            packet.Add("ph", eventType.PerfettoTrackEventTypeToString());
            return packet;
        }

        public static DataDictionary AddEventName(this DataDictionary packet, string eventName) {
            packet.Add("name", eventName);
            return packet;
        }

        public static DataDictionary AddIds(this DataDictionary packet) {
            packet.Add("pid", 1);
            packet.Add("tid", 1);

            return packet;
        }
    }
}