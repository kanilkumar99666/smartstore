using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartStore.Collections;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Services.Localization;

namespace SmartStore.Services.Catalog
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static class CategoryExtensions
    {
        /// <summary>
        /// Sort categories for tree representation
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="parentId">Parent category identifier</param>
        /// <param name="ignoreCategoriesWithoutExistingParent">A value indicating whether categories without parent category in provided category list (source) should be ignored</param>
        /// <returns>Sorted categories</returns>
        public static IList<T> SortCategoryNodesForTree<T>(this IEnumerable<T> source, int parentId = 0, bool ignoreCategoriesWithoutExistingParent = false)
			where T : ICategoryNode
        {
			Guard.NotNull(source, nameof(source));

            var result = new List<T>();
			var sourceCount = source.Count();

            if (sourceCount == 0)
            {
                return result;
            }

            var parentMap = source.ToMultimap(x => x.ParentCategoryId, x => x);
            AddNodesCore(parentId);

            if (!ignoreCategoriesWithoutExistingParent && result.Count != sourceCount)
            {
                // Find categories without parent in provided category source and insert them into result
                foreach (var category in source)
				{
					if (!result.Any(x => x.Id == category.Id))
					{
						result.Add(category);
					}	
				}
            }

            return result;

            void AddNodesCore(int pId)
            {
                var childNodes = parentMap.ContainsKey(pId)
                    ? parentMap[pId]
                    : Enumerable.Empty<T>();

                foreach (var node in childNodes.ToArray())
                {
                    result.Add(node);
                    AddNodesCore(node.Id);
                }
            }
        }

        /// <summary>
        /// Returns a ProductCategory that has the specified values
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="categoryId">Category identifier</param>
        /// <returns>A ProductCategory that has the specified values; otherwise null</returns>
        public static ProductCategory FindProductCategory(this IList<ProductCategory> source, int productId, int categoryId)
        {
			foreach (var productCategory in source)
			{
				if (productCategory.ProductId == productId && productCategory.CategoryId == categoryId)
					return productCategory;
			}

            return null;
        }

		public static string GetCategoryNameIndented(this TreeNode<ICategoryNode> treeNode, 
			string indentWith = "--", 
			int? languageId = null,
			bool withAlias = true)
		{
			Guard.NotNull(treeNode, nameof(treeNode));

			var sb = new StringBuilder();
			var indentSize = treeNode.Depth - 1;
			for (int i = 0; i < indentSize; i++)
			{
				sb.Append(indentWith);
			}

			var cat = treeNode.Value;

			var name = languageId.HasValue
				? cat.GetLocalized(n => n.Name, languageId.Value)
				: cat.Name;

			sb.Append(name);

			if (withAlias && cat.Alias.HasValue())
			{
				sb.Append(" (");
				sb.Append(cat.Alias);
				sb.Append(")");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds a category breadcrumb (path) for a particular category node
		/// </summary>
		/// <param name="categoryNode">The category node</param>
		/// <param name="languageId">The id of language. Pass <c>null</c> to skip localization.</param>
		/// <param name="withAlias"><c>true</c> appends the category alias - if specified - to the name</param>
		/// <param name="separator">The separator string</param>
		/// <returns>Category breadcrumb path</returns>
		public static string GetCategoryPath(this ICategoryNode categoryNode, 
			ICategoryService categoryService, 
			int? languageId = null, 
			string aliasPattern = null,
			string separator = " � ")
		{
			Guard.NotNull(categoryNode, nameof(categoryNode));

			var treeNode = categoryService.GetCategoryTree(categoryNode.Id, true);
			if (treeNode != null)
			{
				return categoryService.GetCategoryPath(treeNode, languageId, aliasPattern, separator);
			}

			return string.Empty;
		}
	}
}
