using System;

namespace PointObjectDetection.Core
{
    /// Разработчик 4 (Оля)
    public static class ThresholdCalculator
    {
        public static double NormalQuantile(double probability)
        {
            // Граничные случаи
            if (probability <= 0) return -10;
            if (probability >= 1) return 10;

            // Симметрия: для p < 0.5 вычисляем через 1-p
            if (probability < 0.5)
                return -NormalQuantile(1 - probability);

            // Коэффициенты для аппроксимации
            double[] a = {
                -3.969683028665376e+01,
                 2.209460984245205e+02,
                -2.759285104961687e+02,
                 1.383577518672690e+02,
                -3.066479806614716e+01,
                 2.506628277459239e+00
            };

            double[] b = {
                -5.447609879822406e+01,
                 1.615858368580409e+02,
                -1.556989798598866e+02,
                 6.680131188771972e+01,
                -1.328068155288572e+01
            };

            double[] c = {
                -7.784894002430293e-03,
                -3.223964580411365e-01,
                -2.400758277161838e+00,
                -2.549732539343734e+00,
                 4.374664141464968e+00,
                 2.938163982698783e+00
            };

            double[] d = {
                 7.784695709041462e-03,
                 3.224671290700398e-01,
                 2.445134137142996e+00,
                 3.754408661907416e+00
            };

            double q = probability - 0.5;
            double r;

            // Область центральной части (|q| <= 0.425)
            if (Math.Abs(q) <= 0.425)
            {
                r = 0.180625 - q * q;
                return q * (((((a[5] * r + a[4]) * r + a[3]) * r + a[2]) * r + a[1]) * r + a[0]) /
                         ((((b[4] * r + b[3]) * r + b[2]) * r + b[1]) * r + b[0]);
            }

            // Область хвостов
            r = (q < 0) ? probability : 1 - probability;
            r = Math.Sqrt(-Math.Log(r));

            double result = (((((c[5] * r + c[4]) * r + c[3]) * r + c[2]) * r + c[1]) * r + c[0]) /
                            ((((d[3] * r + d[2]) * r + d[1]) * r + d[0]));

            return (q < 0) ? -result : result;
        }

        /// Вычисление границ доверительного интервала
        public static (double lower, double upper) ComputeBounds(
            double mean,
            double stdDev,
            double falseAlarmProb)
        {
            double lower, upper;

            // Если нет разброса
            if (stdDev < 0.001)
            {
                lower = mean - 5;
                upper = mean + 5;
            }
            else
            {
                double k = NormalQuantile(1 - falseAlarmProb / 2);
                k = Math.Min(k, 5.0);

                lower = mean - k * stdDev;
                upper = mean + k * stdDev;
            }

            // Обрезаем до физических пределов ВСЕГДА
            lower = Math.Max(0, lower);
            upper = Math.Min(255, upper);

            return (lower, upper);
        }

        /// Функция сегментации: проверяет, является ли пиксель объектом
        public static bool SegmentPixel(double brightness, double lower, double upper, double stdDev)
        {
            // Если есть разброс в окрестности 
            if (stdDev > 10) 
            {
                // Обрезаем границы
                double effectiveLower = Math.Max(0, lower);
                double effectiveUpper = Math.Min(255, upper);

                return brightness <= effectiveLower || brightness >= effectiveUpper;
            }

            // Если контраста нет - используем обычную проверку
            return brightness < lower || brightness > upper;
        }
    }
}