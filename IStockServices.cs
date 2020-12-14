using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ValueLabtest.Model;

namespace ValueLabtest.Service
{
    public interface IStockServices
    {
        Task<List<StockModel>> GetQuoteDetailAsync();
    }
}
