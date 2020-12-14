using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ValueLabtest.Model;

namespace ValueLabtest.Service
{
    public class StockServices : IStockServices
    {
        public async Task<List<StockModel>> GetQuoteDetailAsync()
        {
            var url = "https://financialmodelingprep.com/api/v3/quote/AAWW, AAL, CPAAW,PRAA, PAAS, RYAAY ?apikey=b351eb4b7226aefb40eb0da9df7cc616";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = null;
            string messageResut = string.Empty;
            try
            {

                response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
            messageResut  =response.Content.ReadAsStringAsync().Result;
                
            }
            catch(Exception  exe)
            {
                throw new HttpRequestException(exe.Message);
            }
         
            return DeserialiseRequest(messageResut);
    }

        public List<StockModel> DeserialiseRequest(string messageResut)
        {
            List<StockModel> stockModels = null;
            try
            {
                stockModels = JsonConvert.DeserializeObject<List<StockModel>>(messageResut);
            }
            catch (Exception ex)
            {
                throw new BadRequestException("Unexpected deserialisation error.", ex);
            }
            return stockModels;

        }



    }
}
