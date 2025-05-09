using Qdrant.Client;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using System.Threading.Tasks;

namespace AiChatBackend.Services;

public class QdrantService(ILogger<QdrantService> logger, IConfiguration config)
{
    private readonly ILogger<QdrantService> logger = logger;
    private readonly string host = config["Qdrant:Host"];
    private readonly int port = int.Parse(config["Qdrant:Port"]);

    public async Task<bool> IsCollectionExistAsync(string collectionName, CancellationToken ct = default)
    {
        try
        {
            using var client = new QdrantClient(host, port);
            return await client.CollectionExistsAsync(collectionName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return false;
        }
    }

    public async Task<ResponseBase> CreateCollectionAsync(string collectionName, CancellationToken ct = default)
    {
        try
        {
            using var client = new QdrantClient(host, port);

            if (await IsCollectionExistAsync(collectionName, ct))
                return new() { IsSuccess = false, Message = "Collection already exist" };

            await client.CreateCollectionAsync(collectionName, vectorsConfig: new VectorParams()
            {
                Size = 100,
                Distance = Distance.Cosine
            }, cancellationToken: ct);

            return new() { IsSuccess = true };
        }
        catch (Exception ex)
        {
            return new() { IsSuccess = false, Message = ex.Message };
        }
    }

    public async Task<ResponseBase> UpsertAsync(string collectionName, CancellationToken ct = default)
    {
        try
        {
            using var client = new QdrantClient(host, port);
            var qvs = new QdrantVectorStore(client);

            if (!await IsCollectionExistAsync(collectionName, ct))
            {
                var resp = await CreateCollectionAsync(collectionName, ct);
                if (!resp.IsSuccess)
                    return resp;
            }

            List<PointStruct> points = [];
            PointStruct ps = new()
            {
                 
            };


            var upsertResp = await client.UpsertAsync(collectionName, points, cancellationToken: ct);
            var r = new
            {
                upsertResp.HasOperationId,
                upsertResp.Status,
                upsertResp.OperationId
            };

            return new()
            {
                IsSuccess = upsertResp.Status != UpdateStatus.ClockRejected || upsertResp.Status != UpdateStatus.UnknownUpdateStatus,
                Message = JsonSerializer.Serialize(r)
            };

        }
        catch (Exception ex)
        {
            return new() { IsSuccess = false, Message = ex.Message };
        }
    }


}
