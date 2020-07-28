using System;
using System.Collections.Generic;
using LogicUtilities;

namespace TruthTable
{
    internal class Program
    {
        private static void Main() //(p->r)|(!s->!t)|(!u->v)
        {
            Console.Title = "TruthTable By Xin Shiyu";
            Console.WriteLine("Please enter an expression,\n" +
                "! stands for not, & stands for and, and | stands for or.\n" +
                "Use lowercase letters as variables.\n" +
                "Parentheses can be used as well.");
            for(; ; )
            {
                string exp = Console.ReadLine();
                if (exp == null || exp.Length == 0) break;
                var tree = Parser.Parse(exp, out var dict);
                List<List<string>> table = new List<List<string>>();
                List<Proposition> expPlaceList = new List<Proposition>();
                int count = 0;
                table.Add(new List<string>());
                makeTruthDict(tree);
                void makeTruthDict(Proposition node)
                {
                    ++count;
                    table[0].Add(node.ToString());
                    expPlaceList.Add(node);
                    if (node is Compound)
                    {
                        (node as Compound).Children.ForEach(x => makeTruthDict(x));
                    }
                }
                for (int i = 0; i < (1 << dict.Count); ++i)
                {
                    table.Add(new List<string>());
                    int j = 0;
                    foreach (var pair in dict)
                    {
                        pair.Value.Truth = GetBit(i, j);
                        ++j;
                    }
                    foreach (Proposition e in expPlaceList)
                    {
                        table[^1].Add(e.Evaluate? "T" : "F");
                    }
                }
                foreach (List<string> row in table)
                {
                    foreach (string col in row)
                    {
                        Console.Write("{0} ", col);
                    }
                    Console.WriteLine();
                }
            }
        }

        private static bool GetBit(int val, int place)
        {
            return ((val >> place) & 1) == 1;
        }

    }
}
