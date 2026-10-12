using Pixeller.Models.Canvas;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pixeller.AppServices.Bitmap.Quantization;

public class KMeans
{
    private sealed class Accumulator(int clusterCount)
    {
        public readonly double[] SumR = new double[clusterCount];
        public readonly double[] SumG = new double[clusterCount];
        public readonly double[] SumB = new double[clusterCount];
        public readonly long[] SumW = new long[clusterCount];
        public int ChangedCount;
    }

    public static Palette GetPalette(Dictionary<uint, int> histogram, int colorCount, int transW = 0, int maxIter = 20)
    {
        var colorsCount = histogram.Count;
        var clusterCount = Math.Max(1, transW > 0 ? colorCount - 1 : colorCount);

        if (colorsCount <= clusterCount)
            return QuantizationService.BuildPalette(histogram, transW, colorCount);

        var (colorsR, colorsG, colorsB, colorsWeight) = Reduce(histogram);
        colorsCount = colorsR.Length;

        var seed = MedianCut.GetPalette(histogram, clusterCount).Colors;
        clusterCount = seed.Count;

        var centerR = new float[clusterCount];
        var centerG = new float[clusterCount];
        var centerB = new float[clusterCount];

        for (var j = 0; j < clusterCount; j++)
        {
            centerR[j] = seed[j].R;
            centerG[j] = seed[j].G;
            centerB[j] = seed[j].B;
        }

        var assign = new int[colorsCount];
        Array.Fill(assign, -1);

        const int chunkSize = 4096;
        var chunkCount = (colorsCount + chunkSize - 1) / chunkSize;

        var total = new Accumulator(clusterCount);
        var sync = new object();

        for (var iter = 0; iter < maxIter; iter++)
        {
            total = new Accumulator(clusterCount);

            Parallel.For(0, chunkCount, () => new Accumulator(clusterCount), (chunk, _, local) =>
            {
                var end = Math.Min(colorsCount, (chunk + 1) * chunkSize);

                for (var i = chunk * chunkSize; i < end; i++)
                {
                    var r = colorsR[i];
                    var g = colorsG[i];
                    var b = colorsB[i];

                    var bestCluster = 0;
                    var bestDistance = float.MaxValue;

                    for (var j = 0; j < clusterCount; j++)
                    {
                        var dr = r - centerR[j];
                        var dg = g - centerG[j];
                        var db = b - centerB[j];
                        var distance = dr * dr + dg * dg + db * db;

                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestCluster = j;
                        }
                    }

                    if (assign[i] != bestCluster)
                    {
                        assign[i] = bestCluster;
                        local.ChangedCount++;
                    }

                    var weight = colorsWeight[i];
                    local.SumR[bestCluster] += r * weight;
                    local.SumG[bestCluster] += g * weight;
                    local.SumB[bestCluster] += b * weight;
                    local.SumW[bestCluster] += weight;
                }

                return local;
            }, local =>
            {
                lock (sync)
                {
                    total.ChangedCount += local.ChangedCount;

                    for (var j = 0; j < clusterCount; j++)
                    {
                        total.SumR[j] += local.SumR[j];
                        total.SumG[j] += local.SumG[j];
                        total.SumB[j] += local.SumB[j];
                        total.SumW[j] += local.SumW[j];
                    }
                }
            });

            for (var j = 0; j < clusterCount; j++)
            {
                if (total.SumW[j] == 0) continue;

                centerR[j] = (float)(total.SumR[j] / total.SumW[j]);
                centerG[j] = (float)(total.SumG[j] / total.SumW[j]);
                centerB[j] = (float)(total.SumB[j] / total.SumW[j]);
            }

            if (total.ChangedCount < colorsCount * 0.01) break;
        }

        var candidates = new Dictionary<uint, int>(clusterCount);
        for (var j = 0; j < clusterCount; j++)
        {
            if (total.SumW[j] == 0) continue;

            var key = QuantizationService.Pack(
                (byte)Math.Round(centerR[j]),
                (byte)Math.Round(centerG[j]),
                (byte)Math.Round(centerB[j]));

            candidates[key] = (int)total.SumW[j];
        }

        return QuantizationService.BuildPalette(candidates, transW, colorCount);
    }

    private static (float[] r, float[] g, float[] b, int[] w) Reduce(Dictionary<uint, int> histogram)
    {
        var buckets = new Dictionary<uint, (long r, long g, long b, int w)>();

        foreach (var (key, weight) in histogram)
        {
            ref var bucket = ref CollectionsMarshal.GetValueRefOrAddDefault(buckets, key & 0xFFF8F8F8u, out _);
            bucket.r += (byte)(key >> 16) * (long)weight;
            bucket.g += (byte)(key >> 8) * (long)weight;
            bucket.b += (byte)key * (long)weight;
            bucket.w += weight;
        }

        var r = new float[buckets.Count];
        var g = new float[buckets.Count];
        var b = new float[buckets.Count];
        var w = new int[buckets.Count];

        var i = 0;
        foreach (var bucket in buckets.Values)
        {
            r[i] = (float)bucket.r / bucket.w;
            g[i] = (float)bucket.g / bucket.w;
            b[i] = (float)bucket.b / bucket.w;
            w[i++] = bucket.w;
        }

        return (r, g, b, w);
    }
}