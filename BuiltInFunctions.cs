using System;
using System.Collections.Generic;

public static class BuiltInFunctions
{
    public static List<FunctionElement> GetFunctions()
    {
        List<FunctionElement> functions = new List<FunctionElement>();

        functions.Add(CreateMain());

        return functions;
    }

    private static FunctionElement CreateMain()
    {
        var symbols = LanguageSymbols.Instance;

        LanguageFunction functionInfo = new LanguageFunction();

        functionInfo.Name = "!_Main";
        functionInfo.Arguments = new List<LanguageFunction.FunctionArg>();
        functionInfo.ReturnTypes = new List<LanguageType>();
        
        symbols.AddUserFunction(functionInfo);

        FunctionElement funcElement = new FunctionElement();
        funcElement.Info = functionInfo;
        
        StatementListElement statements = new StatementListElement();

        funcElement.AddChild(statements);

        FunctionCallElement callElement = new FunctionCallElement();

        LanguageFunction mainFuncInfo = null;
        try
        {
            mainFuncInfo = symbols.GetFunction("Main", new List<string>());
        }
        catch(CompilationException)
        {
            Compilation.WriteError("Main() wasn't found. Did you forget it?", -1);
        }
        callElement.FunctionInfo = mainFuncInfo;
        callElement.CallArguments = new List<ValueElement>();

        statements.AddChild(callElement);

        SingleGenOpElement exitElement = new SingleGenOpElement();
        exitElement.Operation = new GenOp{ Code = GenCodes.Exit, ArgCount = 0 };

        statements.AddChild(exitElement);

        return funcElement;
    }
}