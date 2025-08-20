namespace Skinde.Tests
{
    public class BranchTests
    {
        [Fact]
        public void CurrentBranch()
        {
            var branchName = Build.GitHelper.GetCurrentBranchName();
            Assert.NotNull(branchName);
        }
    }
}