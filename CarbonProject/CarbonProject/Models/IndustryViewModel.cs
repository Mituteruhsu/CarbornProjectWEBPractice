using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace CarbonProject.Models
{
    public class IndustryViewModel
    {
        public string Industry_Id { get; set; }
        public string Major_Category_Code { get; set; }
        public string Major_Category_Name { get; set; }
        public string Middle_Category_Code { get; set; }
        public string Middle_Category_Name { get; set; }
    }
}