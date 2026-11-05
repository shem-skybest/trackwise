using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Queries;

public sealed class GetBlogPostQuery : IQuery<Result<BlogPostDto>>
{
    public Guid Id { get; set; }
}
