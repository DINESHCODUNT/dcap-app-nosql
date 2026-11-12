using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ICommentRepository
    {
        Task<IEnumerable<CommentModel>> GetCommentsByProjectIdAsync(int projectId);

        Task<IEnumerable<CommentModel>> GetCommentsByProjectTaskIdAsync(int projectTaskId);

        Task<IEnumerable<CommentModel>> CreateCommentAsync(int projectId, int? projectTaskId, string userId, string comment);
        Task<IEnumerable<CommentModel>> UpdateCommentAsync(int commentId, int projectId, int? projectTaskId, string comment, string user);
        Task<IEnumerable<CommentModel>> DeleteCommentAsync(int commentId, int projectId, string comment, string user);
    }
}
