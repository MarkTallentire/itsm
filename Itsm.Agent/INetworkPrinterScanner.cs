using Itsm.Common.Models;

namespace Itsm.Agent;

public interface INetworkPrinterScanner
{
    Task<List<NetworkPrinterInfo>> ScanAsync(CancellationToken cancellationToken = default);
}
