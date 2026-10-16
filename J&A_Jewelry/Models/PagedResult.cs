using System.Collections.Generic;

namespace J_A_Jewelry.Models
{
    // Shared paged-list envelope for high-volume endpoints.
    // Serialized as { data, page, pageSize, totalCount } (camelCase);
    // with ReferenceHandler.Preserve the data array arrives wrapped in
    // $id/$values — the Flutter ApiService.getPaged() unwraps that.
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}