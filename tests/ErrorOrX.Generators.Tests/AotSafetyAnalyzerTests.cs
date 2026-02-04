namespace ErrorOrX.Generators.Tests;

/// <summary>
///     Tests for the AotSafetyAnalyzer (EOE034-EOE038).
///     Verifies AOT-unsafe patterns are detected with actionable guidance.
/// </summary>
public class AotSafetyAnalyzerTests : AnalyzerTestBase<AotSafetyAnalyzer>
{
    #region EOE037 - Expression.Compile

    [Fact]
    public Task EOE037_ExpressionCompile()
    {
        const string Source = """
                              using System;
                              using System.Linq.Expressions;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Func<int, int> Compile()
                                  {
                                      Expression<Func<int, int>> expr = x => x * 2;
                                      return {|EOE037:expr.Compile()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE038 - Dynamic keyword

    [Fact]
    public Task EOE038_DynamicKeyword()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public object Process(object input)
                                  {
                                      {|EOE038:dynamic|} d = input;
                                      return d.ToString();
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE034 - Activator.CreateInstance

    [Fact]
    public Task EOE034_ActivatorCreateInstance_Generic()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public object Create()
                                  {
                                      return {|EOE034:Activator.CreateInstance<string>()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE034_ActivatorCreateInstance_TypeOf()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public object Create()
                                  {
                                      return {|EOE034:Activator.CreateInstance(typeof(string))|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE035 - Type.GetType

    /// <summary>
    ///     String literal is safe for the trimmer - no warning should be emitted.
    /// </summary>
    [Fact]
    public Task EOE035_TypeGetType_StringLiteral_NoWarning()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Type? GetTypeByName()
                                  {
                                      // String literal is analyzable by trimmer - no warning
                                      return Type.GetType("System.String");
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    /// <summary>
    ///     Variable argument prevents trimmer analysis - should warn.
    /// </summary>
    [Fact]
    public Task EOE035_TypeGetType_Variable()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Type? GetTypeByName(string typeName)
                                  {
                                      return {|EOE035:Type.GetType(typeName)|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    /// <summary>
    ///     Case-insensitive search breaks trimmer analysis - should warn.
    /// </summary>
    [Fact]
    public Task EOE035_TypeGetType_CaseInsensitive()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Type? GetTypeByName()
                                  {
                                      // ignoreCase: true breaks trimmer analysis
                                      return {|EOE035:Type.GetType("System.String", false, true)|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    /// <summary>
    ///     String literal with explicit ignoreCase: false is safe.
    /// </summary>
    [Fact]
    public Task EOE035_TypeGetType_CaseSensitive_NoWarning()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Type? GetTypeByName()
                                  {
                                      // String literal + case-sensitive is safe
                                      return Type.GetType("System.String", false, false);
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    /// <summary>
    ///     Variable for ignoreCase parameter is potentially unsafe - should warn.
    /// </summary>
    [Fact]
    public Task EOE035_TypeGetType_VariableIgnoreCase()
    {
        const string Source = """
                              using System;

                              namespace AotTest;

                              public class MyService
                              {
                                  public Type? GetTypeByName(bool ignoreCase)
                                  {
                                      // Variable ignoreCase could be true at runtime
                                      return {|EOE035:Type.GetType("System.String", false, ignoreCase)|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion

    #region EOE036 - Reflection over members

    [Fact]
    public Task EOE036_GetProperties()
    {
        const string Source = """
                              using System;
                              using System.Reflection;

                              namespace AotTest;

                              public class MyService
                              {
                                  public PropertyInfo[] GetProps()
                                  {
                                      return {|EOE036:typeof(string).GetProperties()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE036_GetMethods()
    {
        const string Source = """
                              using System;
                              using System.Reflection;

                              namespace AotTest;

                              public class MyService
                              {
                                  public MethodInfo[] GetMethods()
                                  {
                                      return {|EOE036:typeof(string).GetMethods()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE036_GetConstructors()
    {
        const string Source = """
                              using System;
                              using System.Reflection;

                              namespace AotTest;

                              public class MyService
                              {
                                  public ConstructorInfo[] GetCtors()
                                  {
                                      return {|EOE036:typeof(string).GetConstructors()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE036_GetFields()
    {
        const string Source = """
                              using System;
                              using System.Reflection;

                              namespace AotTest;

                              public class MyService
                              {
                                  public FieldInfo[] GetFields()
                                  {
                                      return {|EOE036:typeof(string).GetFields()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    [Fact]
    public Task EOE036_GetMembers()
    {
        const string Source = """
                              using System;
                              using System.Reflection;

                              namespace AotTest;

                              public class MyService
                              {
                                  public MemberInfo[] GetMembers()
                                  {
                                      return {|EOE036:typeof(string).GetMembers()|};
                                  }
                              }
                              """;

        return VerifyAsync(Source);
    }

    #endregion
}
