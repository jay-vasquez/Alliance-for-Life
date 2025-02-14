﻿using Alliance_for_Life.Controllers;
using Alliance_for_Life.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace Alliance_for_Life.ViewModels

{
    public class SubContractorFormViewModel
    {
        public System.Guid Id { get; set; }

        [Required]
        [Display(Name = "Organization Name")]
        public string OrgName { get; set; }

        [Display(Name = "Key Contact")]
        [Required]
        public string Director { get; set; }


        [Required]
        public string City { get; set; }

        [Required]
        public string County { get; set; }

        [Required]
        [Display(Name = "A2A Region")]
        public GeoRegion? Region { get; set; }

        [Required]
        public int EIN { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        [Display(Name = "Zip Code")]
        public int ZipCode { get; set; }

        [Required]
        [Display(Name = "Allocated Contract Amount")]
        public double AllocatedContractAmount { get; set; }

        [Display(Name = "Allocated Adjustments")]
        public double AllocatedAdjustments { get; set; }

        [Required]
        [Display(Name = "Street Address")]
        public string Address1 { get; set; }

        [Display(Name = "P.O. Box")]
        public string PoBox { get; set; }

        public bool Active { get; set; }

        public DateTime SubmittedDate { get; set; }

        [Display(Name = "Affiliate Region")]
        public AffiliatesRegion? AffiliateRegion { get; set; }

        //used to set the heading of the page
        public string Heading { get; set; }

      //  used to switch between actions in the controller
        public string Action
        {
            get
            {
                Expression<Func<SubContractorController, ActionResult>> update =
                    (c => c.Update(this));

                Expression<Func<SubContractorController, ActionResult>> create =
                    (c => c.Create(this));

                var action = (Id != Guid.Empty) ? update : create;
                return (action.Body as MethodCallExpression).Method.Name;
            }

        }


    }
}