
namespace TestTrackingDiagrams.PlantUml
{
    [Serializable]
    internal class TestTrackingDiagramsException : Exception
    {
        public TestTrackingDiagramsException()
        {
        }

        public TestTrackingDiagramsException(string? message) : base(message)
        {
        }

        public TestTrackingDiagramsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}