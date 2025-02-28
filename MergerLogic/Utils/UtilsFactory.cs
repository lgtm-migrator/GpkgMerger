﻿using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using MergerLogic.Clients;
using MergerLogic.ImageProcessing;

namespace MergerLogic.Utils
{
    public class UtilsFactory : IUtilsFactory
    {
        private readonly IPathUtils _pathUtils;
        private readonly ITimeUtils _timeUtils;
        private readonly IGeoUtils _geoUtils;
        private readonly IFileSystem _fileSystem;
        private readonly IServiceProvider _container;
        private readonly IHttpRequestUtils _httpRequestUtils;
        private readonly IImageFormatter _imageFormatter;

        public UtilsFactory(IPathUtils pathUtils, ITimeUtils timeUtils, IGeoUtils geoUtils, IFileSystem fileSystem, IServiceProvider container,
            IHttpRequestUtils httpRequestUtils, IImageFormatter formatter)
        {
            this._pathUtils = pathUtils;
            this._timeUtils = timeUtils;
            this._geoUtils = geoUtils;
            this._fileSystem = fileSystem;
            this._container = container;
            this._httpRequestUtils = httpRequestUtils;
            this._imageFormatter = formatter;
        }

        #region dataUtils

        public IFileClient GetFileUtils(string path)
        {
            return new FileClient(path, this._geoUtils, this._fileSystem, this._imageFormatter);
        }

        public IGpkgClient GetGpkgUtils(string path)
        {
            var logger = this._container.GetRequiredService<ILogger<GpkgClient>>();
            return new GpkgClient(path, this._timeUtils, logger, this._fileSystem, this._geoUtils, this._imageFormatter);
        }

        public IHttpSourceClient GetHttpUtils(string path)
        {
            IPathPatternUtils pathPatternUtils = this.GetPathPatternUtils(path);
            return new HttpSourceClient(path, this._httpRequestUtils, pathPatternUtils, this._geoUtils, this._imageFormatter);
        }

        public IS3Client GetS3Utils(string path)
        {
            string bucket = this._container.GetRequiredService<IConfigurationManager>().GetConfiguration("S3", "bucket");
            IAmazonS3? client = this._container.GetService<IAmazonS3>();
            if (client is null || bucket == string.Empty)
            {
                throw new Exception("S3 Data utils requires s3 client to be configured");
            }

            return new S3Client(client, this._pathUtils, this._geoUtils, this._imageFormatter, bucket, path);
        }

        public T GetDataUtils<T>(string path) where T : IDataUtils
        {
            if (typeof(IFileClient).IsAssignableFrom(typeof(T)))
            {
                return (T)(Object)this.GetFileUtils(path);
            }
            if (typeof(IGpkgClient).IsAssignableFrom(typeof(T)))
            {
                return (T)(Object)this.GetGpkgUtils(path);
            }
            if (typeof(IHttpSourceClient).IsAssignableFrom(typeof(T)))
            {
                return (T)(Object)this.GetHttpUtils(path);
            }
            if (typeof(IS3Client).IsAssignableFrom(typeof(T)))
            {
                return (T)(Object)this.GetS3Utils(path);
            }
            throw new NotImplementedException("Invalid Utils type");
        }

        #endregion dataUtils

        public IPathPatternUtils GetPathPatternUtils(string pattern)
        {
            return new PathPatternUtils(pattern);
        }
    }
}
