using System;

namespace SnakeAid.Core.Requests.SnakeCatchingMission
{
    public class UpdateMissionStatusRequest
    {
        public string? Notes { get; set; }
        
        /// <summary>
        /// ID của môi trường bắt rắn
        /// </summary>
        public int? CatchingEnvironmentId { get; set; }
    }
}
