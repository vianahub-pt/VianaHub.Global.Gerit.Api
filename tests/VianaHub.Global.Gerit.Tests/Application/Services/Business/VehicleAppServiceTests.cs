using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Vehicle;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Services.Business
{
    public class VehicleAppServiceTests
    {
        [Fact(DisplayName = "VehicleAppService - BulkUpload retorna false sem ficheiro")]
        [Trait("Application", "")]
        public async Task BulkUploadAsync_ShouldReturnFalse_WhenNoFile()
        {
            var repoMock = new Mock<IVehicleDataRepository>();
            var domainMock = new Mock<IVehicleDomainService>();
            var notifyMock = new Mock<INotify>();
            var localizationMock = new Mock<ILocalizationService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var fileValidationMock = new Mock<IFileValidationService>();

            fileValidationMock.Setup(f => f.ValidateFile(null)).Returns(false);

            var service = new VehicleAppService(repoMock.Object, domainMock.Object, notifyMock.Object, localizationMock.Object, currentUserMock.Object, fileValidationMock.Object);

            var result = await service.BulkUploadAsync(null, CancellationToken.None);

            Assert.False(result);
            fileValidationMock.Verify(f => f.ValidateFile(null), Times.Once);
        }

        [Fact(DisplayName = "VehicleAppService - BulkUpload processa CSV valido")]
        [Trait("Application", "")]
        public async Task BulkUploadAsync_ShouldProcessValidCsv()
        {
            var repoMock = new Mock<IVehicleDataRepository>();
            var domainMock = new Mock<IVehicleDomainService>();
            var notifyMock = new Mock<INotify>();
            var localizationMock = new Mock<ILocalizationService>();
            var currentUserMock = new Mock<ICurrentUserService>();
            var fileValidationMock = new Mock<IFileValidationService>();

            currentUserMock.Setup(c => c.GetTenantId()).Returns(1);

            // build CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Plate;Brand;Model;Year;Color;FuelType");
            csv.AppendLine("ABC-1234;Ford;Focus;2018;Red;Gasoline");
            csv.AppendLine("XYZ-9876;Toyota;Corolla;2020;Blue;Diesel");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var stream = new MemoryStream(bytes);

            var formFile = new FormFile(stream, 0, bytes.Length, "file", "vehicles.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            fileValidationMock.Setup(f => f.ValidateFile(It.IsAny<IFormFile>())).Returns(true);
            repoMock.Setup(r => r.ExistsByPlateAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            domainMock.Setup(d => d.CreateAsync(It.IsAny<VianaHub.Global.Gerit.Domain.Entities.Business.VehicleEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var service = new VehicleAppService(repoMock.Object, domainMock.Object, notifyMock.Object, localizationMock.Object, currentUserMock.Object, fileValidationMock.Object);

            var result = await service.BulkUploadAsync(formFile, CancellationToken.None);

            Assert.True(result);
            fileValidationMock.Verify(f => f.ValidateFile(It.IsAny<IFormFile>()), Times.Once);
            repoMock.Verify(r => r.ExistsByPlateAsync(1, "ABC-1234", It.IsAny<CancellationToken>()), Times.Once);
            repoMock.Verify(r => r.ExistsByPlateAsync(1, "XYZ-9876", It.IsAny<CancellationToken>()), Times.Once);
            domainMock.Verify(d => d.CreateAsync(It.IsAny<VianaHub.Global.Gerit.Domain.Entities.Business.VehicleEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
