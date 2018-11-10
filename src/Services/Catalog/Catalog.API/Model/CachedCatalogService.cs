﻿using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.eShopOnContainers.Services.Catalog.API.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using Catalog.API.Extensions;

namespace Catalog.API.Model
{
    public class CachedCatalogService : ICachedCatalogService
    {
        private readonly IDistributedCache _cache;

        public static readonly string TOTAL_KEY = "TOTAL_CATALOG_ITEMS";
        public static readonly string TOTAL_TYPES_KEY = "TOTAL_CATALOG_TYPES";
        public static readonly string TOTAL_BRANDS_KEY = "TOTAL_BRAND_TYPES";

        private readonly ILogger<CachedCatalogService> _logger;
        private readonly CatalogService _catalogService;

        public CachedCatalogService(IDistributedCache cache, CatalogService catalogService, ILogger<CachedCatalogService> logger)
        {
            _cache = cache;
            _catalogService = catalogService;
            _logger = logger;
        }

        private async Task ResetAsync()
        {
            await _cache.TryResetAsync(TOTAL_KEY, await _catalogService.ListAllAsync());
        }

        public async Task<CatalogItem> AddAsync(CatalogItem entity, bool saveChanges = true)
        {
            var item = await _catalogService.AddAsync(entity, saveChanges);
            await ResetAsync();
            return item;
        }

        public async Task<CatalogItem> UpdateAsync(CatalogItem entity, bool saveChanges)
        {
            var item = await _catalogService.UpdateAsync(entity, saveChanges);
            await ResetAsync();
            return item;
        }

        public async Task<bool?> DeleteAsync(int id, bool saveChanges = true)
        {
            var result = await _catalogService.DeleteAsync(id, saveChanges);
            await ResetAsync();
            return result;
        }

        public async Task<IEnumerable<CatalogItem>> ListAllAsync()
        {
            var catalogItems = await _cache.TryGetAsync<List<CatalogItem>>(TOTAL_KEY);
            if (catalogItems == null)
            {
                catalogItems = (await _catalogService.ListAllAsync()).ToList();
                await _cache.TrySetAsync(TOTAL_KEY, catalogItems);
            }
            return catalogItems;
        }

        public async Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            if (filter == null)
                return null;

            var item = await _cache.TryGetAsync<CatalogItem>(filter.Key);
            if (item == null)
            {
                item = (await _catalogService.GetSingleBySpecAsync(spec));
                await _cache.TrySetAsync(filter.Key, item);
            }
            return item;
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            if (filter == null)
                return null;

            var items = await _cache.TryGetAsync<List<CatalogItem>>(filter.Key);
            if (items == null)
            {
                items = (await _catalogService.ListAsync(spec)).ToList();

                if (filter.PageSize.HasValue)
                {
                    items = items.Skip(filter.PageSize.Value * (filter.PageIndex.HasValue ? filter.PageIndex.Value : 1)).Take(filter.PageSize.Value).ToList();                    
                }
                await _cache.TrySetAsync(filter.Key, items);
            }
            return items;
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(string ids, ISpecification<CatalogItem> spec, CacheCatalogFilter filter)
        {
            if (filter == null)
                return null;

            var items = await _cache.TryGetAsync<List<CatalogItem>>(filter.Key);
            if (items == null)
            {
                items = (await _catalogService.ListAsync(ids, spec)).ToList();

                if (filter.PageSize.HasValue)
                {
                    items = items.Skip(filter.PageSize.Value * (filter.PageIndex.HasValue ? filter.PageIndex.Value : 1)).Take(filter.PageSize.Value).ToList();
                }
                await _cache.TrySetAsync(filter.Key, items);
            }
            return items;
        }

        public async Task<IEnumerable<CatalogType>> ListAllCatalogTypeAsync()
        {
            var catalogTypes = await _cache.TryGetAsync<List<CatalogType>>(TOTAL_TYPES_KEY);
            if (catalogTypes == null)
            {
                catalogTypes = (await _catalogService.ListAllCatalogTypeAsync()).ToList();
                await _cache.TrySetAsync(TOTAL_TYPES_KEY, catalogTypes);
            }

            return catalogTypes;
        }

        public async Task<IEnumerable<CatalogBrand>> ListAllCatalogBrandAsync()
        {
            var catalogBrands = await _cache.TryGetAsync<List<CatalogBrand>>(TOTAL_BRANDS_KEY);
            if (catalogBrands == null)
            {
                catalogBrands = (await _catalogService.ListAllCatalogBrandAsync()).ToList();
                await _cache.TrySetAsync(TOTAL_BRANDS_KEY, catalogBrands);
            }

            return catalogBrands;
        }

        public async Task<CatalogItem> GetSingleBySpecAsync(ISpecification<CatalogItem> spec)
        {
            return (await _catalogService.GetSingleBySpecAsync(spec));
        }

        public async Task<IEnumerable<CatalogItem>> ListAsync(ISpecification<CatalogItem> spec)
        {
            return (await _catalogService.ListAsync(spec)).ToList();
        }
    }
}