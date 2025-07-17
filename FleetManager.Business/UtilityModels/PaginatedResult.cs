using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.UtilityModels
{
    // Pagination helper
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public static async Task<PaginatedResult<T>> CreateAsync(IQueryable<T> source, int page, int pageSize)
        {
            var result = new PaginatedResult<T> { Page = page, PageSize = pageSize };
            result.TotalCount = await source.CountAsync();
            result.Items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return result;
        }
    }
}
