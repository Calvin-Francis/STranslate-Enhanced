using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STranslate.Core;

/// <summary>
///     自动为类的所有方法或方法添加日志[AutoLogger]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AutoLoggerAttribute : OnMethodBoundaryAspect
{
    public bool EnableElapsed { get; set; } = true;
    public int ThreadId { get; private set; }
    public string ThreadName { get; private set; } = "";
    private const string IndentChar = "|---";//缩进符号
    private const string StartPrefix = "[S]";
    private const string EndPrefix = "[E]";
    private static int _indentLevel;//缩进级别
    private int _currentIndentLevel;
    private const int ParaMaxlength = 15000;

    protected string ModuleName { get; private set; } = "";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
#if DEBUG
    protected readonly Stopwatch _sw = new();
#endif

    private static ILogger InternalLogger { get; set; } = null!;
    public static void InitializeLogger(ILogger logger) => InternalLogger = logger.NonNull();


    public override void OnEntry(MethodExecutionArgs args)
    {
        if (IsIgnoredMethod(args)) return;
        ModuleName = args.Instance?.GetType().Name.Replace("LogDecorator", "") ?? "UnknownModule";
#if DEBUG
        _sw.Start();
#endif
        if (args.ReturnValue is Task t)
        {
            _currentIndentLevel = _indentLevel;
        }
        else
        {
            _currentIndentLevel = _indentLevel;
            _indentLevel++;
        }
        var paras = GenerateParaString(args);
        InternalLogger.LogInformation($"{StartPrefix}{GenerateIndent()}:{ModuleName}.{args.Method.Name}({paras})");

    }

    private static string GenerateParaString(MethodExecutionArgs args)
    {
        if (args.Arguments.Length <= 0) return "";
        var paras = GetParamRawString(args);

        if (paras.Length > ParaMaxlength)
        {
            paras = paras.Substring(0, ParaMaxlength) + "...]，参数截断，长度：{paras.Length}";
        }
        return paras;
    }

    private static string GetParamRawString(MethodExecutionArgs args)
    {
        string paras = "";
        var parameters = args.Method.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            var name = parameters[i].Name;
            try
            {
                var jsonValue = JsonSerializer.Serialize(args.Arguments[i], Options);
                paras += $"{{{name}:{jsonValue}}},";
            }
            catch (Exception e)
            {
                paras += $"AutoLogger 返回结果序列化异常]{{{name}:{e.Message}}},";
            }
        }
        paras = paras.TrimEnd(',');
        if (args.Arguments.Length > 1)
        {
            paras = "[" + paras + "]";
        }

        return paras;
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        if (IsIgnoredMethod(args)) return;
        if (args.ReturnValue is Task t)
        {
            t.ContinueWith(task =>
            {
                LogEnd(args);
                _indentLevel--;
            });
        }
        else
        {
            LogEnd(args);
            _indentLevel--;
        }
    }


    protected virtual void LogEnd(MethodExecutionArgs args)
    {
#if DEBUG
        _sw.Stop();
        var returnValue = "void";
        if (args.ReturnValue != null)
        {
            returnValue = JsonSerializer.Serialize(args.ReturnValue, Options);
            if (returnValue.Length > ParaMaxlength)
            {
                returnValue = returnValue.Substring(0, ParaMaxlength) + "...";
            }
        }

        var str = "";
        if (EnableElapsed)
        {
            str = $",Elapsed:{_sw.Elapsed.TotalSeconds:0.###}s";
        }
        InternalLogger.LogInformation(
            $"{EndPrefix}{GenerateIndent()}:{ModuleName}.{args.Method.Name},return:{returnValue}{str}");

#else
        InternalLogger.LogInformation($"{EndPrefix}{GenerateIndent()}:{ModuleName}.{args.Method.Name}");
#endif
    }

    private static bool IsIgnoredMethod(MethodExecutionArgs args) =>
        args.Method.Name.StartsWith("set_") || args.Method.Name.StartsWith("get_")
        || args.Method.Name.StartsWith("add_") || args.Method.Name.StartsWith("remove_")
#if !DEBUG
        || args.Method.IsPrivate
#endif
        ;

    public override void OnException(MethodExecutionArgs args)
    {
#if DEBUG
        _sw.Stop();
        InternalLogger.LogError(
                    $"[AutoLogger(DEBUG)]:{GenerateIndent()}{ModuleName}:{args.Exception.Message}{args.Exception.StackTrace}");
#else
        InternalLogger.LogError(
                $"[Error]:{GenerateIndent()}{ModuleName}:{args.Exception.Message}{args.Exception.StackTrace}");
#endif
        _indentLevel--;
    }

    private string GenerateIndent(bool enableIndent = true)
    {
        ThreadId = Environment.CurrentManagedThreadId;
        ThreadName = Thread.CurrentThread.Name ?? "";
        var indent = $"[{ThreadId.ToString().PadLeft(4, '0')}-{ThreadName.PadLeft(7, '-')}]";
        if (!enableIndent)
        {
            return indent;
        }
        for (int i = 0; i < _currentIndentLevel; i++)
        {
            indent += IndentChar;
        }
        return indent;
    }
}