using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SSMVCCoreApp.Models.Abstract;
using SSMVCCoreApp.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSMVCCoreApp.Models.Concrete
{
    public class EfProductRepository : IProductRepository, IDisposable
    {
        private SportsStoreDbContext _context;
        private readonly ILogger<EfProductRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _distributedCache;

        public EfProductRepository(SportsStoreDbContext context, ILogger<EfProductRepository> logger, IConfiguration configuration, IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _distributedCache = distributedCache;
        }

        #region IProductRespository Members
        public async Task CreateAsync(Product product)
        {
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProductRepository.CreateAsync(product={product})");
                throw;
            }
        }

        public async Task DeleteAsync(int productId)
        {
            try
            {
                Product prod = await _context.Products.FindAsync(productId);
                _context.Products.Remove(prod);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"From ProductRepository.DeleteAsync - productId={productId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProductRepository.DeleteAsync(productId={productId})");
                throw;
            }
        }

        public async Task<Product> FindProductByIDAsync(int productId)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(productId);
                _logger.LogInformation($"****AC From ProductRepository.FindProductBuIDAsync - productId={productId}");
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in  ProductRepository.FindProductBuIDAsync(productId={productId})");
                throw;
            }
        }

        public async Task<List<Product>> FindProductsByCategoryAsync(string category)
        {
            try
            {
                var result = await _context.Products.Where(p => p.Category == category).ToListAsync();
                _logger.LogInformation($"ProductRepository.FindProductsByCategoryAsync - category={category}", category);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProductRepository.FindProductsByCategoryAsync(category={category})");
                throw;
            }
        }


        //Without Redis Caching
        //public async Task<List<Product>> GetAllProductsAsync()
        //{
        //    try
        //    {
        //        var productsList = await _context.Products.ToListAsync();
        //        _logger.LogInformation($"ProductRepository.GetAllProductsAsync");
        //        return productsList;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in ProductRepository.GetAllProductsAsync");
        //        throw;
        //    }

        //}


        public void ClearCache()
        {
            _distributedCache.RemoveAsync("ProductsList");
            _logger.LogInformation("Distributed Cache Cleared");
        }

        //With Redis Caching
        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                List<Product> productsList = null;
                if (_configuration["EnableRedisCaching"] == "true")
                {
                    var cachedProductsList = await _distributedCache.GetStringAsync("ProductsList");
                    if (!string.IsNullOrEmpty(cachedProductsList))
                    {
                        productsList = JsonConvert.DeserializeObject<List<Product>>(cachedProductsList);
                        _logger.LogInformation($"ProductRepository.GetAllProductsAsync : Read from Redis Cache");
                    }
                    else
                    {
                        productsList = await _context.Products.ToListAsync();
                        var dataToBeCached = JsonConvert.SerializeObject(productsList);
                        DistributedCacheEntryOptions entryOptions = new DistributedCacheEntryOptions();
                        entryOptions.SetAbsoluteExpiration(new TimeSpan(0, 3, 0));
                        await _distributedCache.SetStringAsync("ProductsList", dataToBeCached, entryOptions);
                        _logger.LogInformation($"ProductRepository.GetAllProductsAsync : Products List Cached");
                    }
                }
                else
                {
                    productsList = await _context.Products.ToListAsync();
                    _logger.LogInformation($"ProductRepository.GetAllProductsAsync");
                }

                return productsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProductRepository.GetAllProductsAsync");
                throw;
            }
        }

        public async Task UpdateAsync(Product product)
        {
            try
            {
                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"ProductRepository.UpdateAsync - product={product}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProductRepository.UpdateAsync(product={product})");
                throw;
            }
        }

        #endregion

        #region IDisposable Member
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Free managed resources
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
        }

    }
}
