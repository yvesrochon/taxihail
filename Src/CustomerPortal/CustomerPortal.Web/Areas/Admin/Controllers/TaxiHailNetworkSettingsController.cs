﻿using CustomerPortal.Web.Android;
using CustomerPortal.Web.Entities;
using CustomerPortal.Web.Entities.Network;
using MongoRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CustomerPortal.Web.Areas.Admin.Controllers
{
    public class TaxiHailNetworkSettingsController : TaxiHailNetworkSettingsControllerBase
    {

       
        public TaxiHailNetworkSettingsController()
            : base(new MongoRepository<TaxiHailNetworkSettings>())
        {
        }


        public ActionResult Index(string id)
        {
            var network = Repository.FirstOrDefault(x => x.Id == id);
            
            if (network == null)
            {
                var company = new MongoRepository<Company>().First(x => x.Id == id);

                    network = new TaxiHailNetworkSettings
                    {
                        Id = company.CompanyKey
                    };
            }
            return PartialView(network);
        }

        [HttpPost]
        public JsonResult Index(TaxiHailNetworkSettings model,string networkId=null)
        {
            if (ModelState.IsValid)
            {
                if (networkId != null)
                {
                    model.Id = networkId;
                }
                if (!model.IsInNetwork)
                {
                    foreach (var taxiHailNetworkSettings in Repository)
                    {
                        var preference = taxiHailNetworkSettings.Preferences.FirstOrDefault(x => x.CompanyKey == model.Id);
                        if (preference!=null)
                        {
                            taxiHailNetworkSettings.Preferences.Remove(preference);
                            Repository.Update(taxiHailNetworkSettings);
                        }
                    }
                }

                Repository.Update(model);
                return Json(new { Success = true, Message = "Changes Saved" });
            }

            return Json(new { Success = false, Message = "All fields are required" });
        }

    }
}
