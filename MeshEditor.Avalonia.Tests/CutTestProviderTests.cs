using MeshEditor.Cuts;
using NUnit.Framework;

namespace MeshEditor.AvaloniaUI.Tests
{
	/// <summary>
	/// Expressions of the cut editor ("Cut / Select area" by expression), compiled at runtime.
	/// </summary>
	public class CutTestProviderTests
	{
		[Test]
		public void Default_expression_of_the_cut_editor()
		{
			CutTest test = CutTestProvider.ProvideTestFunction(@"x >= 0.0 and y >= 0.0");

			Assert.That(test(1, 2, 3), Is.True);
			Assert.That(test(-1, 2, 3), Is.False);
			Assert.That(test(1, -2, 3), Is.False);
		}

		[TestCase("sqrt(x*x + y*y) < 1.5 or z > 10", 1f, 1f, 0f, true)]
		[TestCase("sqrt(x*x + y*y) < 1.5 or z > 10", 2f, 2f, 0f, false)]
		[TestCase("sqrt(x*x + y*y) < 1.5 or z > 10", 2f, 2f, 11f, true)]
		[TestCase("abs(x) <= max(y, z) AND pow(2, 3) == 8", -2f, 1f, 2f, true)]
		[TestCase("x > 1/2", 0.1f, 0f, 0f, true)] // C# integer division: 1/2 == 0
		public void Expressions_have_csharp_semantics(string expression, float x, float y, float z, bool expected)
		{
			Assert.That(CutTestProvider.ProvideTestFunction(expression)(x, y, z), Is.EqualTo(expected));
		}

		[Test]
		public void Invalid_expression_reports_compilation_errors()
		{
			var exception = Assert.Throws<RuntimeCompilationException>(() => CutTestProvider.ProvideTestFunction("x >= "));
			Assert.That(exception.GetErrorMessages(), Is.Not.Empty);
		}
	}
}
