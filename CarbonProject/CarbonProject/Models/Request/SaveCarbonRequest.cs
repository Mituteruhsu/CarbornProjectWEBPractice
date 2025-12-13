namespace CarbonProject.Models.Request
{
    public class SaveCarbonRequest
    {
        public string Name { get; set; }
        public decimal Usage { get; set; }
        public decimal Factor { get; set; }
        public decimal Emission { get; set; }
    }
}
