using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class BlogPostMapper
{
    [MapperIgnoreSource(nameof(BlogPost.DomainEvents))]
    [MapperIgnoreTarget(nameof(BlogPostDto.AuthorInitials))]
    public static partial BlogPostDto ToDto(this BlogPost entity);
}
