using Moq;
using AutoMapper;
using NatacaoAPI.DTOs.Turma;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NatacaoAPI.Tests.Services
{
    public class TurmaServiceTests
    {
        private readonly Mock<ITurmaRepository> _mockTurmaRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly TurmaService _service;

        public TurmaServiceTests()
        {
            _mockTurmaRepo = new Mock<ITurmaRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new TurmaService(_mockTurmaRepo.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetAllAsync_DeveRetornarTurmasComVagasCalculadas()
        {
            // Arrange
            var turmas = new List<Turma>
            {
                new Turma { Id = 1, Nome = "Turma A", CapacidadeMaxima = 10, DataHoraInicio = DateTime.Now, DataHoraFim = DateTime.Now.AddHours(1), Professor = new Usuario { Nome = "Prof. Maria" } },
                new Turma { Id = 2, Nome = "Turma B", CapacidadeMaxima = 5, DataHoraInicio = DateTime.Now, DataHoraFim = DateTime.Now.AddHours(1), Professor = new Usuario { Nome = "Prof. João" } }
            };

            _mockTurmaRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(turmas);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(1)).ReturnsAsync(3);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(2)).ReturnsAsync(5); // Lotada

            _mockMapper.Setup(m => m.Map<TurmaResponseDTO>(It.Is<Turma>(t => t.Id == 1)))
                .Returns(new TurmaResponseDTO { Id = 1, Nome = "Turma A", CapacidadeMaxima = 10 });
            _mockMapper.Setup(m => m.Map<TurmaResponseDTO>(It.Is<Turma>(t => t.Id == 2)))
                .Returns(new TurmaResponseDTO { Id = 2, Nome = "Turma B", CapacidadeMaxima = 5 });

            // Act
            var result = (await _service.GetAllAsync()).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(7, result[0].VagasDisponiveis);
            Assert.Equal(0, result[1].VagasDisponiveis);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarNull_QuandoTurmaNaoExiste()
        {
            // Arrange
            _mockTurmaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Turma?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_DeveRetornarTurmaComVagas()
        {
            // Arrange
            var turma = new Turma { Id = 1, Nome = "Turma Teste", CapacidadeMaxima = 15, Professor = new Usuario { Nome = "Prof. Ana" } };
            _mockTurmaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turma);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(1)).ReturnsAsync(8);
            _mockMapper.Setup(m => m.Map<TurmaResponseDTO>(turma))
                .Returns(new TurmaResponseDTO { Id = 1, Nome = "Turma Teste", CapacidadeMaxima = 15 });

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result!.VagasDisponiveis);
        }

        [Fact]
        public async Task DeleteAsync_DeveRetornarFalse_QuandoTurmaNaoExiste()
        {
            // Arrange
            _mockTurmaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Turma?)null);

            // Act
            var result = await _service.DeleteAsync(999, professorId: 1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_DeveRetornarTrue_QuandoTurmaExiste()
        {
            // Arrange
            var turma = new Turma { Id = 1, Nome = "Turma Delete", ProfessorId = 1 };
            _mockTurmaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turma);
            _mockTurmaRepo.Setup(r => r.DeleteAsync(turma)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(1, professorId: 1);

            // Assert
            Assert.True(result);
            _mockTurmaRepo.Verify(r => r.DeleteAsync(turma), Times.Once);
        }
    }
}