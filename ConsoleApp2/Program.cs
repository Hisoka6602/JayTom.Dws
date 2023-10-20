using System;
using System.Linq;
using ConsoleApp2;
using System.Text;
using Newtonsoft.Json;
using System.Collections;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;
using JayTom.Dws.Interface.Sunnen;
using JayTom.Dws.Interface.Szjy188;
using static JayTom.Dws.Interface.Szjy188.SzjyApi;

internal class Program {

    private static async Task Main(string[] args) {
        /*float x = 5, y = 18, z = 14;
        string formula = "x > 10.0 AND y < 20.1 AND z <> 12.0 AND o>6";

        // 使用DataTable的Compute方法计算公式的值
        bool result = (bool)new System.Data.DataTable().Compute(formula.Replace("x", x.ToString()).Replace("y", y.ToString()).Replace("z", z.ToString()), null);

        if (result) {
            Console.WriteLine("公式成立");
        }
        else {
            Console.WriteLine("公式不成立");
        }*/

        var validateWeight = ValidateWeight(34);
        var validateSorting = ValidateSorting(11, 12, 13, 1800);
        return;

        var szjyApi = new SzjyApi(null);
        szjyApi.SetParameters(new SzjyApiParam {
            UserName = "quanlai07",
            Password = "Ql123456",
            Url = "https://www.szjy188.com/auto-entry"
        });
        var uploadResponse = await szjyApi.UploadData("1234567890aa",
            1.05, DateTime.Now, 10, 20,
            30, 40, null, null, "Box");
        Console.WriteLine(uploadResponse);
        Console.ReadLine();
    }

    // 从计算公式中提取变量名称
    private static string[] ExtractVariableNames(string formula) {
        var variables = new HashSet<string>();
        foreach (var token in formula.Split(' ')) {
            if (token.Length > 0 && char.IsLetter(token[0])) {
                variables.Add(token);
            }
        }
        return variables.ToArray();
    }

    public static bool ValidateWeight(double weight) {
        string formula = "It > 30 && It < 50 && It != 33";
        try {
            // 解析并计算表达式
            var expression = DynamicExpressionParser
                .ParseLambda(new[] { Expression.Parameter(typeof(double), "it") }, typeof(bool), formula);
            // 编译并执行表达式
            return (bool)(expression.Compile().DynamicInvoke(weight) ?? false);
        }
        catch (Exception e) {
            return false;
        }
    }

    public static bool ValidateSorting(double length, double width, double height, double volume) {
        string formula = "Length > 10 and Width < 20 and Width > 10 and Height > 8 and Height < 50 and Volume > 105";
        try {
            // 解析并计算表达式
            ParameterExpression[] parameters = {
                Expression.Parameter(typeof(double), "Length"),
                Expression.Parameter(typeof(double), "Width"),
                Expression.Parameter(typeof(double), "Height"),
                Expression.Parameter(typeof(double), "Volume")
            };
            LambdaExpression expression = DynamicExpressionParser.ParseLambda(parameters, typeof(bool), formula);

            // 编译并执行表达式
            return (bool)(expression.Compile().DynamicInvoke(length, width, height, volume) ?? false);
        }
        catch (Exception e) {
            return false;
        }
    }
}

public class ExpressionEvaluator {
    private readonly Func<Dictionary<string, object>, object> _evaluator;

    public ExpressionEvaluator(LambdaExpression expression) {
        _evaluator = expression.Compile() as Func<Dictionary<string, object>, object>;
    }

    public T Evaluate<T>(Dictionary<string, object> arguments) {
        var result = _evaluator.Invoke(arguments.ToDictionary(kv => kv.Key, kv => (object)Convert.ChangeType(kv.Value, typeof(float))));
        return (T)Convert.ChangeType(result, typeof(T));
    }
}

public static class DynamicExpression {

    public static ExpressionEvaluator CompileLambda(string expression, params string[] parameterNames) {
        var parameterExpressions = parameterNames.Select(name => Expression.Parameter(typeof(float), name)).ToArray();
        var lambdaBody = DynamicExpressionParser.ParseLambda(parameterExpressions, null, expression).Body;
        var lambdaExpression = Expression.Lambda(lambdaBody, parameterExpressions);
        return new ExpressionEvaluator(lambdaExpression);
    }
}