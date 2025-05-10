using NUnit.Framework;
using Unity.PerformanceTesting;

public class SamplePerformanceTest
{
    [Test, Performance, Category("performance")]
    public void SimpleMethod_PerformanceTest()
    {
        Measure.Method(() =>
        {
            // Code à mesurer
            var sum = 0;
            for (int i = 0; i < 1000; i++)
                sum += i;
        })
        .WarmupCount(5)
        .MeasurementCount(20)
        .Run();
    }
}