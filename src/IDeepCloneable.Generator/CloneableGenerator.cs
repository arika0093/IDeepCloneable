using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IDeepCloneable.Generator;

[Generator]
public class CloneableGenerator : IIncrementalGenerator
{
    private const string DeepCloneMethodName = "DeepClone";
    private const string DeepCloneableAttributeMetadataName = "DeepCloneableAttribute";
    private const string DeepCloneableAttributeFullName = "global::DeepCloneableAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                DeepCloneableAttributeMetadataName,
                predicate: static (node, _) => true,
                transform: static (ctx, _) => GetRelationalAllClassInfo(ctx)
            )
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(
            classDeclarations,
            static (spc, source) => Execute(source!, spc)
        );
    }

    private static EquatableArray<ClassInfo> GetRelationalAllClassInfo(GeneratorAttributeSyntaxContext context)
    {
        // TODO
        // ここでは以下の情報を抽出し、ClassInfoの配列として返す必要があります。
        // * `[DeepCloneable]`でマークされた型から到達可能なすべてのクラスについて、`ClassInfo`を作成し、配列として返します。
        // * 「到達可能」とは、以下の定義に従う:
        //   * `[DeepCloneable]`でマークされたクラス自身(A)
        //   * Aを継承する全てのクラス
        //   * Aのプロパティ/フィールドが参照する全てのクラス(再帰的に)
        // * `ClassInfo`には以下の情報を含めます:
        //   * `string`: クラス名
        //   * `string`: Fullnameのクラス名（global::から始まる）
        //   * `string`: 名前空間
        //   * `EquatableArray<string>`: 子が持つクラス名のリスト（フルネーム） ここでは子のみを対象とし、孫以降は含めない
        //   * `bool`: 型がnullableかどうか
        //   * `bool`: 型がレコードかどうか
        //   * `bool`: 参照型か値型か
        //   * `bool`: 内部（ネスト含む）すべての型が値型または不変型（stringなど）かどうか
        //   * `bool`: 配列かどうか（より正確にはコレクション初期化子を持つか）
        //   * `bool`: DeepCloneメソッドを生成するか（`[DeepCloneable]`属性がある、または`[DeepCloneable]`クラスを継承している場合）
        //   * `bool`: 型が抽象かどうか
        //   * `bool`: `DeepClone()`メソッドを生成する必要があるか ([DeepCloneable]属性がある、または[DeepCloneable]クラスを継承している場合)
    }

    private static void Execute(EquatableArray<ClassInfo> classInfos, SourceProductionContext context)
    {
        // TODO
        // ここでは、classInfosを使用してソースコードを生成し、context.AddSourceを使用して生成されたコードを追加します。
        // まず、各ClassInfoに対して以下のようなコードを生成する必要があります。
        // これは単一ファイル DeepCloneExtensions.g.cs にまとめて生成します。
        // [EditorBrowsable(EditorBrowsableState.Never)]
        // internal static partial class DeepCloneExtensions
        // {
        //      [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //      [EditorBrowsable(EditorBrowsableState.Never)]
        //      // DeepCloneメソッドを生成する必要がある場合はInternal, それ以外ならprivate
        //      private static (対象の型) 型名_CloneInternal(this (対象の型) original)
        //      {
        //          // DeepCloneの実装
        //          // もし自身がrecord, structの場合, with式を使ってコピーを作成し、参照型の子要素をCloneInternalメソッドでクローンする
        //          return original with {
        //              Prop1 = 対象の型名_CloneInternal(original.Prop1),
        //              Prop2 = 対象の型名_CloneInternal(original.Prop2),
        //              // ... 
        //          };
        //          // もしclassの場合、新しいインスタンスを作成し、各プロパティ/フィールドを代入、必要に応じてCloneInternalメソッドでクローンする
        //          var randomize_name = new (対象の型);
        //          randomize_name.Prop1 = original.Prop1; // 値型または不変型の場合
        //          randomize_name.Prop2 = 対象の型名_CloneInternal(original.Prop2); // 参照型の場合
        //          // ...
        //          return randomize_name;
        //      }
        //
        //      // これを全てのClassInfoに対して繰り返す
        //      // 値型または不変型（stringなど）の場合は、CloneInternalメソッドを生成せず、元の値をそのまま返すようにします。
        // }

        // その際、特別な処理を行う型については、以下のようなコードを生成します。
        //      [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //      [EditorBrowsable(EditorBrowsableState.Never)]
        //      private static List<対象のクラス> List_対象のクラス_CloneInternal(this List<対象のクラス> original)
        //      {
        //          var list = new List<対象のクラス>(original.Count);
        //#if NET8_0_OR_GREATER
        //          CollectionMarshal.SetCount(list, original.Count);
        //#endif
        //          foreach (var item in original){
        //              list.Add(DeepCloneExtensions.対象のクラス_CloneInternal(item));
        //          }
        //          return list;
        //      }
        // 
        // これらは以下のようなクラス(別ファイル)に情報をまとめておき
        // internal record SpecialTypeInfo
        // {
        //      public string TargetTypeStartWith { get; init; } // 例: "global::System.Collections.Generic.List<"
        //      public string IsMatch(string typeFullName) => typeFullName.StartsWith(TargetTypeStartWith);
        //      public string GetTypeName(string innerTypeFullName) => $"{TargetTypeStartWith}{innerTypeFullName}>";
        //      public string CloneMethodTemplate(string innerTypeFullName) { get; init; } // 上記のListのCloneInternalメソッドのテンプレート
        // }
        // 事前に配列を用意して
        // private static List<SpecialTypeInfo> SpecialTypeInfos = [ new ListTypeInfo(), new DictionaryTypeInfo(), ... ];
        // 以下のように判定・参照します。
        // foreach (var specialTypeInfo in SpecialTypeInfos)
        // {
        //      if (specialTypeInfo.IsMatch(classInfo.FullClassName))
        //      {
        //          var innerTypeFullName = ...; // specialTypeInfo.TargetTypeStartWith以降の型名を抽出
        //          var cloneMethodCode = specialTypeInfo.CloneMethodTemplate(innerTypeFullName);
        //          // 生成コードに追加
        //          break;
        //      }
        // }

        // 最後に、DeepCloneメソッドを生成する必要があるClassInfoに対して、以下のようなコードを生成します。
        // namespace (名前空間)
        // {
        //      partial class (クラス名) : IDeepCloneable<(クラス名)>
        //      {
        //          /// <inheritdoc />
        //          [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //          // もし継承元がDeepCloneを持っている場合はoverrideを付与、それ以外はvirtualを付与
        //          // sealedクラスの場合はoverrideもvirtualも付与しない
        //          public virtual (クラス名) DeepClone() => DeepCloneExtensions.(クラス名)_CloneInternal(this);
        //          // CloneInternalメソッドを呼び出すだけ
        //      }
        // }

        // NOTICE:
        // 生成コードに関しては以下のルールを守ること:
        // * 全ての生成、参照される型名は global:: から始まる完全修飾名を使用して名前の衝突を避けるようにします。
        // * コード生成時にはインデント処理機能付きのStringBuilder(IndentedStringBuilder)を使用し、適切にインデントを付与します。
        // * 生成されるファイルのヘッダー部分は共通で以下の内容とします。
        // // <auto-generated>
        // // This file was generated by the IDeepCloneable source generator (ver: {ThisAssembly.AssemblyFileVersion}).
        // // </auto-generated>
        // #nullable disable
        // #pragma warning disable
        // using System;
        // using System.Collections.Immutable;
        // using System.Linq;
    }

    private record ClassInfo
    {
        // TODO
        // ここではISymbolやSyntaxNodeを使わずに、必要な情報だけをプロパティとして定義します。
        // コンストラクタではなく、required プロパティのinitアクセサを使用して初期化します。
        // 配列の場合はEquatableArray<T>を使用し、等価性を保持します。
        // e.g. public required string ClassName { get; init; }
    }
    
}
