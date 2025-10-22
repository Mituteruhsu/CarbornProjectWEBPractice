using System;
using System.Collections.Generic;
using System.Data;

namespace CarbonProject.Models
{
    public class ESGProgress
    {
        public int Year { get; set; }
        public int TotalActions { get; set; }
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public int NotStartedCount { get; set; }
        public double TotalReductionTon { get; set; }
        public double AverageProgress { get; set; }
    }
}