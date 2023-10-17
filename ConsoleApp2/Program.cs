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

        var uploadResponse = await new SunnenApi(null).UploadData("1234567890aa",
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