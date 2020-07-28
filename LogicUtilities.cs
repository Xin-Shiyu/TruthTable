using System;
using System.Collections.Generic;

namespace LogicUtilities
{
    internal enum OperatorType
    {
        And,
        Or,
        Not,
        Imp,
        DCon
    }

    internal abstract class Proposition
    {
        public abstract bool? Truth { get; set; }
        public abstract bool Evaluate { get; }
    }

    internal class Compound : Proposition
    {
        public OperatorType Type;
        public List<Proposition> Children;
        public override bool? Truth
        {
            get
            {
                return Evaluate;
            }

            set
            {
                throw new Exception("不允许给非原子节点设置真值");
            }
        }

        public override bool Evaluate => Type switch
        {
            OperatorType.Not => !Children[0].Evaluate,
            OperatorType.And => Children[0].Evaluate && Children[1].Evaluate,
            OperatorType.Or => Children[0].Evaluate || Children[1].Evaluate,
            OperatorType.Imp => !Children[0].Evaluate || Children[1].Evaluate,
            OperatorType.DCon => Children[0].Evaluate == Children[1].Evaluate,
            _ => throw new Exception("绝对不会报这个错的")
        };

        public Compound(OperatorType type)
        {
            Type = type;
        }
        public override string ToString() => Type switch
        {
            OperatorType.Not => "(!" + Children[0].ToString() + ")",
            OperatorType.And => "(" + Children[0].ToString() + "&" + Children[1].ToString() + ")",
            OperatorType.Or => "(" + Children[0].ToString() + "|" + Children[1].ToString() + ")",
            OperatorType.Imp => "(" + Children[0].ToString() + "->" + Children[1].ToString() + ")",
            OperatorType.DCon => "(" + Children[0].ToString() + "<->" + Children[1].ToString() + ")",
            _ => throw new Exception("绝对不会报这个错的")
        };
    }

    internal class Atom : Proposition
    {
        public char Name;
        public override bool? Truth { get; set; }
        public Atom(char name, bool? truth = null)
        {
            Name = name;
            Truth = truth;
        }
        public override bool Evaluate => Truth switch
        {
            null => throw new Exception("Truth of atom not determined."),
            _ => (bool)Truth
        };

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    internal static class Parser
    {
        private static List<string> ToPostfix(string s)
        {
            // 优先级由大到小：! & | -> <->
            Stack<string> stack = new Stack<string>();
            List<string> list = new List<string>();
            char lastChar = '\0';
            char lastB2Char = '\0';
            foreach (char c in s)
            {
                switch (c)
                {
                    case '(':
                        stack.Push("(");
                        break;
                    case ')':
                        while (!(stack.Count == 0 || stack.Peek() == "("))
                        {
                            list.Add(stack.Pop());
                        }
                        if (stack.Count != 0 && stack.Peek() == "(")
                        {
                            stack.Pop();
                        }

                        break;
                    case '!':
                        stack.Push("!");
                        break;
                    case '&':
                        while (stack.Count != 0 && (stack.Peek() == "!"))
                        {
                            list.Add(stack.Pop());
                        }
                        stack.Push("&");
                        break;
                    case '|':
                        while (stack.Count != 0 && (stack.Peek() == "&" || stack.Peek() == "!"))
                        {
                            list.Add(stack.Pop());
                        }
                        stack.Push("|");
                        break;
                    case '>':
                        if (lastChar == '-')
                        {
                            if (lastB2Char == '<')
                            {
                                while (stack.Count != 0 && (stack.Peek() == "&" || stack.Peek() == "|" || stack.Peek() == "->" || stack.Peek() == "!"))
                                {
                                    list.Add(stack.Pop());
                                }
                                stack.Push("<->");
                            }
                            else
                            {
                                while (stack.Count != 0 && (stack.Peek() == "&" || stack.Peek() == "|" || stack.Peek() == "!"))
                                {
                                    list.Add(stack.Pop());
                                }
                                stack.Push("->");
                            }
                        }
                        break;
                    case '<':
                    case '-':
                        break; //忽略，我们不在这里处理
                    default:
                        list.Add(c.ToString());
                        break;
                }
                lastB2Char = lastChar;
                lastChar = c;
            }
            while (stack.Count != 0)
            {
                list.Add(stack.Pop());
            }
            return list;
        }

        private static Proposition CreateTreeFromList(List<string> list, ref Dictionary<char, Atom> dict)
        {
            Stack<Proposition> stackProp = new Stack<Proposition>();
            foreach (string token in list)
            {
                switch (token)
                {
                    case "&":
                        stackProp.Push(new Compound(OperatorType.And)
                        {
                            Children = new List<Proposition>
                            {
                                stackProp.Pop(),
                                stackProp.Pop()
                            }
                        });
                        break;
                    case "|":
                        stackProp.Push(new Compound(OperatorType.Or)
                        {
                            Children = new List<Proposition>
                            {
                                stackProp.Pop(),
                                stackProp.Pop()
                            }
                        });
                        break;
                    case "!":
                        stackProp.Push(new Compound(OperatorType.Not)
                        {
                            Children = new List<Proposition>
                            {
                                stackProp.Pop(),
                            }
                        });
                        break;
                    case "->":
                        Proposition prop2 = stackProp.Pop(); //维持顺序
                        Proposition prop1 = stackProp.Pop();
                        stackProp.Push(new Compound(OperatorType.Imp)
                        {
                            Children = new List<Proposition>
                            {
                                prop1,
                                prop2
                            }
                        });
                        break;
                    case "<->":
                        stackProp.Push(new Compound(OperatorType.DCon)
                        {
                            Children = new List<Proposition>
                            {
                                stackProp.Pop(),
                                stackProp.Pop()
                            }
                        });
                        break;
                    default:
                        if (dict[token[0]] != null)
                        {
                            stackProp.Push(dict[token[0]]);
                        }
                        else
                        {
                            dict[token[0]] = new Atom(token[0]);
                            stackProp.Push(dict[token[0]]);
                        }
                        break;
                }
            }
            return stackProp.Pop();
        }

        private const string abc = "abcdefghijklmnopqrstuvwxyz";

        public static Proposition Parse(string exp, out Dictionary<char, Atom> dict)
        {
            bool[] map = new bool[26];
            dict = new Dictionary<char, Atom>();
            foreach (char c in exp)
            {
                if (abc.Contains(c) && !map[(int)c - 97])
                {
                    map[(int)c - 97] = true;
                    dict.Add(c, null);
                }
            }
            return CreateTreeFromList(ToPostfix(exp), ref dict);
        }
    }
}