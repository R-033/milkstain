using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Milkstain
{
    public class Equations
    {
        const int StackSize = 2048;
        static readonly float EPSILON = Mathf.Epsilon;

        private static readonly float[] Stack = new float[StackSize];

        public static Action<State> Compile(string code)
        {
            return CompileEquation(code.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        }

        delegate float Func1(float x);
        delegate float Func2(float x, float y);
        delegate float Func3(float x, float y, float z);

        static readonly Dictionary<string, Func1> Funcs1Arg = new Dictionary<string, Func1>
        {
            {"sqr", Func_Sqr},
            {"sqrt", Func_Sqrt},
            {"sign", Func_Sign},
            {"rand", Func_Rand},
            {"bnot", Func_Bnot},
            {"abs", Func_Abs},
            {"int", Func_Int},
            {"log", Mathf.Log},
            {"log10", Mathf.Log10},
            {"sin", Mathf.Sin},
            {"cos", Mathf.Cos},
            {"tan", Mathf.Tan},
            {"asin", Mathf.Asin},
            {"acos", Mathf.Acos},
            {"atan", Mathf.Atan},
            {"exp", Mathf.Exp}
        };

        static readonly Dictionary<string, Func2> Funcs2Arg = new Dictionary<string, Func2>
        {
            {"pow", Func_Pow},
            {"sigmoid", Func_Sigmoid},
            {"bor", Func_Bor},
            {"band", Func_Band},
            {"equal", Func_Equal},
            {"above", Func_Above},
            {"below", Func_Below},
            {"min", Func_Min},
            {"max", Func_Max},
            {"atan2", Mathf.Atan2}
        };

        static float Func_Int(float x)
        {
            return (int)x;
        }

        static float Func_Abs(float x)
        {
            if (x < 0f)
            {
                return -x;
            }

            return x;
        }

        static float Func_Sqr(float x)
        {
            return x * x;
        }

        static float Func_Sqrt(float x)
        {
            if (x < 0f)
            {
                return Mathf.Sqrt(-x);
            }

            return Mathf.Sqrt(x);
        }

        static float Func_Pow(float x, float y)
        {
            float result = Mathf.Pow(x, y);

            if (!float.IsInfinity(result) && !float.IsNaN(result))
            {
                return result;
            }

            return 0f;
        }

        static float Func_Sign(float x)
        {
            if (Func_Abs(x) < EPSILON)
            {
                return 0f;
            }

            return x < 0f ? -1f : 1f;
        }

        static float Func_Min(float x, float y)
        {
            return x < y ? x : y;
        }

        static float Func_Max(float x, float y)
        {
            return x > y ? x : y;
        }

        static float Func_Sigmoid(float x, float y)
        {
            float t = 1f + Mathf.Exp(-x * y);
            return Func_Abs(t) > EPSILON ? 1f / t : 0f;
        }

        static float Func_Rand(float x)
        {
            return UnityEngine.Random.Range(0, (int)x);
        }

        static float Func_Bor(float x, float y)
        {
            return Func_Abs(x) > EPSILON || Func_Abs(y) > EPSILON ? 1f : 0f;
        }

        static float Func_Bnot(float x)
        {
            return Func_Abs(x) < EPSILON ? 1f : 0f;
        }

        static float Func_Band(float x, float y)
        {
            return Func_Abs(x) > EPSILON && Func_Abs(y) > EPSILON ? 1f : 0f;
        }

        static float Func_Equal(float x, float y)
        {
            return Func_Abs(x - y) < EPSILON ? 1f : 0f;
        }

        static float Func_Above(float x, float y)
        {
            return x > y ? 1f : 0f;
        }

        static float Func_Below(float x, float y)
        {
            return x < y ? 1f : 0f;
        }

        private static List<string> TokenizeExpression(string Expression)
        {
            List<string> tokens = new List<string>();

            if (string.IsNullOrWhiteSpace(Expression))
            {
                return tokens;
            }

            int eqIndex = Expression.IndexOf('=');

            if (eqIndex < 0)
            {
                //Debug.LogError("no assignment in expression " + Expression);
                return tokens;
            }

            tokens.Add(Expression.Substring(0, eqIndex));

            Expression = Expression.Substring(eqIndex + 1);

            string tokenBuffer = "";

            foreach (char c in Expression)
            {
                if (c == '+' || c == '-' || c == '*' || c == '/' || c == '(' || c == ')' || c == ',' || c == '%' || c == '|' || c == '&')
                {
                    if (tokenBuffer != "")
                    {
                        tokens.Add(tokenBuffer);
                        tokenBuffer = "";
                    }

                    tokens.Add(c.ToString());

                    continue;
                }

                tokenBuffer += c;
            }

            if (tokenBuffer != "")
            {
                tokens.Add(tokenBuffer);
            }

            return tokens;
        }

        private static Action<State> CompileEquation(List<List<string>> Equation)
        {
            var stack = Stack;

            List<Action<State>> actionsList = new List<Action<State>>();
            
            foreach (var line in Equation)
            {
                if (line.Count == 0)
                    continue;
                
                string varName = line[0];

                State.RegisterVariable(varName);
                int varIndex = State.VariableNameTable[varName];

                int stackIndex = 0;

                string finalToken = CompileExpression(line.Skip(1).ToList(), actionsList, ref stackIndex);

                int finalType = ParseVariable(finalToken, out int finalIndex, out float finalValue);

                switch (finalType)
                {
                    case 0:
                        actionsList.Add((State Variables) =>
                        {
                            Variables.Set(varIndex, stack[finalIndex]);
                        });
                        break;
                    case 1:
                        actionsList.Add((State Variables) =>
                        {
                            Variables.Set(varIndex, finalValue);
                        });
                        break;
                    case 2:
                        actionsList.Add((State Variables) =>
                        {
                            Variables.Set(varIndex, Variables.Heap[finalIndex]);
                        });
                        break;
                }
            }

            var actionArray = actionsList.ToArray();

            return (State Variables) =>
            {
                for (int i = 0; i < actionArray.Length; i++)
                {
                    actionArray[i](Variables);
                }
            };
        }

        private static int ParseVariable(string token, out int index, out float value)
        {
            if (token[0] == '#')
            {
                index = int.Parse(token.Substring(1));
                value = 0f;

                return 0;
            }

            if (token[0] == '.' || token[0] == '-' || char.IsDigit(token[0]))
            {
                index = 0;

                if (token == ".")
                {
                    token = "0";
                }

                if (!float.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                {
                    Debug.LogError("Invalid variable number: " + token);
                }

                return 1;
            }

            State.RegisterVariable(token);
            index = State.VariableNameTable[token];
            value = 0f;

            return 2;
        }

        private static string CompileExpression(List<string> Tokens, List<Action<State>> actionsList, ref int stackIndex)
        {
            var stack = Stack;

            string debugOut = "";

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                debugOut += Tokens[tokenNum] + ", ";
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == ")")
                {
                    throw new System.Exception("Unmatched closing parenthesis: " + debugOut);
                }
                
                if (token == "(")
                {
                    bool success = false;
                    bool isFunction = false;

                    if (tokenNum > 0)
                    {
                        string prev = Tokens[tokenNum - 1];

                        if (prev != "*" && prev != "/" && prev != "+" && prev != "-" && prev != "%" && prev != "|" && prev != "&")
                        {
                            isFunction = true;
                        }
                    }

                    int depth = 0;

                    for (int tokenNum2 = tokenNum + 1; tokenNum2 < Tokens.Count; tokenNum2++)
                    {
                        string token2 = Tokens[tokenNum2];

                        if (token2 == "(")
                        {
                            depth++;

                            continue;
                        }

                        if (token2 == ")")
                        {
                            if (depth == 0)
                            {
                                if (isFunction)
                                {
                                    List<List<string>> arguments = new List<List<string>>();

                                    arguments.Add(new List<string>());

                                    int depth2 = 0;

                                    for (int i = tokenNum + 1; i < tokenNum2; i++)
                                    {
                                        string token3 = Tokens[i];

                                        if (token3 == "(")
                                        {
                                            depth2++;
                                        }
                                        else if (token3 == ")")
                                        {
                                            depth2--;
                                        }

                                        if (depth2 == 0 && token3 == ",")
                                        {
                                            arguments.Add(new List<string>());

                                            continue;
                                        }

                                        arguments[arguments.Count - 1].Add(token3);
                                    }

                                    List<string> argumentValues = new List<string>();

                                    foreach (List<string> argument in arguments)
                                    {
                                        argumentValues.Add(CompileExpression(argument, actionsList, ref stackIndex));
                                    }

                                    string functionName = Tokens[tokenNum - 1];

                                    int funcIndex = stackIndex++;
                                    string funcId = "#" + funcIndex;

                                    switch (arguments.Count)
                                    {
                                        case 1:
                                            var func1 = Funcs1Arg[functionName];
                                            int arg1_0 = ParseVariable(argumentValues[0], out int arg1_0_Index, out float arg1_0_Value);

                                            switch (arg1_0)
                                            {
                                                case 0:
                                                    actionsList.Add((State Variables) =>
                                                    {
                                                        stack[funcIndex] = func1(stack[arg1_0_Index]);
                                                    });
                                                    break;
                                                case 1:
                                                    float result = func1(arg1_0_Value);
                                                    actionsList.Add((State Variables) =>
                                                    {
                                                        stack[funcIndex] = func1(arg1_0_Value);
                                                    });
                                                    break;
                                                case 2:
                                                    actionsList.Add((State Variables) =>
                                                    {
                                                        stack[funcIndex] = func1(Variables.Heap[arg1_0_Index]);
                                                    });
                                                    break;
                                            }

                                            break;
                                        case 2:
                                            var func2 = Funcs2Arg[functionName];
                                            int arg2_0 = ParseVariable(argumentValues[0], out int arg2_0_Index, out float arg2_0_Value);
                                            int arg2_1 = ParseVariable(argumentValues[1], out int arg2_1_Index, out float arg2_1_Value);

                                            switch (arg2_0)
                                            {
                                                case 0:
                                                    switch (arg2_1)
                                                    {
                                                        case 0:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(stack[arg2_0_Index], stack[arg2_1_Index]);
                                                            });
                                                            break;
                                                        case 1:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(stack[arg2_0_Index], arg2_1_Value);
                                                            });
                                                            break;
                                                        case 2:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(stack[arg2_0_Index], Variables.Heap[arg2_1_Index]);
                                                            });
                                                            break;
                                                    }
                                                    break;
                                                case 1:
                                                    switch (arg2_1)
                                                    {
                                                        case 0:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(arg2_0_Value, stack[arg2_1_Index]);
                                                            });
                                                            break;
                                                        case 1:
                                                            float result = func2(arg2_0_Value, arg2_1_Value);
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = result;
                                                            });
                                                            break;
                                                        case 2:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(arg2_0_Value, Variables.Heap[arg2_1_Index]);
                                                            });
                                                            break;
                                                    }
                                                    break;
                                                case 2:
                                                    switch (arg2_1)
                                                    {
                                                        case 0:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(Variables.Heap[arg2_0_Index], stack[arg2_1_Index]);
                                                            });
                                                            break;
                                                        case 1:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(Variables.Heap[arg2_0_Index], arg2_1_Value);
                                                            });
                                                            break;
                                                        case 2:
                                                            actionsList.Add((State Variables) =>
                                                            {
                                                                stack[funcIndex] = func2(Variables.Heap[arg2_0_Index], Variables.Heap[arg2_1_Index]);
                                                            });
                                                            break;
                                                    }
                                                    break;
                                            }

                                            break;
                                        case 3:
                                            int arg3_0 = ParseVariable(argumentValues[0], out int arg3_0_Index, out float arg3_0_Value);
                                            int arg3_1 = ParseVariable(argumentValues[1], out int arg3_1_Index, out float arg3_1_Value);
                                            int arg3_2 = ParseVariable(argumentValues[2], out int arg3_2_Index, out float arg3_2_Value);

                                            switch (arg3_0)
                                            {
                                                case 0:
                                                    switch (arg3_1)
                                                    {
                                                        case 0:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 1:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? arg3_1_Value : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? arg3_1_Value : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? arg3_1_Value : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 2:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(stack[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                    }
                                                    break;
                                                case 1:
                                                    switch (arg3_1)
                                                    {
                                                        case 0:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? stack[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? stack[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? stack[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 1:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? arg3_1_Value : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    float result = Func_Abs(arg3_0_Value) > 0f ? arg3_1_Value : arg3_2_Value;
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = result;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = arg3_0_Value != 0f ? arg3_1_Value : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 2:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? Variables.Heap[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? Variables.Heap[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(arg3_0_Value) > 0f ? Variables.Heap[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                    }
                                                    break;
                                                case 2:
                                                    switch (arg3_1)
                                                    {
                                                        case 0:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? stack[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 1:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? arg3_1_Value : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? arg3_1_Value : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? arg3_1_Value : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                        case 2:
                                                            switch (arg3_2)
                                                            {
                                                                case 0:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : stack[arg3_2_Index];
                                                                    });
                                                                    break;
                                                                case 1:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : arg3_2_Value;
                                                                    });
                                                                    break;
                                                                case 2:
                                                                    actionsList.Add((State Variables) =>
                                                                    {
                                                                        stack[funcIndex] = Func_Abs(Variables.Heap[arg3_0_Index]) > 0f ? Variables.Heap[arg3_1_Index] : Variables.Heap[arg3_2_Index];
                                                                    });
                                                                    break;
                                                            }
                                                            break;
                                                    }
                                                    break;
                                            }

                                            break;
                                    }

                                    Tokens.RemoveRange(tokenNum - 1, tokenNum2 - tokenNum + 1);

                                    Tokens[tokenNum - 1] = funcId;
                                    
                                    tokenNum--;
                                }
                                else
                                {
                                    int funcIndex = stackIndex++;
                                    string funcId = "#" + funcIndex;

                                    string resultToken = CompileExpression(Tokens.Skip(tokenNum + 1).Take(tokenNum2 - tokenNum - 1).ToList(), actionsList, ref stackIndex);

                                    int resultType = ParseVariable(resultToken, out int resultIndex, out float resultValue);

                                    switch (resultType)
                                    {
                                        case 0:
                                            actionsList.Add((State Variables) =>
                                            {
                                                stack[funcIndex] = stack[resultIndex];
                                            });
                                            break;
                                        case 1:
                                            actionsList.Add((State Variables) =>
                                            {
                                                stack[funcIndex] = resultValue;
                                            });
                                            break;
                                        case 2:
                                            actionsList.Add((State Variables) =>
                                            {
                                                stack[funcIndex] = Variables.Heap[resultIndex];
                                            });
                                            break;
                                    }

                                    Tokens.RemoveRange(tokenNum, tokenNum2 - tokenNum);

                                    Tokens[tokenNum] = funcId;
                                }

                                success = true;

                                break;
                            }
                            else
                            {
                                depth--;

                                continue;
                            }
                        }
                    }

                    if (!success)
                    {
                        throw new System.Exception("Unmatched opening parenthesis: " + debugOut);
                    }

                    continue;
                }
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == "+")
                {
                    if (tokenNum == 0 || Tokens[tokenNum - 1] == "*" || Tokens[tokenNum - 1] == "/" || Tokens[tokenNum - 1] == "+" || Tokens[tokenNum - 1] == "-" || Tokens[tokenNum - 1] == "%" || Tokens[tokenNum - 1] == "|" || Tokens[tokenNum - 1] == "&")
                    {
                        string next = Tokens[tokenNum + 1];

                        int funcIndex = stackIndex++;
                        string funcId = "#" + funcIndex;

                        int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                        switch (nextType)
                        {
                            case 0:
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = +stack[nextTypeIndex];
                                });
                                break;
                            case 1:
                                float stackResult = +nextTypeValue;
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = stackResult;
                                });
                                break;
                            case 2:
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = +Variables.Heap[nextTypeIndex];
                                });
                                break;
                        }

                        Tokens.RemoveRange(tokenNum, 1);

                        Tokens[tokenNum] = funcId;
                    }
                }

                if (token == "-")
                {
                    if (tokenNum == 0 || Tokens[tokenNum - 1] == "*" || Tokens[tokenNum - 1] == "/" || Tokens[tokenNum - 1] == "+" || Tokens[tokenNum - 1] == "-" || Tokens[tokenNum - 1] == "%" || Tokens[tokenNum - 1] == "|" || Tokens[tokenNum - 1] == "&")
                    {
                        string next = Tokens[tokenNum + 1];

                        int funcIndex = stackIndex++;
                        string funcId = "#" + funcIndex;

                        int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                        switch (nextType)
                        {
                            case 0:
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = -stack[nextTypeIndex];
                                });
                                break;
                            case 1:
                                float stackResult = -nextTypeValue;
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = stackResult;
                                });
                                break;
                            case 2:
                                actionsList.Add((State Variables) =>
                                {
                                    stack[funcIndex] = -Variables.Heap[nextTypeIndex];
                                });
                                break;
                        }

                        Tokens.RemoveRange(tokenNum, 1);

                        Tokens[tokenNum] = funcId;
                    }
                }
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == "*")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] * stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] * nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] * Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue * stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    float stackResult = prevTypeValue * nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue * Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] * stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] * nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] * Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }

                if (token == "/")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = stack[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = stack[prevTypeIndex] / div;
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = nextTypeValue;
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = stack[prevTypeIndex] / div;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = Variables.Heap[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = stack[prevTypeIndex] / div;
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = stack[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = prevTypeValue / div;
                                    });
                                    break;
                                case 1:
                                    float stackResult = nextTypeValue == 0f ? 0f : prevTypeValue / nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = Variables.Heap[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = prevTypeValue / div;
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = stack[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = Variables.Heap[prevTypeIndex] / div;
                                    });
                                    break;
                                case 1:
                                    if (nextTypeValue == 0f)
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = 0f;
                                        });
                                    }
                                    else
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = Variables.Heap[prevTypeIndex] / nextTypeValue;
                                        });
                                    }
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        float div = Variables.Heap[nextTypeIndex];
                                        if (div == 0f) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = Variables.Heap[prevTypeIndex] / div;
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }

                if (token == "%")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)stack[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = (int)stack[prevTypeIndex] % div;
                                    });
                                    break;
                                case 1:
                                    int div = (int)nextTypeValue;
                                    if (div == 0)
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = 0f;
                                        });
                                    }
                                    else
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = (int)stack[prevTypeIndex] % div;
                                        });
                                    }
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)Variables.Heap[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = (int)stack[prevTypeIndex] % div;
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            int prevTypeValueInt = (int)prevTypeValue;
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)stack[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = prevTypeValueInt % div;
                                    });
                                    break;
                                case 1:
                                    int div = (int)nextTypeValue;
                                    if (div == 0)
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = 0f;
                                        });
                                    }
                                    else
                                    {
                                        float stackResult = prevTypeValueInt % div;
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = stackResult;
                                        });
                                    }
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)Variables.Heap[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = prevTypeValueInt % div;
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)stack[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] % div;
                                    });
                                    break;
                                case 1:
                                    int div = (int)nextTypeValue;
                                    if (div == 0)
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = 0f;
                                        });
                                    }
                                    else
                                    {
                                        actionsList.Add((State Variables) =>
                                        {
                                            stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] % div;
                                        });
                                    }
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        int div = (int)Variables.Heap[nextTypeIndex];
                                        if (div == 0) stack[funcIndex] = 0f;
                                        else stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] % div;
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == "+")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] + stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] + nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] + Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue + stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    float stackResult = prevTypeValue + nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue + Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] + stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] + nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] + Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }

                if (token == "-")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] - stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] - nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stack[prevTypeIndex] - Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue - stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    float stackResult = prevTypeValue - nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValue - Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] - stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] - nextTypeValue;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = Variables.Heap[prevTypeIndex] - Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == "&")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] & (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int nextTypeValueInt = (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] & nextTypeValueInt;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] & (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            int prevTypeValueInt = (int)prevTypeValue;
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValueInt & (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int stackResult = prevTypeValueInt & (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValueInt & (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] & (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int nextTypeValueInt = (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] & nextTypeValueInt;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] & (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }
            }

            for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
            {
                string token = Tokens[tokenNum];

                if (token == "|")
                {
                    string prev = Tokens[tokenNum - 1];
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    int prevType = ParseVariable(prev, out int prevTypeIndex, out float prevTypeValue);
                    int nextType = ParseVariable(next, out int nextTypeIndex, out float nextTypeValue);

                    switch (prevType)
                    {
                        case 0:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] | (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int nextTypeValueInt = (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] | nextTypeValueInt;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)stack[prevTypeIndex] | (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 1:
                            int prevTypeValueInt = (int)prevTypeValue;
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValueInt | (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int stackResult = prevTypeValueInt | (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = stackResult;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = prevTypeValueInt | (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                        case 2:
                            switch (nextType)
                            {
                                case 0:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] | (int)stack[nextTypeIndex];
                                    });
                                    break;
                                case 1:
                                    int nextTypeValueInt = (int)nextTypeValue;
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] | nextTypeValueInt;
                                    });
                                    break;
                                case 2:
                                    actionsList.Add((State Variables) =>
                                    {
                                        stack[funcIndex] = (int)Variables.Heap[prevTypeIndex] | (int)Variables.Heap[nextTypeIndex];
                                    });
                                    break;
                            }
                            break;
                    }

                    Tokens.RemoveRange(tokenNum - 1, 2);

                    Tokens[tokenNum - 1] = funcId;

                    tokenNum--;
                }
            }

            if (Tokens.Count != 1)
            {
                string a = "";
                foreach (var token in Tokens)
                {
                    a += token + ", ";
                }
                throw new System.Exception("evaluation failed: " + debugOut + " => " + a);
            }

            return Tokens[0];
        }
    }
}
