using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Platform.BlogPosts.Commands;

internal sealed class DeleteBlogPostHandler(IMasterUnitOfWork masterUow)
    : DeleteMasterEntityHandler<DeleteBlogPostCommand, BlogPost>(masterUow);
