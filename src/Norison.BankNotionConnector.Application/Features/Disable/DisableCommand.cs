using MediatR;

namespace Norison.BankNotionConnector.Application.Features.Disable;

public class DisableCommand : IRequest
{
    public long ChatId { get; set; }
}