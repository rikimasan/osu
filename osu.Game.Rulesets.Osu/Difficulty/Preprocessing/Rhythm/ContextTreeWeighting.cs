// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing.Rhythm
{
    public class CtwNode
    {
        private readonly int alphabetSize;
        private readonly int[] counts;
        private double logProbKT;
        private double logProbWeighted;
        private CtwNode?[]? children;

        private int totalCount;

        public CtwNode(int alphabetSize)
        {
            this.alphabetSize = alphabetSize;
            counts = new int[alphabetSize];
        }

        /// <summary>
        /// Returns the KT-estimated log-probability of the symbol before updating counts.
        /// KT estimator: P(s) = (n_s + 0.5) / (n + K/2)
        /// </summary>
        public double UpdateKT(int symbol)
        {
            double prob = (counts[symbol] + 0.5) / (totalCount + alphabetSize / 2.0);
            double logProb = Math.Log(prob);

            logProbKT += logProb;
            counts[symbol]++;
            totalCount++;

            return logProb;
        }

        public CtwNode GetOrCreateChild(int symbol)
        {
            children ??= new CtwNode[alphabetSize];
            return children[symbol] ??= new CtwNode(alphabetSize);
        }
    }

    public class ContextTreeWeighting
    {
        private readonly int maxDepth;
        private readonly int alphabetSize;
        private readonly CtwNode root;
        private readonly int[] contextBuffer;
        private int bufferCount;

        public ContextTreeWeighting(int maxDepth, int alphabetSize)
        {
            this.maxDepth = maxDepth;
            this.alphabetSize = alphabetSize;
            root = new CtwNode(alphabetSize);
            contextBuffer = new int[maxDepth];
        }
    }
}
