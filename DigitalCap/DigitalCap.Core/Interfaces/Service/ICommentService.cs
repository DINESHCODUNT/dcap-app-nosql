using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ICommentService
    {
        // MVC pattern methods (matching original)
        Task<IEnumerable<CommentModel>> GetComments(int projectId, int? projectTaskId);
        Task<IEnumerable<CommentModel>> TransferExistingNotesToComment(int projectId, int? projectTaskId, string currentUser, string notes);
        IEnumerable<CommentModel> SetCommentsEditableBasedOnUser(IEnumerable<CommentModel> comments, string currentUser);
        Task<IEnumerable<CommentModel>> UpdateComment(int id, int projectId, string comment, string user);
        Task<IEnumerable<CommentModel>> CreateComment(int projectId, int? projectTaskId, string UserId, string comment);
        Task<IEnumerable<CommentModel>> DeleteComment(int id, int projectId, string comment, string user);
    }
}
