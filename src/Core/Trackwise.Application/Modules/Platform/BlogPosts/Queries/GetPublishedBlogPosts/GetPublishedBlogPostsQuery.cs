using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Queries;

public sealed class GetPublishedBlogPostsQuery : SearchableQuery, IQuery<PagedResult<BlogPostDto>>
{
    public string? Category { get; set; }
}
