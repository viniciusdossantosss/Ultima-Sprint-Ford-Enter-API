using Moq;
using AutoMapper;
using NatacaoAPI.DTOs.Reserva;
using NatacaoAPI.Models;
using NatacaoAPI.Repositories.Interfaces;
using NatacaoAPI.Services;

namespace NatacaoAPI.Tests.Services
{
    /// <summary>
    /// Testes unitários para ReservaService.
    /// Cobrem estritamente as regras de negócio RF001 (capacidade) e RF002 (conflito de horário).
    ///
    /// Padrão: Arrange-Act-Assert (AAA)
    /// Isolamento: Moq para simular repositórios (sem banco de dados real)
    /// </summary>
    public class ReservaServiceTests
    {
        private readonly Mock<IReservaRepository> _mockReservaRepo;
        private readonly Mock<ITurmaRepository> _mockTurmaRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ReservaService _service;

        public ReservaServiceTests()
        {
            _mockReservaRepo = new Mock<IReservaRepository>();
            _mockTurmaRepo = new Mock<ITurmaRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new ReservaService(
                _mockReservaRepo.Object,
                _mockTurmaRepo.Object,
                _mockMapper.Object);
        }

        // ═══════════════════════════════════════════════════════════
        // RF001: Capacidade máxima da turma impede nova reserva
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateAsync_DeveRetornar400_QuandoCapacidadeMaximaAtingida()
        {
            // Arrange
            var turma = new Turma
            {
                Id = 1,
                Nome = "Turma Teste",
                CapacidadeMaxima = 2, // Limite de 2 alunos
                DiaSemana = DiaSemana.Segunda,
                HorarioInicio = new TimeSpan(8, 0, 0),
                HorarioFim = new TimeSpan(9, 0, 0)
            };

            _mockTurmaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turma);
            // Já existem 2 reservas ativas = turma lotada
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(1)).ReturnsAsync(2);

            var dto = new ReservaCreateDTO { TurmaId = 1 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(dto, alunoId: 99));

