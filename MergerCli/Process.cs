using MergerCli.Utils;
using MergerLogic.Batching;
using MergerLogic.DataTypes;
using MergerLogic.ImageProcessing;
using MergerLogic.Utils;
using Microsoft.Extensions.Logging;

namespace MergerCli
{
    internal class Process : IProcess
    {
        private Func<Coord, Tile?> _getTileByCoord;

        private readonly IConfigurationManager _config;
        private readonly ITileMerger _tileMerger;
        private readonly ILogger _logger;

        public Process(IConfigurationManager configuration, ITileMerger tileMerger, ILogger<Process> logger)
        {
            this._config = configuration;
            this._tileMerger = tileMerger;
            this._logger = logger;
        }

        public void Start(TileFormat targetFormat, IData baseData, IData newData, int batchSize,
            BatchStatusManager batchStatusManager)
        {
            batchStatusManager.InitializeLayer(newData.Path);
            List<Tile> tiles = new List<Tile>(batchSize);
            long totalTileCount = newData.TileCount();
            long tileProgressCount = 0;

            string? resumeBatchIdentifier = batchStatusManager.GetLayerBatchIdentifier(newData.Path);
            if (resumeBatchIdentifier != null)
            {
                newData.setBatchIdentifier(resumeBatchIdentifier);
                // fix resume progress bug for gpkg, fs and web, fixing it for s3 requires storing additional data.
                if (newData.Type != DataType.S3)
                {
                    tileProgressCount = int.Parse(resumeBatchIdentifier);
                }
            }

            this._logger.LogInformation($"Total amount of tiles to merge: {totalTileCount - tileProgressCount}");

            bool uploadOnly = _config.GetConfiguration<bool>("GENERAL", "uploadOnly");
            _getTileByCoord = uploadOnly || baseData.IsNew ?
                (_) => null
                :
                (targetCoords) => baseData.GetCorrespondingTile(targetCoords, true);

            do
            {
                List<Tile> newTiles = newData.GetNextBatch(out string batchIdentifier);
                batchStatusManager.SetCurrentBatch(newData.Path, batchIdentifier);

                tiles.Clear();
                for (int i = 0; i < newTiles.Count; i++)
                {
                    var newTile = newTiles[i];
                    var targetCoords = newTile.GetCoord();
                    List<CorrespondingTileBuilder> correspondingTileBuilders = new List<CorrespondingTileBuilder>()
                    {
                        () => _getTileByCoord(targetCoords),
                        () => newTile
                    };

                    byte[]? image = this._tileMerger.MergeTiles(correspondingTileBuilders, targetCoords, targetFormat);

                    if (image != null)
                    {
                        newTile = new Tile(newTile.Z, newTile.X, newTile.Y, image);
                        tiles.Add(newTile);
                    }
                }

                baseData.UpdateTiles(tiles);

                tileProgressCount += tiles.Count;
                this._logger.LogInformation($"Tile Count: {tileProgressCount} / {totalTileCount}");
            } while (tiles.Count == batchSize);

            batchStatusManager.CompleteLayer(newData.Path);
            newData.Reset();
            // base data wrap up is in program as the same base data object is used in multiple calls 
        }

        public void Validate(IData baseData, IData newData)
        {
            List<Tile> newTiles;
            bool hasSameTiles = true;

            long totalTileCount = newData.TileCount();
            long tilesChecked = 0;
            this._logger.LogInformation($"Base tile Count: {baseData.TileCount()}, New tile count: {totalTileCount}");

            do
            {
                newTiles = newData.GetNextBatch(out _);

                int baseMatchCount = 0;
                int newTileCount = 0;
                for (int i = 0; i < newTiles.Count; i++)
                {
                    Tile newTile = newTiles[i];
                    bool baseTileExists = baseData.TileExists(newTile.GetCoord());

                    if (baseTileExists)
                    {
                        ++baseMatchCount;
                    }
                    else
                    {
                        this._logger.LogError($"Missing tile: {newTile}");
                    }
                }

                newTileCount += newTiles.Count;
                tilesChecked += newTiles.Count;
                this._logger.LogInformation($"Total tiles checked: {tilesChecked}/{totalTileCount}");
                hasSameTiles = newTileCount == baseMatchCount;
            } while (hasSameTiles && newTiles.Count > 0);

            newData.Reset();

            this._logger.LogInformation($"Target's valid: {hasSameTiles}");
        }
    }
}
