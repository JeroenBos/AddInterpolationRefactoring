using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeRefactorings;
using RoslynNUnitLight;

namespace AddInterpolationRefactoring.Tests
{
    [TestClass]
    public class CodeRefactoringTester : CodeRefactoringTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeRefactoringProvider CreateProvider()
        {
            return new AddInterpolationRefactoringCodeRefactoringProvider();
        }

        [TestMethod]
        public void EmptyStringTest()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = ""[||]"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $"""";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void EmptyVerbatimStringTest()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = @""[||]"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $@"""";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void StringTest()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = ""[|TEST|]"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $""TEST"";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void VerbatimStringTest()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = @""[|TEST|]"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $@""TEST"";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void AtOpeningDoubleQuote()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = [||]""TEST"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $""TEST"";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void AtClosingDoubleQuote()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = ""TEST""[||];
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $""TEST"";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void AtVerbatimCharacter()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = [||]@""TEST"";
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $@""TEST"";
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void InOpenEndedString()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = ""TEST[||];
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $""TEST;
            }";

            TestCodeRefactoring(markupCode, expected);
        }
        [TestMethod]
        public void InOpenEndedVerbatimString()
        {
            const string markupCode =
            @"public static void M() 
            {
                var x = @""TEST[||];
            }";

            const string expected =
            @"public static void M() 
            {
                var x = $@""TEST;
            }";

            TestCodeRefactoring(markupCode, expected);
        }
    }
}
