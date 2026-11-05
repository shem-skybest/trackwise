import { getBlogPosts, type BlogPostDto } from "@trackwise/shared/api";
import { createListStore } from "@trackwise/shared/stores";

/**
 * Store for the blog posts list page.
 */
export const BlogPostsListStore = createListStore<BlogPostDto>(getBlogPosts, {
  defaultSortField: "-CreatedAt",
  defaultPageSize: 10,
});
