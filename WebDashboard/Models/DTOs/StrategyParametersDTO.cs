using System.ComponentModel.DataAnnotations;

namespace BinanceTradingBot.WebDashboard.Models.DTOs
{
    public class StrategyParametersDTO
    {
        public string StrategyName { get; set; } = string.Empty;

        [Range(1, 100, ErrorMessage = "La période RSI doit être entre 1 et 100")]
        public int RsiPeriod { get; set; } = 14;

        [Range(1, 100, ErrorMessage = "La valeur RSI survendu doit être entre 1 et 100")]
        public int RsiOversold { get; set; } = 30;

        [Range(1, 100, ErrorMessage = "La valeur RSI suracheté doit être entre 1 et 100")]
        public int RsiOverbought { get; set; } = 70;

        [Range(1, 100, ErrorMessage = "La période rapide MACD doit être entre 1 et 100")]
        public int MacdFastPeriod { get; set; } = 12;

        [Range(1, 100, ErrorMessage = "La période lente MACD doit être entre 1 et 100")]
        public int MacdSlowPeriod { get; set; } = 26;

        [Range(1, 100, ErrorMessage = "La période de signal MACD doit être entre 1 et 100")]
        public int MacdSignalPeriod { get; set; } = 9;

        [Range(1, 100, ErrorMessage = "La période BB doit être entre 1 et 100")]
        public int BbPeriod { get; set; } = 20;

        [Range(0.1, 10, ErrorMessage = "L'écart type BB doit être entre 0.1 et 10")]
        public double BbStdDev { get; set; } = 2.0;

        [Range(0, 1, ErrorMessage = "Le seuil de largeur BB doit être entre 0 et 1")]
        public double BbWidthThreshold { get; set; } = 0.05;

        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }
}