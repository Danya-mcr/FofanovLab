using System;

namespace PointObjectDetection.Core
{
    /// <summary>
    /// Разработчик 4 (Оля)
    /// Вычисление доверительного интервала и сегментация пикселей
    /// </summary>
    public static class ThresholdCalculator
    {
        // Кеш для коэффициента k
        private static double _cachedP = -1;
        private static double _cachedK;

        /// <summary>
        /// Вычисление коэффициента k из интегрального уравнения:
        /// ∫_{-k}^{k} (1/√(2π)) · exp(-x²/2) dx = 1 - p
        /// Решается через обратную функцию ошибок: k = √2 · erf⁻¹(1-p)
        /// </summary>
        public static double GetK(double falseAlarmProb)
        {
            // Если p не изменился — возвращаем из кеша
            if (Math.Abs(_cachedP - falseAlarmProb) < 1e-10)
                return _cachedK;

            double confidence = 1.0 - falseAlarmProb;
            _cachedK = Math.Sqrt(2.0) * ErfInv(confidence);
            _cachedK = Math.Min(_cachedK, 5.0);
            _cachedP = falseAlarmProb;

            return _cachedK;
        }

        /// <summary>
        /// Вычисление границ доверительного интервала [μ - kσ, μ + kσ]
        /// </summary>
        public static (double lower, double upper) ComputeBounds(
            double mean, double stdDev, double falseAlarmProb)
        {
            double k = GetK(falseAlarmProb);

            double lower = mean - k * stdDev;
            double upper = mean + k * stdDev;

            // Обрезаем до физических пределов яркости [0, 255]
            lower = Math.Max(0, lower);
            upper = Math.Min(255, upper);

            return (lower, upper);
        }

        /// <summary>
        /// Сегментация: проверка, является ли пиксель объектом
        /// </summary>
        public static bool SegmentPixel(
            double brightness, double lower, double upper, double stdDev)
        {
            // Контрастный фон: используем обрезанные границы
            if (stdDev > 10)
            {
                double effectiveLower = Math.Max(0, lower);
                double effectiveUpper = Math.Min(255, upper);
                return brightness <= effectiveLower || brightness >= effectiveUpper;
            }

            // Однородный фон: используем исходные границы
            return brightness < lower || brightness > upper;
        }

        // ============ ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ============

        /// <summary>
        /// Функция ошибок (error function)
        /// erf(x) = (2/√π) · ∫₀ˣ exp(-t²) dt
        /// Аппроксимация с точностью ~1.5·10⁻⁷
        /// </summary>
        private static double Erf(double x)
        {
            double sign = Math.Sign(x);
            x = Math.Abs(x);

            double t = 1.0 / (1.0 + 0.3275911 * x);
            double result = 1.0 - (((((1.061405429 * t - 1.453152027) * t)
                + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t
                * Math.Exp(-x * x);

            return sign * result;
        }

        /// <summary>
        /// Обратная функция ошибок
        /// Аппроксимация для |x| < 1
        /// </summary>
        private static double ErfInv(double x)
        {
            double a = 0.147;
            double ln = Math.Log(1 - x * x);
            double part1 = 2.0 / (Math.PI * a) + ln / 2.0;
            double part2 = ln / a;

            return Math.Sign(x) * Math.Sqrt(Math.Sqrt(part1 * part1 - part2) - part1);
        }
    }
}