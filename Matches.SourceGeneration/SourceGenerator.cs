using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

namespace Matches.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public class SourceGenerator : IIncrementalGenerator
{
    private const string KindSuffixStr = "Kind";
    private const string BaseNamespace = "Matches.Generated";
    private const string DiscriminatedUnionAttributeName = "DiscriminatedUnionAttribute";
    private const string DiscriminatedUnionCaseAttributeName = "DiscriminatedUnionCaseAttribute";
    private const string GenericPlaceholderTypeName = "GenericPlaceholder";
    private const string ClassConstraintPlaceholderTypeName = "ClassConstraintPlaceholder";
    private const string StructConstraintPlaceholderTypeName = "StructConstraintPlaceholder";
    private const string ConstructorConstraintPlaceholderTypeName = "ConstructorConstraintPlaceholder";
    private const string NotNullConstraintPlaceholderTypeName = "NotNullConstraintPlaceholder";
    private const string UnmanagedConstraintPlaceholderTypeName = "UnmanagedConstraintPlaceholder";

    private static readonly SymbolDisplayFormat FullyQualifiedWithoutGlobalNameDisplayFormat =
        new
        (
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

    private static readonly SymbolDisplayFormat FullyQualifiedWithGlobalNameDisplayFormat =
        new
        (
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
        );

    private static readonly SymbolDisplayFormat FullyQualifiedWithGlobalWithGenericsNameDisplayFormat =
        new
        (
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
        );

    private static readonly DiagnosticDescriptor GenerationFailedDiagnosticDescriptor =
        new
        (
            "MTCHSG001",
            "Generation failed",
            "Generation failed due to unexpected exception '{0}'",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor InvalidDiscriminatedUnionKindEnumNameDiagnosticDescriptor =
        new
        (
            "MTCHSG002",
            "Invalid discriminated union kind enum name",
            $"Discriminated union kind enum must end with '{KindSuffixStr}' suffix but got '{{0}}'",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor MissingDiscriminatedUnionKindEnumCasesDiagnosticDescriptor =
        new
        (
            "MTCHSG003",
            "Missing discriminated union kind enum cases",
            "Discriminated union kind enum must have at least one case",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor MissingDiscriminatedUnionAttributeDiagnosticDescriptor =
        new
        (
            "MTCHSG004",
            $"Missing {DiscriminatedUnionCaseAttributeName}",
            $"Discriminated union kind enum cases must have '{DiscriminatedUnionCaseAttributeName}' attribute",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor DiscriminatedUnionCaseTypeGenericArgumentNotSpecifiedExplicitlyDiagnosticDescriptor =
        new
        (
            "MTCHSG005",
            "Discriminated union case type generic argument not specified explicitly",
            "Specify type explicitly",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor InvalidDiscriminatedUnionCaseTypeGenericArgumentsCountDiagnosticDescriptor =
        new
        (
            "MTCHSG006",
            "Invalid discriminated union case type generic arguments count",
            "Expected {0} generic arguments but got {1}",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor InvalidDiscriminatedUnionCaseGenericArgumentsDiagnosticDescriptor =
        new
        (
            "MTCHSG007",
            "Invalid discriminated union case type generic arguments",
            "Expected no specified generic arguments for the type '{0}'",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor DuplicateDiscriminatedUnionCaseGenericConstraintDiagnosticDescriptor =
        new
        (
            "MTCHSG008",
            "Duplicate discriminated union case generic constraint",
            "Generic constraint {0} is duplicated",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    private static readonly DiagnosticDescriptor InvalidDiscriminatedUnionCaseGenericSpecialConstraintPositionDiagnosticDescriptor =
        new
        (
            "MTCHSG009",
            "Invalid discriminated union case generic special constraint position",
            "Special constraints must be positioned before other constraints",
            "Matches.SourceGeneration",
            DiagnosticSeverity.Error,
            true
        );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput
        (
            ctx =>
            {
                ctx.AddSource
                (
                    $"{DiscriminatedUnionAttributeName}.g.cs",
                    $$"""
                      using System;
                      
                      namespace {{BaseNamespace}};
                      
                      [AttributeUsage(AttributeTargets.Enum)]
                      public class {{DiscriminatedUnionAttributeName}} : Attribute { }
                      
                      """
                );

                ctx.AddSource
                (
                    $"{DiscriminatedUnionCaseAttributeName}.g.cs",
                    $$"""
                      #nullable enable
                      using System;
                      
                      namespace {{BaseNamespace}};
                      
                      [AttributeUsage(AttributeTargets.Field)]
                      #pragma warning disable CS9113
                      public class {{DiscriminatedUnionCaseAttributeName}}(Type? type, params Type?[] genericArguments) : Attribute { }
                      #pragma warning restore CS9113
                      #nullable restore
                      """
                );

                ctx.AddSource
                (
                    "Placeholders.g.cs",
                    $"""
                     namespace {BaseNamespace};
                     
                     public struct {GenericPlaceholderTypeName};
                     public struct {ClassConstraintPlaceholderTypeName};
                     public struct {StructConstraintPlaceholderTypeName};
                     public struct {ConstructorConstraintPlaceholderTypeName};
                     public struct {NotNullConstraintPlaceholderTypeName};
                     public struct {UnmanagedConstraintPlaceholderTypeName};
                     
                     """
                );
            }
        );

        var fromSyntaxProvider =
            context.SyntaxProvider.CreateSyntaxProvider
            (
                static (node, _) => node is EnumDeclarationSyntax { AttributeLists.Count: not 0 },
                static (ctx, _) => (EnumDeclarationSyntax)ctx.Node
            )
            .Where(x => x is not null);

        var fromCompilationProvider = context.CompilationProvider.Combine(fromSyntaxProvider.Collect());

        context.RegisterSourceOutput
        (
            fromCompilationProvider,
            static (productionContext, syntax) =>
            {
                try
                {
                    Handle(productionContext, syntax.Left, syntax.Right);
                }
                catch (Exception ex)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(GenerationFailedDiagnosticDescriptor, null, ex.Message));
                }
            }
        );
    }

    private static void Handle(SourceProductionContext context, Compilation compilation, ImmutableArray<EnumDeclarationSyntax> enumDeclArr)
    {
        foreach (var enumDecl in enumDeclArr)
        {
            if (!enumDecl.Identifier.Text.EndsWith(KindSuffixStr))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidDiscriminatedUnionKindEnumNameDiagnosticDescriptor, enumDecl.GetLocation(), enumDecl.Identifier.Text));
                return;
            }

            var semanticModel = compilation.GetSemanticModel(enumDecl.SyntaxTree);
            var enumSymbol = CSharpExtensions.GetDeclaredSymbol(semanticModel, enumDecl)!;
            var enumSymbolNameWithGlobal = enumSymbol.ToDisplayString(FullyQualifiedWithGlobalNameDisplayFormat);

            var basicName = enumSymbol.Name.Substring(0, enumSymbol.Name.Length - KindSuffixStr.Length);
            var basicNameFirstLowered = basicName.Length is 1 ? basicName.ToLower() : $"{basicName[0].ToString().ToLower()}{basicName.Substring(1)}";
            var interfaceName = $"I{basicName}";

            List<(string ChoiceName, string TypeNameStr, string TypeNameFirstLoweredStr, string[] TypeGenerics, string[] Constraints, string TypeValueParameterTypeNameStr)> choiceInfos = [];

            if (enumDecl.Members.Count == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingDiscriminatedUnionKindEnumCasesDiagnosticDescriptor, enumDecl.GetLocation()));
                return;
            }

            foreach (var enumMemberDecl in enumDecl.Members)
            {
                var enumFieldSymbol = CSharpExtensions.GetDeclaredSymbol(semanticModel, enumMemberDecl)!;

                var attribute = enumFieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString(FullyQualifiedWithGlobalNameDisplayFormat) == $"global::{BaseNamespace}.{DiscriminatedUnionCaseAttributeName}");
                
                if (attribute is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MissingDiscriminatedUnionAttributeDiagnosticDescriptor, enumMemberDecl.GetLocation()));
                    return;
                }

                var typeNameStr = $"{enumFieldSymbol.Name}{basicName}";

                var typeNameFirstLoweredStr =
                    typeNameStr.Length == 1
                        ? typeNameStr[0].ToString().ToLower()
                        : $"{typeNameStr[0].ToString().ToLower()}{typeNameStr.Substring(1)}";

                if (attribute.ConstructorArguments[0].IsNull)
                {
                    choiceInfos.Add((enumFieldSymbol.Name, typeNameStr, typeNameFirstLoweredStr, [], [], string.Empty));
                    continue;
                }

                var typeSymbol = (INamedTypeSymbol)attribute.ConstructorArguments[0].Value!;
                var genericArguments = attribute.ConstructorArguments[1].Values;

                if (typeSymbol.ToDisplayString(FullyQualifiedWithGlobalNameDisplayFormat) == $"global::{BaseNamespace}.{GenericPlaceholderTypeName}")
                {
                    var genericStr = $"T{enumFieldSymbol.Name}";

                    if (genericArguments.IsDefaultOrEmpty)
                    {
                        choiceInfos.Add((enumFieldSymbol.Name, typeNameStr, typeNameFirstLoweredStr, [genericStr], [], genericStr));
                    }
                    else
                    {
                        var singleValueConstraintTypeNames =
                            genericArguments
                                .Select(x => x.Value)
                                .OfType<INamedTypeSymbol>()
                                .Select(x => x.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat))
                                .ToArray();

                        List<string> singleValueConstraints = [];
                        List<string> visited = [];

                        int i;

                        for (i = 0; i < singleValueConstraintTypeNames.Length; ++i)
                        {
                            var constraintTypeName = singleValueConstraintTypeNames[i];

                            if (visited.Contains(constraintTypeName))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(DuplicateDiscriminatedUnionCaseGenericConstraintDiagnosticDescriptor, enumMemberDecl.GetLocation(), constraintTypeName));
                                return;
                            }

                            var specialConstraint =
                                constraintTypeName switch
                                {
                                    $"global::{BaseNamespace}.{ClassConstraintPlaceholderTypeName}" => "class",
                                    $"global::{BaseNamespace}.{StructConstraintPlaceholderTypeName}" => "struct",
                                    $"global::{BaseNamespace}.{ConstructorConstraintPlaceholderTypeName}" => "new()",
                                    $"global::{BaseNamespace}.{NotNullConstraintPlaceholderTypeName}" => "notnull",
                                    $"global::{BaseNamespace}.{UnmanagedConstraintPlaceholderTypeName}" => "unmanaged",
                                    _ => string.Empty
                                };

                            if (specialConstraint.Length is 0)
                            {
                                break;
                            }

                            visited.Add(constraintTypeName);
                            singleValueConstraints.Add(specialConstraint);
                        }

