
using System;
using System.Linq;

namespace Project_Version1.Helpers
{
    public static class PaginationHelper
    {
        public static IQueryable<T> Paginate<T>(IQueryable<T> query, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            return query.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}