            Assert.Contains("RF001", ex.Message);
            Assert.Contains("capacidade máxima", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_DevePermitirReserva_QuandoHaVagas()
        {
            // Arrange
            var turma = new Turma
            {
                Id = 1,
                Nome = "Turma Com Vagas",
                CapacidadeMaxima = 10,
                DiaSemana = DiaSemana.Quarta,
                HorarioInicio = new TimeSpan(14, 0, 0),
                HorarioFim = new TimeSpan(15, 0, 0)
            };

            var reservaCriada = new Reserva
            {
                Id = 1,
                AlunoId = 5,
                TurmaId = 1,
                Status = StatusReserva.Ativa,
                Aluno = new Usuario { Nome = "Aluno Teste" },
                Turma = turma
            };

            var responseDto = new ReservaResponseDTO
            {
                Id = 1,
                AlunoId = 5,
                TurmaNome = "Turma Com Vagas",
                Status = "Ativa"
            };

            _mockTurmaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(turma);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(1)).ReturnsAsync(3); // 3 de 10
            _mockReservaRepo.Setup(r => r.AlunoJaReservouTurmaAsync(5, 1)).ReturnsAsync(false);
            _mockReservaRepo.Setup(r => r.AlunoHasConflictAsync(5, DiaSemana.Quarta,
                new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0))).ReturnsAsync(false);
            _mockReservaRepo.Setup(r => r.CreateAsync(It.IsAny<Reserva>())).ReturnsAsync(reservaCriada);
            _mockReservaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reservaCriada);
            _mockMapper.Setup(m => m.Map<ReservaResponseDTO>(It.IsAny<Reserva>())).Returns(responseDto);

            // Act
            var result = await _service.CreateAsync(new ReservaCreateDTO { TurmaId = 1 }, alunoId: 5);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Ativa", result.Status);
            Assert.Equal(5, result.AlunoId);
            _mockReservaRepo.Verify(r => r.CreateAsync(It.IsAny<Reserva>()), Times.Once);
        }

        // ═══════════════════════════════════════════════════════════
        // RF002: Conflito de horário impede nova reserva
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateAsync_DeveRetornar400_QuandoAlunoTemConflitoDeHorario()
        {
            // Arrange
            var turma = new Turma
            {
                Id = 2,
                Nome = "Turma Conflito",
                CapacidadeMaxima = 10,
                DiaSemana = DiaSemana.Terca,
                HorarioInicio = new TimeSpan(10, 0, 0),
                HorarioFim = new TimeSpan(11, 0, 0)
            };

            _mockTurmaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(turma);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(2)).ReturnsAsync(5); // Tem vagas
            _mockReservaRepo.Setup(r => r.AlunoJaReservouTurmaAsync(10, 2)).ReturnsAsync(false);
            // Aluno já tem aula na Terça das 10:00-11:00
            _mockReservaRepo.Setup(r => r.AlunoHasConflictAsync(10, DiaSemana.Terca,
                new TimeSpan(10, 0, 0), new TimeSpan(11, 0, 0))).ReturnsAsync(true);

            var dto = new ReservaCreateDTO { TurmaId = 2 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(dto, alunoId: 10));

            Assert.Contains("RF002", ex.Message);
            Assert.Contains("mesmo horário", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_DeveRetornar400_QuandoAlunoJaReservouMesmaTurma()
        {
            // Arrange
            var turma = new Turma
            {
                Id = 3,
                Nome = "Turma Duplicada",
                CapacidadeMaxima = 10,
                DiaSemana = DiaSemana.Quinta,
                HorarioInicio = new TimeSpan(16, 0, 0),
                HorarioFim = new TimeSpan(17, 0, 0)
            };

            _mockTurmaRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(turma);
            _mockTurmaRepo.Setup(r => r.GetActiveReservasCountAsync(3)).ReturnsAsync(5);
            // Aluno já tem reserva ativa nesta turma
            _mockReservaRepo.Setup(r => r.AlunoJaReservouTurmaAsync(7, 3)).ReturnsAsync(true);

            var dto = new ReservaCreateDTO { TurmaId = 3 };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(dto, alunoId: 7));

            Assert.Contains("reserva ativa nesta turma", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_DeveRetornar404_QuandoTurmaNaoExiste()
        {
            // Arrange
            _mockTurmaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Turma?)null);

            var dto = new ReservaCreateDTO { TurmaId = 999 };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.CreateAsync(dto, alunoId: 1));
        }

        // ═══════════════════════════════════════════════════════════
        // RF004: Aluno só pode cancelar suas próprias reservas
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task CancelAsync_DeveLancarExcecao_QuandoAlunoNaoEhDono()
        {
            // Arrange
            var reserva = new Reserva
            {
                Id = 1,
                AlunoId = 5,  // Pertence ao aluno 5
                TurmaId = 1,
                Status = StatusReserva.Ativa
            };

            _mockReservaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reserva);

            // Act & Assert — aluno 99 tenta cancelar reserva do aluno 5
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.CancelAsync(1, alunoId: 99));
        }

        [Fact]
        public async Task CancelAsync_DeveRetornarTrue_QuandoAlunoCancelaSuaReserva()
        {
            // Arrange
            var reserva = new Reserva
            {
                Id = 1,
                AlunoId = 5,
                TurmaId = 1,
                Status = StatusReserva.Ativa
            };

            _mockReservaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reserva);
            _mockReservaRepo.Setup(r => r.UpdateAsync(It.IsAny<Reserva>())).ReturnsAsync(reserva);

            // Act
            var result = await _service.CancelAsync(1, alunoId: 5);

            // Assert
            Assert.True(result);
            Assert.Equal(StatusReserva.Cancelada, reserva.Status);
            _mockReservaRepo.Verify(r => r.UpdateAsync(It.Is<Reserva>(
                rv => rv.Status == StatusReserva.Cancelada)), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_DeveLancarExcecao_QuandoReservaJaCancelada()
        {
            // Arrange
            var reserva = new Reserva
            {
                Id = 1,
                AlunoId = 5,
                TurmaId = 1,
                Status = StatusReserva.Cancelada
            };

            _mockReservaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reserva);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CancelAsync(1, alunoId: 5));

            Assert.Contains("já foi cancelada", ex.Message);
        }
    }
}
