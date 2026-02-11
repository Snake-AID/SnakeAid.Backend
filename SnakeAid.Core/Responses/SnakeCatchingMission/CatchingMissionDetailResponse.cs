namespace SnakeAid.Core.Responses.SnakeCatchingMission
{
    public class CatchingMissionDetailResponse
    {
        /// <summary>
        /// ID chi tiết nhiệm vụ
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID nhiệm vụ bắt rắn
        /// </summary>
        public Guid SnakeCatchingMissionId { get; set; }

        /// <summary>
        /// ID loài rắn
        /// </summary>
        public int SnakeSpeciesId { get; set; }

        /// <summary>
        /// Tên loài rắn
        /// </summary>
        public string? SnakeSpeciesName { get; set; }

        /// <summary>
        /// Số lượng rắn bắt được
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Ngày tạo
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Ngày cập nhật
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