                        for (; i < singleValueConstraintTypeNames.Length; ++i)
                        {
                            var constraintTypeName = singleValueConstraintTypeNames[i];

                            if (visited.Contains(constraintTypeName))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(DuplicateDiscriminatedUnionCaseGenericConstraintDiagnosticDescriptor, enumMemberDecl.GetLocation(), constraintTypeName));
                                return;
                            }

                            if 
                            (
                                constraintTypeName is
                                    $"global::{BaseNamespace}.{ClassConstraintPlaceholderTypeName}" or
                                    $"global::{BaseNamespace}.{StructConstraintPlaceholderTypeName}" or
                                    $"global::{BaseNamespace}.{ConstructorConstraintPlaceholderTypeName}" or
                                    $"global::{BaseNamespace}.{NotNullConstraintPlaceholderTypeName}" or
                                    $"global::{BaseNamespace}.{UnmanagedConstraintPlaceholderTypeName}"
                            )
                            {
                                context.ReportDiagnostic(Diagnostic.Create(InvalidDiscriminatedUnionCaseGenericSpecialConstraintPositionDiagnosticDescriptor, enumMemberDecl.GetLocation()));
                                return;
                            }

                            visited.Add(constraintTypeName);

                            singleValueConstraints.Add(constraintTypeName.Replace($"global::{BaseNamespace}.{GenericPlaceholderTypeName}", genericStr));
                        }

                        choiceInfos.Add((enumFieldSymbol.Name, typeNameStr, typeNameFirstLoweredStr, [genericStr], [$"{genericStr} : {string.Join(", ", singleValueConstraints)}"], genericStr));
                    }

                    continue;
                }
                
                if (typeSymbol.IsUnboundGenericType)
                {
                    if (genericArguments.IsDefault)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiscriminatedUnionCaseTypeGenericArgumentNotSpecifiedExplicitlyDiagnosticDescriptor, enumMemberDecl.GetLocation()));
                        return;
                    }

                    if (typeSymbol.TypeParameters.Length != genericArguments.Length)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidDiscriminatedUnionCaseTypeGenericArgumentsCountDiagnosticDescriptor, enumMemberDecl.GetLocation(), typeSymbol.TypeParameters.Length, genericArguments.Length));
                        return;
                    }

                    var genericInfos =
                        typeSymbol.TypeParameters
                            .Zip(genericArguments, (x, y) => (TypeParameterSymbol: x, TypeArgumentObj: y.Value))
                            .Select
                            (
                                x =>
                                {
                                    if (x.TypeArgumentObj is not INamedTypeSymbol typeArgumentSymbol)
                                    {
                                        return (x.TypeParameterSymbol, IsSpecified: false, TypeArgumentStr: $"T{enumFieldSymbol.Name}{x.TypeParameterSymbol.Name}");
                                    }

                                    var typeArgumentSymbolName = typeArgumentSymbol.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat);

                                    return
                                        typeArgumentSymbolName == $"global::{BaseNamespace}.{GenericPlaceholderTypeName}"
                                            ? (x.TypeParameterSymbol, IsSpecified: false, TypeArgumentStr: $"T{enumFieldSymbol.Name}{x.TypeParameterSymbol.Name}")
                                            : (x.TypeParameterSymbol, IsSpecified: true, TypeArgumentStr: typeArgumentSymbolName);

                                }
                            )
                            .ToArray();

                    var notSpecifiedGenericInfos =
                        genericInfos
                            .Where(x => !x.IsSpecified)
                            .Select(x => (x.TypeParameterSymbol, x.TypeArgumentStr))
                            .ToArray();

                    var constraints = new List<string>();

                    foreach (var (typeParameterSymbol, typeArgumentStr) in notSpecifiedGenericInfos)
                    {
                        var typeArgumentConstraints = new List<string>();

                        if (typeParameterSymbol.HasReferenceTypeConstraint)
                        {
                            typeArgumentConstraints.Add("class");
                        }

                        if (typeParameterSymbol.HasValueTypeConstraint)
                        {
                            typeArgumentConstraints.Add("struct");
                        }

                        if (typeParameterSymbol.HasConstructorConstraint)
                        {
                            typeArgumentConstraints.Add("new()");
                        }

                        if (typeParameterSymbol.HasNotNullConstraint)
                        {
                            typeArgumentConstraints.Add("notnull");
                        }

                        if (typeParameterSymbol.HasUnmanagedTypeConstraint)
                        {
                            typeArgumentConstraints.Add("unmanaged");
                        }

                        typeArgumentConstraints.AddRange
                        (
                            typeParameterSymbol.ConstraintTypes
                                .Select
                                (
                                    constraintTypeSymbol => 
                                        constraintTypeSymbol is INamedTypeSymbol constraintNamedTypeSymbol
                                            ? constraintNamedTypeSymbol.IsGenericType
                                                ? GetSymbolNameWithFilledGenericsStr(genericInfos.Select(x => (x.TypeParameterSymbol, x.TypeArgumentStr)), constraintNamedTypeSymbol)
                                                : constraintTypeSymbol.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat)
                                            : genericInfos.Where(x => x.TypeParameterSymbol.Name == constraintTypeSymbol.Name).Select(x => x.TypeArgumentStr).First()
                                )
                        );

                        if (typeArgumentConstraints.Count != 0)
                        {
                            constraints.Add($"{typeArgumentStr} : {string.Join(", ", typeArgumentConstraints)}");
                        }
                    }

                    var typeValueParameterTypeNameStr =
                        $"{typeSymbol.ToDisplayString(FullyQualifiedWithGlobalNameDisplayFormat)}<{string.Join(", ", genericInfos.Select(x => x.TypeArgumentStr))}>";

                    choiceInfos.Add((enumFieldSymbol.Name, typeNameStr, typeNameFirstLoweredStr, notSpecifiedGenericInfos.Select(x => x.TypeArgumentStr).ToArray(), constraints.ToArray(), typeValueParameterTypeNameStr));
                }
                else
                {
                    if (!genericArguments.IsDefaultOrEmpty)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidDiscriminatedUnionCaseGenericArgumentsDiagnosticDescriptor, enumMemberDecl.GetLocation(), typeSymbol.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat)));
                        return;
                    }

                    var typeValueParameterTypeNameStr = typeSymbol.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat);

                    choiceInfos.Add((enumFieldSymbol.Name, typeNameStr, typeNameFirstLoweredStr, [], [], typeValueParameterTypeNameStr));
                }
            }

            var namespaceStr = $"namespace {BaseNamespace}{(enumSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : $".{enumSymbol.ContainingNamespace.ToDisplayString(FullyQualifiedWithoutGlobalNameDisplayFormat)}")}";

            var allGenerics = choiceInfos.SelectMany(x => x.TypeGenerics).ToArray();
            var allConstraints = choiceInfos.SelectMany(x => x.Constraints).ToArray();
            var allGenericsStr = allGenerics.Length == 0 ? string.Empty : $"<{string.Join(", ", allGenerics)}>";
            var allConstraintsStr = allConstraints.Length == 0 ? string.Empty : $" {string.Join(" ", allConstraints.Select(y => $"where {y}"))}";
            var allGenericsWithResultStr = $"<{string.Join(", ", allGenerics.Concat(["TResult"]))}>";

            context.AddSource
            (
                $"{basicName}.g.cs",
                $$"""
                  {{namespaceStr}};
                  
                  public interface {{interfaceName}}{{allGenericsStr}}{{allConstraintsStr}}
                  {
                      {{enumSymbolNameWithGlobal}} Kind { get; }
                  }
                  
                  {{
                      string.Join
                      (
                          "\n\n", 
                          choiceInfos.Select
                          (
                              x =>
                                  x.TypeValueParameterTypeNameStr.Length is 0
                                      ? $$"""
                                          public record {{x.TypeNameStr}}{{allGenericsStr}}() : {{interfaceName}}{{allGenericsStr}}{{allConstraintsStr}}
                                          {
                                              public {{enumSymbolNameWithGlobal}} Kind => {{enumSymbolNameWithGlobal}}.{{x.ChoiceName}};
                                          }
                                          """
                                      : $$"""
                                          public record {{x.TypeNameStr}}{{allGenericsStr}}({{x.TypeValueParameterTypeNameStr}} Value) : {{interfaceName}}{{allGenericsStr}}{{allConstraintsStr}}
                                          {
                                              public {{enumSymbolNameWithGlobal}} Kind => {{enumSymbolNameWithGlobal}}.{{x.ChoiceName}};
                                          }
                                          """

                          )
                      )
                  }}
                  
                  public static class {{basicName}}
                  {
                  {{
                      string.Join
                      (
                          "\n\n", 
                          choiceInfos
                              .Select
                              (
                                  x =>
                                      x.TypeValueParameterTypeNameStr.Length is 0
                                          ? $"""
                                                 public static {x.TypeNameStr}{allGenericsStr} Get{x.TypeNameStr}{allGenericsStr}(){allConstraintsStr} =>
                                                     new();
                                             """
                                          :  $"""
                                                  public static {x.TypeNameStr}{allGenericsStr} Get{x.TypeNameStr}{allGenericsStr}({x.TypeValueParameterTypeNameStr} value){allConstraintsStr} =>
                                                      new(value);
                                              
                                                  public static {x.TypeValueParameterTypeNameStr} GetValue{allGenericsStr}({x.TypeNameStr}{allGenericsStr} {x.TypeNameFirstLoweredStr}){allConstraintsStr} =>
                                                      {x.TypeNameFirstLoweredStr}.Value;
                                              """
                              )
                      )
                  }}
                  
                      public static TResult Match{{allGenericsWithResultStr}}({{interfaceName}}{{allGenericsStr}} {{basicNameFirstLowered}}, {{string.Join(", ", choiceInfos.Select(x => x.TypeValueParameterTypeNameStr.Length is 0 ? $"Func<TResult> func{x.ChoiceName}" : $"Func<{x.TypeValueParameterTypeNameStr}, TResult> func{x.ChoiceName}"))}}){{allConstraintsStr}} =>
                          {{basicNameFirstLowered}}.Kind switch
                          {
                  {{
                      string.Join("\n", choiceInfos.Select(x => $"            {enumSymbolNameWithGlobal}.{x.ChoiceName} => func{x.ChoiceName}({(x.TypeValueParameterTypeNameStr.Length is 0 ? string.Empty : $"GetValue(({x.TypeNameStr}{allGenericsStr}){basicNameFirstLowered})")}),"))
                  }}
                              _ => throw new Exception("Enum value not handled")
                          };
                  
                      public static System.Threading.Tasks.Task<TResult> MatchAsync{{allGenericsWithResultStr}}({{interfaceName}}{{allGenericsStr}} {{basicNameFirstLowered}}, {{string.Join(", ", choiceInfos.Select(x => x.TypeValueParameterTypeNameStr.Length is 0 ? $"Func<System.Threading.Tasks.Task<TResult>> func{x.ChoiceName}" : $"Func<{x.TypeValueParameterTypeNameStr}, System.Threading.Tasks.Task<TResult>> func{x.ChoiceName}"))}}){{allConstraintsStr}} =>
                          {{basicNameFirstLowered}}.Kind switch
                          {
                  {{string.Join("\n", choiceInfos.Select(x => $"            {enumSymbolNameWithGlobal}.{x.ChoiceName} => func{x.ChoiceName}({(x.TypeValueParameterTypeNameStr.Length is 0 ? string.Empty : $"GetValue(({x.TypeNameStr}{allGenericsStr}){basicNameFirstLowered})")}),"))}}
                              _ => throw new Exception("Enum value not handled")
                          };
                  }
                  
                  """
            );
        }
    }

    private static string GetSymbolNameWithFilledGenericsStr(IEnumerable<(ITypeParameterSymbol TypeParameterSymbol, string TypeArgumentStr)> genericInfos, INamedTypeSymbol symbol)
    {
        var genericArguments = symbol.TypeArguments;

        var genericStrList = new List<string>();

        foreach (var genericArgument in genericArguments)
        {
            if (genericArgument.TypeKind == TypeKind.TypeParameter)
            {
                var argumentTypeSymbol =
                    genericInfos
                        .Where(x => x.TypeParameterSymbol.Name == genericArgument.Name)
                        .Select(x => x.TypeArgumentStr)
                        .First();

                if (argumentTypeSymbol is not null)
                {
                    genericStrList.Add(argumentTypeSymbol);
                }
            }
            else
            {
                var genericArgumentSymbol = (INamedTypeSymbol)genericArgument;

                genericStrList.Add
                (
                    genericArgumentSymbol.IsGenericType
                        ? GetSymbolNameWithFilledGenericsStr(genericInfos, genericArgumentSymbol)
                        : genericArgumentSymbol.ToDisplayString(FullyQualifiedWithGlobalWithGenericsNameDisplayFormat)
                );
            }
        }

        return $"{symbol.ToDisplayString(FullyQualifiedWithGlobalNameDisplayFormat)}<{string.Join(", ", genericStrList)}>";
    }
}
