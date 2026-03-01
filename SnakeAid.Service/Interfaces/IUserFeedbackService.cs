using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SnakeAid.Core.Requests.UserFeedback;
using SnakeAid.Core.Responses.UserFeedback;

namespace SnakeAid.Service.Interfaces
{
    public interface IUserFeedbackService
    {
        /// <summary>
        /// Tạo feedback mới từ user hiện tại cho target user
        /// </summary>
        /// <param name="request">Thông tin feedback</param>
        /// <param name="raterId">ID của user đang đánh giá (lấy từ token)</param>
        /// <returns>UserFeedbackResponse với thông tin feedback và rating đã cập nhật</returns>
        Task<UserFeedbackResponse> CreateFeedbackAsync(CreateUserFeedbackRequest request, Guid raterId);

        /// <summary>
        /// Lấy tất cả feedback của một target user
        /// </summary>
        /// <param name="targetUserId">ID của user nhận đánh giá</param>
        /// <returns>Danh sách feedback</returns>
        Task<List<UserFeedbackResponse>> GetFeedbacksByTargetUserIdAsync(Guid targetUserId);
    }
}
