namespace CarbonProject.Models
{
    // �s�W Model�GCarbonData
    // �o�Ӽҫ��Ω󭺭��i��²�����ұƩ�ƾڡC
    // ��ƥثe�O�R�A�����A���ӥi�H�s����Ʈw�Ϋ�ݺ޲z�ݧ�s�C
    public class CarbonData
    {
        public string CompanyName { get; set; }
        public decimal CurrentEmission { get; set; }   // �ثe�ұƩ�q
        public decimal TargetEmission { get; set; }    // �~�ץؼкұƩ�q
        public decimal AchievementRate { get; set; }   // �F���v (0~1)
    }
    // �~�׺ұƩ���
    public class AnnualEmission
    {
        public int Year { get; set; }
        public decimal Emission { get; set; } // ���G��
    }
    // ���~�ұƩ�ؼ�
    public class CarbonGoal
    {
        public decimal CurrentEmission { get; set; }  // �ثe�Ʃ�q
        public decimal TargetEmission { get; set; }   // �ؼбƩ�q
        public decimal ProgressRate => TargetEmission == 0 ? 0 : 1 - (CurrentEmission / TargetEmission);
    }
    // �Ω��X �~�׺ұ�/���~�ұƩ� ��� Model
    public class DataGoalsViewModel
    {
        public List<AnnualEmission> AnnualEmissions { get; set; }
        public CarbonGoal Goal { get; set; }
    }
}