using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Commands;

internal sealed class UnpublishBlogPostHandler(
    IMasterUnitOfWork masterUow,
    ILogger<UnpublishBlogPostHandler> logger) : IAppRequestHandler<UnpublishBlogPostCommand, Result>
{
    public async Task<Result> Handle(UnpublishBlogPostCommand req, CancellationToken ct)
    {
        var blogPost = await masterUow.Repository<BlogPost>().GetByIdAsync(req.Id, ct);

        if (blogPost is null)
        {
            return Result.Fail($"Blog post with ID '{req.Id}' not found");
        }

        blogPost.Status = BlogPostStatus.Draft;
        blogPost.UpdatedAt = DateTime.UtcNow;

        masterUow.Repository<BlogPost>().Update(blogPost);
        await masterUow.SaveChangesAsync(ct);

        logger.LogInformation("Unpublished blog post {Id}", req.Id);
        return Result.Ok();
    }
}
