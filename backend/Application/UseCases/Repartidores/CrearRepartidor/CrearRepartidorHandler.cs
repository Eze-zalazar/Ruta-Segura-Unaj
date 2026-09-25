namespace Application.UseCases.Repartidores.CrearRepartidor;

using Application.Interfaces.Persistence;
using Domain.Entities;
using Domain.Exceptions;

public class CrearRepartidorHandler
{
    private readonly IRepartidorRepository _repartidorRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CrearRepartidorHandler(
        IRepartidorRepository repartidorRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _repartidorRepository = repartidorRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(CrearRepartidorCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Password))
            throw new DomainException("La contraseña no puede estar vacía.");

        // Validar unicidad de email
        var usuarioExistente = await _usuarioRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (usuarioExistente != null)
            throw new DomainException("Ya existe un usuario registrado con el email especificado.");

        // Hashing simulado/básico (la autenticación completa con salt/BCrypt corresponde a RF01)
        var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(command.Password));

        var repartidor = new Repartidor(
            command.Nombre,
            command.Email,
            command.Telefono,
            passwordHash,
            command.Vehiculo,
            command.Disponible);

        await _repartidorRepository.AddAsync(repartidor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return repartidor.Id;
    }
}
