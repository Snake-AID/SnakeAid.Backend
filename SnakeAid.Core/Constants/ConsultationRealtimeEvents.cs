namespace SnakeAid.Core.Constants;

public static class ConsultationRealtimeEvents
{
    public const string ConsultationCallEnded = "ConsultationCallEnded";

    public static class ConsultationCallEndReasons
    {
        public const string Timeout = "timeout";
        public const string ParticipantEnded = "participant_ended";
    }
}
