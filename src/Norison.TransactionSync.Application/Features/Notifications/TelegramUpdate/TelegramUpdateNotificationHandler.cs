using MediatR;

using Norison.TransactionSync.Application.Features.Commands;
using Norison.TransactionSync.Application.Features.Commands.Disable;
using Norison.TransactionSync.Application.Features.Commands.Enable;
using Norison.TransactionSync.Application.Features.Commands.SetSettings;

using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Norison.TransactionSync.Application.Features.Notifications.TelegramUpdate;

public class TelegramUpdateNotificationHandler(ITelegramBotClient client, ISender sender)
    : INotificationHandler<TelegramUpdateNotification>
{
    public async Task Handle(TelegramUpdateNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Update is not { Type: UpdateType.Message, Message.Type: MessageType.Text })
        {
            return;
        }

        var message = notification.Update.Message!;
        var username = message.Chat.Username!;
        var chatId = message.Chat.Id;
        var text = message.Text!;

        try
        {
            var response = await HandleUpdateAsync(chatId, username, text, cancellationToken);
            await client.SendTextMessageAsync(chatId, response.Message, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            await client.SendTextMessageAsync(chatId,
                "An error occurred while processing your command. Error: " + exception.Message,
                cancellationToken: cancellationToken);
        }
    }

    private async Task<TelegramCommandResponse> HandleUpdateAsync(
        long chatId, string username, string text, CancellationToken cancellationToken)
    {
        return text switch
        {
            "/start" => HandleStartCommandAsync(),
            "/enable" => await HandleEnableCommandAsync(chatId, cancellationToken),
            "/disable" => await HandleDisableCommandAsync(chatId, cancellationToken),
            _ when text.StartsWith("1.") => await HandleSetSettingsCommandAsync(chatId, username, text,
                cancellationToken),
            _ => new TelegramCommandResponse { Message = "I don't understand your command." }
        };
    }

    private static TelegramCommandResponse HandleStartCommandAsync()
    {
        return new TelegramCommandResponse
        {
            Message =
                "Welcome to the Transaction Sync bot. To provide your integration data please enter them in the following format:\n\n" +
                "1. <your-notion-token>\n" +
                "2. <your-monobank-token>\n" +
                "3. <your-monobank-account-name>\n"
        };
    }

    private async Task<TelegramCommandResponse> HandleEnableCommandAsync(
        long chatId, CancellationToken cancellationToken)
    {
        var command = new EnableCommand { ChatId = chatId };
        await sender.Send(command, cancellationToken);
        return new TelegramCommandResponse { Message = "Monobank synchronization is enabled." };
    }

    private async Task<TelegramCommandResponse> HandleDisableCommandAsync(
        long chatId, CancellationToken cancellationToken)
    {
        var command = new DisableCommand { ChatId = chatId };
        await sender.Send(command, cancellationToken);
        return new TelegramCommandResponse { Message = "Monobank synchronization is disabled." };
    }

    private async Task<TelegramCommandResponse> HandleSetSettingsCommandAsync(
        long chatId, string username, string text, CancellationToken cancellationToken)
    {
        SetSettingsCommand command = new() { ChatId = chatId, Username = username, Text = text };
        await sender.Send(command, cancellationToken);
        return new TelegramCommandResponse { Message = "The setting has been added successfully." };
    }
}