//using AdmMaster.DataContracts.Base;
//using Polly;
//using Polly.Retry;
//using System.Net;

//namespace PraJah.Backend.Domain.Services.Base.Retry
//{
//    /// <summary>
//    /// Contains methods that defines the retry policy.
//    /// </summary>
//    public static class RetryPolicy
//    {
//        /// <summary>
//        /// Gets the retry policy.
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="resultPredicate"></param>
//        /// <param name="retryCount"></param>
//        /// <param name="retryIntervalInMiliseconds"></param>
//        /// <returns></returns>
//        public static AsyncRetryPolicy<T> Get<T>(Func<T, bool> resultPredicate, int retryCount, int retryIntervalInMiliseconds)
//        {
//            return Policy
//                .HandleResult(resultPredicate)
//                .Or<Exception>()
//                .WaitAndRetryAsync(
//                    retryCount: retryCount,
//                    sleepDurationProvider => TimeSpan.FromMilliseconds(retryIntervalInMiliseconds),
//                    onRetry: (retryException, timeSpan, retryNumber, context) =>
//                    {
//                        // TODO: logar retentativas.
//                    });
//        }

//        /// <summary>
//        /// Gets the retry policy for an HTTP operation.
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="retryCount"></param>
//        /// <param name="retryIntervalInMiliseconds"></param>
//        /// <returns></returns>
//        public static AsyncRetryPolicy<T> Get<T>(int retryCount, int retryIntervalInMiliseconds)
//            where T : OperationResponseBase
//        {
//            var httpStatusCodesToRetry = new List<HttpStatusCode>
//            {
//                HttpStatusCode.InternalServerError,
//                HttpStatusCode.ServiceUnavailable
//            };

//            return Get(
//                new Func<T, bool>(r => httpStatusCodesToRetry.Contains(r.HttpStatusCode)),
//                retryCount,
//                retryIntervalInMiliseconds);
//        }
//    }
//}
