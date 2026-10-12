using Pixeller.Models.Canvas;
using System;
using System.Collections.Generic;

namespace Pixeller.AppServices.Bitmap.Quantization;

public static class Octree
{
    private const int MaxDepth = 8;

    private struct Node 
    {
        public long R, G, B;
        public int Count; 
        public bool IsLeaf; 
    }

    public static Palette GetPalette(Dictionary<uint, int> histogram, int colorCount, int transparentWeight = 0)
    {
        var slots = Math.Max(1, transparentWeight > 0 ? colorCount - 1 : colorCount);

        var nodes = new Node[4096];
        var children = new int[4096 * 8];
        var nodeCount = 1;

        var levels = new List<int>[MaxDepth];
        for (var i = 0; i < MaxDepth; i++) levels[i] = [];

        var leafCount = 0;

        foreach (var (key, weight) in histogram)
        {
            var r = (key >> 16) & 0xFF;
            var g = (key >> 8) & 0xFF;
            var b = key & 0xFF;

            var node = 0;
            nodes[0].Count += weight;

            for (var d = 0; d < MaxDepth; d++)
            {
                var shift = 7 - d;
                var idx = (int)((((r >> shift) & 1) << 2) | (((g >> shift) & 1) << 1) | ((b >> shift) & 1));
                var slot = node * 8 + idx;
                var child = children[slot];

                if (child == 0)
                {
                    if (nodeCount == nodes.Length)
                    {
                        Array.Resize(ref nodes, nodeCount * 2);
                        Array.Resize(ref children, nodeCount * 16);
                    }

                    child = nodeCount++;
                    children[slot] = child;

                    if (d == MaxDepth - 1)
                    {
                        nodes[child].IsLeaf = true;
                        leafCount++;
                    }
                    else
                        levels[d + 1].Add(child);
                }

                nodes[child].Count += weight;
                node = child;
            }

            nodes[node].R += r * weight;
            nodes[node].G += g * weight;
            nodes[node].B += b * weight;
        }

        for (var level = MaxDepth - 1; level >= 1 && leafCount > slots; level--)
        {
            var list = levels[level];
            list.Sort((a, b) => nodes[a].Count.CompareTo(nodes[b].Count));

            foreach (var n in list)
            {
                if (leafCount <= slots) break;

                var merged = 0;
                for (var c = 0; c < 8; c++)
                {
                    var ch = children[n * 8 + c];
                    if (ch == 0) continue;

                    nodes[n].R += nodes[ch].R;
                    nodes[n].G += nodes[ch].G;
                    nodes[n].B += nodes[ch].B;
                    children[n * 8 + c] = 0;
                    merged++;
                }

                nodes[n].IsLeaf = true;
                leafCount += 1 - merged;
            }
        }

        var candidates = new Dictionary<uint, int>();
        var stack = new Stack<int>();
        stack.Push(0);

        while (stack.Count > 0)
        {
            var n = stack.Pop();
            ref var node = ref nodes[n];

            if (node.IsLeaf)
            {
                if (node.Count > 0)
                {
                    var key = QuantizationService.Pack(
                        (byte)(node.R / node.Count),
                        (byte)(node.G / node.Count),
                        (byte)(node.B / node.Count));

                    candidates[key] = candidates.GetValueOrDefault(key) + node.Count;
                }
                continue;
            }

            for (var c = 0; c < 8; c++)
            {
                var ch = children[n * 8 + c];
                if (ch != 0) stack.Push(ch);
            }
        }

        return QuantizationService.BuildPalette(candidates, transparentWeight, colorCount);
    }
}