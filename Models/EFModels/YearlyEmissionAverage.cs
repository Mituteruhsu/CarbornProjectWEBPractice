using System;
using System.ComponentModel.DataAnnotations;

namespace CarbonProject.Models.EFModels
{
    public class YearlyEmissionAverage
    {
        public int Year { get; set; }
        public int CompanyCount { get; set; }
        public decimal TotalEmissionSum { get; set; }
        public decimal AverageAcrossYears { get; set; }
    }
}