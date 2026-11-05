using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Commands;

public sealed class DeleteBlogPostCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
