using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SnakeAid.Core.Requests.FilterQuestion
{
    public class FilterSnakeByAnswersRequest
    {
        /// <summary>
        /// List of selected filter option IDs from questionnaire
        /// Example: [1, 4, 9, 11, 16, 20] (6 options from 6 questions)
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 đáp án")]
        public List<int> SelectedOptionIds { get; set; } = new List<int>();
    }
}
