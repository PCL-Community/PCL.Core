using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PCL.Core.SourceGenerators;

[Generator(LanguageNames.CSharp)]
public class LifecycleScopeGenerator : IIncrementalGenerator
{
    private const string ScopeAttributeType = "PCL.Core.App.LifecycleScopeAttribute";

    private const string StartMethodAttributeType = "PCL.Core.App.LifecycleStartAttribute";
    private const string StopMethodAttributeType = "PCL.Core.App.LifecycleStopAttribute";
    private const string ArgumentHandlerMethodAttributeType = "PCL.Core.App.LifecycleArgumentHandlerAttribute`1";

    private static readonly HashSet<string> _MethodAttributeTypes = [
        StartMethodAttributeType, StopMethodAttributeType,
        ArgumentHandlerMethodAttributeType
    ];

    private class ScopeMethodModel
    {
        public string MethodName { get; set; } = null!;
    }

    private class StartMethodModel : ScopeMethodModel;

    private class StopMethodModel : ScopeMethodModel;

    private class ArgumentHandlerMethodModel : ScopeMethodModel
    {
        public string ArgumentName { get; set; } = null!;
        public string ArgumentQualifiedTypeName { get; set; } = null!;
        public string ArgumentDefaultValue { get; set; } = null!;
    }

    private class ScopeModel
    {
        public string Namespace { get; set; } = null!;
        public string TypeName { get; set; } = null!;
        public string QualifiedTypeName => $"{Namespace}.{TypeName}";
        public string Identifier { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool AsyncStart { get; set; }
        public List<ScopeMethodModel> Methods { get; } = [];
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            ScopeAttributeType,
            static (node, _) =>
            {
                // 过滤 partial class
                if (node is not ClassDeclarationSyntax syntax) return false;
                return syntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
            },
            static (INamedTypeSymbol TypeSymbol, ScopeModel Model)? (ctx, _) =>
            {
                if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol) return null;
                var attr = ctx.Attributes[0];
                var args = attr.ConstructorArguments;
                var scopeIdentifier = args[0].Value!.ToString();
                var scopeName = args[1].Value!.ToString();
                var scopeAsyncStart = true;
                if (args.Length > 2 && args[2].Value is bool v) scopeAsyncStart = v;
                var ns = typeSymbol.ContainingNamespace.ToDisplayString();
                var typeName = typeSymbol.Name;
                return (typeSymbol, new ScopeModel
                {
                    Namespace = ns,
                    TypeName = typeName,
                    Identifier = scopeIdentifier,
                    Name = scopeName,
                    AsyncStart = scopeAsyncStart
                });
            }
        ).Where(static i => i != null).Select(static (i, _) => i.GetValueOrDefault());
        var collected = candidates.Collect();
        context.RegisterSourceOutput(collected, static (spc, types) =>
        {
            foreach (var (symbol, model) in types)
            {
                model.Methods.Clear();
                foreach (var member in symbol.GetMembers())
                {
                    var attrTypeName = string.Empty;
                    var attr = member.GetAttributes().FirstOrDefault(data =>
                    {
                        attrTypeName = data.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return attrTypeName != null && _MethodAttributeTypes.Contains(attrTypeName);
                    });
                    if (attr == null) continue;
                    var methodName = member.Name;
                    model.Methods.Add(attrTypeName switch {
                        StartMethodAttributeType => new StartMethodModel { MethodName = methodName },
                        StopMethodAttributeType => new StopMethodModel { MethodName = methodName },
                        ArgumentHandlerMethodAttributeType => GetArgumentHandlerMethodModel(),
                        _ => throw new ArgumentOutOfRangeException()
                    });
                    continue;
                    ArgumentHandlerMethodModel GetArgumentHandlerMethodModel()
                    {
                        var args = attr.ConstructorArguments;
                        var argumentName = args[0].Value!.ToString();
                        var argumentTypeName = attr.AttributeClass!.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var argumentDefaultValue = args.Length > 1 ? args[1].ToCSharpString() : $"new {argumentTypeName}()";
                        return new ArgumentHandlerMethodModel
                        {
                            MethodName = methodName,
                            ArgumentName = argumentName,
                            ArgumentDefaultValue = argumentDefaultValue,
                            ArgumentQualifiedTypeName = argumentTypeName
                        };
                    }
                }
                spc.AddSource($"{model.QualifiedTypeName}.g.cs", _GenerateScopeSource(model));
            }
        });
    }

    private static string _GenerateScopeSource(ScopeModel model)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// 此文件由 Source Generator 自动生成，请勿手动修改");
        sb.AppendLine();
        sb.AppendLine("using PCL.Core.App;");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {model.Namespace};");
        sb.AppendLine();

        sb.AppendLine($"partial class {model.TypeName} : ILifecycleService");
        sb.AppendLine("{");
        sb.AppendLine($"    public string Identifier => \"{model.Identifier}\";");
        sb.AppendLine($"    public string Name => \"{model.Name}\";");
        sb.AppendLine($"    public bool SupportAsyncStart => {(model.AsyncStart ? "true" : "false")};");
        sb.AppendLine("    private static LifecycleContext Context => _context!;");
        sb.AppendLine();
        sb.AppendLine("    private static LifecycleContext? _context;");
        sb.AppendLine($"    private {model.TypeName}() {{ _context = Lifecycle.GetContext(this); }}");

        sb.AppendLine("}");

        return sb.ToString();
    }
}
