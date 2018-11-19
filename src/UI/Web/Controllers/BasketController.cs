﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.eShopWeb.Web.Services;
using Microsoft.eShopWeb.Web.ViewModels;
using Polly.CircuitBreaker;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.eShopWeb.Infrastructure.Identity;
//using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Web.Interfaces;

namespace Microsoft.eShopWeb.Web.Controllers
{
    //[Authorize]
    [Route("Basket/[action]")]
    public class BasketController : Controller
    {
        private readonly IBasketService _basketSvc;
        private readonly ICatalogService _catalogSvc;
        private readonly Microsoft.eShopWeb.ApplicationCore.Interfaces.IIdentityParser<ApplicationUser> _appUserParser;

        public BasketController(IBasketService basketSvc, ICatalogService catalogSvc, ApplicationCore.Interfaces.IIdentityParser<ApplicationUser> appUserParser)
        {
            _basketSvc = basketSvc;
            _catalogSvc = catalogSvc;
            _appUserParser = appUserParser;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = _appUserParser.Parse(HttpContext.User);
                var vm = await _basketSvc.GetBasket(user);

                return View(vm);
            }
            catch (BrokenCircuitException)
            {
                // Catch error when Basket.api is in circuit-opened mode                 
                HandleBrokenCircuitException();
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Index(Dictionary<string, int> quantities, string action)
        {
            try
            {
                var user = _appUserParser.Parse(HttpContext.User);
                var basket = await _basketSvc.SetQuantities(user, quantities);
                if (action == "[ Checkout ]")
                {
                    return RedirectToAction("Create", "Order");
                }
            }
            catch (BrokenCircuitException)
            {
                // Catch error when Basket.api is in circuit-opened mode                 
                HandleBrokenCircuitException();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddItemToBasket(CatalogItem productDetails)
        {
            try
            {
                if (productDetails?.Id != null)
                {
                    var user = _appUserParser.Parse(HttpContext.User);
                    await _basketSvc.AddItemToBasket(user, productDetails.Id);
                }
                return RedirectToAction("Index", "Catalog");
            }
            catch (BrokenCircuitException)
            {
                // Catch error when Basket.api is in circuit-opened mode                 
                HandleBrokenCircuitException();
            }

            return RedirectToAction("Index", "Catalog", new { errorMsg = ViewBag.BasketInoperativeMsg });
        }

        private void HandleBrokenCircuitException()
        {
            ViewBag.BasketInoperativeMsg = "Basket Service is inoperative, please try later on. (Business Msg Due to Circuit-Breaker)";
        }
    }
}
