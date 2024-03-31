namespace InnerTube.Tests;

public class SearchTests
{
	private InnerTube _innerTube;

	[SetUp]
	public void Setup()
	{
		_innerTube = new InnerTube();
	}

	[TestCase("Big Buck Bunny", null, TestName = "Normal search")]
	public async Task Search(string query, string paramArgs)
	{
		Assert.Pass(Convert.ToBase64String(await _innerTube.SearchAsync(query)));
	}
}