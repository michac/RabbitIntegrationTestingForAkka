using System.Threading;
using System.Threading.Tasks;

namespace RabbitIntegrationTestingForAkka
{
    public static class CancellationTokenExtensions
    {
        public static Task WaitForCancellation(this CancellationToken token)
        {
            // create task completion source, register it to the token, then wait for source to complete
            var source = new TaskCompletionSource<int>();
            token.Register(() => source.SetResult(0));
            return source.Task;
        }
    }
}