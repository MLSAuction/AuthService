using Moq;
using AuthService.Repositories;
using AuthService.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AuthService.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuthServiceTests
{
    [TestFixture]
    public class AuthTests
    {
        private Mock<IAuthRepository> _authRepositoryStub;
        private AuthController _authController;

        [SetUp]
        public void Setup()
        {
            _authRepositoryStub = new Mock<IAuthRepository>();
            var loggerStub = new Mock<ILogger<AuthController>>();
            var configurationStub = new Mock<IConfiguration>();

            _authController = new AuthController(loggerStub.Object, configurationStub.Object, _authRepositoryStub.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _authController.Dispose();
        }

        [Test]
        [TestCase("John", "Doe", true)]
        [TestCase("Jane", "Deere", false)]
        public async Task LoginReturnsJwt(string username, string password, bool expectedResult)
        {
            // arrange
            AuthDTO authDTO = new AuthDTO { Username = username, Password = password };

            if (expectedResult)
            {
                _authRepositoryStub.Setup(repo => repo.Login(authDTO)).ReturnsAsync("dummyToken");
            }
            else
            {
                _authRepositoryStub.Setup(repo => repo.Login(authDTO)).ReturnsAsync((string)null);
            }

            // act
            var result = await _authController.Login(authDTO);

            // assert
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.IsInstanceOf<string>(okResult.Value);
            }
            else
            {
                Assert.IsInstanceOf<BadRequestObjectResult>(result);
                var badResult = (BadRequestObjectResult)result;

                Assert.AreEqual(400, badResult.StatusCode);
            }
        }
    }
}