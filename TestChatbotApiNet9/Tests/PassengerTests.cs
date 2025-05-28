using ChatbotApiNet9.Models;

namespace TestChatbotApiNet9.Tests
{
    public class PassengerTests
    {
        [Fact]
        public void ValidateCPF_ValidCPF_ReturnsTrue()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "52998224725", // CPF válido para teste
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateCPF();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateCPF_InvalidCPF_ReturnsFalse()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "11111111111", // CPF inválido (todos os dígitos iguais)
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateCPF();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateCPF_FormattedCPF_ReturnsTrue()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "529.982.247-25", // CPF válido formatado
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateCPF();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateRG_ValidRG_ReturnsTrue()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12.345.678-9",
                CPF = "52998224725",
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateRG();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateRG_ShortRG_ReturnsFalse()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "123456", // RG muito curto
                CPF = "52998224725",
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateRG();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateBirthDate_ValidDate_ReturnsTrue()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "52998224725",
                BirthDate = new DateTime(1990, 1, 1)
            };

            // Act
            bool result = passenger.ValidateBirthDate();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateBirthDate_FutureDate_ReturnsFalse()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "52998224725",
                BirthDate = DateTime.Today.AddDays(1) // Data futura
            };

            // Act
            bool result = passenger.ValidateBirthDate();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateBirthDate_TooRecent_ReturnsFalse()
        {
            // Arrange
            var passenger = new Passenger
            {
                Name = "João Silva",
                RG = "12345678",
                CPF = "52998224725",
                BirthDate = DateTime.Today.AddYears(-1) // Menos de 2 anos
            };

            // Act
            bool result = passenger.ValidateBirthDate();

            // Assert
            Assert.False(result);
        }
    }
}
