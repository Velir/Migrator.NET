using Migrator.Providers;
using NUnit.Framework;

namespace Migrator.Tests.Providers
{
	[TestFixture]
	public class GenericProviderTests
	{
		[Test]
		public void CanJoinColumnsAndValues()
		{
			GenericTransformationProvider provider = new GenericTransformationProvider();
			string result = provider.JoinColumnsAndValues(new string[] { "foo", "bar" }, new string[] { "123", "456" });

			Assert.AreEqual("foo='123', bar='456'", result);
		}
	}

	class GenericTransformationProvider : TransformationProvider
	{
		private const int DEFAULT_COMMAND_TIMEOUT = 30;

		public GenericTransformationProvider()
			: base(null, null, DEFAULT_COMMAND_TIMEOUT)
		{
		}

		public override bool ConstraintExists(string table, string name)
		{
			return false;
		}
	}
}