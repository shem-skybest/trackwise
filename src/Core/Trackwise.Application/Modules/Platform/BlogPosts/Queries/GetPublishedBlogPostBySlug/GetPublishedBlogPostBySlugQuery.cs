using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Queries;

public sealed class GetPublishedBlogPostBySlugQuery : IQuery<Result<BlogPostDto>>
{
    public string Slug { get; set; } = string.Empty;
}
