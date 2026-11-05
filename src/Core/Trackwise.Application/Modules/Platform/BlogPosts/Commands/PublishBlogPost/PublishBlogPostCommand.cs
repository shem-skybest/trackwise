using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Commands;

public sealed class PublishBlogPostCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
