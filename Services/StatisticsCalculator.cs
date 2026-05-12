using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace PointObjectDetection.Core
{
    public static class StatisticsCalculator
    {
        //Расчет среднего значения и стандартного отклонения по окрестности пикселя
        public static unsafe (double mean, double stdDev) CalculateStatistics(
            Bitmap image, int centerX, int centerY, int windowSize,
            bool[,] damageMask)
        {
            int width = image.Width;
            int height = image.Height;
            int radius = windowSize / 2;
            List<double> values = new List<double>(windowSize * windowSize);

            //Блокировка битмап в памяти для быстрого доступа
            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                byte* ptr = (byte*)bmpData.Scan0.ToPointer();
                int stride = bmpData.Stride;

                //Перебор пикселей в заданной окрестности
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int y = centerY + dy;
                    if (y < 0 || y >= height) continue;

                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        //Исключение центрального пикселя, который может быть объектом
                        if (dx == 0 && dy == 0) continue;

                        int x = centerX + dx;
                        if (x < 0 || x >= width) continue;

                        //Пропускаем повреждённые пиксели
                        if (damageMask != null && damageMask[x, y]) continue;

                        //Прямой доступ к байтам через указатель
                        byte* pixel = ptr + y * stride + x * 3;
                        double brightness = (pixel[2] + pixel[1] + pixel[0]) / 3.0;
                        values.Add(brightness);
                    }
                }
            }
            finally
            {
                //Разблокируем памяти
                image.UnlockBits(bmpData);
            }

            if (values.Count == 0) return (0, 0);

            //Вычисление среднего
            double sum = 0;
            foreach (double val in values) sum += val;
            double mean = sum / values.Count;
            
            //Вычисление СКО
            double sumSquaredDiff = 0;
            foreach (double val in values)
                sumSquaredDiff += (val - mean) * (val - mean);
            double variance = sumSquaredDiff / values.Count;
            double stdDev = Math.Sqrt(variance);    

            return (mean, stdDev);
        }

        //Перебор всех пикселей изображения и применение функции сегментации
        public static bool[,] IterateAllPixels(
            Bitmap image,
            bool[,] damageMask,
            int windowSize,
            int objectSide,
            Func<int, int, double, double, bool> segmentationFunc)
        {
            int width = image.Width;
            int height = image.Height;
            bool[,] resultMask = new bool[width, height];

            //Построчный обход всех пикселей изображения
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (damageMask != null && damageMask[x, y])
                        continue;

                    //Получение статистики по окрестности
                    var (mean, stdDev) = CalculateStatistics(image, x, y, windowSize, damageMask);

                    //Вызов функции сегментации от Разработчика 4,
                    resultMask[x, y] = segmentationFunc(x, y, mean, stdDev);
                }
            }

            return resultMask;
        }
    }
}