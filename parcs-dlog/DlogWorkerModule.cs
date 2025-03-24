using Parcs.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace parcs.Dlog
{
    public class DlogWorkerModule : IModule
    {
        public async Task RunAsync(IModuleInfo moduleInfo, CancellationToken cancellationToken = default)
        {
            long pLong = await moduleInfo.Parent.ReadLongAsync();
            long gLong = await moduleInfo.Parent.ReadLongAsync();
            long hLong = await moduleInfo.Parent.ReadLongAsync();
            long startLong = await moduleInfo.Parent.ReadLongAsync();
            long endLong = await moduleInfo.Parent.ReadLongAsync();
            long stepLong = await moduleInfo.Parent.ReadLongAsync();

            BigInteger p = new BigInteger(pLong);
            BigInteger g = new BigInteger(gLong);
            BigInteger h = new BigInteger(hLong);
            BigInteger start = new BigInteger(startLong);
            BigInteger end = new BigInteger(endLong);
            BigInteger step = new BigInteger(stepLong);

            BigInteger current = BigInteger.ModPow(g, start, p);
            BigInteger factor = BigInteger.ModPow(g, step, p);

            long? solution = null;
            for (BigInteger x = start; x < end; x += step)
            {
                if (current == h && solution == null)
                {
                    solution = (long)x;
                    await moduleInfo.Parent.WriteDataAsync(true);
                    await moduleInfo.Parent.WriteDataAsync(solution.Value);
                }
                current = (current * factor) % p;
            }

        }
    }
}
