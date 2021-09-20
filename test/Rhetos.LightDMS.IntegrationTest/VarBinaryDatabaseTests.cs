﻿using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Rhetos.LightDMS.IntegrationTest.Utilities;
using Rhetos.LightDMS.TestApp;
using Rhetos.Utilities;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Rhetos.LightDMS.IntegrationTest
{
    public class VarBinaryDatabaseTests : IDisposable
    {
        private static WebApplicationFactory<Startup> _factory;

        private const string _fileName = "DownloadSqlFileStreamTest.txt";
        private readonly Guid _documentVersionId = Guid.NewGuid();
        private readonly string _fileContent = Guid.NewGuid().ToString();
        private readonly Guid _fileContentId = Guid.NewGuid();

        public VarBinaryDatabaseTests()
        {
            var connectionString = new ConnectionString(TestConfigurations.Instance.VarBinaryDatabaseConnString);
            _factory = new CustomWebApplicationFactory<Startup>(container =>
            {
                container.Register(context => connectionString).SingleInstance();
            });

            TestDataUtilities.SeedDocumentVersionAndFileContent(_factory,
                _documentVersionId,
                _fileContentId,
                _fileName,
                _fileContent,
                useFileStream: false);
        }

        public void Dispose()
        {
            try
            {
                TestDataUtilities.CleanupDocumentVersionAndFileContent(_factory, _documentVersionId, _fileContentId);
            }
            finally
            {
                _factory.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task Upload_ShouldSucceed()
        {
            // Arrange
            var client = _factory.CreateClient();
            using var request = TestDataUtilities.GenerateUploadRequest(1);

            // Act
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            var uploadResponse = JsonSerializer.Deserialize<UploadSuccessResponse>(responseBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(uploadResponse);
        }

        [Theory]
        [InlineData("LightDMS/Download/{0}")]
        [InlineData("LightDMS/Download?id={0}")]
        public async Task Download_ShouldSucceed(string route)
        {
            // Arrange
            var client = _factory.CreateClient();
            var requestUri = string.Format(route, _documentVersionId);
            var downloadRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);

            // Act
            var response = await client.SendAsync(downloadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(_fileContent, responseContent);
        }

        [Theory]
        [InlineData("LightDMS/DownloadPreview/{0}?filename={1}")]
        [InlineData("LightDMS/DownloadPreview?id={0}&filename={1}")]
        public async Task DownloadPreview_ShouldSucceed(string route)
        {
            // Arrange
            var client = _factory.CreateClient();
            var requestUri = string.Format(route, _fileContentId, _fileName);
            var downloadRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);

            // Act
            var response = await client.SendAsync(downloadRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(_fileContent, responseContent);
        }
    }
}
